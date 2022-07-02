module PkPass.Interop.JsConsole

    open Microsoft.JSInterop

    let log (reference : IJSObjectReference) (jsRuntime: IJSRuntime) =
        jsRuntime.InvokeVoidAsync("console.log", reference)


