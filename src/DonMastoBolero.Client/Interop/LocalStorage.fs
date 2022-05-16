module PkPass.Client.LocalStorage

open Microsoft.JSInterop

let setItem (key: string) (value: string) (jsRuntime: IJSInProcessRuntime) =
    jsRuntime.InvokeVoid("localStorage.setItem", key, value)

let getItem (key: string) (jsRuntime: IJSInProcessRuntime) =
    // LocalStorage JS API returns null when item cannot be found with key
    jsRuntime.Invoke<string>("localStorage.getItem", key) |> Option.ofObj
