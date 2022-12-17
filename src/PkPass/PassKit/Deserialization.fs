module PkPass.PassKit.Deserialization

open System
open System.Text
open System.Text.Json
open PkPass
open Resets
open Extensions

/// <summary>
/// A string in the CSS style like "rgb(0,12,255)". Could be used in CSS variables.
/// </summary>
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
    { // Standard Keys: Required values for all passes
      description: LocalizableString
      formatVersion: int
      organizationName: LocalizableString
      passTypeIdentifier: string
      serialNumber: string
      teamIdentifier: string
      // Expiration keys: Information about when a pass expires and whether it is still valid
      // A pass is marked as expired if the current date is after the pass’s expiration date, or if the pass has been
      // explicitly marked as voided
      /// <summary>
      /// Optional date and time when the pass expires. The value must be acomplete date with hours and minutes and may
      /// optionally include seconds.
      /// </summary>
      expirationDate: DateTimeOffset option
      /// <summary>
      /// Optional indication that the pass is void. For example a one time coupon that has been redeemed. The default
      /// value is false.
      /// The value is <see cref="Option.None"/> if no value was provided.
      /// </summary>
      voided: bool option

      // Visual appearance keys: Keys that define the visual style and appearance of the pass
      /// <summary>
      /// Optional background color of the pass
      /// </summary>
      backgroundColor: CssColor option
      /// <summary>
      /// Optional foreground color of the pass
      /// </summary>
      foregroundColor: CssColor option
      /// <summary>
      /// Optional color of the labeled text, specified as a CSS-style RGB triple. For example, rgb(255, 255, 255).
      /// If omitted, the label color is determined automatically.
      /// </summary>
      labelColor: CssColor option
      /// <summary>
      /// Optional text displayed next to the logo on the pass
      /// </summary>
      logoText: LocalizableString option
      /// <summary>
      /// An optional barcode. For iOS 8 and earlier. Deprecated since iOS 9 use <see cref="barcodes"/> instead
      /// </summary>
      barcode: Barcode option
      barcodes: Barcode list option
      /// <summary>
      /// Recommended for event tickets and boarding passes; otherwise optional.
      /// Date and time when the pass becomes relevant. For example, the start time of a movie.
      /// The value must be a complete date with hours and minutes, and may optionally include seconds.
      /// </summary>
      relevanceDate: DateTimeOffset option }

type AttributedValue =
    | HtmlAnchorTag of href: string * label: string
    | Date of DateTimeOffset
    | Number of int

type FieldValue =
    | LocalizableString of LocalizableString
    | Date of DateTimeOffset
    | Number of int

    override this.ToString() =
        match this with
        | LocalizableString (LocalizableString.LocalizableString localizableString) -> localizableString
        | Date date -> date.ToString()
        | Number number -> number.ToString()

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

module TextAlignment =
    let tryParse =
        function
        | "PKTextAlignmentLeft" -> Some TextAlignment.Left
        | "PKTextAlignmentCenter" -> Some TextAlignment.Center
        | "PKTextAlignmentRight" -> Some TextAlignment.Right
        | "PKTextAlignmentNatural" -> Some TextAlignment.Natural
        | _ -> None

type Field =
    { attributedValue: AttributedValue option
      changeMessage: LocalizableFormatString option
      dataDetectorTypes: DataDetectorType list option
      key: string
      label: LocalizableString option
      textAlignment: TextAlignment option
      value: FieldValue }

    static member Default key value =
        { attributedValue = None
          changeMessage = None
          dataDetectorTypes = None
          key = key
          value = value
          textAlignment = None
          label = None }

type private FieldDeserializationState =
    { attributedValue: AttributedValue option
      changeMessage: LocalizableFormatString option
      dataDetectorTypes: DataDetectorType list option
      key: string option
      label: LocalizableString option
      textAlignment: TextAlignment option
      value: FieldValue option }

    static member Default =
        { attributedValue = None
          changeMessage = None
          dataDetectorTypes = None
          key = None
          value = None
          textAlignment = None
          label = None }

type DeserializationError =
    /// <summary>
    /// A property that is required by definition is missing in the JSON
    /// </summary>
    /// <param name="name">The name of the property that is missing but required</param>
    | RequiredPropertyMissing of name: string
    /// <summary>
    /// A property that was encountered but is not known or handled
    /// </summary>
    /// <param name="name">The name of the unknown property</param>
    /// <param name="tokenType">The token type of the properties value to identify the data</param>
    /// <param name="value">The value of the property</param>
    | UnexpectedProperty of name: string * tokenType: JsonTokenType * value: object
    // Dont like the boxing here but the value should only be used for logging or displaying
    | UnexpectedValue of tokenType: JsonTokenType * value: object
    | OutOfBoundValue of tokenType: JsonTokenType * value: object * whereHint: string
    | UnexpectedToken of tokenType: JsonTokenType * whereHint: string * lastPropertyName: string option
    /// <summary>
    /// An error when a value is invalid because it can only be one of the allowed values
    /// </summary>
    /// <param name="JsonTokenType">The type of the value</param>
    /// <param name="allowedValues">The allowed values that the property can assume</param>
    /// <param name="actualValue">The value that it actually was and is not in <see cref="allowedValues"/></param>
    | InvalidValue of JsonTokenType: JsonTokenType * allowedValues: object array * actualValue: object

let private tryFinishFieldDeserialization (state: FieldDeserializationState) : Result<Field, DeserializationError> =
    match state with
    | { key = None } -> nameof state.key |> RequiredPropertyMissing |> Error
    | { value = None } -> nameof state.key |> RequiredPropertyMissing |> Error
    | { key = Some key
        label = label
        value = Some value
        textAlignment = textAlignment } ->
        { Field.key = key
          Field.label = label
          Field.value = value
          attributedValue = None
          changeMessage = None
          textAlignment = textAlignment
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

type PassStructureDeserializationState =
    { passStructure: PassStructure
      errors: DeserializationError list }

    static member Default =
        { passStructure = PassStructure.Default
          errors = [] }

type TransitType =
    | Air
    | Boat
    | Bus
    | Generic
    | Train

/// <summary>
/// Boarding pass structure extends standard pass structure (<see cref="PassStructure"/>) with a mandatory transit type
/// </summary>
type BoardingPassStructure = BoardingPassStructure of PassStructure * TransitType
type BoardingPass = BoardingPass of PassDefinition * BoardingPassStructure
type Coupon = Coupon
type EventTicket = EventTicket of PassDefinition * PassStructure
type GenericPass = GenericPass of PassDefinition * PassStructure
type StoreCard = StoreCard

[<RequireQualifiedAccess>]
type PassStyle = 
    | BoardingPass of BoardingPass
    | Coupon of Coupon
    | EventTicket of EventTicket
    | Generic of GenericPass
    | StoreCard of StoreCard

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


/// <summary>
/// Describes the result of a pass deserialization
/// </summary>
type DeserializationResult<'T> =
    /// <summary>
    /// The deserialization of the pass ran without issues and the structure is valid according to requirements
    /// </summary>
    | Ok of 'T
    /// <summary>
    /// The deserialization encountered one or more errors but could recover and return a possibly incorrect pass.
    /// There might be issues with the pass that lead to a reduced user experience
    /// </summary>
    /// <param name="errors">A list of errors that occured and might cause the pass to appear incorrect</param>
    | Recovered of 'T * errors: DeserializationError list
    /// <summary>
    /// The deserialization failed because there was an unrecoverable error while deserialization like invalid JSON.
    /// Big bad.
    /// </summary>
    | Failed of DeserializationError

let private tryFinishBarcodeDeserialization (state: BarcodeDeserializationState) =
    match state with
    | { format = None } -> nameof state.format |> RequiredPropertyMissing |> Error
    | { message = None } -> nameof state.message |> RequiredPropertyMissing |> Error
    | { messageEncoding = None } -> nameof state.messageEncoding |> RequiredPropertyMissing |> Error
    | { alternateText = alternateText
        format = Some format
        message = Some message
        messageEncoding = Some messageEncoding } ->
        Barcode(alternateText, format, message, messageEncoding) |> Result.Ok

let private handleInvalidProperty (reader: Utf8JsonReader byref) (propertyName: string option) =
    let tokenType, value =
        match reader.TokenType with
        | JsonTokenType.String -> JsonTokenType.String, reader.GetString() |> box |> Some
        | JsonTokenType.Number -> JsonTokenType.Number, reader.GetInt32() |> box |> Some
        | JsonTokenType.StartObject ->
            // Continue and ignore object content until object is closed
            while reader.Read() && reader.TokenType <> JsonTokenType.EndObject do
                ()

            JsonTokenType.StartObject, None
        | JsonTokenType.EndObject -> JsonTokenType.EndObject, None
        | JsonTokenType.Comment -> JsonTokenType.Comment, reader.GetString() |> box |> Some
        | JsonTokenType.None -> JsonTokenType.None, None
        | JsonTokenType.StartArray ->
            // Continue and ignore object content until object is closed
            while reader.Read() && reader.TokenType <> JsonTokenType.EndArray do
                ()

            JsonTokenType.StartArray, None
        | JsonTokenType.EndArray -> JsonTokenType.EndArray, None
        | JsonTokenType.PropertyName -> JsonTokenType.PropertyName, reader.GetString() |> box |> Some
        | JsonTokenType.True -> JsonTokenType.True, reader.GetBoolean() |> box |> Some
        | JsonTokenType.False -> JsonTokenType.False, reader.GetBoolean() |> box |> Some
        | JsonTokenType.Null -> JsonTokenType.Null, None
        | outsideEnumValue -> outsideEnumValue, None

    match propertyName with
    | Some property -> UnexpectedProperty(property, tokenType, value)
    | None -> UnexpectedValue(tokenType, value)

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
                | otherFormat -> OutOfBoundValue(JsonTokenType.String, otherFormat, "barcodeFormat") |> Error
            | Some "message" -> deserializeBarcode &reader None { state with message = reader.GetString() |> Some }
            | Some "messageEncoding" ->
                deserializeBarcode
                    &reader
                    None
                    { state with messageEncoding = reader.GetString() |> Encoding.GetEncoding |> Some }
            | other -> handleInvalidProperty &reader other |> Error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeBarcode, lastPropertyName)
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
                    { state with label = reader.GetString() |> LocalizableString.LocalizableString |> Some }
            | Some "value" ->
                let isDate, date = reader.TryGetDateTimeOffset()

                if isDate then
                    let value = FieldValue.Date date
                    deserializeField &reader None { state with value = Some value }
                else
                    let value =
                        reader.GetString()
                        |> LocalizableString.LocalizableString
                        |> FieldValue.LocalizableString

                    deserializeField &reader None { state with value = Some value }
            | Some "textAlignment" ->
                let alignmentString = reader.GetString()

                match TextAlignment.tryParse alignmentString with
                | None ->
                    DeserializationError.InvalidValue(
                        JsonTokenType.String,
                        [| "PKTextAlignmentLeft",
                           "PKTextAlignmentCenter",
                           "PKTextAlignmentRight",
                           "PKTextAlignmentNatural" |],
                        alignmentString
                    )
                    |> Error
                | alignment -> deserializeField &reader None { state with textAlignment = alignment }


            | other -> handleInvalidProperty &reader other |> Error
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "value" ->
                let value = reader.GetInt32() |> FieldValue.Number

                deserializeField &reader None { state with value = Some value }
            | other -> handleInvalidProperty &reader other |> Error
        | otherToken -> UnexpectedToken(otherToken, nameof deserializeField, lastPropertyName) |> Error

let rec private deserializeFields (reader: Utf8JsonReader byref) (resultFields: Field list) =
    if not <| reader.Read() then
        Result.Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> Result.Ok resultFields
        | JsonTokenType.StartObject ->
            match deserializeField &reader None FieldDeserializationState.Default with
            | Result.Ok field -> deserializeFields &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken -> UnexpectedToken(otherToken, nameof deserializeBarcode, None) |> Error

let completePassStructureDeserialization state =
    match state with
    | { errors = []
        passStructure = passStructure } -> DeserializationResult.Ok passStructure
    | _ -> DeserializationResult.Recovered(state.passStructure, state.errors)

let rec private deserializePassStructure
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: PassStructureDeserializationState)
    =
    if not <| reader.Read() then
        completePassStructureDeserialization state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> completePassStructureDeserialization state
        | JsonTokenType.PropertyName -> deserializePassStructure &reader (reader.GetString() |> Some) state
        | JsonTokenType.StartArray ->
            // I feel like I can reduce duplicate code here because the only thing that differs between cases is applying the result
            // to the state
            match lastPropertyName with
            // Assume if it has the property it has to have a value
            | Some "headerFields" ->
                match deserializeFields &reader [] with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with headerFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructure &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "primaryFields" ->
                match deserializeFields &reader [] with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with primaryFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructure &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "secondaryFields" ->
                match deserializeFields &reader [] with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with secondaryFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructure &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "backFields" ->
                match deserializeFields &reader [] with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with backFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructure &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "auxiliaryFields" ->
                match deserializeFields &reader [] with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with auxiliaryFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructure &reader None newState
                | Error error -> DeserializationResult.Failed error
            | name ->
                let error = handleInvalidProperty &reader name
                let newState = { state with errors = error :: state.errors }
                deserializePassStructure &reader None newState

        | otherToken ->
            let error =
                UnexpectedToken(otherToken, nameof deserializePassStructure, lastPropertyName)

            let newState = { state with errors = error :: state.errors }
            deserializePassStructure &reader None newState


let rec private deserializeBarcodes (reader: Utf8JsonReader byref) (resultFields: Barcode list) =
    if not <| reader.Read() then
        Result.Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray -> Result.Ok resultFields
        | JsonTokenType.StartObject ->
            match deserializeBarcode &reader None BarcodeDeserializationState.Default with
            | Result.Ok field -> deserializeBarcodes &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken -> UnexpectedToken(otherToken, nameof deserializeBarcode, None) |> Error

/// <summary>
/// Represents an unfinished collection of values that can be contained in a pass.
/// This represents the pass before it ran through validation
/// </summary>
type PassDeserializationState =
    { description: LocalizableString option
      formatVersion: int option
      serialNumber: string option
      organizationName: LocalizableString option
      passTypeIdentifier: string option
      teamIdentifier: string option
      expirationDate: DateTimeOffset option
      voided: bool option
      backgroundColor: CssColor option
      foregroundColor: CssColor option
      logoText: LocalizableString option
      labelColor: CssColor option
      barcode: Barcode option
      barcodes: Barcode list option
      //TODO move out of JSON representation
      /// <summary>
      /// A function that decides what pass type gets finally created from the pass definition and creates it
      /// </summary>
      createPass: (PassDefinition -> PassStyle) option
      /// <summary>
      /// Recommended for event tickets and boarding passes; otherwise optional.
      /// Date and time when the pass becomes relevant. For example, the start time of a movie.
      /// The value must be a complete date with hours and minutes, and may optionally include seconds.
      /// </summary>
      relevanceDate: DateTimeOffset option
      isPastRootStartObject: bool
      /// <summary>
      /// A list of error that were encountered while deserialization but could be recovered from
      /// </summary>
      errors: DeserializationError list }

    static member Default =
        { description = None
          formatVersion = None
          serialNumber = None
          organizationName = None
          passTypeIdentifier = None
          teamIdentifier = None
          expirationDate = None
          voided = None
          backgroundColor = None
          foregroundColor = None
          logoText = None
          labelColor = None
          barcode = None
          barcodes = None
          createPass = None
          relevanceDate = None
          isPastRootStartObject = false
          errors = [] }

/// <summary>
/// Tries to convert an "unstable" pass that is just a collection of the values that can appear to a type safe pass
/// structure that has to fulfill the requirements of a pass. If not all requirements are fulfilled the function returns
/// an error detailing what is missing or was wrong.
/// </summary>
/// <param name="state">
/// The state representing all known properties with their values just that they are not guaranteed to exist
/// </param>
let private tryFinishPassDeserialization (state: PassDeserializationState) : PassStyle DeserializationResult =
    match state with
    // All the cases where a property was never added but is required
    | { description = None } ->
        nameof state.description
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    | { formatVersion = None } ->
        nameof state.formatVersion
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    | { organizationName = None } ->
        nameof state.organizationName
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    | { passTypeIdentifier = None } ->
        nameof state.passTypeIdentifier
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    | { serialNumber = None } ->
        nameof state.serialNumber
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    | { teamIdentifier = None } ->
        nameof state.teamIdentifier
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    | { createPass = None } ->
        nameof state.createPass
        |> RequiredPropertyMissing
        |> DeserializationResult.Failed
    // Deconstruct valid unfinished pass definition and build valid one with it
    | { description = Some description
        formatVersion = Some formatVersion
        organizationName = Some organizationName
        passTypeIdentifier = Some passTypeIdentifier
        serialNumber = Some serialNumber
        teamIdentifier = Some teamIdentifier
        expirationDate = expirationDate
        voided = voided
        backgroundColor = backgroundColor
        foregroundColor = foregroundColor
        labelColor = labelColor
        logoText = logoText
        barcode = barcode
        barcodes = barcodes
        createPass = Some constructPass
        relevanceDate = relevanceDate } ->
        { description = description
          formatVersion = formatVersion
          serialNumber = serialNumber
          organizationName = organizationName
          passTypeIdentifier = passTypeIdentifier
          teamIdentifier = teamIdentifier
          expirationDate = expirationDate
          voided = voided
          backgroundColor = backgroundColor
          foregroundColor = foregroundColor
          labelColor = labelColor
          logoText = logoText
          barcode = barcode
          barcodes = barcodes
          relevanceDate = relevanceDate }
        |> constructPass
        |> DeserializationResult.Ok


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
                    { state with description = reader.GetString() |> LocalizableString.LocalizableString |> Some }
            | Some "organizationName" ->
                deserializePass
                    &reader
                    None
                    { state with organizationName = reader.GetString() |> LocalizableString.LocalizableString |> Some }
            | Some "passTypeIdentifier" ->
                deserializePass &reader None { state with passTypeIdentifier = reader.GetString() |> Some }
            | Some "serialNumber" ->
                deserializePass &reader None { state with serialNumber = reader.GetString() |> Some }
            | Some "teamIdentifier" ->
                deserializePass &reader None { state with teamIdentifier = reader.GetString() |> Some }
            | Some "relevantDate" ->
                deserializePass &reader None { state with relevanceDate = reader.GetDateTimeOffset() |> Some }
            | Some "labelColor" ->
                let labelColor = reader.GetString() |> Option.ofObj |> Option.map CssColor

                let newState = { state with labelColor = labelColor }
                deserializePass &reader None newState
            | Some "backgroundColor" ->
                // Might consolidate back- and foreground color code
                let backgroundColor = reader.GetString() |> Option.ofObj |> Option.map CssColor

                let newState = { state with backgroundColor = backgroundColor }
                deserializePass &reader None newState
            | Some "foregroundColor" ->
                let foregroundColor = reader.GetString() |> Option.ofObj |> Option.map CssColor

                let newState = { state with foregroundColor = foregroundColor }
                deserializePass &reader None newState
            | Some "logoText" ->
                let logoText =
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map LocalizableString.LocalizableString

                let newState = { state with logoText = logoText }
                deserializePass &reader None newState
            | Some "expirationDate" ->
                // Expiration date is optional
                let expirationDate = reader.TryGetDateTimeOffset() |> Option.fromTry
                let newState = { state with expirationDate = expirationDate }
                deserializePass &reader None newState
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePass &reader None { state with errors = error :: state.errors }
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "formatVersion" ->
                deserializePass &reader None { state with formatVersion = reader.GetInt32() |> Some }
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePass &reader None { state with errors = error :: state.errors }
        | JsonTokenType.StartObject ->
            match lastPropertyName with
            // When we start deserializing we need to advance once past the root object start
            | _ when not state.isPastRootStartObject ->
                deserializePass &reader None { state with isPastRootStartObject = true }
            | Some "barcode" ->
                match deserializeBarcode &reader None BarcodeDeserializationState.Default with
                | Result.Ok barcode -> deserializePass &reader None { state with barcode = Some barcode }
                // This maps Result<Barcode,TError> to Result<Pass.TError>. Is there a simpler way?
                | Error error -> Failed error
            | Some "eventTicket" ->
                // Would use Result.bind but cant because of reader byref :)
                let result =
                    deserializePassStructure &reader None PassStructureDeserializationState.Default

                match result with
                | DeserializationResult.Ok structure ->
                    let transformer definition = EventTicket(definition, structure) |> PassStyle.EventTicket
                    deserializePass &reader None { state with createPass = Some transformer }
                | Recovered (structure, errors) ->
                    let transformer definition = EventTicket(definition, structure) |> PassStyle.EventTicket
                    let newState = { state with errors = state.errors @ errors; createPass = Some transformer }
                    deserializePass &reader None newState
                | Failed error -> Failed error
            | Some "generic" ->
                let result =
                    deserializePassStructure &reader None PassStructureDeserializationState.Default

                match result with
                | DeserializationResult.Ok structure ->
                    let transformer definition = GenericPass(definition, structure) |> PassStyle.Generic
                    deserializePass &reader None { state with createPass = Some transformer }
                | Recovered (structure, errors) ->
                    let transformer definition = GenericPass(definition, structure) |> PassStyle.Generic
                    let newState = { state with errors = state.errors @ errors ; createPass = Some transformer}
                    deserializePass &reader None newState
                | Failed error -> Failed error
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePass &reader None { state with errors = error :: state.errors }

        | JsonTokenType.StartArray ->
            match lastPropertyName with
            | Some "barcodes" ->
                match deserializeBarcodes &reader [] with
                | Result.Ok barcodeList -> deserializePass &reader None { state with barcodes = Some barcodeList }
                | Error error -> Failed error
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePass &reader None { state with errors = error :: state.errors }
        | otherToken -> UnexpectedToken(otherToken, nameof deserializePass, lastPropertyName) |> Failed
