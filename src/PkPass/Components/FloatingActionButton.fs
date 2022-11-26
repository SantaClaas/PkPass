namespace PkPass.Components

open Bolero
open Bolero.Html
open Bolero.Html.attr
open System.Collections.Generic
open Microsoft.AspNetCore.Components

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
