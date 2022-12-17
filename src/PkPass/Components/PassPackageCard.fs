namespace PkPass.Components

open System
open System.Threading.Tasks 
open Bolero
open Bolero.Html
open Bolero.Html.attr
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Images
open PkPass.PassKit.Package
open PkPass.PassKit.Field
open PkPass.PassKit.PassStructure
open PkPass.Events
open PkPass.Events.Html

type PassPackageCardMessage = DeletePass of passName : string
type PassPackageCard() =
    inherit ElmishComponent<PassPackage,PassPackageCardMessage>()
    
    let scrollContainerReference = HtmlRef()
    let deleteSectionReference = HtmlRef()
    let mutable intersectionObserver = Option.None

    let createPngDataUrl base64String = $"data:image/png;base64,{base64String}"
    let renderPassThumbnail thumbnail =
        match thumbnail with
        | Thumbnail (Image.Base64 base64String) ->
            img {
                attr.``class`` "w-20 rounded-lg"
                base64String |> createPngDataUrl |> attr.src
            }
    let renderPassImages images =
        cond images (fun images ->
            match images with
            | EventTicketImages(common, eventImageOption) ->
                cond eventImageOption (fun eventImageOption ->
                    match eventImageOption with
                    | EventTicketImageOption.StripImage _ -> p { "Strip image is not yet supported. I'm working on it" }
                    | EventTicketImageOption.Other(background, thumbnail) -> renderPassThumbnail thumbnail ))

    let renderPrimaryField (field: Field) (headerFields : Field list option) =
        cond field.label (fun label ->
            match label with
            | Some (LocalizableString.LocalizableString label) ->
                h3 {
                    attr.``class`` "flex justify-between items-end mb-1"

                    Html.span {
                        attr.``class`` "font-bold uppercase text-xs tracking-wider text-emphasis-low"
                        label
                    }

                    match headerFields with
                    | Some [ first ] ->
                        Html.span {
                            attr.``class`` "text-sm text-emphasis-medium leading-none"
                            string first.value
                        }
                    | _ -> Html.empty ()
                }
            | _ -> Html.empty ())

    let renderEventTicket passStructure fileName images dispatch =
        li {
            attr.``class`` "bg-white/5 rounded-xl overflow-hidden"
            
            div {
                // Source: https://oh-snap.netlify.app/#overscroll 👏
                let swipeActionStyles ="flex justify-center first:justify-end last:justify-start items-center \
                                        text-2xl gap-3 p-3 "
                attr.``class``
                    "grid grid-cols-[100%_100%] grid-rows-[[action]_1fr] \
                    overflow-x-scroll overscroll-x-contain \
                    snap-x snap-mandatory snap-always text-white \
                    invisible-scrollbar"
                    
                scrollContainerReference
                    
                div {
                    attr.``class`` $"{swipeActionStyles} snap-center overflow-y-hidden bg-inherit z-10 shadow"
                    
                    div {
                        attr.``class`` "flex gap-3 justify-between"
                        renderPassImages images
                        
                        div {
                            attr.``class`` "flex flex-col justify-between"
                            
                            div {
                                cond passStructure.primaryFields (fun fields ->
                                    match fields with
                                    | Some [ first ] -> 
                                        concat {
                                            renderPrimaryField first passStructure.headerFields

                                            h2 {
                                                attr.``class`` "leading-none text-lg font-medium text-emphasis-high"
                                                string first.value
                                            }
                                        }
                                    | _ -> Html.empty ())
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
                                                | _ -> Html.empty ()) 

                                            h4 {
                                                attr.``class`` "leading-none text-sm font-medium text-emphasis-medium"

                                                string first.value
                                            }       
                                        }
                                    | _ -> Html.empty ())
                            }
                        }
                    }
                }

                div {
                    attr.``class`` $"{swipeActionStyles} snap-center bg-red-500"
                    on.intersect (fun arguments -> 
                        if arguments.IsIntersecting then
                            fileName
                            |> PassPackageCardMessage.DeletePass
                            |> dispatch
                        else ())
                    deleteSectionReference
                    "delete"
                }
            }
        }
        
    [<Inject>]
    member val JsRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.View package dispatch =
        cond package (fun package ->
            match package with
            | PassPackage.EventTicket (fileName, EventTicket(definition, structure), images) -> renderEventTicket structure fileName images dispatch
            | _ -> li { "Sorry this pass type is not supported yet" })

    override this.OnAfterRenderAsync isFirstRender =
        if isFirstRender then
            task {
                // let! jsFunction = this.JsRuntime.InvokeAsync<IJSObjectReference>("Function", "a", "b", "return a + b")
                // jsFunction.
                // Create intersection observer and invoke a .NET function when the action overlaps to take action here
                use! jsModule = this.JsRuntime.InvokeAsync<IJSObjectReference>("import", "/scrollsnap.js")
                // Save the intersection observer and dispose it when the component is disposed
                // Might add update of CSS variable to change size of icon depending on how far the delete is swiped open
                let! observer = jsModule.InvokeAsync<IJSObjectReference>("createObserver", scrollContainerReference, deleteSectionReference)
                intersectionObserver <- Some observer
                return ()
            }
        else 
            Task.CompletedTask

    interface IAsyncDisposable with
        member this.DisposeAsync () = 
            match intersectionObserver with
            | Some observer -> 
                task { 
                    do! observer.InvokeVoidAsync("disconnect")
                    do! observer.DisposeAsync ()
                } |> ValueTask
            | Option.None-> ValueTask.CompletedTask
    