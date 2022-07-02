module PkPass.App2

open System
open System.IO.Compression
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open PkPass.Interop
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package


// Describes all the errors that can occur when loading a pass
type LoadPassError =
    | LoadCacheUrlError
    | LoadPassDataFromCacheError of Exception
    | DeserializationError of DeserializationError

type AppError =
    | LoadPassError of LoadPassError
    | UnknownError of Exception

// Home page can be displaying loaded passes or it is currently loading passes
type HomePageModel =
    | LoadingPasses
    // Passes loaded and display load result
    | PassesLoaded of Result<PassPackage, LoadPassError> array

type AppPage = Home of HomePageModel
type HomePageMessage = SetPassLoadResult of Result<PassPackage, LoadPassError> array

type AppMessage =
    | SetPage of page: AppPage
    | HomePageMessage of HomePageMessage
    | LogError of AppError

type AppModel = { activePage: AppPage }

open Elmish

let updateHomePage (message: HomePageMessage) (model: AppModel) =
    match message with
    | SetPassLoadResult results ->
        model,
        results
        |> HomePageModel.PassesLoaded
        |> AppPage.Home
        |> SetPage
        |> Cmd.ofMsg

let update (message: AppMessage) (model: AppModel) =
    match message with
    | SetPage page -> { activePage = page }, Cmd.none
    | HomePageMessage homePageMessage -> updateHomePage homePageMessage model
    | LogError appError ->
        match appError with
        | LoadPassError passError ->
            match passError with
            | LoadCacheUrlError -> Console.WriteLine "Error whole loading cache urls"
            | LoadPassDataFromCacheError ``exception`` ->
                Console.WriteLine $"Error whole loading pass files data from cache:{Environment.NewLine}{``exception``}"
            | DeserializationError deserializationError ->
                Console.WriteLine $"Error while deserialization:{Environment.NewLine}{deserializationError}"
        | UnknownError ``exception`` ->
            Console.WriteLine $"An unexpected error occured:{Environment.NewLine}{``exception``}"

        model, Cmd.none


let router: Router<AppPage, AppModel, AppMessage> =
    { getEndPoint = fun model -> model.activePage
      setRoute =
        fun path ->
            HomePageModel.LoadingPasses
            |> Home
            |> SetPage
            |> Some
      getRoute = fun page -> "/" }

let createPngDataUrl base64String = $"data:image/png;base64,{base64String}"

let homePage (model: HomePageModel) (dispatch: AppMessage Dispatch) =
    match model with
    | LoadingPasses -> main { "Loading passes..." }
    | PassesLoaded loadResults ->
        main {
            attr.``class`` "p-4"

            h1 {
                attr.``class`` "text-xl font-lighter mb-2 tracking-widest"
                "Passes"
            }

            ul {
                forEach loadResults (fun result ->
                    match result with
                    | Error loadPassError -> p { "Sorry could not load that pass" }
                    | Ok passPackage ->
                        match passPackage.pass with
                        | EventTicket (passDefinition, passStructure) ->
                            div {
                                attr.``class`` "bg-white/5 flex gap-3 p-3 rounded-xl justify-between"

                                match passPackage.thumbnail with
                                | PassThumbnail (Image.Base64 base64String) ->
                                    img {
                                        attr.``class`` "w-20"
                                        base64String |> createPngDataUrl |> attr.src
                                    }

                                div {
                                    attr.``class`` "flex flex-col justify-between"

                                    div {
                                        match passStructure.primaryFields with
                                        | Some [ first ] ->
                                            match first.label with
                                            | Some (LocalizableString.LocalizableString label) ->
                                                h3 {
                                                    attr.``class`` "text-xs font-bold uppercase tracking-wide text-emphasis-low"
                                                    label
                                                }
                                            | _ -> empty ()

                                            h2 {
                                                attr.``class`` "leading-none text-lg font-light text-emphasis-high"

                                                match first.value with
                                                | Date dateTimeOffset -> dateTimeOffset.ToString()
                                                | Number number -> number.ToString()
                                                | LocalizableString (LocalizableString.LocalizableString value) -> value
                                            }
                                        | _ -> empty ()
                                    }

                                    div {
                                        match passStructure.secondaryFields with
                                        | Some [ first ] ->
                                            match first.label with
                                            | Some (LocalizableString.LocalizableString label) ->
                                                h5 {
                                                    attr.``class`` "text-xs text-emphasis-low uppercase"
                                                    label
                                                }
                                            | _ -> empty ()

                                            h4 {
                                                attr.``class`` "leading-none text-sm font-medium text-emphasis-medium"

                                                match first.value with
                                                | Date dateTimeOffset -> dateTimeOffset.ToString()
                                                | Number number -> number.ToString()
                                                | LocalizableString (LocalizableString.LocalizableString value) -> value
                                            }
                                        | _ -> empty ()
                                    }
                                }
                            }
                        | _ -> div { "Sorry this pass type is not supported yet" })
            }
        }

let view (model: AppModel) (dispatch: AppMessage Dispatch) =
    match model.activePage with
    | Home page -> homePage page dispatch


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
        { activePage = AppPage.Home LoadingPasses }, createInitialLoadCommand jsRuntime httpClient

    Program.mkProgram initialize update view
    |> Program.withRouter router

type App2() =
    inherit ProgramComponent<AppModel, AppMessage>()

    [<Inject>]
    member val HttpClient = Unchecked.defaultof<HttpClient> with get, set

    override this.Program =
        createProgram this.JSRuntime this.HttpClient
