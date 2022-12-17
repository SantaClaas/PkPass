namespace PkPass

open System
open System.Net.Http
open Bolero
open Bolero.Html
open Elmish
open Microsoft.AspNetCore.Components
open PkPass.Components
open PkPass.HomePage
open PkPass.LoadPass

module App =
    type AppError =
        | LoadPassError of LoadPassError
        | UnknownError of Exception

    type AppMessage =
        | HomePageMessage of HomePageMessage
        | LogError of AppError

    type AppState = HomePageState of HomePageState

    module AppState =
        let ``default`` = HomePageState.LoadingPasses |> AppState.HomePageState


    let update message state jsRuntime httpClient =
        match state with
        | HomePageState homePageState ->
            match message with
            | HomePageMessage homePageMessage ->
                let newHomePageModel, command =
                    update homePageMessage homePageState jsRuntime httpClient

                let newCommand = command |> Cmd.map AppMessage.HomePageMessage

                newHomePageModel |> AppState.HomePageState, newCommand

            | LogError appError ->
                match appError with
                | LoadPassError passError ->
                    match passError with
                    | LoadCacheUrlError -> Console.WriteLine "Error whole loading cache urls"
                    | LoadPassDataFromCacheError ``exception`` ->
                        Console.WriteLine
                            $"Error whole loading pass files data from cache:{Environment.NewLine}{``exception``}"
                    | LoadPassError.DeserializationError deserializationError ->
                        Console.WriteLine $"Error while deserialization:{Environment.NewLine}{deserializationError}"
                    | LoadPassError.EventTicketWithInvalidImages ->
                        Console.WriteLine
                            "Event ticket has strip image and background / thumbnail image but it can not have a background or thumbnail image if a strip image is defined"
                    | LoadPassError.NoPassJsonFile -> Console.WriteLine "There was no pass.json in the package"
                    | LoadPassError.RequiredImagesMissing imageNames -> Console.WriteLine $"The following required images were missing in the package {String.Join(',', imageNames)}"
                    | LoadPassError.RequiredImageMissing imageName -> Console.WriteLine $"The image {imageName} is missing"
                    

                | UnknownError ``exception`` ->
                    Console.WriteLine $"An unexpected error occured:{Environment.NewLine}{``exception``}"

                state, Cmd.none


    // We call "model" "state" and use it to determine page
    // Always set to home page for now
    let router: Router<AppState, AppState, AppMessage> =
        { getEndPoint = fun model -> model
          setRoute = fun path -> HomePageMessage.LoadPasses |> AppMessage.HomePageMessage |> Some
          getRoute = fun page -> "/"

        }

    let view state dispatch =
        main {
            attr.``class`` "p-4"

            cond state (fun state ->
                match state with
                | HomePageState homePageState ->
                    view homePageState (fun message -> AppMessage.HomePageMessage message |> dispatch))

            comp<VersionInformation> { attr.empty () }
        }


    let createProgram jsRuntime httpClient =
        // When the app starts it should be set into a loading state with an initial command that loads the cached passes
        // And then sets the loading state to complete
        let initialize _ = AppState.``default``, Cmd.none

        let update model dispatch =
            update model dispatch jsRuntime httpClient

        Program.mkProgram initialize update view |> Program.withRouter router

    type App2() =
        inherit ProgramComponent<AppState, AppMessage>()

        [<Inject>]
        member val HttpClient = Unchecked.defaultof<HttpClient> with get, set

        override this.Program = createProgram this.JSRuntime this.HttpClient
