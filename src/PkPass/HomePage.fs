namespace PkPass

open System
open Bolero.Html
open Components
open Elmish
open Microsoft.JSInterop
open PkPass.Interop
open PkPass.Interop.Window
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package

// Describes all the errors that can occur when loading a pass
type LoadPassError =
    | LoadCacheUrlError
    | LoadPassDataFromCacheError of Exception
    | DeserializationError of DeserializationError
    
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
        
        
    
    let update (message: HomePageMessage) (model: HomePageState) (jsRuntime: IJSRuntime) =
        match message with
        | LoadPasses ->
            Console.WriteLine "Uhm. This is awkward"
            model, Cmd.none
        | SetPassLoadResult results ->
            HomePageState.PassesLoaded results, Cmd.none
        | RequestFileFromUser -> 
            let requestFile () =
                let acceptedFileTypes =
                    Map.ofList [ ("application/vnd.apple.pkpass", [| ".pkpass" |])
                                 ("application/vnd.apple.pkpasses", [| ".pkpasses" |]) ]
    
                let options : ShowOpenFilePickerOptions =
                    { types =
                        [| {| description = "Pass files"
                              accept = acceptedFileTypes |} |] }
    
                Window.showOpenFilePicker options jsRuntime
    
            model, Cmd.OfTask.either requestFile () AddUserSelectedFiles (UnknownError >> LogError)
        | AddUserSelectedFiles fileHandles ->
            fileHandles |> Array.length |> printfn "Loaded %O files"
            model, Cmd.none
        | LogError (UnknownError ``exception``) ->
            Console.WriteLine $"An unexpected error occured:{Environment.NewLine}{``exception``}"
            model, Cmd.none

    let private createPngDataUrl base64String = $"data:image/png;base64,{base64String}"
    let view (model: HomePageState) (dispatch: HomePageMessage Dispatch) =
        Console.WriteLine $"Rendering home page with ${model.GetType()}"
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
                                            attr.``class`` "w-20 rounded-lg"
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
                                                        attr.``class`` "flex justify-between items-end mb-1"
    
                                                        span {
                                                            attr.``class``
                                                                "font-bold uppercase text-xs tracking-wider text-emphasis-low"
    
                                                            label
                                                        }
    
                                                        match passStructure.headerFields with
                                                        | Some [ first ] ->
                                                            span {
                                                                attr.``class`` "text-sm text-emphasis-medium leading-none"
                                                                string first.value
                                                            }
                                                        | _ -> empty ()
                                                    }
    
                                                | _ -> empty ()
    
                                                h2 {
                                                    attr.``class`` "leading-none text-lg font-medium text-emphasis-high"
                                                    string first.value
                                                }
                                            | _ -> empty ()
                                        }
    
                                        div {
                                            match passStructure.secondaryFields with
                                            | Some [ first ] ->
                                                match first.label with
                                                | Some (LocalizableString.LocalizableString label) ->
                                                    h5 {
                                                        attr.``class`` "text-xs tracking-wider text-emphasis-low uppercase"
                                                        label
                                                    }
                                                | _ -> empty ()
    
                                                h4 {
                                                    attr.``class`` "leading-none text-sm font-medium text-emphasis-medium"
                                                    string first.value
                                                }
                                            | _ -> empty ()
                                        }
                                    }
                                }
                            | _ -> div { "Sorry this pass type is not supported yet" })
                }
    
                comp<FloatingActionButton> { on.click (fun _ -> dispatch RequestFileFromUser) }
            }
