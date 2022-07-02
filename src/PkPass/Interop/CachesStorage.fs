namespace PkPass.Interop

open System
open Microsoft.JSInterop

type CacheStorage = CacheStorage of IJSObjectReference
type Cache = Cache of IJSObjectReference
type Request = Request of IJSObjectReference
module CacheStorage =
    let open' (name: String) (jsRuntime: IJSRuntime) =
        task {
            let! cache = jsRuntime.InvokeAsync<IJSObjectReference>("caches.open", name)
            return Cache cache
        }
        
    let getKeys =
        function
        | CacheStorage.CacheStorage reference ->
            task {
                let! cachesArray = reference.InvokeAsync<IJSInProcessObjectReference>("keys")

                return
                    cachesArray
                    |> JsArray
                    |> JsEnumerable<IJSObjectReference>
                    |> Seq.choose id
                    |> Seq.map Cache
            }

module Cache =
    let getKeys =
        function
        | Cache reference ->
            task {
                // Returns a promise with an array of Requests that we should be able to use as in process reference
                let! requestsArray = reference.InvokeAsync<IJSInProcessObjectReference>("keys")
                return
                    requestsArray
                    |> JsArray
                    |> JsEnumerable<IJSObjectReference>
                    |> Seq.choose id
                    |> Seq.map Request
            }
            