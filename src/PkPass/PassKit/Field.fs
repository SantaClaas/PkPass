module PkPass.PassKit.Field

open System
open System.Text.Json
open PkPass.PassKit.Errors
open PkPass.PassKit.Barcode

//TODO not allowed for primary fields or back fields
type TextAlignment =
    | Left
    | Center
    | Right
    // Natural is default even though it is optional?
    | Natural

type DataDetectorType =
    | PhoneNumber
    | Link
    | Address
    | CalendarEvent

type AttributedValue =
    | HtmlAnchorTag of href: string * label: string
    | Date of DateTimeOffset
    | Number of int

type LocalizableString = LocalizableString of string
type LocalizableFormatString = LocalizableFormatString of string

module TextAlignment =
    let tryParse =
        function
        | "PKTextAlignmentLeft" -> Some TextAlignment.Left
        | "PKTextAlignmentCenter" -> Some TextAlignment.Center
        | "PKTextAlignmentRight" -> Some TextAlignment.Right
        | "PKTextAlignmentNatural" -> Some TextAlignment.Natural
        | _ -> None

[<RequireQualifiedAccess>]
type FieldValue =
    | LocalizableString of LocalizableString
    | Date of DateTimeOffset
    | Number of int

    override this.ToString() =
        match this with
        | LocalizableString (LocalizableString.LocalizableString localizableString) -> localizableString
        | Date date -> date.ToString()
        | Number number -> number.ToString()

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

let rec private deserializeFieldInternal
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: FieldDeserializationState)
    =
    if not <| reader.Read() then
        state |> tryFinishFieldDeserialization
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> state |> tryFinishFieldDeserialization
        | JsonTokenType.PropertyName -> deserializeFieldInternal &reader (reader.GetString() |> Some) state
        | JsonTokenType.String ->
            match lastPropertyName with
            | Some "key" -> deserializeFieldInternal &reader None { state with key = reader.GetString() |> Some }
            | Some "label" ->
                deserializeFieldInternal
                    &reader
                    None
                    { state with label = reader.GetString() |> LocalizableString.LocalizableString |> Some }
            | Some "value" ->
                let isDate, date = reader.TryGetDateTimeOffset()

                if isDate then
                    let value = FieldValue.Date date
                    deserializeFieldInternal &reader None { state with value = Some value }
                else
                    let value =
                        reader.GetString()
                        |> LocalizableString.LocalizableString
                        |> FieldValue.LocalizableString

                    deserializeFieldInternal &reader None { state with value = Some value }
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
                | alignment -> deserializeFieldInternal &reader None { state with textAlignment = alignment }
            | other -> handleInvalidProperty &reader other |> Error
        | JsonTokenType.Number ->
            match lastPropertyName with
            | Some "value" ->
                let value = reader.GetInt32() |> FieldValue.Number

                deserializeFieldInternal &reader None { state with value = Some value }
            | other -> handleInvalidProperty &reader other |> Error
        | otherToken ->
            UnexpectedToken(otherToken, nameof deserializeFieldInternal, lastPropertyName)
            |> Error

let private deserializeField (reader: Utf8JsonReader byref) =
    deserializeFieldInternal &reader None FieldDeserializationState.Default

let rec private deserializeFieldsInternal (reader: Utf8JsonReader byref) (resultFields: Field list) =
    if not <| reader.Read() then
        Result.Ok resultFields
    else
        match reader.TokenType with
        | JsonTokenType.EndArray ->
            resultFields
            |> List.rev
            |> Result.Ok 
        | JsonTokenType.StartObject ->
            match deserializeField &reader with
            | Result.Ok field -> deserializeFieldsInternal &reader (field :: resultFields)
            | Error error -> Error error
        | otherToken -> UnexpectedToken(otherToken, nameof deserializeBarcode, None) |> Error
        
let deserializeFields (reader: Utf8JsonReader byref) = deserializeFieldsInternal &reader []