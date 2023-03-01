module PkPass.PassKit.Deserialization

open System
open System.Text.Json
open PkPass
open PkPass.PassKit.Errors
open PkPass.PassKit.Field
open PkPass.PassKit.PassStructure
open PkPass.PassKit.Barcode
open Extensions

/// <summary>
/// A string in the CSS style like "rgb(0,12,255)". Could be used in CSS variables.
/// </summary>
type CssColor = CssColor of string

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

/// <summary>
/// Represents an unfinished collection of values that can be contained in a pass.
/// This represents the pass before it ran through validation
/// </summary>
type private PassDeserializationState =
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


let rec private deserializePassInternal
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
        | JsonTokenType.PropertyName -> deserializePassInternal &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            // Pass none as last property name to last property name
            match lastPropertyName with
            | Some "description" ->
                deserializePassInternal
                    &reader
                    None
                    { state with description = reader.GetString() |> LocalizableString.LocalizableString |> Some }
            | Some "organizationName" ->
                deserializePassInternal
                    &reader
                    None
                    { state with organizationName = reader.GetString() |> LocalizableString.LocalizableString |> Some }
            | Some "passTypeIdentifier" ->
                deserializePassInternal &reader None { state with passTypeIdentifier = reader.GetString() |> Some }
            | Some "serialNumber" ->
                deserializePassInternal &reader None { state with serialNumber = reader.GetString() |> Some }
            | Some "teamIdentifier" ->
                deserializePassInternal &reader None { state with teamIdentifier = reader.GetString() |> Some }
            | Some "relevantDate" ->
                deserializePassInternal &reader None { state with relevanceDate = reader.GetDateTimeOffset() |> Some }
            | Some "labelColor" ->
                let labelColor = reader.GetString() |> Option.ofObj |> Option.map CssColor

                let newState = { state with labelColor = labelColor }
                deserializePassInternal &reader None newState
            | Some "backgroundColor" ->
                // Might consolidate back- and foreground color code
                let backgroundColor = reader.GetString() |> Option.ofObj |> Option.map CssColor

                let newState = { state with backgroundColor = backgroundColor }
                deserializePassInternal &reader None newState
            | Some "foregroundColor" ->
                let foregroundColor = reader.GetString() |> Option.ofObj |> Option.map CssColor

                let newState = { state with foregroundColor = foregroundColor }
                deserializePassInternal &reader None newState
            | Some "logoText" ->
                let logoText =
                    reader.GetString()
                    |> Option.ofObj
                    |> Option.map LocalizableString.LocalizableString

                let newState = { state with logoText = logoText }
                deserializePassInternal &reader None newState
            | Some "expirationDate" ->
                // Expiration date is optional
                let expirationDate = reader.TryGetDateTimeOffset() |> Option.fromTry
                let newState = { state with expirationDate = expirationDate }
                deserializePassInternal &reader None newState
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePassInternal &reader None { state with errors = error :: state.errors }
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "formatVersion" ->
                deserializePassInternal &reader None { state with formatVersion = reader.GetInt32() |> Some }
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePassInternal &reader None { state with errors = error :: state.errors }
        | JsonTokenType.StartObject ->
            match lastPropertyName with
            // When we start deserializing we need to advance once past the root object start
            | _ when not state.isPastRootStartObject ->
                deserializePassInternal &reader None { state with isPastRootStartObject = true }
            | Some "barcode" ->
                match deserializeBarcode &reader with
                | Result.Ok barcode -> deserializePassInternal &reader None { state with barcode = Some barcode }
                // This maps Result<Barcode,TError> to Result<Pass.TError>. Is there a simpler way?
                | Error error -> Failed error
            | Some "eventTicket" ->
                // Would use Result.bind but cant because of reader byref :)
                let result =
                    deserializePassStructure &reader

                match result with
                | DeserializationResult.Ok structure ->
                    let transformer definition = EventTicket(definition, structure) |> PassStyle.EventTicket
                    deserializePassInternal &reader None { state with createPass = Some transformer }
                | Recovered (structure, errors) ->
                    let transformer definition = EventTicket(definition, structure) |> PassStyle.EventTicket
                    let newState = { state with errors = state.errors @ errors; createPass = Some transformer }
                    deserializePassInternal &reader None newState
                | Failed error -> Failed error
            | Some "generic" ->
                let result =
                    deserializePassStructure &reader

                match result with
                | DeserializationResult.Ok structure ->
                    let transformer definition = GenericPass(definition, structure) |> PassStyle.Generic
                    deserializePassInternal &reader None { state with createPass = Some transformer }
                | Recovered (structure, errors) ->
                    let transformer definition = GenericPass(definition, structure) |> PassStyle.Generic
                    let newState = { state with errors = state.errors @ errors ; createPass = Some transformer}
                    deserializePassInternal &reader None newState
                | Failed error -> Failed error
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePassInternal &reader None { state with errors = error :: state.errors }

        | JsonTokenType.StartArray ->
            match lastPropertyName with
            | Some "barcodes" ->
                match deserializeBarcodes &reader with
                | Result.Ok barcodeList -> deserializePassInternal &reader None { state with barcodes = Some barcodeList }
                | Error error -> Failed error
            | other ->
                let error = handleInvalidProperty &reader other
                deserializePassInternal &reader None { state with errors = error :: state.errors }
        | otherToken -> UnexpectedToken(otherToken, nameof deserializePassInternal, lastPropertyName) |> Failed

let deserializePass (reader: Utf8JsonReader byref) = deserializePassInternal &reader None PassDeserializationState.Default