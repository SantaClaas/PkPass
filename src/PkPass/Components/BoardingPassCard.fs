namespace PkPass.Components

open System.Threading.Tasks
open Bolero
open Bolero.Builders
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.FSharp.Core
open Microsoft.JSInterop
open PkPass.PassKit
open PkPass.PassKit.Barcode
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Field
open PkPass.PassKit.Images
open PkPass.PassKit.Package

module Elements =

    let private createPngDataUrl base64String = $"data:image/png;base64,{base64String}"
   
    let toText = function
        | FieldValue.LocalizableString (LocalizableString value) ->
            value |> text
        | FieldValue.Date date ->
            date.ToLocalTime() |> string |> text
        | FieldValue.Number number ->
            number |> string |> text
    let private headerFieldsRow' (Logo (Base64 base64)) (logoText: LocalizableString option) (headerFields: Field list option) =
        let headerField
            { value=value }
            =
            div {
                attr.``class`` "bg-elevation-3 w-full text-end h-7 align-middle"

                p {
                    attr.``class`` "align-middle"
                    
                    cond value toText
                }
            }
            
        header {
            attr.``class`` "bg-elevation-2 h-12 px-2 pt-2 pb-1 w-full flex justify-between items-center"

            div {
                attr.``class`` "bg-elevation-3 w-full h-7 align-middle"

                img {
                    attr.``class`` "h-full aspect-auto"
                    base64 |> createPngDataUrl |> attr.src
                }
            }

            cond logoText (function
                | Some (LocalizableString localizableString) ->
                    div {
                        attr.``class`` "bg-elevation-3 w-full text-center h-7 align-middle"

                        p {
                            attr.``class`` "align-middle"
                            localizableString
                        }
                    }
                | None -> empty ())
            
            cond headerFields (function
                | Some fields ->
                    forEach fields headerField
                | None ->
                    empty ())
        }

    let private headerFieldsRow (logoText: string) =
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
    let private barcode' (Barcode(alternateText, barcodeFormat, message, _)) =
        article {
            attr.``class`` "bg-elevation-2 px-2 pb-2 pt-1"

            div {
                attr.``class`` "bg-elevation-3 rounded p-3"
                
                cond barcodeFormat (function
                    | Qr ->
                        let (Base64 base64) = Barcode.createQrCode message
                        img {
                            attr.``class`` "aspect-square w-60 bg-elevation-4 m-auto rounded-xl"
                            attr.alt alternateText
                            base64 |> createPngDataUrl |> attr.src
                        })
            }
        }

    let private passCardWithBackground
        (backgroundImage: BackgroundImage option)
        (fieldsSection: Node)
        (barcodeSection: Node)
        =
        article {
            attr.``class``
                "origin-top transition-transform scale-[var(--scale)] bg-elevation-1 rounded-lg aspect-[1/1.62] \
                 overflow-hidden flex flex-col justify-between mb-10 shadow-xl text-emphasis-high"

            match backgroundImage with
            | Some (BackgroundImage (Base64 base64String)) ->
                $"""background-image: url("{createPngDataUrl base64String}");""" |> attr.style
            | None -> attr.empty ()

            fieldsSection
            barcodeSection
        }
    //TODO can I solve this with the ElementBuilder like it's a regular element
    /// <summary>
    /// Creates a basic element in which the pass type specific layout gets nested
    /// </summary>
    let private passCard = passCardWithBackground None

    let private fields =
        article {
            attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
            div { attr.``class`` "bg-elevation-3 w-full h-full rounded" }
        }
    let private fieldsRow' fields =
        article {
            attr.``class`` "bg-elevation-2 h-12 px-2 py-2"
            div {
                attr.``class`` "bg-elevation-3 w-full h-full rounded"
                
                cond fields (function
                        | Some fields ->
                            forEach fields (fun {value=value} ->
                                p {
                                    value |> toText
                                })
                        | None ->
                            empty ())
            }
        }

    let boardingPass package =
        passCard
            (section {
                // Header fields
                headerFieldsRow "Boarding pass"
                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 flex justify-between h-20 px-2 py-1"
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
                    attr.``class`` "bg-elevation-2 h-8 px-2 py-1"
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
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

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

    let eventTicketWithBackgroundImage
        ({ pass = (EventTicket ({ logoText = logoText
                                  barcode = barcode },
                                { headerFields = headers
                                  primaryFields = primaryFields
                                  secondaryFields = secondaryFields
                                  auxiliaryFields = auxiliaryFields }))
           images = (EventTicketImages (CommonImages (logo, _), _)) }: EventTicketPassPackage)
        backgroundImage
        (Thumbnail (Base64 base64))
        =
        passCardWithBackground
            (Some backgroundImage)
            (section {
                headerFieldsRow' logo logoText headers

                article {
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

                    div {
                        attr.``class`` "flex gap-1 bg-elevation-3 h-full w-full p-1"

                        div {
                            attr.``class`` "flex flex-col gap-1 bg-elevation-4 h-full w-full p-1"
                            div {
                                attr.``class`` "bg-elevation-6 h-2/3 w-full p-1"
                                cond primaryFields (function
                                    | Some fields ->
                                        forEach fields (fun {value=value} ->
                                            p {
                                                value |> toText
                                            })
                                    | None ->
                                        empty())
                            }
                            div {
                                attr.``class`` "bg-elevation-6 h-1/3 w-full p-1"
                                cond secondaryFields (function
                                    | Some fields ->
                                        forEach fields (fun {value=value} ->
                                            p {
                                                value |> toText
                                            })
                                    | None ->
                                        empty())
                            }
                        }

                        img {
                            attr.``class`` "h-full bg-elevation-4 rounded-lg"

                            base64 |> createPngDataUrl |> attr.src
                        }
                    }
                }

                fieldsRow' auxiliaryFields
            })

            (cond barcode (function
                | Some barcode ->
                    section { barcode' barcode}
                | None ->
                    empty ()))

    let eventTicketWithStripImage package =
        passCard
            (section {
                headerFieldsRow "Event ticket 2"

                // Primary fields
                article {
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

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
                    attr.``class`` "bg-elevation-2 rounded h-32 py-1"

                    div {
                        attr.``class`` "flex gap-1 bg-elevation-3 h-full w-full p-1"
                        div { attr.``class`` "gap-1 bg-elevation-4 h-full w-full p-1 rounded-lg" }
                        div { attr.``class`` "h-full aspect-[3/4] bg-elevation-4 rounded-lg" }
                    }
                }

                fields
            })

            (section { barcode () })

    // Store card and coupon basically have the same layout
    let storeCard = coupon
