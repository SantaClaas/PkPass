namespace PkPass.Components

open Bolero
open Bolero.Html.attr
open PkPass.Events

type VersionInformation() =
    inherit Component()

    let version =
        typeof<VersionInformation>
            .Assembly.GetName()
            .Version.ToString(3)

    override this.Render() =
        Html.span {
            ``class``
                "fixed bottom-0 left-0 mb-4 ml-4 \
                       text-emphasis-low text-xs"

            version
        }