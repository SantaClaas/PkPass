namespace PkPass.Components

open System.Collections.Generic
open Bolero
open Bolero.Html
open Bolero.Html.attr
open Microsoft.AspNetCore.Components

type ContainedButtonModel = { label: string; icon: Node option }

type FilledButton() =
    inherit ElmishComponent<ContainedButtonModel, unit>()

    [<Parameter(CaptureUnmatchedValues = true)>]
    member val AdditionalAttributes = Unchecked.defaultof<IReadOnlyDictionary<string, obj>> with get, set

    override this.View model _ =
        button {

            let paddingX =
                match model.icon with
                | Some _ -> "pl-4 pr-6"
                | None -> "px-6"

            ``class``
                $"dark:bg-primary-100 dark:hover:bg-primary-200 \
                 dark:disabled:bg-slate-100 dark:active:bg-primary-300 \
                 dark:text-slate-900 dark:disabled:text-slate-500 uppercase \
                 text-label-lg font-label-lg leading-label-lg tracking-label-lg \
                 {paddingX} py-2 h-10 rounded-full \
                 transition-colors hover:shadow focus:outline-none \
                 flex gap-2 items-center"

            Attributes.additionalAttributes this.AdditionalAttributes

            match model.icon with
            | Some node -> node
            | None -> Node.Empty()

            model.label
        }
