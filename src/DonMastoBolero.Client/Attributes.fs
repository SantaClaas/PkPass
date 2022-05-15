module Attributes

open System.Collections.Generic
open Bolero
open Bolero.Html


let additionalAttributes (additionalAttributes: IReadOnlyDictionary<string, obj>) =
    match additionalAttributes with
    | null -> attr.empty ()
    | _ ->
        if Seq.isEmpty additionalAttributes then
            attr.empty ()
        else
            let toAttribute (pair: KeyValuePair<string, obj>) = pair.Key => pair.Value

            additionalAttributes
            |> Seq.map toAttribute
            |> Attr.Attrs
