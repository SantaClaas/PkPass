// Just a random collection of interop js function that we import
module Module

open Microsoft.JSInterop
type Module = Module of IJSInProcessObjectReference

let initialize (runtime: IJSRuntime) =
    task {
        let! module' = runtime.InvokeAsync<IJSInProcessObjectReference>("import", "/module.js")

        return Module module'
    }

let get<'T> (attribute: string) (reference: IJSInProcessObjectReference) (jsModule: Module) =
    match jsModule with
    | Module module' -> module'.Invoke<'T>("getAttribute", reference, attribute)

let createObjectUrl (blobOrFile: IJSInProcessObjectReference) (jsModule: Module) =
    match jsModule with
    | Module module' -> module'.Invoke<string>("please", blobOrFile)
