namespace PkPass

open System
open System.IO.Compression
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Bolero
open Elmish
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open PkPass.HomePage
open PkPass.Interop
open PkPass.Interop.Window
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package

module App =
    type AppError =
        | LoadPassError of LoadPassError
        | UnknownError of Exception

    type AppMessage =
        | HomePageMessage of HomePageMessage
        | LogError of AppError

    type AppState = HomePageState of HomePageState

    module AppState =
        let ``default`` =
            HomePageState.LoadingPasses
            |> AppState.HomePageState


    let update (message: AppMessage) (state: AppState) (jsRuntime: IJSRuntime) =
        match state with
        | HomePageState homePageState ->
            match message with
            | HomePageMessage homePageMessage ->
                Console.WriteLine $"Received home message {homePageMessage.GetType()}"

                let newHomePageModel, command =
                    HomePage.update homePageMessage homePageState jsRuntime

                let newCommand = command |> Cmd.map AppMessage.HomePageMessage
                Console.WriteLine $"New home page model is {newHomePageModel.GetType()}"
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
                | UnknownError ``exception`` ->
                    Console.WriteLine $"An unexpected error occured:{Environment.NewLine}{``exception``}"

                state, Cmd.none


    // We call "model" "state" and use it to determine page
    // Always set home page for now
    let router: Router<AppState, AppState, AppMessage> =
        { getEndPoint = fun model -> model
          setRoute =
            fun path ->
                HomePageMessage.LoadPasses
                |> AppMessage.HomePageMessage
                |> Some
          getRoute = fun page -> "/"

        }

    //    type AppRouter () =
//        interface Bolero.IRouter<string,int> with
//
//    let router1 = AppRouter ()
//
    let view (state: AppState) (dispatch: AppMessage Dispatch) =
        match state with
        | HomePageState homePageState ->
            HomePage.view homePageState (fun message -> AppMessage.HomePageMessage message |> dispatch)
    //        match state.activePage with
//        | Home _ ->
//            Console.WriteLine $"Rendering app with model {state.GetType()}"
//            HomePage.view state.homePageModel (fun message -> AppMessage.HomePageMessage message |> dispatch)
////            homePage model dispatch


    // Load passes
    // 1. Load cache urls
    // Inefficient way to get a property from a JS object reference
    let getProperty<'T> (reference: IJSObjectReference) (properties: string array) (jsRuntime: IJSRuntime) =
        task {
            let! asString = jsRuntime.InvokeAsync<string>("JSON.stringify", reference, properties)
            return JsonSerializer.Deserialize<'T>(asString)
        }

    let loadCacheUrls jsRuntime =
        task {
            let! cache = jsRuntime |> CacheStorage.open' "files"

            let! requests = Cache.getKeys cache

            let getRequestUrl request =
                match request with
                | Request reference -> getProperty<{| url: string |}> reference [| "url" |] jsRuntime

            let getUrlTasks = requests |> Seq.map getRequestUrl

            let! urls = Task.WhenAll getUrlTasks
            return urls |> Array.map (fun request -> request.url)
        }

    // 2. Load passes from urls
    let loadPassDataFromCacheUrl (client: HttpClient) (url: string) =
        task {
            try
                let! stream = client.GetStreamAsync url
                let archive = new ZipArchive(stream)
                return archive |> AsZip |> Ok
            with
            | exception' -> return LoadPassDataFromCacheError exception' |> Error
        }

    let loadPassesDataFromCacheUrls (client: HttpClient) (urls: string array) =
        task {
            let tasks = Array.map (loadPassDataFromCacheUrl client) urls

            return! Task.WhenAll tasks
        }

    // 3. Parse passes
    // We load all the data of the passes at once (for now) instead of a more complex lazy approach that can be implemented later
    let loadPass (package: PassPackageData) : Result<PassPackage, DeserializationError> =
        let pass = getPass package

        match pass with
        | Error error -> Error error
        | Ok pass ->
            { pass = pass
              background = getBackground package
              thumbnail = getThumbnail package
              logo = getLogo package }
            |> Ok

    let loadPasses = Array.map loadPass

    let completelyLoadPasses jsRuntime client =
        task {

            // 1. Load cache urls
            let! cacheUrls = loadCacheUrls jsRuntime
            // 2. Load passes from cache urls
            let! packagesDataLoadResults = cacheUrls |> loadPassesDataFromCacheUrls client

            let toPassPackage (result: Result<PassPackageData, LoadPassError>) : Result<PassPackage, LoadPassError> =
                match result with
                | Error error -> Error error
                | Ok package ->
                    match loadPass package with
                    | Error error -> LoadPassError.DeserializationError error |> Error
                    | Ok pass -> Ok pass

            // 3. Extract data from pass files
            let passPackageResults = Array.map toPassPackage packagesDataLoadResults

            return passPackageResults
        }

    let createInitialLoadCommand (jsRuntime: IJSRuntime) (httpClient: HttpClient) =
        let loadAndSet () =
            completelyLoadPasses jsRuntime httpClient

        Cmd.OfTask.either
            loadAndSet
            ()
            (HomePageMessage.SetPassLoadResult
             >> AppMessage.HomePageMessage)
            (UnknownError >> LogError)






    let createProgram (jsRuntime: IJSRuntime) (httpClient: HttpClient) =
        // When the app starts it should be set into a loading state with an initial command that loads the cached passes
        // And then sets the loading state to complete
        let initialize _ =
            AppState.``default``, createInitialLoadCommand jsRuntime httpClient

        let update model dispatch = update model dispatch jsRuntime

        Program.mkProgram initialize update view
        |> Program.withRouter router

    type App2() =
        inherit ProgramComponent<AppState, AppMessage>()

        [<Inject>]
        member val HttpClient = Unchecked.defaultof<HttpClient> with get, set

        override this.Program = createProgram this.JSRuntime this.HttpClient
