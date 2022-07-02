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
    type AppPage = Home of HomePage
    
    type AppError =
        | LoadPassError of LoadPassError
        | UnknownError of Exception
    type AppMessage =
        | SetPage of page: AppPage
        | HomePageMessage of HomePageMessage
        | RequestFileFromUser
        | AddUserSelectedFiles of FileSystemFileHandle array
        | LogError of AppError
    
    type AppModel = { activePage: AppPage; homePageModel: HomePageModel }
    module AppModel =
        let private home =
            HomePage
            |> AppPage.Home
        let ``default`` = {activePage = home; homePageModel = LoadingPasses }

   
    let update (message: AppMessage) (model: AppModel) (jsRuntime: IJSRuntime) =
        match message with
        | SetPage page -> {model with activePage = page }, Cmd.none
        | HomePageMessage homePageMessage ->
            Console.WriteLine $"Received home message {homePageMessage.GetType()}"
            let newHomePageModel, command = HomePage.update homePageMessage model.homePageModel jsRuntime
            let newCommand = command |> Cmd.map AppMessage.HomePageMessage
            Console.WriteLine $"New home page model is {newHomePageModel.GetType()}"
            {model with homePageModel = newHomePageModel}, newCommand
            
        | RequestFileFromUser ->
            let requestFile () =
                let acceptedFileTypes =
                    Map.ofList [ ("application/vnd.apple.pkpass", [| ".pkpass" |])
                                 ("application/vnd.apple.pkpasses", [| ".pkpasses" |]) ]
    
                let options  =
                    { types =
                        [| {| description = "Pass files"
                              accept = acceptedFileTypes |} |] }
    
                showOpenFilePicker options jsRuntime
    
            model, Cmd.OfTask.either requestFile () AddUserSelectedFiles (UnknownError >> LogError)
        | AddUserSelectedFiles fileHandles ->
            fileHandles |> Array.length |> printfn "Loaded %O files"
            model, Cmd.none
        | LogError appError ->
            match appError with
            | LoadPassError passError ->
                match passError with
                | LoadCacheUrlError -> Console.WriteLine "Error whole loading cache urls"
                | LoadPassDataFromCacheError ``exception`` ->
                    Console.WriteLine $"Error whole loading pass files data from cache:{Environment.NewLine}{``exception``}"
                | LoadPassError.DeserializationError deserializationError ->
                    Console.WriteLine $"Error while deserialization:{Environment.NewLine}{deserializationError}"
            | UnknownError ``exception`` ->
                Console.WriteLine $"An unexpected error occured:{Environment.NewLine}{``exception``}"
    
            model, Cmd.none
    
    
    let router: Router<AppPage, AppModel, AppMessage> =
        { getEndPoint = fun model -> model.activePage
          setRoute =
            fun path ->
                // Always set home page for now
                HomePage
                |> Home
                |> SetPage
                |> Some 
          getRoute = fun page -> "/" }
  
    let view (model: AppModel) (dispatch: AppMessage Dispatch) =
        match model.activePage with
        | Home _ ->
            Console.WriteLine $"Rendering app with model {model.GetType()}"
            HomePage.view model.homePageModel (fun message -> AppMessage.HomePageMessage message |> dispatch)
//            homePage model dispatch
    
    
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
    
            let getUrlTasks =
                requests |> Seq.map getRequestUrl
    
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
            let tasks =
                Array.map (loadPassDataFromCacheUrl client) urls
    
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
            let passPackageResults =
                Array.map toPassPackage packagesDataLoadResults
    
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
            AppModel.``default``, createInitialLoadCommand jsRuntime httpClient
    
        let update model dispatch = update model dispatch jsRuntime
    
        Program.mkProgram initialize update view
        |> Program.withRouter router
    
    type App2() =
        inherit ProgramComponent<AppModel, AppMessage>()
    
        [<Inject>]
        member val HttpClient = Unchecked.defaultof<HttpClient> with get, set
    
        override this.Program =
            createProgram this.JSRuntime this.HttpClient
