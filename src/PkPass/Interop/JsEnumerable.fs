namespace PkPass.Interop

open System
open System.Collections.Generic
open Microsoft.JSInterop

type JsArray = JsArray of IJSInProcessObjectReference
type JsEnumerator<'T>(array: JsArray) =

//TODO use values api not keys to reduce it to one interop call
    // Array or Map have the keys() method which returns the iterator with the next() method which is
    // similar to IEnumerator.MoveNext. But unlike Enumerator it does not have a reset. Therefore we save a
    // reference to the array or other iterable object and create a new iterator when reset is called
    let mutable currentIterator =
        match array with
        | JsArray reference -> reference.Invoke<IJSInProcessObjectReference> "keys"

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
                    | JsArray reference ->                        
                        reference.Invoke<'T>("at", value.value) |> Some
                true

        member this.Current = box current

        member this.Reset() =
            currentIterator <-
                match array with
                | JsArray reference -> reference.Invoke<IJSInProcessObjectReference> "keys"

            ()

    interface IDisposable with
        member this.Dispose() =
            match array with
            | JsArray reference -> reference.Dispose()

    interface IAsyncDisposable with
        member this.DisposeAsync() =
            match array with
            | JsArray reference -> reference.DisposeAsync()


type JsEnumerable<'T>(array: JsArray) =
    interface IEnumerable<'T option> with
        member this.GetEnumerator() : IEnumerator<'T option> =
            new JsEnumerator<'T>(array) :> IEnumerator<'T option>

        member this.GetEnumerator() : Collections.IEnumerator =
            new JsEnumerator<'T>(array) :> Collections.IEnumerator