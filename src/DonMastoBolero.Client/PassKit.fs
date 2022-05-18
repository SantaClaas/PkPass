module PkPass.PassKit

open System
open System.Text
open System.Text.Json
type CssColor = CssColor of string

//TODO barcodes in the barcodes array are allowed to have type PKBarcodeFormatCode128 but not the single barcode
type BarcodeFormat =
    | Qr
    | Pdf417
    | Aztec
    
type Barcode = Barcode of alternateText: string option * format: BarcodeFormat * message: string * messageEncoding: Encoding

type PassDefinition =
    | PassDefinition of
        description: string *
        formatVersion: int *
        organizationName: string *
        passTypeIdentifier: string *
        serialNumber: string *
        teamIdentifier: string *
        foregroundColor: CssColor option *
        labelColor: CssColor option *
        // Deprecated since iOS 9 use barcodes instead
        barcode: Barcode option *
        barcodes: Barcode array option

type Field = Field of key: string * label: string * value: string

type PassStructure =
    | PassStructure of
        auxiliaryFields: Field array option *
        backFields: Field array option *
        headerFields: Field array option *
        primaryFields: Field array option *
        secondaryFields: Field array option

type TransitType =
    | Air
    | Boat
    | Bus
    | Generic
    | Train
// Boarding pass structure extends standard pass structure with a mandatory transit type
type BoardingPassStructure = BoardingPassStructure of PassStructure * TransitType
type Pass =
    | BoardingPass of PassDefinition * BoardingPassStructure
    | Coupon
    | EventTicket of PassDefinition * PassStructure
    | Generic
    | StoredCard

let deserializeBarcode (reader: Utf8JsonReader inref) =

    let mutable format = Unchecked.defaultof<_>
    let mutable alternateText = None
    let mutable message = Unchecked.defaultof<_>

    let mutable messageEncoding =
        Unchecked.defaultof<_>

    let mutable lastProperty = None

    // Read until end of object
    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndObject do
        match reader.TokenType with
        // Enter object
//        | JsonTokenType.StartObject -> ()
        | JsonTokenType.PropertyName -> lastProperty <- reader.GetString() |> Option.ofObj
        | JsonTokenType.String ->
            match lastProperty with
            | Some "altText" -> alternateText <- reader.GetString() |> Option.ofObj
            | Some "format" ->
                format <-
                    match reader.GetString() with
                    | "PKBarcodeFormatQR" -> BarcodeFormat.Qr
                    | "PKBarcodeFormatPDF417" -> BarcodeFormat.Pdf417
                    | "PKBarcodeFormatAztec" -> BarcodeFormat.Aztec
                    | otherFormat ->
                        failwith
                            $"Barcode format '{otherFormat}' is not recognized or supported but a format has to be provided"

            | Some "message" -> message <- reader.GetString()
            | Some "messageEncoding" -> messageEncoding <- reader.GetString() |> Encoding.GetEncoding
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty} in barcode object"
            | None ->
                failwith
                    $"Got string token type with value '{reader.GetString()}' but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        | otherToken -> failwith $"Unexpected other token '{otherToken}' in barcode object"

    Barcode(alternateText = alternateText, format = format, message = message, messageEncoding = messageEncoding)


let deserializeField (reader: Utf8JsonReader inref) =
    let mutable key = Unchecked.defaultof<_>
    let mutable label = Unchecked.defaultof<_>
    let mutable value = Unchecked.defaultof<_>
    let mutable lastProperty = None

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndObject do
        match reader.TokenType with
        | JsonTokenType.StartObject -> ()
        | JsonTokenType.PropertyName -> lastProperty <- reader.GetString() |> Some
        | JsonTokenType.String ->
            match lastProperty with
            // Match to union field names
            | Some "key" -> key <- reader.GetString()
            | Some "label" -> label <- reader.GetString()
            | Some "value" -> value <- reader.GetString()
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty} in field object"
            | None ->
                failwith
                    $"Got string token type with value '{reader.GetString()}' but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        | otherToken -> failwith $"Unexpected other token '{otherToken}' in pass structure object"

    Field(key, label, value)

let deserializeFields (reader: Utf8JsonReader inref) =
    let mutable arrayValues = []

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndArray do
        arrayValues <- (deserializeField &reader) :: arrayValues

    arrayValues

let deserializePassStructure (reader: Utf8JsonReader inref) =

    let mutable headerFields = None
    let mutable primaryFields = None
    let mutable secondaryFields = None
    let mutable auxiliaryFields = None
    let mutable backFields = None

    let mutable lastProperty = None
    // Read until end of object
    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndObject do
        match reader.TokenType with
        // Enter object
        | JsonTokenType.PropertyName -> lastProperty <- reader.GetString() |> Option.ofObj
        | JsonTokenType.StartArray ->
            match lastProperty with
            | Some "headerFields" -> headerFields <- deserializeFields &reader |> List.toArray |> Some
            | Some "primaryFields" -> primaryFields <- deserializeFields &reader |> List.toArray |> Some
            | Some "secondaryFields" -> secondaryFields <- deserializeFields &reader |> List.toArray |> Some
            | Some "backFields" -> backFields <- deserializeFields &reader |> List.toArray |> Some
            | Some "auxiliaryFields" -> auxiliaryFields <- deserializeFields &reader |> List.toArray |> Some
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None -> failwith $"Got {reader.TokenType} token type but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | otherToken -> failwith $"Unexpected other token '{otherToken}' in pass structure object"

    PassStructure(auxiliaryFields, backFields, headerFields, primaryFields, secondaryFields)


let deserializeBarcodes (reader: Utf8JsonReader inref) =
    let mutable arrayValues = []

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndArray do
        arrayValues <- (deserializeBarcode &reader) :: arrayValues

    arrayValues

let deserializePass (data: byte ReadOnlySpan) =
    let reader = Utf8JsonReader(data)
    let mutable description =
            Unchecked.defaultof<_>

    let mutable formatVersion = 0

    let mutable serialNumber =
        Unchecked.defaultof<_>

    let mutable organizationName =
        Unchecked.defaultof<_>

    let mutable passTypeIdentifier =
        Unchecked.defaultof<string>

    let mutable teamIdentifier =
        Unchecked.defaultof<_>

    let mutable foregroundColor = None
    let mutable labelColor = None
    let mutable barcode = None
    let mutable barcodes = None

    let mutable constructPass: PassDefinition -> Pass =
        Unchecked.defaultof<_>

    let mutable relevantDate = None
    let mutable lastProperty = None

    while reader.Read() && reader.TokenType <> JsonTokenType.EndObject do
        match reader.TokenType with
        | JsonTokenType.PropertyName -> lastProperty <- reader.GetString() |> Option.ofObj
        | JsonTokenType.String ->
            match lastProperty with
            | Some "description" -> description <- reader.GetString()
            | Some "organizationName" -> organizationName <- reader.GetString()
            | Some "passTypeIdentifier" -> passTypeIdentifier <- reader.GetString()
            | Some "serialNumber" -> serialNumber <- reader.GetString()
            | Some "teamIdentifier" -> teamIdentifier <- reader.GetString()
            | Some "relevantDate" -> relevantDate <- reader.GetDateTimeOffset() |> Some
            | Some "labelColor" ->
                labelColor <-
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map CssColor
            | Some "foregroundColor" ->
                foregroundColor <-
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map CssColor
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None ->
                failwith
                    $"Got {reader.TokenType} token type with value '{reader.GetString()}' but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        | JsonTokenType.Number ->
            match lastProperty with
            | Some "formatVersion" -> formatVersion <- reader.GetInt32()
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None ->
                failwith
                    $"Got {reader.TokenType} token type with value '{reader.GetInt64()}' but no property name proceeded this value"

            // Clean last property after we extracted the properties value
            lastProperty <- None

        | JsonTokenType.StartObject ->
            match lastProperty with
            | Some "barcode" -> barcode <- deserializeBarcode &reader |> Some
            | Some "eventTicket" ->
                let structure = deserializePassStructure &reader
                constructPass <- fun definition -> EventTicket(definition, structure)
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None -> failwith $"Got {reader.TokenType} token type but no property name proceeded this value"

            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | JsonTokenType.StartArray ->
            match lastProperty with
            | Some "barcodes" -> barcodes <- (deserializeBarcodes &reader) |> List.toArray |> Some
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None -> failwith $"Got {reader.TokenType} token type but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | _ -> Console.WriteLine $"Not supported token type '{reader.TokenType}'"

    let definition =
        PassDefinition(
            description,
            formatVersion,
            organizationName,
            passTypeIdentifier,
            serialNumber,
            teamIdentifier,
            foregroundColor,
            labelColor,
            barcode,
            barcodes
        )

    constructPass definition
    