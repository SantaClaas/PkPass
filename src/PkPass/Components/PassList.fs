namespace PkPass.Components

open Bolero
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop
open System.Threading.Tasks
open Bolero.Html
open PkPass.Components.Elements

type PassList() =
    inherit Component()
    let listReference = HtmlRef()

    [<Inject>]
    member val JsRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    override this.Render() =
        ul {
            attr.``class``
                "absolute top-0 left-0 p-4 text-slate-900 snap-y snap-mandatory h-full w-full overflow-y-auto"

            listReference


            li {
                attr.``class`` "snap-center sticky top-0 origin-top"
                boardingPass ()
            }

            li {
                attr.``class`` "snap-center sticky top-0 origin-top pt-2"

                coupon ()
            }

            li {
                attr.``class`` "snap-center sticky top-0 origin-top pt-5"

                eventTicketWithBackgroundImage ()
            }

            li {
                attr.``class`` "snap-center sticky top-0 origin-top pt-8"

                eventTicketWithStripImage ()
            }
            
            li {
                attr.``class`` "snap-center sticky top-0 origin-top pt-8"

                genericPass ()
            }
        // forEach loadResults (fun result ->
        //     cond result (fun result ->
        //         match result with
        //         | Error loadPassError ->
        //             li {
        //                 h2 { $"Sorry could not load that pass" }
        //                 p { string loadPassError }
        //             }
        //         | Result.Ok passPackage ->
        //             ecomp<PassPackageCard, _, _> passPackage (function
        //                 | PassPackageCardMessage.DeletePass passName -> HomePageMessage.DeletePass passName |> dispatch) {
        //             attr.empty ()
        //         }))
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


