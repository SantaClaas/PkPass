namespace PkPass

open System
open System.Net.Http
open Bolero.Html
open Components
open Elmish
open Microsoft.JSInterop
open PkPass.Interop
open PkPass.LoadPass
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package
open FSharp.Core.Result

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
        | DeletePass of passName: string
        | AddUserSelectedFiles of FileSystemFileHandle array
        | LogError of HomePageError

    let deleteCachedPassFile fileName jsRuntime =
        task {
            let! cache = CacheStorage.open' "files" jsRuntime
            let! _ = Cache.delete $"/files/{fileName}" cache
            return ()
        }

    let update (message: HomePageMessage) (model: HomePageState) (jsRuntime: IJSRuntime) (httpClient: HttpClient) =
        match message with
        | LoadPasses ->
            let loadAndSet () =
                completelyLoadPasses jsRuntime httpClient

            let command =
                Cmd.OfTask.either loadAndSet () HomePageMessage.SetPassLoadResult (UnknownError >> LogError)

            model, command
        | SetPassLoadResult results -> HomePageState.PassesLoaded results, Cmd.none
        | DeletePass passName ->
            let delete () = deleteCachedPassFile passName jsRuntime
            model, Cmd.OfTask.either delete () (fun _ -> LoadPasses) (UnknownError >> LogError)
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

    let private renderEventTicket passDefinition passStructure passPackage dispatch =
        li {
            attr.``class`` "bg-white/5 rounded-xl overflow-hidden"
            div {
                // Source: https://oh-snap.netlify.app/#overscroll 👏
                let swipeActionStyles ="flex justify-center first:justify-end last:justify-start items-center \
                                        text-2xl gap-3 p-3 "

                attr.``class``
                    "grid grid-cols-[100%_100%] grid-rows-[[action]_1fr] \
                     overflow-x-scroll overscroll-x-contain \
                     snap-x snap-mandatory snap-always text-white"

                div {
                    attr.``class`` $"{swipeActionStyles} snap-center overflow-y-hidden bg-inherit z-10 shadow"

                    div {
                        attr.``class`` "flex gap-3 justify-between"

                        // Just trigger a delete for now until I have implemented a proper interaction
                        // If I feel fancy it is going to be a swipe
                        on.click (fun _ ->
                            passPackage.fileName
                            |> HomePageMessage.DeletePass
                            |> dispatch)

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
                                                        attr.``class``
                                                            "text-xs tracking-wider text-emphasis-low uppercase"

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
                }
                div {
                    attr.``class`` $"{swipeActionStyles} snap-center bg-red-500"
                    "delete"
                }
            }
        }

    let private renderPassPackage passPackage dispatch =
        cond passPackage.pass (fun pass ->
            match pass with
            | EventTicket (passDefinition, passStructure) ->
                renderEventTicket passDefinition passStructure passPackage dispatch
            | _ -> li { "Sorry this pass type is not supported yet" })

    let private passesPreviewList loadResults dispatch =
        ul {
            forEach loadResults (fun result ->
                cond result (fun result ->
                    match result with
                    | Error loadPassError -> p { "Sorry could not load that pass" }
                    | Ok passPackage -> renderPassPackage passPackage dispatch))

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
                ecomp<AddPassFloatingActionButton, _, _> () (fun _ ->
                    Console.WriteLine "Dispaych"
                    dispatch HomePageMessage.LoadPasses) {
                    attr.empty ()
                }
            }
