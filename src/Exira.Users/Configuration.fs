﻿namespace Exira.Users

module internal Configuration =
    open FSharp.Configuration
    open System.Web.Hosting

    let private configPath = HostingEnvironment.MapPath "~/Web.yaml"

    type WebConfig = YamlConfig<"Web.yaml">
    let webConfig = WebConfig()
    webConfig.LoadAndWatch configPath |> ignore