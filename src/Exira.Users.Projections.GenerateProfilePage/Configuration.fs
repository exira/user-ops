﻿namespace Exira.Users.Projections.GenerateProfilePage

module Configuration =
    open System.IO
    open System.Reflection
    open FSharp.Configuration

    let entryAssembly = Assembly.GetEntryAssembly()
    let executablePath = entryAssembly.Location |> Path.GetDirectoryName
    let configPath = Path.Combine(executablePath, "Projection.yaml")

    type ProjectionConfig = YamlConfig<"Projection.yaml">
    let projectionConfig = ProjectionConfig()
    projectionConfig.LoadAndWatch configPath |> ignore