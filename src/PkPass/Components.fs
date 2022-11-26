module Components

open System
open System.Threading.Tasks 
open Bolero
open Bolero.Html
open Bolero.Html.attr
open System.Collections.Generic
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package
open PkPass.Events
open PkPass.Events.Html

type ContainedButtonModel = { label: string; icon: Node option }

type FilledButton() =
    inherit ElmishComponent<ContainedButtonModel, unit>()

    [<Parameter(CaptureUnmatchedValues = true)>]
    member val AdditionalAttributes = Unchecked.defaultof<IReadOnlyDictionary<string, obj>> with get, set

    override this.View model _ =
        button {

            let paddingX =
                match model.icon with
                | Some _ -> "pl-4 pr-6"
                | None -> "px-6"

            ``class``
                $"dark:bg-primary-100 dark:hover:bg-primary-200 \
                 dark:disabled:bg-slate-100 dark:active:bg-primary-300 \
                 dark:text-slate-900 dark:disabled:text-slate-500 uppercase \
                 text-label-lg font-label-lg leading-label-lg tracking-label-lg \
                 {paddingX} py-2 h-10 rounded-full \
                 transition-colors hover:shadow focus:outline-none \
                 flex gap-2 items-center"

            Attributes.additionalAttributes this.AdditionalAttributes

            match model.icon with
            | Some node -> node
            | None -> Node.Empty()

            model.label
        }

type AssistiveText =
    | None
    | HelperText of string
    // If there is an error it should have information what the error is
    | ErrorText of string

type OutlineInputModel =
    { value: string option
      // "Every text field should have a label" -> material.io
      label: string
      id: string
      placeholder: string option
      assistiveText: AssistiveText }

type OutlinedInput() =
    inherit ElmishComponent<OutlineInputModel, string option>()

    [<Parameter(CaptureUnmatchedValues = true)>]
    member val AdditionalAttributes = Unchecked.defaultof<IReadOnlyDictionary<string, obj>> with get, set

    override this.View model dispatch =
        let paddingBottom =
            match model.assistiveText with
            | None -> "mb-6"
            | HelperText _
            | ErrorText _ -> String.Empty

        div {
            div {
                ``class`` $"relative rounded-md focus-within:border-primary-100 group {paddingBottom}"

                input {
                    ``class``
                        "transition-all touch-manipulation py-3 px-4 w-full \
                         rounded-md \
                         dark:bg-elevation-2 hover:dark:bg-elevation-1 focus:dark:bg-elevation-0 autofill:bg-black \
                         leading-none dark:text-emphasis-high \
                         placeholder:dark:text-emphasis-medium \
                         outline-none \
                         placeholder:opacity-0 \
                         placeholder:focus:opacity-100 \
                         placeholder:transition-all \
                         placeholder:text-lg"

                    model.value
                    |> Option.map value
                    |> Option.defaultWith attr.empty

                    id model.id

                    model.placeholder
                    |> Option.defaultValue " "
                    |> placeholder

                    Attributes.additionalAttributes this.AdditionalAttributes

                    on.change (fun arguments ->
                        let value =
                            arguments.Value
                            |> Option.ofObj
                            |> Option.map string
                        // Need to update value because it changes rendering even if the parent component does not care about value change
                        this.Model <- { model with value = value }
                        dispatch value)
                }

                div {
                    ``class``
                        "flex absolute top-0 left-0 origin-top-left w-full h-full \
                         touch-manipulation pointer-events-none"

                    let borderColor =
                        match model.assistiveText with
                        | HelperText _
                        | None ->
                            "group-focus-within:border-primary-100 group-hover:group-focus-within:border-primary-100 border-emphasis-low group-hover:border-emphasis-medium"
                        | ErrorText _ -> "border-red-300"

                    // Filler start
                    div { ``class`` $"w-4 {borderColor} border-y border-l rounded-l-md" }

                    // Filler middle
                    div {
                        let isEmpty =
                            model.value
                            |> Option.map String.IsNullOrEmpty
                            |> Option.defaultValue true

                        let (translate, fontSize, borderTop, borderHoverColor) =
                            if isEmpty then
                                "translate-y-1/3", String.Empty, String.Empty, String.Empty
                            else
                                "-translate-y-3.5",
                                "text-xs",
                                "border-t-transparent",
                                "group-hover:border-t-transparent "

                        ``class``
                            $"border-y {borderColor} \
                            {borderHoverColor}
                            group-focus-within:border-t-transparent group-hover:group-focus-within:border-t-transparent group-focus-within:group-hover:border-t-transparent \
                            px-0.5 {borderTop}"

                        div {
                            ``class`` $"transition-all group-focus-within:-translate-y-3.5 {translate}"

                            let textColor =
                                match model.assistiveText with
                                | ErrorText _ -> "text-red-300"
                                | HelperText _
                                | None when isEmpty -> "text-emphasis-medium group-focus-within:text-primary-100"
                                | HelperText _
                                | None -> "text-emphasis-low group-focus-within:text-primary-100"

                            Html.label {
                                ``class`` $"{textColor} {fontSize} group-focus-within:text-xs whitespace-nowrap"
                                ``for`` model.id

                                model.label
                            }
                        }
                    }

                    // Filler end
                    div { ``class`` $"w-full border-y border-r {borderColor} rounded-r-md" }

                    let values =
                        match model.assistiveText with
                        | HelperText text -> Some("text-emphasis-medium", text, Html.span)
                        | ErrorText text -> Some("text-red-300", text, Html.span)
                        | None -> Option.None

                    match values with
                    | Option.None -> Html.empty ()
                    | Some (textColor, text, element) ->
                        element {
                            ``class`` $"pl-4 text-xs {textColor}"
                            text
                        }
                }
            }
        }

type VersionInformation() =
    inherit Component()

    let version =
        typeof<VersionInformation>
            .Assembly.GetName()
            .Version.ToString(3)

    override this.Render() =
        Html.span {
            ``class``
                "fixed bottom-0 left-0 mb-4 ml-4 \
                       text-emphasis-low text-xs"

            version
        }




type FloatingActionButton() =
    inherit Component()

    [<Parameter(CaptureUnmatchedValues = true)>]
    member val AdditionalAttributes = Unchecked.defaultof<IReadOnlyDictionary<string, obj>> with get, set

    member val ButtonReference = HtmlRef()

    //<svg xmlns="http://www.w3.org/2000/svg" height="48" width="48"><path d="M22.5 38V25.5H10v-3h12.5V10h3v12.5H38v3H25.5V38Z"/></svg>
    override this.Render() =
        button {
            attr.``class``
                "fixed bottom-0 right-0 mb-4 mr-4 p-3 \
                            text-black bg-primary-300 rounded-full stroke-2 shadow"

            Attributes.additionalAttributes this.AdditionalAttributes
            this.ButtonReference

            rawHtml
                """<svg class="w-11 h-11" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path></svg>"""
        }

type AddPassFloatingActionButton() =
    inherit ElmishComponent<unit, unit>()

    let fallBackInputReference = HtmlRef()
    let buttonReference = HtmlRef()

    [<Literal>]
    let pkPassMimeType =
        "application/vnd.apple.pkpass"

    [<Literal>]
    let pkPassesMimeType =
        "application/vnd.apple.pkpasses"

    [<Literal>]
    let pkPassFileExtension = ".pkpass"

    [<Literal>]
    let pkPassesFileExtension = ".pkpasses"

    [<Inject>]
    member val JsRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    //TODO make this not callable from literally everywhere else. Use a relay or something
    [<JSInvokable>]
    member this.PassesChanged () =
        this.Dispatch ()
    override this.OnAfterRenderAsync isFirstRender =
        if isFirstRender then
            let reference = DotNetObjectReference.Create this
            this.JsRuntime.InvokeAsync("registerClick", buttonReference.Value, fallBackInputReference.Value, reference).AsTask()
        else Task.CompletedTask
        
        
    override this.View _ dispatch =
        concat {
            button {
                attr.``class``
                    "fixed bottom-0 right-0 mb-4 mr-4 p-3 \
                                text-black bg-primary-300 rounded-full stroke-2 shadow"

                buttonReference

                rawHtml
                    """<svg class="w-11 h-11" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path></svg>"""
            }

            input {
                ``type`` "file"
                style "visibility: hidden;"
                accept (String.Join(',', pkPassMimeType, pkPassesMimeType, pkPassFileExtension, pkPassesFileExtension))
                fallBackInputReference
            }
        }   

type PassPackageCardMessage = DeletePass of passName : string
type PassPackageCard() =
    inherit ElmishComponent<PassPackage,PassPackageCardMessage>()
    
    let scrollContainerReference = HtmlRef()
    let deleteSectionReference = HtmlRef()
    let mutable intersectionObserver = Option.None

    let createPngDataUrl base64String = $"data:image/png;base64,{base64String}"
    let renderPassThumbnail thumbnail =
        match thumbnail with
        | PassThumbnail (Image.Base64 base64String) ->
            img {
                attr.``class`` "w-20 rounded-lg"
                base64String |> createPngDataUrl |> attr.src
            }

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

    let renderEventTicket passStructure package dispatch =
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
                        renderPassThumbnail package.thumbnail
                        
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
                            package.fileName
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
        cond package.pass (fun pass ->
            match pass with
            | EventTicket (passDefinition, passStructure: PassStructure) -> renderEventTicket passStructure package dispatch
            | _ -> li { "Sorry this pass type is not supported yet" } )

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
    