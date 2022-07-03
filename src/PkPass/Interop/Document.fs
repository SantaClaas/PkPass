module PkPass.Interop.Document

open Microsoft.JSInterop
open PkPass.Interop

let createElement (tagName: string) (jsRuntime : IJSRuntime)=
     task {
         let! elementReference = jsRuntime.InvokeAsync<IJSObjectReference> ("document.createElement", tagName)
         return Element elementReference
     }
    
    