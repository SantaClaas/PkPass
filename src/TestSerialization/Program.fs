open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System
open System.IO
open Microsoft.FSharp.Core


// Css color is a string in the format of "rgb(255,255,255)". We add parsing when required. For now this is just to note that this is a string in a specific format
type CssColor = CssColor of string

type BarcodeFormat =
    | Qr
    | Pdf417
    | Aztec
//TODO barcodes in the barcodes array are allowed to have type PKBarcodeFormatCode128 but not the single barcode

type Barcode = Barcode of altText: string option * format: BarcodeFormat * message: string * messageEncoding: Encoding

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



let readRequired (reader: Utf8JsonReader) =
    if not <| reader.Read() then
        failwith "Expected reader to continue reading"

    ()

let failWithIsRequired =
    failwithf "%s is required"

let assertNotNull<'T> value name =
    match value with
    | null -> failWithIsRequired name
    | _ -> ()

let readBarcode (reader: byref<Utf8JsonReader>) =

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

    Barcode(altText = alternateText, format = format, message = message, messageEncoding = messageEncoding)

let readBarcodes (reader: byref<Utf8JsonReader>) =
    let mutable arrayValues = []

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndArray do
        arrayValues <- (readBarcode &reader) :: arrayValues

    arrayValues

let readField (reader: byref<Utf8JsonReader>) =
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

let readFields (reader: byref<Utf8JsonReader>) =
    let mutable arrayValues = []

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndArray do
        arrayValues <- (readField &reader) :: arrayValues

    arrayValues

let readPassStructure (reader: byref<Utf8JsonReader>) =

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
            | Some "headerFields" -> headerFields <- readFields &reader |> List.toArray |> Some
            | Some "primaryFields" -> primaryFields <- readFields &reader |> List.toArray |> Some
            | Some "secondaryFields" -> secondaryFields <- readFields &reader |> List.toArray |> Some
            | Some "backFields" -> backFields <- readFields &reader |> List.toArray |> Some
            | Some "auxiliaryFields" -> auxiliaryFields <- readFields &reader |> List.toArray |> Some
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None -> failwith $"Got {reader.TokenType} token type but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | otherToken -> failwith $"Unexpected other token '{otherToken}' in pass structure object"

    PassStructure(auxiliaryFields, backFields, headerFields, primaryFields, secondaryFields)

//TODO don't use converter and use reader instead to avoid throwing exceptions and use error union instead to display error and log error in UI
type PackageDefinitionJsonConverter() =
    inherit JsonConverter<Pass>()

    override this.Read(reader, typeToConvert, options) =

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
                | Some "barcode" -> barcode <- readBarcode &reader |> Some
                | Some "eventTicket" ->
                    let structure = readPassStructure &reader
                    constructPass <- fun definition -> EventTicket(definition, structure)
                | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
                | None -> failwith $"Got {reader.TokenType} token type but no property name proceeded this value"

                // Clean last property after we extracted the properties value
                lastProperty <- None
            // Next read advance will leave the object
            | JsonTokenType.StartArray ->
                match lastProperty with
                | Some "barcodes" -> barcodes <- (readBarcodes &reader) |> List.toArray |> Some
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

        constructPass (definition)

    override this.Write(writer, value, options) =
        failwith "Writing is not yet implemented"


let options = JsonSerializerOptions()

options.Converters.Add
<| PackageDefinitionJsonConverter()

let stream =
    File.OpenRead "C:\Users\claas\Downloads\.pkpass\pass.json"

let o =
    JsonSerializer.Deserialize<Pass>(stream, options)
    
Console.WriteLine o
