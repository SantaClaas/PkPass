namespace PkPass.Components

open System.Threading.Tasks
open Bolero
open Bolero.Builders
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop

module Elements =
    let private headerFields (logoText: string) =
        header {
            attr.``class`` "bg-elevation-2 h-12 px-2 pt-2 pb-1 w-full flex justify-between items-center"

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
            attr.``class`` "bg-elevation-2 px-2 pb-2 pt-1"

            div {
                attr.``class`` "bg-elevation-3 rounded p-3"
                div { attr.``class`` "aspect-square w-60 bg-elevation-4 m-auto rounded-xl" }
            }
        }

    //TODO can I solve this with the ElementBuilder like it's a regular element
    /// <summary>
    /// Creates a basic element in which the pass type specific layout gets nested
    /// </summary>
    let private passCard (fieldsSection : Node) (barcodeSection : Node) =
        article {
            attr.``class``
                "origin-top transition-transform scale-[var(--scale)] bg-elevation-1 rounded-lg aspect-[1/1.62] \
                 overflow-hidden flex flex-col justify-between mb-10 shadow-xl text-emphasis-high"
                 
            fieldsSection
            barcodeSection
        }
    
    let a () =
        passCard
            (section {
                // Header fields
                headerFields "Boarding pass"
                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 flex justify-between h-20 px-2 py-1"
                    div { attr.``class`` "aspect-video bg-elevation-3 rounded" }
                    div { attr.``class`` "aspect-video bg-elevation-3 rounded" }
                }

                // Auxiliary fields
                article {
                    attr.``class`` "bg-elevation-2 h-10 px-2 py-1"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }

                article {
                    attr.``class`` "bg-elevation-2 h-10 px-2 py-1"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }
            })
            
            (section {
                article {
                    attr.``class`` "bg-elevation-2 h-8 px-2 py-1"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }

                barcode ()
            })
        
    let boardingPass () =
        passCard
            (section {
                // Header fields
                headerFields "Boarding pass"
                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 flex justify-between h-20 px-2 py-1"
                    div { attr.``class`` "aspect-video bg-elevation-3 rounded" }
                    div { attr.``class`` "aspect-video bg-elevation-3 rounded" }
                }

                // Auxiliary fields
                article {
                    attr.``class`` "bg-elevation-2 h-10 px-2 py-1"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }

                article {
                    attr.``class`` "bg-elevation-2 h-10 px-2 py-1"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }
            })
            
            (section {
                article {
                    attr.``class`` "bg-elevation-2 h-8 px-2 py-1"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }

                barcode ()
            })

    let coupon () =
        passCard
            (section {
                headerFields "Coupon"

                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

                    div {
                        // Strip image
                        attr.``class`` "bg-elevation-3 rounded-none p-2 w-full h-full"
                        div { attr.``class`` "w-full h-full bg-elevation-4 rounded" }
                    }
                }

                // Secondary and auxiliary fields
                article {
                    attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }
            })

            (section { barcode () })
        

    let eventTicketWithBackgroundImage () =
        passCard
            (section {
                headerFields "Event ticket 1"

                article {
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

                    div {
                        attr.``class`` "flex gap-1 bg-elevation-3 h-full w-full p-1"

                        div {
                            attr.``class`` "flex flex-col gap-1 bg-elevation-4 h-full w-full p-1"
                            div { attr.``class`` "bg-elevation-6 h-2/3 w-full p-1" }
                            div { attr.``class`` "bg-elevation-6 h-1/3 w-full p-1" }
                        }

                        div { attr.``class`` "h-full aspect-[3/4] bg-elevation-4 rounded-lg" }
                    }
                }

                article {
                    attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }
            })

            (section { barcode () })

    let eventTicketWithStripImage () =
        passCard
            (section {
                headerFields "Event ticket 2"

                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

                    div {
                        // Strip image
                        attr.``class`` "bg-elevation-3 rounded-none p-2 w-full h-full"
                        div { attr.``class`` "w-full h-full bg-elevation-4 rounded" }
                    }
                }

                article {
                    attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }

                article {
                    attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }
            })

            (section { barcode () })

    let genericPass () =
        passCard
            (section {
                headerFields "Generic pass"

                article {
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

                    div {
                        attr.``class`` "flex gap-1 bg-elevation-3 h-full w-full p-1"
                        div { attr.``class`` "gap-1 bg-elevation-4 h-full w-full p-1 rounded-lg" }
                        div { attr.``class`` "h-full aspect-[3/4] bg-elevation-4 rounded-lg" }
                    }
                }

                article {
                    attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
                    div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
                }
            })
            
            (section { barcode () })
        
