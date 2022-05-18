module PkPass.Client.App

open System
open System.Net.Mime
open Microsoft.AspNetCore.Components.Web
open Bolero
open Bolero.Html
open Bolero.Html.attr
open Elmish
open Microsoft.AspNetCore.StaticFiles
open Microsoft.JSInterop
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open System.Threading.Tasks
open FileSystemFileHandle
open System.Net.Http
open System.IO.Compression
open System.IO
open Microsoft.AspNetCore
open PkPass

module Command = Cmd

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/open">] Open

type FileName = FileName of string
type Model = { page: Page; files: FileName array }

let initializeModel () = { page = Home; files = Array.empty }

type Error = LoadFilesError of exn

type Message =
    | SetPage of Page
    | SetFiles of FileName array
    | LogError of Error

let flip f x y = f y x

let update (logger: ILogger) message model =
    match message with
    | SetPage page -> { model with page = page }, Command.none
    | SetFiles files -> { model with files = files }, Command.none
    | LogError error ->
        match error with
        | LoadFilesError ``exception`` ->
            logger.LogError(``exception``, "Error while loading files")
            model, Command.none

let zipArchiveList (FileName name) =
    let fileToNode (file: string) =
        match FileExtensionContentTypeProvider()
                  .TryGetContentType(file)
            with
        | true, contentType when contentType.StartsWith "image/" ->
            let data = File.ReadAllBytes file
            let base64 = Convert.ToBase64String data

            let dataUrl =
                $"data:{contentType};base64,{base64}"

            img { src dataUrl }
        | true, _ when Path.GetFileName file = "pass.json" ->
            let pass = PassKit.readPass file
            Console.WriteLine pass
            Html.empty()
        | _, _ -> p { file }


    let directoryName = name + " extracted"
    ZipFile.ExtractToDirectory(name, directoryName)
    let files = Directory.GetFiles directoryName
    forEach files fileToNode


let openPage model =
    concat {
        comp<PageTitle> { "Passes" }
        div { $"loaded {model.files.Length} files" }

        Html.forEach model.files zipArchiveList
    }

let homePage =
    // Jus the same for now
    openPage

let view (model: Model) dispatch =
    match model.page with
    | Home -> homePage model
    | Open -> openPage model

let router =
    Router.infer SetPage (fun model -> model.page)



let program (jsRuntime: IJSRuntime) logger (client: HttpClient) =
    let runtime =
        jsRuntime :?> IJSInProcessRuntime

    let update = update logger

    let createAsync (index: int) (jsModule: IJSObjectReference) =
        task {
            let! fileHandle = jsModule.InvokeAsync<IJSObjectReference>("getLoadedFile", index)
            //let! fileHandle = jsModule.InvokeAsync<IJSObjectReference>("loadedPasses.at", index)
            let! file = fileHandle.InvokeAsync<IJSObjectReference>("getFile")

            let! objectUrl = runtime.InvokeAsync<string>("URL.createObjectURL", file)
            let! fileName = jsModule.InvokeAsync<string>("getAttribute", file, "name")
            return objectUrl, fileName
        }

    let loadFile (url: string, fileName: string) =
        task {
            use! browserStream = client.GetStreamAsync url

            use fileStream =
                (IO.File.OpenWrite fileName)

            do! browserStream.CopyToAsync fileStream
            return FileName fileName
        }

    let loadFiles () =
        task {
            let! jsModule = runtime.InvokeAsync<IJSObjectReference>("import", "./module.js")
            let! count = jsRuntime.InvokeAsync<int>("getStuff")

            let! createTasks =
                [| for index in 0 .. (count - 1) -> createAsync index jsModule |]
                |> Task.WhenAll

            return! createTasks |> Array.map loadFile |> Task.WhenAll
        }








    //let startCommand =
    //    Command.batch [ Command.OfFunc.either loadClientId () idToMessage idErrorToMessage
    //                    Command.OfFunc.either loadClientSecret () secretToMessage secretErrorToMessage ]

    let logError = LoadFilesError >> LogError

    let startCommand =
        Command.OfTask.either loadFiles () SetFiles logError

    Program.mkProgram (fun _ -> initializeModel (), startCommand) update view
    |> Program.withRouter router


type App() =
    inherit ProgramComponent<Model, Message>()

    [<Inject>]
    member val Logger = Unchecked.defaultof<ILogger<App>> with get, set

    [<Inject>]
    member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set

    [<Parameter>]
    [<SupplyParameterFromQuery(Name = "code")>]
    member val AuthorizationCode = Unchecked.defaultof<string> with get, set

    [<Inject>]
    member val HttpClient = Unchecked.defaultof<HttpClient> with get, set


    override this.Program =
        program this.JSRuntime this.Logger this.HttpClient
