module PkPass.Interop.Window

open System
open System.Text.Json
open Microsoft.JSInterop


// Functions for calling JS methods on the global window object
type ShowOpenFilePickerOptions =
    { types: {| description: string
                accept: Map<string, string array> |} array }

let showOpenFilePicker (options: ShowOpenFilePickerOptions) (jsRuntime: IJSRuntime) =
    task {
        let! fileHandlesArray = jsRuntime.InvokeAsync<IJSInProcessObjectReference>("window.showOpenFilePicker", options)

        return
            fileHandlesArray
            |> JsArray
            |> JsEnumerable<IJSInProcessObjectReference>
            |> Seq.choose id
            |> Seq.map FileSystemFileHandle.FileSystemFileHandle
            |> Seq.toArray
    }
