namespace PkPass.Components

open System
open Bolero
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open System.Threading.Tasks
open Bolero.Html
open PkPass.Components.Elements
open PkPass.LoadPass
open PkPass.PassKit.Images
open PkPass.PassKit.Package

type PassList() =
    inherit ElmishComponent<Result<PassPackage, LoadPassError> array, unit>()
    let listReference = HtmlRef()
    let listItem (pass : Node) =
        li {        
            attr.``class`` "snap-center sticky top-0 origin-top"
            pass
        }


    [<Inject>]
    member val JsRuntime = Unchecked.defaultof<IJSRuntime> with get, set
    
    override this.View loadPassResults _ =
        ul {
            attr.``class``
                "absolute top-0 left-0 p-4 text-slate-900 snap-y snap-mandatory h-full w-full overflow-y-auto"

            listReference
            
            forEach loadPassResults (fun result ->
                cond result (fun result ->
                    match result with
                    | Error loadPassError ->
                        li {
                            h2 { $"Sorry could not load that pass" }
                            p { string loadPassError }
                        }
                    | Result.Ok passPackage ->
                        match passPackage with
                        | PassPackage.EventTicket package ->
                            match package.images with
                            | EventTicketImages(commonImages, EventTicketImageOption.Other (backgroundImage, thumbnail)) ->
                                eventTicketWithBackgroundImage backgroundImage thumbnail
                            | EventTicketImages(commonImages, EventTicketImageOption.StripImage eventTicketImageOption) ->
                                eventTicketWithStripImage ()
                               
                        |> listItem
                        ))
        }
        

    override this.OnAfterRenderAsync isFirstRender =
        match listReference.Value, isFirstRender with
        | Some listReference, true ->
            task {
                use! jsModule = this.JsRuntime.InvokeAsync<IJSObjectReference>("import", "/cards3d.js")
                do! jsModule.InvokeVoidAsync("scaleOnScroll", listReference)

                return ()
            }
        | _, _ -> Task.CompletedTask
