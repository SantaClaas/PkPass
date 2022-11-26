namespace PkPass.Components

open System
open Bolero
open Bolero.Html
open Bolero.Html.attr
open System.Collections.Generic
open Microsoft.AspNetCore.Components
open PkPass.Events
open PkPass.Events.Html

type AssistiveText =
    | None
    | HelperText of string
    // If there is an error it should have information what the error is
    | ErrorText of string

type OutlineInputModel =
    { value: string option
      // "Every text field should have a label" -> material.io
      label: string
      id: string
      placeholder: string option
      assistiveText: AssistiveText }

type OutlinedInput() =
    inherit ElmishComponent<OutlineInputModel, string option>()

    [<Parameter(CaptureUnmatchedValues = true)>]
    member val AdditionalAttributes = Unchecked.defaultof<IReadOnlyDictionary<string, obj>> with get, set

    override this.View model dispatch =
        let paddingBottom =
            match model.assistiveText with
            | None -> "mb-6"
            | HelperText _
            | ErrorText _ -> String.Empty

        div {
            div {
                ``class`` $"relative rounded-md focus-within:border-primary-100 group {paddingBottom}"

                input {
                    ``class``
                        "transition-all touch-manipulation py-3 px-4 w-full \
                         rounded-md \
                         dark:bg-elevation-2 hover:dark:bg-elevation-1 focus:dark:bg-elevation-0 autofill:bg-black \
                         leading-none dark:text-emphasis-high \
                         placeholder:dark:text-emphasis-medium \
                         outline-none \
                         placeholder:opacity-0 \
                         placeholder:focus:opacity-100 \
                         placeholder:transition-all \
                         placeholder:text-lg"

                    model.value
                    |> Option.map value
                    |> Option.defaultWith attr.empty

                    id model.id

                    model.placeholder
                    |> Option.defaultValue " "
                    |> placeholder

                    Attributes.additionalAttributes this.AdditionalAttributes

                    on.change (fun arguments ->
                        let value =
                            arguments.Value
                            |> Option.ofObj
                            |> Option.map string
                        // Need to update value because it changes rendering even if the parent component does not care about value change
                        this.Model <- { model with value = value }
                        dispatch value)
                }

                div {
                    ``class``
                        "flex absolute top-0 left-0 origin-top-left w-full h-full \
                         touch-manipulation pointer-events-none"

                    let borderColor =
                        match model.assistiveText with
                        | HelperText _
                        | None ->
                            "group-focus-within:border-primary-100 group-hover:group-focus-within:border-primary-100 border-emphasis-low group-hover:border-emphasis-medium"
                        | ErrorText _ -> "border-red-300"

                    // Filler start
                    div { ``class`` $"w-4 {borderColor} border-y border-l rounded-l-md" }

                    // Filler middle
                    div {
                        let isEmpty =
                            model.value
                            |> Option.map String.IsNullOrEmpty
                            |> Option.defaultValue true

                        let (translate, fontSize, borderTop, borderHoverColor) =
                            if isEmpty then
                                "translate-y-1/3", String.Empty, String.Empty, String.Empty
                            else
                                "-translate-y-3.5",
                                "text-xs",
                                "border-t-transparent",
                                "group-hover:border-t-transparent "

                        ``class``
                            $"border-y {borderColor} \
                            {borderHoverColor}
                            group-focus-within:border-t-transparent group-hover:group-focus-within:border-t-transparent group-focus-within:group-hover:border-t-transparent \
                            px-0.5 {borderTop}"

                        div {
                            ``class`` $"transition-all group-focus-within:-translate-y-3.5 {translate}"

                            let textColor =
                                match model.assistiveText with
                                | ErrorText _ -> "text-red-300"
                                | HelperText _
                                | None when isEmpty -> "text-emphasis-medium group-focus-within:text-primary-100"
                                | HelperText _
                                | None -> "text-emphasis-low group-focus-within:text-primary-100"

                            Html.label {
                                ``class`` $"{textColor} {fontSize} group-focus-within:text-xs whitespace-nowrap"
                                ``for`` model.id

                                model.label
                            }
                        }
                    }

                    // Filler end
                    div { ``class`` $"w-full border-y border-r {borderColor} rounded-r-md" }

                    let values =
                        match model.assistiveText with
                        | HelperText text -> Some("text-emphasis-medium", text, Html.span)
                        | ErrorText text -> Some("text-red-300", text, Html.span)
                        | None -> Option.None

                    match values with
                    | Option.None -> Html.empty ()
                    | Some (textColor, text, element) ->
                        element {
                            ``class`` $"pl-4 text-xs {textColor}"
                            text
                        }
                }
            }
        }


