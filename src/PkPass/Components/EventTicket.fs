﻿module PkPass.Components.EventTicket


open System.Text
open FSharp.Core
open Bolero
open Bolero.Html
open PkPass.PassKit.Barcode
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Field
open PkPass.PassKit.Images
open PkPass.PassKit.Package
open ZXing.Common
open ZXing.Rendering

let createPngDataUrl base64String = $"data:image/png;base64,{base64String}"

let passCardWithBackground
    (backgroundImage: BackgroundImage option)
    (foregroundColor: CssColor option)
    (backgroundColor: CssColor option)
    (labelColor: CssColor option)
    (fieldsSection: Node)
    (barcodeSection: Node)
    =
    let toVariableString variableName (CssColor color) = $"{variableName}: {color};"

    let foregroundVariable =
        foregroundColor |> Option.map (toVariableString "--pass-foreground")

    let backgroundVariable =
        backgroundColor |> Option.map (toVariableString "--pass-background")

    let labelColorVariable =
        labelColor |> Option.map (toVariableString "--pass-label-color")

    let append (string: string) (state: StringBuilder) = state.Append string

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
             text-emphasis-high bg-[var(--pass-background)] text-[var(--pass-foreground)] p-3"

        if style.Length <> 0 then
            style |> string |> attr.style
        else
            attr.empty ()

        fieldsSection
        barcodeSection
    }

let private toText value =
    cond value (function
        | FieldValue.LocalizableString (LocalizableString value) -> value |> text
        | FieldValue.Date date -> date.ToLocalTime() |> string |> text
        | FieldValue.Number number -> number |> string |> text)

let fieldLabel (label: LocalizableString option) =
    cond label (function
        | Some (LocalizableString localizableString) ->
            p {
                attr.``class`` "text-[var(--pass-label-color)] text-xs font-bold leading-none"
                localizableString
            }
        | None -> empty ())

let fieldValue (value: FieldValue) =
    p {
        attr.``class`` "leading-tight"

        value |> toText
    }

let private headerField ({ value = value; label = label }: Field) =
    div {
        attr.``class`` "w-full text-end align-middle"
        fieldLabel label
        fieldValue value
    }

let fieldsRow' fields =
    article {
        attr.``class`` "grid grid-cols-3 gap-3 mb-3 text-lg"

        cond fields (function
            | Some fields ->
                forEach fields (fun ({ value = value; label = label } : Field) ->
                    span {
                        attr.``class`` "inline-block"
                        fieldLabel label
                        fieldValue value
                    })
            | None -> empty ())
    }

let headerFieldsRow' (Logo (Base64 base64)) (logoText: LocalizableString option) (headerFields: Field list option) =
    header {
        attr.``class`` "h-14 w-full flex justify-between items-center"

        img {
            attr.``class`` "h-full aspect-auto"

            attr.alt
                "A thumbnail for the event ticket probably showing the poster or something related to the event. A proper alternate text is not provided."

            base64 |> createPngDataUrl |> attr.src
        }

        cond logoText (function
            | Some (LocalizableString localizableString) ->
                div {
                    attr.``class`` "w-full"

                    p {
                        attr.``class`` "leading-none"
                        localizableString
                    }
                }
            | None -> empty ())

        cond headerFields (function
            | Some fields -> forEach fields headerField
            | None -> empty ())
    }

// let barcode' (Barcode(alternateText, barcodeFormat, message, messageEncoding)) =
let barcode' (barcode) =
    article {

        div {
            attr.``class`` "rounded"

            cond barcode.format (function
                | Qr ->
                    let (Base64 base64) = createQrCode barcode.message

                    img {
                        attr.``class`` "aspect-square w-60 m-auto rounded-xl"

                        match barcode.alternateText with
                        | Some (AlternateText text) -> text
                        | None -> sprintf """A QR code for the pass with the message or value '%s'.""" barcode.message
                        |> attr.alt

                        base64 |> createPngDataUrl |> attr.src
                    }
                | Aztec ->
                    let writer = ZXing.Aztec.AztecWriter()
                    // let data = message |> messageEncoding.GetBytes
                    // let matrix = writer.encode (data, ZXing.BarcodeFormat.AZTEC, 500, 500, Map.empty)
                    let matrix: BitMatrix = writer.encode (barcode.message, ZXing.BarcodeFormat.AZTEC, 500, 500)
                    let renderer = SvgRenderer()
                    let render = renderer.Render(matrix, ZXing.BarcodeFormat.AZTEC, System.String.Empty)
                    rawHtml render.Content
                | _ ->
                    p {
                        attr.``class`` "text-white"
                        "This barcode format is not yet supported"
                    })
        }
    }

let renderThumbnail (Thumbnail (Base64 base64)) =
    img {
        attr.``class`` "w-1/3 h-full rounded-lg"

        base64 |> createPngDataUrl |> attr.src
    }

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
    //TODO different event pass options in one function
    thumbnail
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
                                    forEach fields (fun { value = value; label = label } ->
                                        concat {
                                            fieldLabel label
                                            fieldValue value
                                        })
                                | None -> empty ())
                        }

                        div {
                            attr.``class`` "h-1/3 w-full"

                            cond secondaryFields (function
                                | Some fields ->
                                    forEach fields (fun { value = value; label = label } ->
                                        concat {
                                            fieldLabel label
                                            fieldValue value
                                        })
                                | None -> empty ())
                        }
                    }

                    renderThumbnail thumbnail
                }
            }

            fieldsRow' auxiliaryFields
        })

        (cond barcode (function
            | Some barcode -> section { barcode' barcode }
            | None -> empty ()))
