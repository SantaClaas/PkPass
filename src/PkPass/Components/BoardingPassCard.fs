namespace PkPass.Components

open FSharp.Core
open Bolero.Html
open PkPass.Components.EventTicket

module Elements =

 

    let private headerFieldsRow (logoText: string) =
        header {
            attr.``class`` "bg-elevation-2 h-12 w-full flex justify-between items-center mb-3"

            div {
                attr.``class`` "bg-elevation-3 w-full h-7 align-middle"

                p {
                    attr.``class`` "align-middle"
                    "Logo"
                }
            }

            div {
                attr.``class`` "bg-elevation-3 w-full text-center h-7 align-middle"

                p {
                    attr.``class`` "align-middle"
                    logoText
                }
            }

            div {
                attr.``class`` "bg-elevation-3 w-full text-end h-7 align-middle"

                p {
                    attr.``class`` "align-middle"
                    "Header fields"
                }
            }
        }

    let private barcode () =
        article {

            div {
                attr.``class`` "bg-elevation-3 rounded p-3"
                div { attr.``class`` "aspect-square w-60 bg-elevation-4 m-auto rounded-xl" }
            }
        }

    //TODO can I solve this with the ElementBuilder like it's a regular element
    /// <summary>
    /// Creates a basic element in which the pass type specific layout gets nested
    /// </summary>
    let private passCard = passCardWithBackground None None None None

    let private fields =
        article {
            attr.``class`` "bg-elevation-2 h-12"
            div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
        }

    let boardingPass package =
        passCard
            (section {
                // Header fields
                headerFieldsRow "Boarding pass"
                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 flex justify-between h-20"
                    div { attr.``class`` "aspect-video bg-elevation-3 rounded" }
                    div { attr.``class`` "aspect-video bg-elevation-3 rounded" }
                }

                // Auxiliary fields
                fields

                fields
            })

            (section {
                // Footer
                article {
                    attr.``class`` "bg-elevation-2 h-8"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }

                barcode ()
            })

    let coupon package =
        passCard
            (section {
                headerFieldsRow "Coupon"

                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 rounded h-32"

                    div {
                        // Strip image
                        attr.``class`` "bg-elevation-3 rounded-none p-2 w-full h-full"
                        div { attr.``class`` "w-full h-full bg-elevation-4 rounded" }
                    }
                }

                // Secondary and auxiliary fields
                fields
            })

            (section { barcode () })

    let eventTicketWithStripImage package =
        passCard
            (section {
                headerFieldsRow "Event ticket 2"

                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 rounded h-32"

                    div {
                        // Strip image
                        attr.``class`` "bg-elevation-3 rounded-none p-2 w-full h-full"
                        div { attr.``class`` "w-full h-full bg-elevation-4 rounded" }
                    }
                }

                fields

                fields
            })

            (section { barcode () })

    let genericPass package =
        passCard
            (section {
                headerFieldsRow "Generic pass"

                article {
                    attr.``class`` "bg-elevation-2 rounded h-32"

                    div {
                        attr.``class`` "flex gap-1 bg-elevation-3 h-full w-full"
                        div { attr.``class`` "gap-1 bg-elevation-4 h-full w-full rounded-lg" }
                        div { attr.``class`` "h-full aspect-[3/4] bg-elevation-4 rounded-lg" }
                    }
                }

                fields
            })

            (section { barcode () })

    // Store card and coupon basically have the same layout
    let storeCard = coupon
