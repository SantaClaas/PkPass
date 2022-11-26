namespace PkPass.Events

open System
open Microsoft.AspNetCore.Components
open Bolero.Html.on

type IntersectionEventArguments() =
    inherit EventArgs()

    member val IsIntersecting = false with get, set

module IntersectionEvent =
    [<Literal>]
    let Name = "intersect"

module Html =
    module on =
        let intersect f = event<IntersectionEventArguments> IntersectionEvent.Name f

// For intersection event handling set up to enable deleting passes
// See more: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling?view=aspnetcore-7.0#custom-event-arguments


[<EventHandlerAttribute("on" + IntersectionEvent.Name,
                        typeof<IntersectionEventArguments>,
                        enableStopPropagation = true,
                        enablePreventDefault = true)>]
module EventHandlers =
    do ()
