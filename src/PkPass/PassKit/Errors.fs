module PkPass.PassKit.Errors

open System.Text.Json
open PkPass.Resets

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


let handleInvalidProperty (reader: Utf8JsonReader byref) (propertyName: string option) =
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