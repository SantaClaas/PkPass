module PkPass.Interop.Window

open System
open System.Text.Json
open System.Threading.Tasks
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

let hasOwnProperty (propertyName: string) (jsRuntime: IJSRuntime) =
    jsRuntime.InvokeAsync<bool>("hasOwnProperty", propertyName)

type Parameter<'T> = Parameter of name: string
type Function = Function of IJSInProcessObjectReference
type Function<'TArgument1> = Function of IJSInProcessObjectReference

module Function =
    let call (Function reference) = reference.InvokeVoid("call")

    let call1<'TArgument1> (function1: Function<'TArgument1>) (argument: 'TArgument1) =
        match function1 with
        | Function reference -> reference.InvokeVoid("call", null, argument)

let ``function`` (code: string) (jsRuntime: IJSRuntime) =
    task {
        let! reference = jsRuntime.InvokeAsync<IJSInProcessObjectReference>("window.Function", code)
        return Function reference
    }

let function1<'TArgument1>
    (parameter: Parameter<'TArgument1>)
    (code: string)
    (jsRuntime: IJSRuntime)
    : Task<Function<'TArgument1>> =
    match parameter with
    | Parameter name ->
        task {
            let! reference = jsRuntime.InvokeAsync<IJSInProcessObjectReference>("window.Function", name, code)
            return Function reference
        }
