namespace PkPass

open System
open System.Net.Http
open Bolero.Html
open Components
open Elmish
open Microsoft.JSInterop
open PkPass.Interop
open PkPass.Interop.Window
open PkPass.LoadPass
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package

// Describes all the errors that can occur when loading a pass

module HomePage =
    // Home page can be displaying loaded passes or it is currently loading passes
    type HomePageState =
        | LoadingPasses
        // Passes loaded and display load result
        | PassesLoaded of Result<PassPackage, LoadPassError> array

    type HomePage = HomePage

    module HomePageState =
        let ``default`` = LoadingPasses

    type HomePageError = UnknownError of Exception

    type HomePageMessage =
        | LoadPasses
        | SetPassLoadResult of Result<PassPackage, LoadPassError> array
        | RequestFileFromUser
        | AddUserSelectedFiles of FileSystemFileHandle array
        | LogError of HomePageError



    let update (message: HomePageMessage) (model: HomePageState) (jsRuntime: IJSRuntime) (httpClient: HttpClient) =
        match message with
        | LoadPasses ->
            let loadAndSet () = completelyLoadPasses jsRuntime httpClient
            let command = Cmd.OfTask.either loadAndSet () HomePageMessage.SetPassLoadResult (UnknownError >> LogError)
            model, command
        | SetPassLoadResult results -> HomePageState.PassesLoaded results, Cmd.none
        | RequestFileFromUser ->
            let requestFile () =
                task {
                    let pkPassMimeType =
                        "application/vnd.apple.pkpass"

                    let pkPassesMimeType =
                        "application/vnd.apple.pkpasses"

                    let pkPassFileExtension = ".pkpass"
                    let pkPassesFileExtension = ".pkpasses"




                    let! isFileAccessSupported = Window.hasOwnProperty "showOpenFilePicker" jsRuntime
                    Console.WriteLine $"is file access supported: {isFileAccessSupported}"

                    if (*isFileAccessSupported*) false then
                        let acceptedFileTypes =
                            Map.ofList [ (pkPassMimeType, [| pkPassFileExtension |])
                                         (pkPassesMimeType, [| pkPassesFileExtension |]) ]

                        let options: ShowOpenFilePickerOptions =
                            { types =
                                [| {| description = "Pass files"
                                      accept = acceptedFileTypes |} |] }

                        return! Window.showOpenFilePicker options jsRuntime
                    else
                        // Create input element
                        // Invoke click
                        let! input = Document.createElement "input" jsRuntime
                        Console.WriteLine $"Created input element"

                        let acceptValue =
                            String.Join(
                                ',',
                                pkPassMimeType,
                                pkPassesMimeType,
                                pkPassFileExtension,
                                pkPassesFileExtension
                            )


                        do! Element.setAttribute "type" "input" input
                        do! Element.setAttribute "accept" acceptValue input

                        Console.WriteLine $"Set accept attribute"

                        do!
                            input
                            |> HtmlElement.createFromElement
                            |> HtmlElement.click


                        Console.WriteLine $"Clicked element"
                        let (Element reference) = input
                        do! JsConsole.log reference jsRuntime
                        return [||]
                }

            model, Cmd.OfTask.either requestFile () AddUserSelectedFiles (UnknownError >> LogError)
        | AddUserSelectedFiles fileHandles ->
            fileHandles
            |> Array.length
            |> printfn "Loaded %O files"

            model, Cmd.none
        | LogError (UnknownError ``exception``) ->
            Console.WriteLine $"An unexpected error occured:{Environment.NewLine}{``exception``}"
            model, Cmd.none

    let private createPngDataUrl base64String = $"data:image/png;base64,{base64String}"

    let private renderPrimaryField (field: Field) (headerFields) =
        cond field.label (fun label ->
            match label with
            | Some (LocalizableString.LocalizableString label) ->
                h3 {
                    attr.``class`` "flex justify-between items-end mb-1"

                    span {
                        attr.``class`` "font-bold uppercase text-xs tracking-wider text-emphasis-low"

                        label
                    }

                    match headerFields with
                    | Some [ first ] ->
                        span {
                            attr.``class`` "text-sm text-emphasis-medium leading-none"
                            string first.value
                        }
                    | _ -> empty ()
                }
            | _ -> empty ())

    let private renderPassThumbnail thumbnail =
        match thumbnail with
            | PassThumbnail (Image.Base64 base64String) ->
                img {
                    attr.``class`` "w-20 rounded-lg"
                    base64String |> createPngDataUrl |> attr.src
                }
                
    let private renderEventTicket passDefinition passStructure passPackage =
         div {
                attr.``class`` "bg-white/5 flex gap-3 p-3 rounded-xl justify-between"
                
                renderPassThumbnail passPackage.thumbnail

                div {
                    attr.``class`` "flex flex-col justify-between"

                    div {
                        cond passStructure.primaryFields (fun fields ->
                            match passStructure.primaryFields with
                            | Some [ first ] ->
                                concat {
                                    renderPrimaryField first passStructure.headerFields

                                    h2 {
                                        attr.``class`` "leading-none text-lg font-medium text-emphasis-high"
                                        string first.value
                                    }
                                }

                            | _ -> empty ())
                    }

                    div {
                        cond passStructure.secondaryFields (fun fields ->
                            match fields with
                            | Some [ first ] ->
                                concat {
                                    cond first.label (fun label ->
                                        match label with
                                        | Some (LocalizableString.LocalizableString label) ->
                                            h5 {
                                                attr.``class`` "text-xs tracking-wider text-emphasis-low uppercase"

                                                label
                                            }
                                        | _ -> empty ())

                                    h4 {
                                        attr.``class`` "leading-none text-sm font-medium text-emphasis-medium"

                                        string first.value
                                    }
                                }
                            | _ -> empty ())
                    }
                }
            }
    let private renderPassPackage passPackage =
        cond passPackage.pass (fun pass ->
            match pass with
            | EventTicket (passDefinition, passStructure) ->
                renderEventTicket passDefinition passStructure passPackage
            | _ -> div { "Sorry this pass type is not supported yet" })

    let private passesPreviewList loadResults dispatch =

        ul {
            forEach loadResults (fun result ->
                cond result (fun result ->
                    match result with
                    | Error loadPassError -> p { "Sorry could not load that pass" }
                    | Ok passPackage -> renderPassPackage passPackage))

        }

    let view (model: HomePageState) (dispatch: HomePageMessage Dispatch) =
        match model with
        | LoadingPasses -> main { "Loading passes..." }
        | PassesLoaded loadResults ->
            concat {

                h1 {
                    attr.``class`` "text-xl font-lighter mb-2 tracking-widest"
                    "Passes"
                }

                passesPreviewList loadResults dispatch

                // Button click has big side effect of requesting files from user and loading them into cache where
                // we need to pick them up
                ecomp<AddPassFloatingActionButton, _, _> () (fun _ -> dispatch HomePageMessage.LoadPasses) { attr.empty () }
            }
