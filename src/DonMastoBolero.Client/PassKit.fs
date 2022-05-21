module PkPass.PassKit

open System
open System.Text
open System.Text.Json
open Resets

type CssColor = CssColor of string

//TODO barcodes in the barcodes array are allowed to have type PKBarcodeFormatCode128 but not the single barcode
type BarcodeFormat =
    | Qr
    | Pdf417
    | Aztec

type Barcode =
    | Barcode of alternateText: string option * format: BarcodeFormat * message: string * messageEncoding: Encoding

type PassDefinition =
    { description: string
      formatVersion: int
      organizationName: string
      passTypeIdentifier: string
      serialNumber: string
      teamIdentifier: string
      foregroundColor: CssColor option
      labelColor: CssColor option
      // Deprecated since iOS 9 use barcodes instead
      barcode: Barcode option
      barcodes: Barcode list option
      relevanceDate: DateTimeOffset option }

type Field =
    { key: string
      label: string
      value: string }

type private UnfinishedField =
    { key: string option
      label: string option
      value: string option }
    static member Default = {
        key = None
        label = None
        value = None
    }

type DeserializationError =
    // A property that is required by definition is missing in the JSON
    | RequiredPropertyMissing of propertyName: string
    | UnexpectedProperty of propertyName: string * tokenType: JsonTokenType * value: object
    // Don't like the boxing here but the value should only be used for logging or displaying
    | UnexpectedValue of tokenType: JsonTokenType * value: object
    | OutOfBoundValue of tokenType: JsonTokenType * value: object * whereHint: string
    | UnexpectedToken of tokenType: JsonTokenType * whereHint: string

let private tryConvertToField (state: UnfinishedField) : Result<Field, DeserializationError> =
    match state with
    | { key = None } ->
        nameof state.key
        |> RequiredPropertyMissing
        |> Error
    | { label = None } ->
        nameof state.key
        |> RequiredPropertyMissing
        |> Error
    | { value = None } ->
        nameof state.key
        |> RequiredPropertyMissing
        |> Error
    | { key = Some key
        label = Some label
        value = Some value } ->
        { Field.key = key
          Field.label = label
          Field.value = value }
        |> Ok

type PassStructure =
    { auxiliaryFields: Field list option
      backFields: Field list option
      headerFields: Field list option
      primaryFields: Field list option
      secondaryFields: Field list option }
    static member Default =
        { primaryFields = None
          auxiliaryFields = None
          headerFields = None
          secondaryFields = None
          backFields = None }

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

let private deserializeBarcode (reader: Utf8JsonReader byref) =

    let mutable format = Unchecked.defaultof<_>
    let mutable alternateText = None
    let mutable message = Unchecked.defaultof<_>

    let mutable messageEncoding =
        Unchecked.defaultof<_>

    let mutable lastProperty = None


    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndObject do
        match reader.TokenType with
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


type private UnfinishedBarcode =
    { alternateText: string option
      format: BarcodeFormat option
      message: string option
      messageEncoding: Encoding option }
    static member Default =
        { alternateText = None
          format = None
          message = None
          messageEncoding = None }

let private tryConvertToFinished (state: UnfinishedBarcode) =
    match state with
    | { format = None } ->
        nameof state.format
        |> RequiredPropertyMissing
        |> Error
    | { message = None } ->
        nameof state.message
        |> RequiredPropertyMissing
        |> Error
    | { messageEncoding = None } ->
        nameof state.messageEncoding
        |> RequiredPropertyMissing
        |> Error
    | { alternateText = alternateText
        format = Some format
        message = Some message
        messageEncoding = Some messageEncoding } ->
        Barcode(alternateText, format, message, messageEncoding)
        |> Ok

let private handleUnexpected (reader: Utf8JsonReader byref) (propertyName: string option) =
    let tokenType, value =
        match reader.TokenType with
        | JsonTokenType.String -> JsonTokenType.String, reader.GetString() |> box |> Some
        | JsonTokenType.Number -> JsonTokenType.Number, reader.GetInt32() |> box |> Some
        | JsonTokenType.StartObject -> JsonTokenType.StartObject, None
        | JsonTokenType.EndObject -> JsonTokenType.EndObject, None
        | JsonTokenType.Comment -> JsonTokenType.Comment, reader.GetString() |> box |> Some
        | JsonTokenType.None -> JsonTokenType.None, None
        | JsonTokenType.StartArray -> JsonTokenType.StartArray, None
        | JsonTokenType.EndArray -> JsonTokenType.EndArray, None
        | JsonTokenType.PropertyName -> JsonTokenType.PropertyName, reader.GetString() |> box |> Some
        | JsonTokenType.True -> JsonTokenType.True, reader.GetBoolean() |> box |> Some
        | JsonTokenType.False -> JsonTokenType.False, reader.GetBoolean() |> box |> Some
        | JsonTokenType.Null -> JsonTokenType.Null, None
        | outsideEnumValue -> outsideEnumValue, None

    match propertyName with
    | Some property -> UnexpectedProperty(property, tokenType, value)
    | None -> UnexpectedValue(tokenType, value)
    |> Error

let rec private deserializeBarcode'
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: UnfinishedBarcode)
    =
    if not <| reader.Read() then
        tryConvertToFinished state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> tryConvertToFinished state
        | JsonTokenType.PropertyName -> deserializeBarcode' &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            match lastPropertyName with
            | Some "altText" ->
                deserializeBarcode' &reader None { state with alternateText = reader.GetString() |> Some }
            | Some "format" ->

                match reader.GetString() with
                | "PKBarcodeFormatQR" -> deserializeBarcode' &reader None { state with format = Some Qr }
                | "PKBarcodeFormatPDF417" -> deserializeBarcode' &reader None { state with format = Some Pdf417 }
                | "PKBarcodeFormatAztec" -> deserializeBarcode' &reader None { state with format = Some Aztec }
                | otherFormat ->
                    OutOfBoundValue(JsonTokenType.String, otherFormat, "barcodeFormat")
                    |> Error
            | Some "message" -> deserializeBarcode' &reader None { state with message = reader.GetString() |> Some }
            | Some "messageEncoding" ->
                deserializeBarcode'
                    &reader
                    None
                    { state with messageEncoding = reader.GetString() |> Encoding.GetEncoding |> Some }
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode')
            |> Error



let rec private deserializeField'
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: UnfinishedField)
    =
    if not <| reader.Read() then
        state |> tryConvertToField
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> state |> tryConvertToField
        | JsonTokenType.PropertyName -> deserializeField' &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            match lastPropertyName with
            | Some "key" -> deserializeField' &reader None { state with key = reader.GetString() |> Some }
            | Some "label" -> deserializeField' &reader None { state with label = reader.GetString() |> Some }
            | Some "value" -> deserializeField' &reader None { state with value = reader.GetString() |> Some }
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeField')
            |> Error

let private deserializeField (reader: Utf8JsonReader byref) : Field =
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

    { key = key
      label = label
      value = value }

let rec private deserializeFields' (reader: Utf8JsonReader byref) (resultFields: Field list) =
    if not <| reader.Read() then
        Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> Ok resultFields
        | JsonTokenType.StartObject ->
            match deserializeField' &reader None UnfinishedField.Default with
            | Ok field -> deserializeFields' &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode')
            |> Error
//        match
//            deserializeField'
//                &reader
//                UnfinishedField.Default
//            with
//        | Ok field -> deserializeFields' &reader (field :: resultFields)
//        | Error error -> Error error

let private deserializeFields (reader: Utf8JsonReader byref) =
    let mutable arrayValues = []

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndArray do
        arrayValues <- (deserializeField &reader) :: arrayValues

    arrayValues

let rec private deserializePassStructure'
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: PassStructure)
    : Result<PassStructure, DeserializationError> =
    if not <| reader.Read() then
        Ok state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> Ok state
        | JsonTokenType.PropertyName -> deserializePassStructure' &reader (reader.GetString() |> Some) state
        | JsonTokenType.StartArray ->
            // I feel like I can reduce duplicate code here because the only thing that differs between cases is applying the result
            // to the state
            match lastPropertyName with
            // Assume if it has the property it has to have a value
            | Some "headerFields" ->
                match deserializeFields' &reader [] with
                | Ok fieldList -> deserializePassStructure' &reader None { state with headerFields = Some fieldList }
                | Error error -> Error error
            | Some "primaryFields" ->
                match deserializeFields' &reader [] with
                | Ok fieldList -> deserializePassStructure' &reader None { state with primaryFields = Some fieldList }
                | Error error -> Error error
            | Some "secondaryFields" ->
                match deserializeFields' &reader [] with
                | Ok fieldList -> deserializePassStructure' &reader None { state with secondaryFields = Some fieldList }
                | Error error -> Error error
            | Some "backFields" ->
                match deserializeFields' &reader [] with
                | Ok fieldList -> deserializePassStructure' &reader None { state with backFields = Some fieldList }
                | Error error -> Error error
            | Some "auxiliaryFields" ->
                match deserializeFields' &reader [] with
                | Ok fieldList -> deserializePassStructure' &reader None { state with auxiliaryFields = Some fieldList }
                | Error error -> Error error
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializePassStructure')
            |> Error

let private deserializePassStructure (reader: Utf8JsonReader byref) =

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
            | Some "headerFields" -> headerFields <- deserializeFields &reader |> Some
            | Some "primaryFields" -> primaryFields <- deserializeFields &reader |> Some
            | Some "secondaryFields" -> secondaryFields <- deserializeFields &reader |> Some
            | Some "backFields" -> backFields <- deserializeFields &reader |> Some
            | Some "auxiliaryFields" -> auxiliaryFields <- deserializeFields &reader |> Some
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None -> failwith $"Got {reader.TokenType} token type but no property name proceeded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | otherToken -> failwith $"Unexpected other token '{otherToken}' in pass structure object"

    { primaryFields = primaryFields
      auxiliaryFields = auxiliaryFields
      headerFields = headerFields
      secondaryFields = secondaryFields
      backFields = backFields }
//    PassStructure(auxiliaryFields, backFields, headerFields, primaryFields, secondaryFields)


let rec private deserializeBarcodes' (reader: Utf8JsonReader byref) (resultFields: Barcode list) =
    if not <| reader.Read() then
        Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> Ok resultFields
        | JsonTokenType.StartObject ->
            match deserializeBarcode' &reader None UnfinishedBarcode.Default with
            | Ok field -> deserializeBarcodes' &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode')
            |> Error

let private deserializeBarcodes (reader: Utf8JsonReader byref) =
    let mutable arrayValues = []

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndArray do
        arrayValues <- (deserializeBarcode &reader) :: arrayValues

    arrayValues

let deserializePass (data: byte ReadOnlySpan) =
    let mutable reader = Utf8JsonReader(data)

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

    let mutable relevanceDate = None
    let mutable lastProperty = None

    let mutable isPastFirstStart = false

    while reader.Read()
          && reader.TokenType <> JsonTokenType.EndObject do
        match reader.TokenType with
        | JsonTokenType.PropertyName -> lastProperty <- reader.GetString() |> Option.ofObj
        | JsonTokenType.String ->
            match lastProperty with
            | Some "description" -> description <- reader.GetString()
            | Some "organizationName" -> organizationName <- reader.GetString()
            | Some "passTypeIdentifier" -> passTypeIdentifier <- reader.GetString()
            | Some "serialNumber" -> serialNumber <- reader.GetString()
            | Some "teamIdentifier" -> teamIdentifier <- reader.GetString()
            | Some "relevantDate" -> relevanceDate <- reader.GetDateTimeOffset() |> Some
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
                    $"Got {reader.TokenType} token type with value '{reader.GetInt64()}' but no property name preceded this value"

            // Clean last property after we extracted the properties value
            lastProperty <- None

        | JsonTokenType.StartObject ->
            match lastProperty with
            | Some "barcode" -> barcode <- deserializeBarcode &reader |> Some
            | Some "eventTicket" ->
                let structure =
                    deserializePassStructure &reader

                constructPass <- fun definition -> EventTicket(definition, structure)
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            // First object start is the root of the object
            | None when not isPastFirstStart ->
                // After this any object that starts without property is invalid
                isPastFirstStart <- true
            | None -> failwith $"Got {reader.TokenType} token type but no property name preceded this value"

            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | JsonTokenType.StartArray ->
            match lastProperty with
            | Some "barcodes" -> barcodes <- (deserializeBarcodes &reader) |> Some
            | Some otherProperty -> failwith $"Unexpected non supported property {otherProperty}"
            | None -> failwith $"Got {reader.TokenType} token type but no property name preceded this value"
            // Clean last property after we extracted the properties value
            lastProperty <- None
        // Next read advance will leave the object
        | _ -> Console.WriteLine $"Not supported token type '{reader.TokenType}'"

    let definition =
        { description = description
          formatVersion = formatVersion
          organizationName = organizationName
          passTypeIdentifier = passTypeIdentifier
          serialNumber = serialNumber
          teamIdentifier = teamIdentifier
          foregroundColor = foregroundColor
          labelColor = labelColor
          barcode = barcode
          barcodes = barcodes
          relevanceDate = relevanceDate }

    constructPass definition

type PassDeserializationState =
    { description: string option
      formatVersion: int option
      serialNumber: string option
      organizationName: string option
      passTypeIdentifier: string option
      teamIdentifier: string option
      foregroundColor: CssColor option
      labelColor: CssColor option
      barcode: Barcode option
      barcodes: Barcode list option
      // Construct pass decided what pass gets build from the definition and the structure in this record
      constructPass: (PassDefinition -> Pass) option
      relevanceDate: DateTimeOffset option
      isPastRootStartObject: bool }
    static member Default =
        { description = None
          formatVersion = None
          serialNumber = None
          organizationName = None
          passTypeIdentifier = None
          teamIdentifier = None
          foregroundColor = None
          labelColor = None
          barcode = None
          barcodes = None
          constructPass = None
          relevanceDate = None
          isPastRootStartObject = false }

let private tryFinishSerialization (state: PassDeserializationState) =
    match state with
    // All the cases where a property was never added but is required
    | { description = None } ->
        nameof state.description
        |> RequiredPropertyMissing
        |> Error
    | { formatVersion = None } ->
        nameof state.formatVersion
        |> RequiredPropertyMissing
        |> Error
    | { organizationName = None } ->
        nameof state.organizationName
        |> RequiredPropertyMissing
        |> Error
    | { passTypeIdentifier = None } ->
        nameof state.passTypeIdentifier
        |> RequiredPropertyMissing
        |> Error
    | { serialNumber = None } ->
        nameof state.serialNumber
        |> RequiredPropertyMissing
        |> Error
    | { teamIdentifier = None } ->
        nameof state.teamIdentifier
        |> RequiredPropertyMissing
        |> Error
    | { constructPass = None } ->
        nameof state.constructPass
        |> RequiredPropertyMissing
        |> Error
    // Deconstruct valid unfinished pass definition and build valid one with it
    | { description = Some description
        formatVersion = Some formatVersion
        organizationName = Some organizationName
        passTypeIdentifier = Some passTypeIdentifier
        serialNumber = Some serialNumber
        teamIdentifier = Some teamIdentifier
        foregroundColor = foregroundColor
        labelColor = labelColor
        barcode = barcode
        barcodes = barcodes
        constructPass = Some constructPass
        relevanceDate = relevanceDate } ->
        { description = description
          formatVersion = formatVersion
          serialNumber = serialNumber
          organizationName = organizationName
          passTypeIdentifier = passTypeIdentifier
          teamIdentifier = teamIdentifier
          foregroundColor = foregroundColor
          labelColor = labelColor
          barcode = barcode
          barcodes = barcodes
          relevanceDate = relevanceDate }
        |> constructPass
        |> Ok


let rec deserializePass'
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: PassDeserializationState)
    =
    if not (reader.Read()) then
        // Exit
        tryFinishSerialization state
    else
        match reader.TokenType with
        // Exit
        | JsonTokenType.EndObject -> tryFinishSerialization state
        | JsonTokenType.PropertyName -> deserializePass' &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            // Pass none as last property name to last property name
            match lastPropertyName with
            | Some "description" ->
                deserializePass' &reader None { state with description = reader.GetString() |> Some }
            | Some "organizationName" ->
                deserializePass' &reader None { state with organizationName = reader.GetString() |> Some }
            | Some "passTypeIdentifier" ->
                deserializePass' &reader None { state with passTypeIdentifier = reader.GetString() |> Some }
            | Some "serialNumber" ->
                deserializePass' &reader None { state with serialNumber = reader.GetString() |> Some }
            | Some "teamIdentifier" ->
                deserializePass' &reader None { state with teamIdentifier = reader.GetString() |> Some }
            | Some "relevantDate" ->
                deserializePass' &reader None { state with relevanceDate = reader.GetDateTimeOffset() |> Some }
            | Some "labelColor" ->
                let labelColor =
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map CssColor

                deserializePass' &reader None { state with labelColor = labelColor }
            | Some "foregroundColor" ->
                let foregroundColor =
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map CssColor

                deserializePass' &reader None { state with foregroundColor = foregroundColor }
            | other -> handleUnexpected &reader other
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "formatVersion" ->
                deserializePass' &reader None { state with formatVersion = reader.GetInt32() |> Some }
            | other -> handleUnexpected &reader other
        | JsonTokenType.StartObject ->
            match lastPropertyName with
            // When we start deserializing we need to advance once past the root object start
            | _ when not state.isPastRootStartObject ->
                deserializePass' &reader None { state with isPastRootStartObject = true }
            | Some "barcode" ->
                match deserializeBarcode' &reader None UnfinishedBarcode.Default with
                | Ok barcode -> deserializePass' &reader None { state with barcode = Some barcode }
                // This maps Result<Barcode,'TError> to Result<Pass.'TError>. Is there a simpler way?
                | Error error -> Error error
            | Some "eventTicket" ->
                let structure =
                    deserializePassStructure' &reader None PassStructure.Default
                // Would use option bind but can't because of reader byref :)
                match structure with
                | Ok structure ->
                    let transformer definition = EventTicket(definition, structure)
                    deserializePass' &reader None { state with constructPass = Some transformer }
                | Error error -> Error error
            | other -> handleUnexpected &reader other

        | JsonTokenType.StartArray ->
            match lastPropertyName with
            | Some "barcodes" ->
                match deserializeBarcodes' &reader [] with
                | Ok barcodeList -> deserializePass' &reader None { state with barcodes = Some barcodeList }
                | Error error -> Error error
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializePass')
            |> Error
