module PkPass.PassKit.Barcode

open System
open System.Text
open System.Text.Json
open PkPass.PassKit.Errors
open QRCoder
open Images

let createQrCode (value: string) =
    use generator = new QRCodeGenerator()
    use data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q)
    use code = new PngByteQRCode(data)
    code.GetGraphic(100) |> Convert.ToBase64String |> Image.Base64

type BarcodeFormat =
    | Qr
    | Pdf417
    | Aztec
    | Code128

type AlternateText = AlternateText of string

type Barcode =
    { alternateText: AlternateText option
      format: BarcodeFormat
      message: string
      messageEncoding: Encoding }

type private BarcodeDeserializationState =
    { alternateText: string option
      format: BarcodeFormat option
      message: string option
      messageEncoding: Encoding option }

    static member Default =
        { alternateText = None
          format = None
          message = None
          messageEncoding = None }

let private tryFinishBarcodeDeserialization (state: BarcodeDeserializationState) =
    match state with
    | { format = None } -> nameof state.format |> RequiredPropertyMissing |> Error
    | { message = None } -> nameof state.message |> RequiredPropertyMissing |> Error
    | { messageEncoding = None } -> nameof state.messageEncoding |> RequiredPropertyMissing |> Error
    | { alternateText = alternateText
        format = Some format
        message = Some message
        messageEncoding = Some messageEncoding } ->
        { Barcode.alternateText = alternateText |> Option.map AlternateText
          format = format
          message = message
          messageEncoding = messageEncoding }
        |> Result.Ok

let rec private deserializeBarcodeInternal
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: BarcodeDeserializationState)
    =
    if not <| reader.Read() then
        tryFinishBarcodeDeserialization state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> tryFinishBarcodeDeserialization state
        | JsonTokenType.PropertyName -> deserializeBarcodeInternal &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            match lastPropertyName with
            | Some "altText" ->
                deserializeBarcodeInternal &reader None { state with alternateText = reader.GetString() |> Some }
            | Some "format" ->

                match reader.GetString() with
                | "PKBarcodeFormatQR" -> deserializeBarcodeInternal &reader None { state with format = Some Qr }
                | "PKBarcodeFormatPDF417" -> deserializeBarcodeInternal &reader None { state with format = Some Pdf417 }
                | "PKBarcodeFormatAztec" -> deserializeBarcodeInternal &reader None { state with format = Some Aztec }
                | otherFormat -> OutOfBoundValue(JsonTokenType.String, otherFormat, "barcodeFormat") |> Error
            | Some "message" ->
                deserializeBarcodeInternal &reader None { state with message = reader.GetString() |> Some }
            | Some "messageEncoding" ->
                deserializeBarcodeInternal
                    &reader
                    None
                    { state with messageEncoding = reader.GetString() |> Encoding.GetEncoding |> Some }
            | other -> handleInvalidProperty &reader other |> Error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcodeInternal, lastPropertyName)
            |> Error

let deserializeBarcode (reader: Utf8JsonReader byref) =
    deserializeBarcodeInternal &reader None BarcodeDeserializationState.Default

let rec private deserializeBarcodesInternal (reader: Utf8JsonReader byref) (resultFields: Barcode list) =
    if not <| reader.Read() then
        Result.Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> resultFields |> List.rev |> Result.Ok
        | JsonTokenType.StartObject ->
            match deserializeBarcode &reader with
            | Result.Ok field -> deserializeBarcodesInternal &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken -> UnexpectedToken(otherToken, nameof deserializeBarcode, None) |> Error


let deserializeBarcodes (reader: Utf8JsonReader byref) = deserializeBarcodesInternal &reader []
