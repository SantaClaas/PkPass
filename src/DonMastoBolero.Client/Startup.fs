namespace PkPass.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open System
open System.Net.Http
open Microsoft.AspNetCore.Components.Web

module Program =

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<App.App>("#main")
        builder.RootComponents.Add<HeadOutlet>("head::after")
        builder.Services.AddScoped<HttpClient>(fun _ ->
            new HttpClient(BaseAddress = Uri builder.HostEnvironment.BaseAddress)) |> ignore
        builder.Build().RunAsync() |> ignore
        0
