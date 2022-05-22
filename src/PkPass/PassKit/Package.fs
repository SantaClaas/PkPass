module PkPass.PassKit.Package

open System
open System.IO
open System.IO.Compression
open System.Text.Json
open Microsoft.FSharp.Core
open PkPass.PassKit.Deserialization

//TODO make package hold the zip archive instead of the location to avoid opening it for every operation
type PassPackage =
    | Compressed of location: string
    | Extracted of location: string

//TODO support loading from url or other sources. Might add additional dataUrl type to shift responsibility of knowing file type from consumer to producer 
type Image = Base64 of string 
type PassBackground = PassBackground of Image
type PassLogo = PassLogo of Image
type PassThumbnail = PassThumbnail of Image
let private getFileFromPackage fileName (package:PassPackage) =
    match package with
    | Compressed location ->
        use zip = ZipFile.OpenRead location 
        let entry = zip.GetEntry fileName
        use entryStream = entry.Open ()
        use memoryStream = new MemoryStream()
        entryStream.CopyTo memoryStream
        memoryStream.ToArray()
    | Extracted location ->
        let path = Path.Combine (location, fileName) 
        File.ReadAllBytes path
        
let getPass (package : PassPackage) =
    let data = getFileFromPackage "pass.json" package
    let mutable reader = Utf8JsonReader data
    deserializePass &reader None PassDeserializationState.Default

let getBackground (package: PassPackage) =
    package |> getFileFromPackage "background.png" |> Convert.ToBase64String |> Image.Base64 |> PassBackground

let getLogo (package: PassPackage) =
    package |> getFileFromPackage "logo.png" |> Convert.ToBase64String |> Image.Base64 |> PassLogo

let getThumbnail (package: PassPackage) =
    package |> getFileFromPackage "thumbnail.png" |> Convert.ToBase64String |> Image.Base64 |> PassThumbnail