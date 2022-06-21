namespace PkPass.Interop

open System
open System.Collections.Generic
open Microsoft.JSInterop

type CacheStorage = CacheStorage of IJSObjectReference
type Cache = Cache of IJSObjectReference

// Get this from keys()
type Iterator = Iterator of IJSObjectReference
type Array = Array of IJSInProcessObjectReference

type Request = Request of IJSObjectReference
[<RequireQualifiedAccess>]
type JsObjectReferences =
    | CacheStorage of CacheStorage
    | Cache of CacheStorage
    | Iterator of Iterator
    | Array of Array
    | Request of Request
// Does not work on (Blazor) server hosting model because of synchronous interop
type JsEnumerator<'T>(array: Array) =

//TODO use values api not keys to reduce it to one interop call
    // Array or Map have the keys() method which returns the iterator with the next() method which is
    // similar to IEnumerator.MoveNext. But unlike Enumerator it does not have a reset. Therefore we save a
    // reference to the array or other iterable object and create a new iterator when reset is called
    let mutable currentIterator =
        match array with
        | Array reference -> reference.Invoke<IJSInProcessObjectReference> "keys"

    let mutable current: 'T option = None

    interface IEnumerator<'T option> with
        member this.Current = current

    interface System.Collections.IEnumerator with
        member this.MoveNext() =
            let value = currentIterator.Invoke<{| value: int; ``done``: bool |}>("next")

            if value.``done`` then
                false
            else 
                current <-
                    match array with
                    | Array reference ->                        
                        reference.Invoke<'T>("at", value.value) |> Some
                true

        member this.Current = box current

        member this.Reset() =
            currentIterator <-
                match array with
                | Array reference -> reference.Invoke<IJSInProcessObjectReference> "keys"

            ()

    interface IDisposable with
        member this.Dispose() =
            match array with
            | Array reference -> reference.Dispose()

    interface IAsyncDisposable with
        member this.DisposeAsync() =
            match array with
            | Array reference -> reference.DisposeAsync()

type JsEnumerable<'T>(array: Array) =
    interface IEnumerable<'T option> with
        member this.GetEnumerator() : IEnumerator<'T option> =
            new JsEnumerator<'T>(array) :> IEnumerator<'T option>

        member this.GetEnumerator() : Collections.IEnumerator =
            new JsEnumerator<'T>(array) :> Collections.IEnumerator


module JsConsole =
    let log (reference : IJSObjectReference) (jsRuntime: IJSRuntime) =
        jsRuntime.InvokeVoidAsync("console.log","Log from F# ", reference)


module CacheStorage =
    let open' (name: String) (jsRuntime: IJSRuntime) =
        task {
            let! cache = jsRuntime.InvokeAsync<IJSObjectReference>("caches.open", name)
            do! JsConsole.log cache jsRuntime
            return Cache cache
        }
    let getKeys =
        function
        | CacheStorage.CacheStorage reference ->
            task {
                let! cachesArray = reference.InvokeAsync<IJSInProcessObjectReference>("keys")

                return
                    cachesArray
                    |> Array
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
                    |> Array
                    |> JsEnumerable<IJSObjectReference>
                    |> Seq.choose id
                    |> Seq.map Request
            }
            