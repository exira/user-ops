﻿namespace Exira.Users.Domain

open Exira.ErrorHandling

module Account =
    open Events

    let internal applyAccountEvent state event =
        match state with
        | Account.Init ->
            match event with
            | AccountCreated e ->
                let account =
                    { AccountInfo.Name = e.Name
                      Email = e.Email
                      Users = e.Users }

                match e.Type with
                | Personal -> PersonalAccount { Account = account } |> Success
                | Company -> CompanyAccount { Account = account } |> Success

        | Account.PersonalAccount account ->
            match event with
            | AccountCreated _ -> stateTransitionFail event state

        | Account.CompanyAccount account ->
            match event with
            | AccountCreated _ -> stateTransitionFail event state

        | Account.Deleted ->
            match event with
            | AccountCreated _ -> stateTransitionFail event state

    let internal applyEvent state event =
        match event with
        | Event.Account e -> applyAccountEvent state e
        | _ -> stateTransitionFail event state

    let getAccountState id = getState (applyEvents applyEvent) Account.Init (toAccountStreamId id)

[<AutoOpen>]
module internal AccountCommandHandler =
    open EventStore.ClientAPI
    open Commands
    open Events
    open Account

    let createAccount (command: CreateAccountCommand) (_, state) =
        // An account can only be created when it does not exist yet
        match state with
        | Account.Init ->
            let accountCreated =
                { AccountCreatedEvent.Name = command.Name
                  Email = EmailInfo.create command.Email
                  Users = command.Users
                  Type = command.Type } |> AccountCreated |> Event.Account

            let streamId = toAccountStreamId command.Name
            let response = Response.AccountCreated streamId
            succeed (streamId, ExpectedVersion.NoStream, [accountCreated], response)

        | Account.PersonalAccount _
        | Account.CompanyAccount _
        | Account.Deleted _ -> fail [AccountAlreadyExists]

    let handleCreateAccount (command: CreateAccountCommand) es =
        async {
            let! state = getAccountState command.Name es

            return!
                state
                |> bind (createAccount command)
                |> bindAsync (save es)
        }