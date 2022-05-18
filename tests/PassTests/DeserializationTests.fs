module Tests

open System
open System.IO
open Xunit
open PkPass.PassKit
open FsUnit.Xunit
open FsUnit.CustomMatchers

[<Fact>]
let ``Can deserialize Cineplex pass JSON`` () =
    // Arrange
    let fileNamePath =
        Path.Combine(__SOURCE_DIRECTORY__, "VendorTestData", "Cineplex", "pass.json")

    let data: byte ReadOnlySpan =
        File.ReadAllBytes fileNamePath
    // Act
    let pass = deserializePass data
    // Assert
    pass |> should be (ofCase <@ EventTicket @>)

    let (EventTicket (PassDefinition (_, _, _, _, _, _, _, _, _, _, relevantDate), _)) =
        pass

    let date =
        DateTimeOffset(2022, 5, 8, 18, 20, 0, TimeSpan.Zero)

    relevantDate |> should equal (Some date)
