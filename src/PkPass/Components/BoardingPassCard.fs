namespace PkPass.Components

open System.Text
open FSharp.Core
open Bolero
open Bolero.Html
open PkPass.PassKit.Barcode
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Field
open PkPass.PassKit.Images
open PkPass.PassKit.Package

module Elements =

    let private createPngDataUrl base64String = $"data:image/png;base64,{base64String}"
   
    
    let toText value =
        cond value (function
        | FieldValue.LocalizableString (LocalizableString value) ->
            value |> text
        | FieldValue.Date date ->
            date.ToLocalTime() |> string |> text
        | FieldValue.Number number ->
            number |> string |> text)
            
    let private fieldLabel (label : LocalizableString option) =
        cond label (function  
            | Some (LocalizableString localizableString) ->
                p {
                    attr.``class`` "text-[var(--pass-label-color)] text-sm font-bold tracking-wider leading-none"
                    localizableString    
                }
            | None -> empty())
        
    let private fieldValue (value : FieldValue) =
        p {
            attr.``class`` "text-lg"
          
            value |> toText
        }
        
    let private headerField
        ({ value=value
           label=label } : Field)
        =
        div {
            attr.``class`` "w-full text-end align-middle"
            fieldLabel label
            fieldValue value
        }
    let private headerFieldsRow' (Logo (Base64 base64)) (logoText: LocalizableString option) (headerFields: Field list option) =
       
            
        header {
            attr.``class`` "h-14 w-full flex justify-between items-center"

            img {
                attr.``class`` "h-full aspect-auto"
                attr.alt "A thumbnail for the event ticket probably showing the poster or something related to the event. A proper alternate text is not provided."
                base64 |> createPngDataUrl |> attr.src
            }
            

            cond logoText (function
                | Some (LocalizableString localizableString) ->
                    div {
                        attr.``class`` "w-full text-center h-7 align-middle"

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
    let private barcode' (Barcode(alternateText, barcodeFormat, message, _)) =
        article {

            div {
                attr.``class`` "rounded"
                
                cond barcodeFormat (function
                    | Qr ->
                        let (Base64 base64) = createQrCode message
                        img {
                            attr.``class`` "aspect-square w-60 m-auto rounded-xl"
                            alternateText
                            |> Option.defaultValue (sprintf """A QR code for the pass with the message or value '%s'.""" message)
                            |> attr.alt
                            
                            base64 |> createPngDataUrl |> attr.src
                        })
            }
        }

    let private passCardWithBackground
        (backgroundImage: BackgroundImage option)
        (foregroundColor: CssColor option)
        (backgroundColor: CssColor option)
        (labelColor: CssColor option)
        (fieldsSection: Node)
        (barcodeSection: Node)
        =
        let toVariableString variableName (CssColor color) = $"{variableName}: {color};"        
        
        let foregroundVariable = foregroundColor
                                 |> Option.map (toVariableString "--pass-foreground")
        let backgroundVariable = backgroundColor
                                 |> Option.map (toVariableString "--pass-background")
        let labelColorVariable = labelColor
                                 |> Option.map (toVariableString "--pass-label-color")
    
        let append (string : string) (state: StringBuilder) =
            state.Append string
            
        let appendBackgroundImage (BackgroundImage (Base64 base64String)) state =
            append
                (base64String 
                |> createPngDataUrl
                |> Printf.sprintf """background-image: url("%s");""")
                state
            
        let style =
            StringBuilder()
            |> Option.foldBack append foregroundVariable 
            |> Option.foldBack append backgroundVariable
            |> Option.foldBack append labelColorVariable
            |> Option.foldBack appendBackgroundImage backgroundImage
                    
        article {
            attr.``class``
                "origin-top transition-transform scale-[var(--scale)] aspect-[1/1.62] \
                 overflow-hidden shadow-xl rounded-lg \
                 flex flex-col gap-3 justify-between \
                 text-emphasis-high text-[var(--pass-foreground)] p-3"
            
            if style.Length <> 0 then
                style
                |> string
                |> attr.style
            else attr.empty ()

            fieldsSection
            barcodeSection
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
    let private fieldsRow' fields =
        article {
            attr.``class`` "h-12"
            div {
                attr.``class`` "w-full h-full rounded"
                
                cond fields (function
                    | Some fields ->
                        forEach fields (fun {value=value;label=label} ->
                            concat {
                                fieldLabel label
                                fieldValue value
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

    let eventTicketWithBackgroundImage
        ({ pass = (EventTicket ({ logoText = logoText
                                  barcode = barcode
                                  backgroundColor = backgroundColor
                                  foregroundColor = foregroundColor
                                  labelColor = labelColor },
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
            foregroundColor
            backgroundColor
            labelColor
            (section {
                headerFieldsRow' logo logoText headers

                article {
                    attr.``class`` "rounded mb-3"

                    div {
                        attr.``class`` "flex gap-3 h-full w-full"

                        div {
                            attr.``class`` "flex flex-col gap-3 h-full w-full"
                            div {
                                attr.``class`` "h-2/3 w-full"
                                cond primaryFields (function
                                    | Some fields ->
                                        forEach fields (fun {value=value;label=label} ->
                                            concat {
                                                fieldLabel label
                                                fieldValue value
                                            })
                                    | None ->
                                        empty())
                            }
                            div {
                                attr.``class`` "h-1/3 w-full"
                                cond secondaryFields (function
                                    | Some fields ->
                                        forEach fields (fun {value=value;label=label} ->
                                            concat {
                                                fieldLabel label
                                                fieldValue value
                                                })
                                    | None ->
                                        empty())
                            }
                        }

                        img {
                            attr.``class`` "w-1/3 aspect-auto rounded-lg"

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
