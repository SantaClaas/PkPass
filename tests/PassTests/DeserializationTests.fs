module Tests

open System
open System.IO
open System.Text.Json
open PkPass.PassKit.Deserialization
open PkPass.PassKit.PassStructure
open Xunit
open FsUnit.Xunit
open FsUnit.CustomMatchers

[<Fact>]
let ``Can deserialize Cineplex pass JSON with recursion`` () =
    // Arrange
    let fileNamePath =
        Path.Combine(__SOURCE_DIRECTORY__, "VendorTestData", "Cineplex", "pass.json")

    let expectedData =
        DateTimeOffset(2022, 5, 8, 18, 20, 0, TimeSpan.Zero)
        |> Some
    // Act
    let data = File.ReadAllBytes fileNamePath

    let mutable reader = Utf8JsonReader data
    let result = deserializePass &reader
    
    // Assert
    result |> should be (ofCase <@ DeserializationResult<PassStyle>.Ok @>)