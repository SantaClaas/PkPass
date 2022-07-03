module PkPass.PassKit.Package

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Net.Http
open System.Text.Json
open Microsoft.FSharp.Core
open PkPass.PassKit.Deserialization

//TODO make package hold the zip archive instead of the location to avoid opening it for every operation
// Describes in what state the raw data of a package can be in.
// These are dumb and do not know what the pass includes or if it is even valid
type PassPackageData =
    | AsZip of ZipArchive * fileName : string
    

//TODO support loading from url or other sources. Might add additional dataUrl type to shift responsibility of knowing file type from consumer to producer
type Image = Base64 of string
type PassBackground = PassBackground of Image
type PassLogo = PassLogo of Image
type PassThumbnail = PassThumbnail of Image

// Pass package is the loaded contents of a zip pass archive folder
type PassPackage =
    { fileName: string
      pass: Pass
      background: PassBackground
      logo: PassLogo
      thumbnail: PassThumbnail }

let private getFileFromPackage fileName (package: PassPackageData) =
    let extractFromArchive (zip: ZipArchive) fileName =
        let entry = zip.GetEntry fileName
        use entryStream = entry.Open()
        use memoryStream = new MemoryStream()
        entryStream.CopyTo memoryStream
        memoryStream.ToArray()

    match package with
    | AsZip (zipArchive, _) -> extractFromArchive zipArchive fileName

let getPass (package: PassPackageData) =
    let data =
        getFileFromPackage "pass.json" package

    let mutable reader = Utf8JsonReader data
    deserializePass &reader None PassDeserializationState.Default

let getBackground (package: PassPackageData) =
    package
    |> getFileFromPackage "background.png"
    |> Convert.ToBase64String
    |> Image.Base64
    |> PassBackground

let getLogo (package: PassPackageData) =
    package
    |> getFileFromPackage "logo.png"
    |> Convert.ToBase64String
    |> Image.Base64
    |> PassLogo

let getThumbnail (package: PassPackageData) =
    package
    |> getFileFromPackage "thumbnail.png"
    |> Convert.ToBase64String
    |> Image.Base64
    |> PassThumbnail


type LoadPackageError =
    | UnexpectedError of Exception
    | UnsuccessfulResponse of HttpStatusCode
// A union to cover all known errors that can happen in the app
type AppError =
    | DeserializationError of DeserializationError
    | LoadPackageError of Exception
