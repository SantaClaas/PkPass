module Tests

open System
open System.IO
open System.Text.Json
open PkPass
open Xunit
open PkPass.PassKit
open FsUnit.Xunit
open FsUnit.CustomMatchers

[<Fact>]
let ``Can deserialize Cineplex pass JSON`` () =
    // Arrange
    let fileNamePath =
        Path.Combine(__SOURCE_DIRECTORY__, "VendorTestData", "Cineplex", "pass.json")

    let expectedData =
        DateTimeOffset(2022, 5, 8, 18, 20, 0, TimeSpan.Zero)
        |> Some
    // Act
    let data: byte ReadOnlySpan = File.ReadAllBytes fileNamePath

    let pass = deserializePass data
    // Assert
    pass |> should be (ofCase <@ EventTicket @>)

    let (EventTicket (passDefinition, _)) = pass

    passDefinition.relevanceDate
    |> should equal expectedData
[<Fact>]
let ``Can deserialize Cineplex pass JSON with recursion`` () =
    // Arrange
    let fileNamePath =
        Path.Combine(__SOURCE_DIRECTORY__, "VendorTestData", "Cineplex", "pass.json")

    let expectedData =
        DateTimeOffset(2022, 5, 8, 18, 20, 0, TimeSpan.Zero)
        |> Some
    // Act
    let data: byte ReadOnlySpan = File.ReadAllBytes fileNamePath

    let mutable reader = Utf8JsonReader data
    let result = deserializePass' &reader None PassDeserializationState.Default
    
    // Assert
    result |> should be (ofCase <@ Result<Pass,DeserializationError>.Ok @>)