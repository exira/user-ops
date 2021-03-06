﻿namespace Exira.Users.Domain

[<AutoOpen>]
module EventStoreHelpers =
    open ExtCore.Control
    open EventStore.ClientAPI.Exceptions
    open Exira.ErrorHandling
    open Exira.EventStore
    open Exira.EventStore.EventStore
    open Events

    // TODO: Plug in schema here
    let private logger = Logging.logger
    let toUserStreamId id = id |> Email.value |> toStreamId "user"
    let toAccountStreamId id = id |> AccountName.value |> toStreamId "account"

    let internal getTypeName o = o.GetType().Name

    let internal stateTransitionFail event state =
        [InvalidStateTransition (sprintf "Invalid event %s for state %s" (event |> getTypeName) (state |> getTypeName))]
        |> fail

    // Apply each event on itself to get to the final state
    let internal applyEvents applyEvent initState events =
        let incrementVersion version state = succeed (version + 1, state)

        // let the called apply an event on the current state
        let foldEvent event foldState =
            let (version, state) = foldState

            applyEvent state event
            |> bind (incrementVersion version)

        // fold each event on the current state
        let eventsFolder foldState event =
            foldState
            |> bind (foldEvent event)

        let startEvent = succeed (-1, initState)

        Seq.fold eventsFolder startEvent events

    let inline internal getState applyEvents initState id es =
        readAllFromStream es id 0
        |> Async.map (applyEvents initState)

    let inline internal save es (id, version, events: seq<Event>, response) =
        async {
            try
                logger.Debug("Saving {@Events}", events)
                do! appendToStream es id version events
                return succeed (id, response)
            with
            | :? WrongExpectedVersionException as ex -> return fail [Error.SaveVersionException ex]
            | ex -> return fail [Error.SaveException ex]
        }