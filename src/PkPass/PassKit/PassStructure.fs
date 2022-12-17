module PkPass.PassKit.PassStructure

open System.Text.Json
open PkPass.PassKit.Errors
open PkPass.PassKit.Field

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


type PassStructureDeserializationState =
    { passStructure: PassStructure
      errors: DeserializationError list }

    static member Default =
        { passStructure = PassStructure.Default
          errors = [] }

let private completePassStructureDeserialization state =
    match state with
    | { errors = []
        passStructure = passStructure } -> DeserializationResult.Ok passStructure
    | _ -> DeserializationResult.Recovered(state.passStructure, state.errors)

let rec private deserializePassStructureInternal
    (reader: Utf8JsonReader byref)
    (lastPropertyName: string option)
    (state: PassStructureDeserializationState)
    =
    if not <| reader.Read() then
        completePassStructureDeserialization state
    else
        match reader.TokenType with
        | JsonTokenType.EndObject -> completePassStructureDeserialization state
        | JsonTokenType.PropertyName -> deserializePassStructureInternal &reader (reader.GetString() |> Some) state
        | JsonTokenType.StartArray ->
            // I feel like I can reduce duplicate code here because the only thing that differs between cases is applying the result
            // to the state
            match lastPropertyName with
            // Assume if it has the property it has to have a value
            | Some "headerFields" ->
                match deserializeFields &reader with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with headerFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructureInternal &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "primaryFields" ->
                match deserializeFields &reader with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with primaryFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructureInternal &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "secondaryFields" ->
                match deserializeFields &reader with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with secondaryFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructureInternal &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "backFields" ->
                match deserializeFields &reader with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with backFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructureInternal &reader None newState
                | Error error -> DeserializationResult.Failed error
            | Some "auxiliaryFields" ->
                match deserializeFields &reader with
                | Result.Ok fieldList ->
                    let newPassStructure = { state.passStructure with auxiliaryFields = Some fieldList }
                    let newState = { state with passStructure = newPassStructure }
                    deserializePassStructureInternal &reader None newState
                | Error error -> DeserializationResult.Failed error
            | name ->
                let error = handleInvalidProperty &reader name
                let newState = { state with errors = error :: state.errors }
                deserializePassStructureInternal &reader None newState

        | otherToken ->
            let error =
                UnexpectedToken(otherToken, nameof deserializePassStructureInternal, lastPropertyName)

            let newState = { state with errors = error :: state.errors }
            deserializePassStructureInternal &reader None newState

let deserializePassStructure (reader: Utf8JsonReader byref) =
    deserializePassStructureInternal &reader None PassStructureDeserializationState.Default
