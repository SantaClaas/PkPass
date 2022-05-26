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
open PkPass.Interop
open PkPass.PassKit
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package

module Command = Cmd

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/open">] Open
    | [<EndPoint "/open/{fileName}">] OpenFile of fileName: string 

type FileName = FileName of string


type Model =
    { page: Page
      cacheUrls : string array
      background: PassBackground option
      logo: PassLogo option
      thumbnail: PassThumbnail option
      passResult: Result<Pass, DeserializationError> option }

let initializeModel () =
    { page = Home
      cacheUrls = [||]
      background = None
      logo = None
      thumbnail = None
      passResult = None }

type Error = LoadFilesError of exn

type Message =
    | SetPage of Page
    | SetPassCacheUrls of string array
    | SetPassResult of Result<Pass, DeserializationError> option
    | SetPassBackground of PassBackground option
    | SetPassLogo of PassLogo option
    | SetPassThumbnail of PassThumbnail option
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

let update (jsRuntime: IJSRuntime) (logger: ILogger) (client: HttpClient) message model =
    match message with
    | SetPage page ->
        match page with
        | OpenFile fileName ->
            // Load file and then open actual open page
//            let loadCommand =
//                load () = loadFromCache fileName
//                Command.OfTask.perform load SetPackage
            model, Command.none
        | _ -> { model with page = page }, Command.none
    | SetPassCacheUrls urls ->
        {model with cacheUrls = urls}, Command.none
    | SetPassBackground background -> { model with background = background }, Command.none
    | SetPassLogo logo -> { model with logo = logo }, Command.none
    | SetPassThumbnail thumbnail -> { model with thumbnail = thumbnail }, Command.none
    | SetPassResult passResultOption -> { model with passResult = passResultOption }, Command.none
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
let createPngDataUrl base64String = $"data:image/png;base64,{base64String}"
let headerRow model passStructure =
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
let bodyRow model passStructure =
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
let auxiliaryFields passStructure =
    match passStructure.auxiliaryFields with
    | None -> Html.empty ()
    | Some fields ->
        div {
            ``class`` "flex"
            forEach fields renderPrimaryField
        }
let barcode (passDefinition : PassDefinition) =
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
                alt alternateText
                src source
            }
        | _ -> div { "Sorry this barcode format is not yet supported :(" }

let eventTicket model passDefinition passStructure =
    div {
        ``class``
            "w-full rounded-3xl text-white p-3 \
                   overflow-hidden \
                   bg-repeat"


        match model.background with
        | None -> empty ()
        | Some (PassBackground (Image.Base64 base64String)) ->
            let source = createPngDataUrl base64String
            style $"background-image: url({source})"

        // Header row
        headerRow model passStructure

        // What I call "body"
        // Body row
        bodyRow model passStructure
        // Auxiliary fields
        auxiliaryFields passStructure

        // Barcode
        //TODO prefer barcodes over barcode which is deprecated-ish and use barcode as fallback
        barcode passDefinition
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
                    eventTicket model passDefinition passStructure
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

let homePage (model : Model) (dispatch: Message Dispatch) =
    concat {
        
        comp<PageTitle> { "Passes" }
        main {
            ``class`` "p-4"
            h1 {
                "Passes"
            }
            
            ul {
                forEach model.cacheUrls (fun url -> li {url})
            }
            
            button {
                on.click (fun _ -> ())
                "Add"
            }
        }
    }

let view (model: Model) (dispatch: Message Dispatch) =
    match model.page with
    | Home -> homePage model dispatch
    | Open -> openPage model
    | OpenFile fileName -> openPage model

let router =
    Router.infer SetPage (fun model -> model.page)


let getProperty<'T> (reference: IJSObjectReference) (properties : string array) (jsRuntime : IJSRuntime)  =
        task {
           let! asString = jsRuntime.InvokeAsync<string>("JSON.stringify", reference, properties)
           return JsonSerializer.Deserialize<'T>(asString)
        }
let program (jsRuntime: IJSRuntime) logger (client: HttpClient) =
    let update = update jsRuntime logger client

    
    let laodCacheUrls () =
        task {
            let! cache =  jsRuntime |> CacheStorage.open' "files"
            
            Console.WriteLine "Opened cache"
            let! requests = Cache.getKeys cache
            let getRequestUrl request =
                match request with
                | Request reference -> getProperty<{|url:string|}> reference [|"url"|] jsRuntime
                
            let getUrlTasks = requests |> Seq.map getRequestUrl
            let! urls = Task.WhenAll getUrlTasks
            return urls |> Array.map (fun request -> request.url)
            }
//            match cache with
//            | Cache reference ->
//                let! array = reference.InvokeAsync<IJSInProcessObjectReference>("keys")
//                do! JsConsole.log array jsRuntime
//                let! requestAsJsObject =  array.InvokeAsync<IJSObjectReference>("at",0)
//                //THIS works!
//                let! stringified = jsRuntime.InvokeAsync<string>("JSON.stringify", requestAsJsObject, Array.singleton "url")
//                let request = JsonSerializer.Deserialize<Request>(stringified)
//                Console.WriteLine $"This should be a request object {request}"
//
//                do! JsConsole.log requestAsJsObject jsRuntime

            
             
//             Console.WriteLine "Got requests"
//             let mutable index = 0
//             // The urls represent all files we have saved in the cache
//             use enumerator =requests.GetEnumerator ()
//             while enumerator.MoveNext() && index < 5 do
//                 index <- index + 1
//                 let  request = enumerator.Current
//                 logger.LogInformation("Request: {Request}", request)
// //            for request in requests do
// //                if index = 4 then
// //                    break
// //                else
// //                logger.LogInformation("Request: {Request}", request)
//             
// //            let urls = requests |> Seq.map (fun request -> request.Url) |> Seq.toArray
//             Console.WriteLine "Got urls"
 
    //let startCommand =
    //    Command.batch [ Command.OfFunc.either loadClientId () idToMessage idErrorToMessage
    //                    Command.OfFunc.either loadClientSecret () secretToMessage secretErrorToMessage ]

    let logError = LoadFilesError >> LogError

    let startCommand = 
        Command.OfTask.either laodCacheUrls () SetPassCacheUrls logError

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