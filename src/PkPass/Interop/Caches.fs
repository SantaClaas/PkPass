module Caches

    open System
    open Microsoft.JSInterop

    type Cache = Cache of IJSObjectReference
    let open' (name : String) (jsRuntime : IJSRuntime) =
        task {
            let! cache = jsRuntime.InvokeAsync ("caches.open", name)
            return Cache cache
        }

