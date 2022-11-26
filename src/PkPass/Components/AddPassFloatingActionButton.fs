namespace PkPass.Components

open System
open System.Threading.Tasks 
open Bolero
open Bolero.Html
open Bolero.Html.attr
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop

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
