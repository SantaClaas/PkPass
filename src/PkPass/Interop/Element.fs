namespace PkPass.Interop

open Microsoft.JSInterop

type Element = Element of IJSObjectReference

module Element =
    let setAttribute (name: string) (value: string) (Element reference) =
        reference.InvokeVoidAsync("setAttribute", name, value)
type HtmlElement = HtmlElement of IJSObjectReference

module HtmlElement =
    let createFromElement (Element reference) = HtmlElement reference
    let click (HtmlElement reference) =
        reference.InvokeVoidAsync "click"