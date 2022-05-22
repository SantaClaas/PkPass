module PkPass.PassKit.Deserialization

open System
open System.Text
open System.Text.Json
open PkPass
open Resets

type CssColor = CssColor of string

//TODO barcodes in the barcodes array are allowed to have type PKBarcodeFormatCode128 but not the single barcode
type BarcodeFormat =
    | Qr
    | Pdf417
    | Aztec

type Barcode =
    | Barcode of alternateText: string option * format: BarcodeFormat * message: string * messageEncoding: Encoding

type LocalizableString = LocalizableString of string
type LocalizableFormatString = LocalizableFormatString of string

type PassDefinition =
    { description: LocalizableString
      formatVersion: int
      organizationName: LocalizableString
      passTypeIdentifier: string
      serialNumber: string
      teamIdentifier: string
      foregroundColor: CssColor option
      labelColor: CssColor option
      // Deprecated since iOS 9 use barcodes instead
      barcode: Barcode option
      barcodes: Barcode list option
      relevanceDate: DateTimeOffset option }

type AttributedValue =
    | HtmlAnchorTag of href: string * label: string
    | Date of DateTimeOffset
    | Number of int

type FieldValue =
    | LocalizableString of LocalizableString
    | Date of DateTimeOffset
    | Number of int

type DataDetectorType =
    | PhoneNumber
    | Link
    | Address
    | CalendarEvent

//TODO not allowed for primary fields or back fields
type TextAlignment =
    | Left
    | Center
    | Right
    // Natural is default even though it is optional?
    | Natural

type Field =
    { attributedValue: AttributedValue option
      changeMessage: LocalizableFormatString option
      dataDetectorTypes: DataDetectorType list option
      key: string
      label: LocalizableString option
      value: FieldValue }
    static member Default key value =
        { attributedValue = None
          changeMessage = None
          dataDetectorTypes = None
          key = key
          value = value
          label = None }

type private FieldDeserializationState =
    { attributedValue: AttributedValue option
      changeMessage: LocalizableFormatString option
      dataDetectorTypes: DataDetectorType list option
      key: string option
      label: LocalizableString option
      value: FieldValue option }
    static member Default =
        { attributedValue = None
          changeMessage = None
          dataDetectorTypes = None
          key = None
          value = None
          label = None }

type DeserializationError =
    // A property that is required by definition is missing in the JSON
    | RequiredPropertyMissing of propertyName: string
    | UnexpectedProperty of propertyName: string * tokenType: JsonTokenType * value: object
    // Dont like the boxing here but the value should only be used for logging or displaying
    | UnexpectedValue of tokenType: JsonTokenType * value: object
    | OutOfBoundValue of tokenType: JsonTokenType * value: object * whereHint: string
    | UnexpectedToken of tokenType: JsonTokenType * whereHint: string

let private tryFinishFieldDeserialization (state: FieldDeserializationState) : Result<Field, DeserializationError> =
    match state with
    | { key = None } ->
        nameof state.key
        |> RequiredPropertyMissing
        |> Error
    | { value = None } ->
        nameof state.key
        |> RequiredPropertyMissing
        |> Error
    | { key = Some key
        label = label
        value = Some value } ->
        { Field.key = key
          Field.label = label
          Field.value = value
          attributedValue = None
          changeMessage = None
          dataDetectorTypes = None }
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

let rec private deserializeBarcode
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: BarcodeDeserializationState)
    =
    if not <| reader.Read() then
        tryFinishBarcodeDeserialization state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> tryFinishBarcodeDeserialization state
        | JsonTokenType.PropertyName -> deserializeBarcode &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            match lastPropertyName with
            | Some "altText" ->
                deserializeBarcode &reader None { state with alternateText = reader.GetString() |> Some }
            | Some "format" ->

                match reader.GetString() with
                | "PKBarcodeFormatQR" -> deserializeBarcode &reader None { state with format = Some Qr }
                | "PKBarcodeFormatPDF417" -> deserializeBarcode &reader None { state with format = Some Pdf417 }
                | "PKBarcodeFormatAztec" -> deserializeBarcode &reader None { state with format = Some Aztec }
                | otherFormat ->
                    OutOfBoundValue(JsonTokenType.String, otherFormat, "barcodeFormat")
                    |> Error
            | Some "message" -> deserializeBarcode &reader None { state with message = reader.GetString() |> Some }
            | Some "messageEncoding" ->
                deserializeBarcode
                    &reader
                    None
                    { state with messageEncoding = reader.GetString() |> Encoding.GetEncoding |> Some }
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode)
            |> Error



let rec private deserializeField
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: FieldDeserializationState)
    =
    if not <| reader.Read() then
        state |> tryFinishFieldDeserialization
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> state |> tryFinishFieldDeserialization
        | JsonTokenType.PropertyName -> deserializeField &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            match lastPropertyName with
            | Some "key" -> deserializeField &reader None { state with key = reader.GetString() |> Some }
            | Some "label" ->
                deserializeField
                    &reader
                    None
                    { state with
                        label =
                            reader.GetString()
                            |> LocalizableString.LocalizableString
                            |> Some }
            | Some "value" ->
                let isDate, date =
                    reader.TryGetDateTimeOffset()

                if isDate then
                    let value = FieldValue.Date date
                    deserializeField &reader None { state with value = Some value }
                else
                    let value =
                        reader.GetString()
                        |> LocalizableString.LocalizableString
                        |> FieldValue.LocalizableString
                    deserializeField &reader None { state with value = Some value }
            | other -> handleUnexpected &reader other
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "value" ->
                let value = reader.GetInt32() |> FieldValue.Number
                deserializeField &reader None { state with value = Some value }
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeField)
            |> Error

let rec private deserializeFields (reader: Utf8JsonReader byref) (resultFields: Field list) =
    if not <| reader.Read() then
        Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> Ok resultFields
        | JsonTokenType.StartObject ->
            match deserializeField &reader None FieldDeserializationState.Default with
            | Ok field -> deserializeFields &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode)
            |> Error

let rec private deserializePassStructure
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: PassStructure)
    : Result<PassStructure, DeserializationError> =
    if not <| reader.Read() then
        Ok state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> Ok state
        | JsonTokenType.PropertyName -> deserializePassStructure &reader (reader.GetString() |> Some) state
        | JsonTokenType.StartArray ->
            // I feel like I can reduce duplicate code here because the only thing that differs between cases is applying the result
            // to the state
            match lastPropertyName with
            // Assume if it has the property it has to have a value
            | Some "headerFields" ->
                match deserializeFields &reader [] with
                | Ok fieldList -> deserializePassStructure &reader None { state with headerFields = Some fieldList }
                | Error error -> Error error
            | Some "primaryFields" ->
                match deserializeFields &reader [] with
                | Ok fieldList -> deserializePassStructure &reader None { state with primaryFields = Some fieldList }
                | Error error -> Error error
            | Some "secondaryFields" ->
                match deserializeFields &reader [] with
                | Ok fieldList -> deserializePassStructure &reader None { state with secondaryFields = Some fieldList }
                | Error error -> Error error
            | Some "backFields" ->
                match deserializeFields &reader [] with
                | Ok fieldList -> deserializePassStructure &reader None { state with backFields = Some fieldList }
                | Error error -> Error error
            | Some "auxiliaryFields" ->
                match deserializeFields &reader [] with
                | Ok fieldList -> deserializePassStructure &reader None { state with auxiliaryFields = Some fieldList }
                | Error error -> Error error
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializePassStructure)
            |> Error

let rec private deserializeBarcodes (reader: Utf8JsonReader byref) (resultFields: Barcode list) =
    if not <| reader.Read() then
        Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> Ok resultFields
        | JsonTokenType.StartObject ->
            match deserializeBarcode &reader None BarcodeDeserializationState.Default with
            | Ok field -> deserializeBarcodes &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode)
            |> Error

type PassDeserializationState =
    { description: LocalizableString option
      formatVersion: int option
      serialNumber: string option
      organizationName: LocalizableString option
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

let private tryFinishPassDeserialization (state: PassDeserializationState) =
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


let rec deserializePass
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: PassDeserializationState)
    =
    if not (reader.Read()) then
        // Exit
        tryFinishPassDeserialization state
    else
        match reader.TokenType with
        // Exit
        | JsonTokenType.EndObject -> tryFinishPassDeserialization state
        | JsonTokenType.PropertyName -> deserializePass &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            // Pass none as last property name to last property name
            match lastPropertyName with
            | Some "description" ->
                deserializePass
                    &reader
                    None
                    { state with
                        description =
                            reader.GetString()
                            |> LocalizableString.LocalizableString
                            |> Some }
            | Some "organizationName" ->
                deserializePass
                    &reader
                    None
                    { state with
                        organizationName =
                            reader.GetString()
                            |> LocalizableString.LocalizableString
                            |> Some }
            | Some "passTypeIdentifier" ->
                deserializePass &reader None { state with passTypeIdentifier = reader.GetString() |> Some }
            | Some "serialNumber" ->
                deserializePass &reader None { state with serialNumber = reader.GetString() |> Some }
            | Some "teamIdentifier" ->
                deserializePass &reader None { state with teamIdentifier = reader.GetString() |> Some }
            | Some "relevantDate" ->
                deserializePass &reader None { state with relevanceDate = reader.GetDateTimeOffset() |> Some }
            | Some "labelColor" ->
                let labelColor =
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map CssColor

                deserializePass &reader None { state with labelColor = labelColor }
            | Some "foregroundColor" ->
                let foregroundColor =
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map CssColor

                deserializePass &reader None { state with foregroundColor = foregroundColor }
            | other -> handleUnexpected &reader other
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "formatVersion" ->
                deserializePass &reader None { state with formatVersion = reader.GetInt32() |> Some }
            | other -> handleUnexpected &reader other
        | JsonTokenType.StartObject ->
            match lastPropertyName with
            // When we start deserializing we need to advance once past the root object start
            | _ when not state.isPastRootStartObject ->
                deserializePass &reader None { state with isPastRootStartObject = true }
            | Some "barcode" ->
                match deserializeBarcode &reader None BarcodeDeserializationState.Default with
                | Ok barcode -> deserializePass &reader None { state with barcode = Some barcode }
                // This maps Result<Barcode,TError> to Result<Pass.TError>. Is there a simpler way?
                | Error error -> Error error
            | Some "eventTicket" ->
                let structure =
                    deserializePassStructure &reader None PassStructure.Default
                // Would use option bind but cant because of reader byref :)
                match structure with
                | Ok structure ->
                    let transformer definition = EventTicket(definition, structure)
                    deserializePass &reader None { state with constructPass = Some transformer }
                | Error error -> Error error
            | other -> handleUnexpected &reader other

        | JsonTokenType.StartArray ->
            match lastPropertyName with
            | Some "barcodes" ->
                match deserializeBarcodes &reader [] with
                | Ok barcodeList -> deserializePass &reader None { state with barcodes = Some barcodeList }
                | Error error -> Error error
            | other -> handleUnexpected &reader other
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializePass)
            |> Error

