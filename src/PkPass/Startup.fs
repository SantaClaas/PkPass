namespace PkPass.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open System
open System.Net.Http
open Microsoft.AspNetCore.Components.Web
open PkPass

module Program =

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<App2.App2>("#main")
        builder.RootComponents.Add<HeadOutlet>("head::after")
        builder.Services.AddScoped<HttpClient>(fun _ ->
            new HttpClient(BaseAddress = Uri builder.HostEnvironment.BaseAddress)) |> ignore
        builder.Build().RunAsync() |> ignore
        0
