module PkPass.Client.App

open System
open System.Buffers.Text
open System.Net.Mime
open System.Text.Json
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
open System.Net.Http
open System.IO.Compression
open System.IO
open PkPass.PassKit
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package

module Command = Cmd

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/open">] Open

type FileName = FileName of string


type Model =
    { page: Page
      passFiles: FileName array option
      background: PassBackground option
      logo: PassLogo option
      thumbnail: PassThumbnail option
      passResult: Result<Pass, DeserializationError> option }

let initializeModel () =
    { page = Home
      passFiles = None
      background = None
      logo = None
      thumbnail = None
      passResult = None }

type Error = LoadFilesError of exn

type Message =
    | SetPage of Page
    | SetPassFiles of FileName array
    | SetPassResult of Result<Pass, DeserializationError> option
    | SetPassBackground of PassBackground option
    | SetPassLogo of PassLogo option
    | SetPassThumbnail of PassThumbnail option
    | RunTestJavaScript
    | SetTest of Caches.Cache
    | LogError of Error

let flip f x y = f y x

let deserializePass (passes: FileName array) =
    let deserializePass (FileName fileName) = Compressed fileName |> getPass

    match passes.Length with
    | 1 -> deserializePass passes[0] |> Some
    | 0 -> None
    | _ -> failwith "TODO implement multiple pass handling 😅"

let loadImage getImage (passes: FileName array) =
    let load (FileName fileName) = Compressed fileName |> getImage

    match passes.Length with
    | 1 -> load passes[0] |> Some
    | 0 -> None
    | _ -> failwith "TODO implement multiple pass handling 😅"

let loadBackground = loadImage getBackground
let loadLogo = loadImage getLogo
let loadThumbnail = loadImage getThumbnail

let update (jsRuntime: IJSRuntime) (logger: ILogger) message model =
    match message with
    | SetPage page -> { model with page = page }, Command.none
    | SetPassFiles passes ->
        //TODO error handling
        let deserializeCommand =
            Command.OfFunc.perform deserializePass passes SetPassResult

        let loadBackgroundCommand =
            Command.OfFunc.perform loadBackground passes SetPassBackground

        let loadLogoCommand =
            Command.OfFunc.perform loadLogo passes SetPassLogo

        let loadThumbnailCommand =
            Command.OfFunc.perform loadThumbnail passes SetPassThumbnail

        { model with passFiles = Some passes },
        Command.batch [ deserializeCommand
                        loadBackgroundCommand
                        loadLogoCommand
                        loadThumbnailCommand ]
    | SetPassBackground background -> { model with background = background }, Command.none
    | SetPassLogo logo -> { model with logo = logo }, Command.none
    | SetPassThumbnail thumbnail -> { model with thumbnail = thumbnail }, Command.none
    | SetPassResult passResultOption -> { model with passResult = passResultOption }, Command.none
    | RunTestJavaScript ->
        //TODO error handling
        let test () = Caches.open' "files" jsRuntime
        let command = Command.OfTask.perform test () SetTest
        model, command
    | SetTest handle ->
        Console.WriteLine handle
        model, Command.none
    | LogError error ->
        match error with
        | LoadFilesError ``exception`` ->
            logger.LogError(``exception``, "Error while loading files")
            model, Command.none

let renderHeaderField field =
    div {
        ``class`` "text-right"

        p {
            ``class`` "text-xs"

            let header =
                match field.label with
                | None -> String.Empty
                | Some (LocalizableString.LocalizableString value) -> value

            header
        }

        p {
            match field.value with
            | LocalizableString (LocalizableString.LocalizableString stringValue) -> stringValue
            | _ -> String.Empty
        }
    }

let renderPrimaryField field =
    div {
        p {
            ``class`` "text-xs"

            let header =
                match field.label with
                | None -> String.Empty
                | Some (LocalizableString.LocalizableString value) -> value

            header
        }

        p {
            ``class`` "text-sm"

            match field.value with
            | LocalizableString (LocalizableString.LocalizableString stringValue) -> stringValue
            | _ -> String.Empty
        }
    }


let openPage model =
    concat {
        comp<PageTitle> { "Passes" }

        main {
            ``class`` "p-4"

            match model.passResult with
            | Some (Ok pass) ->
                match pass with
                | EventTicket (passDefinition, passStructure) ->
                    // Pass card
                    div {
                        ``class``
                            "w-full rounded-3xl text-white p-3 \
                                   overflow-hidden \
                                   bg-repeat"

                        let createPngDataUrl base64String = $"data:image/png;base64,{base64String}"

                        match model.background with
                        | None -> empty ()
                        | Some (PassBackground (Image.Base64 base64String)) ->
                            let source = createPngDataUrl base64String
                            style $"background-image: url({source})"

                        // Header row
                        div {
                            ``class`` "flex justify-between items-center"

                            match model.logo with
                            | None -> Html.empty ()
                            | Some (PassLogo (Image.Base64 base64String)) ->
                                let source = createPngDataUrl base64String

                                img {
                                    ``class`` "w-1/3"
                                    src source
                                }
                            //TODO logo text in between
                            // Header fields
                            match passStructure.headerFields with
                            | None -> Html.empty ()
                            | Some fields ->
                                div {
                                    ``class`` "flex"
                                    forEach fields renderHeaderField
                                }
                        }

                        // What I call "body"
                        // Body row
                        div {
                            ``class`` "flex justify-between"

                            div {
                                ``class`` "flex flex-col"
                                // Primary fields
                                match passStructure.primaryFields with
                                | None -> Html.empty ()
                                | Some fields ->
                                    div {
                                        ``class`` "flex"
                                        forEach fields renderPrimaryField
                                    }

                                // Secondary fields below
                                match passStructure.secondaryFields with
                                | None -> Html.empty ()
                                | Some fields ->
                                    div {
                                        ``class`` "flex"
                                        forEach fields renderPrimaryField
                                    }
                            }

                            match model.thumbnail with
                            | None -> Html.empty ()
                            | Some (PassThumbnail (Image.Base64 base64String)) ->
                                let source = createPngDataUrl base64String

                                img {
                                    ``class`` "w-1/3 rounded-2xl"
                                    src source
                                }
                        }

                        // Auxiliary fields
                        match passStructure.auxiliaryFields with
                        | None -> Html.empty ()
                        | Some fields ->
                            div {
                                ``class`` "flex"
                                forEach fields renderPrimaryField
                            }

                        // Barcode
                        //TODO prefer barcodes over barcode which is deprecated-ish and use barcode as fallback
                        match passDefinition.barcode with
                        | None -> Html.empty ()
                        | Some (Barcode (alternateText, format, message, messageEncoding)) ->
                            match format with
                            | Qr ->
                                let (Image.Base64 base64String) =
                                    Barcode.createQrCode message

                                let source = createPngDataUrl base64String

                                img {
                                    ``class`` "rounded-3xl"
                                    src source
                                }
                            | _ -> div { "Sorry this barcode format is not yet supported :(" }
                    }
                | _ -> p { "Sorry this pass type is not yet supported :(" }
            | Some (Error error) ->
                div {
                    ``class`` "bg-slate-100 h-10 w-full rounded-3xl text-slate-900"

                    h1 {
                        ``class`` "text-3xl"
                        "Oh snap something went wrong while opening the pass :("
                    }
                }
            | None -> p { "Loading, hang on :)" }

        }
    }

let homePage model (dispatch: Message Dispatch) =
    concat {
        
        comp<PageTitle> { "Passes" }
        main {
            ``class`` "p-4"
            h1 {
                "Passes"
            }
            
            button {
                on.click (fun _ -> dispatch RunTestJavaScript)
                "Add"
            }
        }
    }

let view (model: Model) (dispatch: Message Dispatch) =
    match model.page with
    | Home -> homePage model dispatch
    | Open -> openPage model 

let router =
    Router.infer SetPage (fun model -> model.page)



let program (jsRuntime: IJSRuntime) logger (client: HttpClient) =
    let runtime =
        jsRuntime :?> IJSInProcessRuntime

    let update = update jsRuntime logger

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

            use fileStream = (File.OpenWrite fileName)

            do! browserStream.CopyToAsync fileStream
            return FileName fileName
        }

    let loadPass () =
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
        Command.OfTask.either loadPass () SetPassFiles logError

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
