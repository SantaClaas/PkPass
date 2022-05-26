module PkPass.PassKit.Package

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Net.Http
open System.Text.Json
open Microsoft.FSharp.Core
open PkPass.PassKit
open PkPass.PassKit.Deserialization

//TODO make package hold the zip archive instead of the location to avoid opening it for every operation
type PassPackage =
    | Compressed of location: string
    | Extracted of location: string
    | InMemory of data: byte array
    | AsStream of Stream

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
    | InMemory data -> data
    | AsStream stream ->
        match stream with
        | :? MemoryStream as memoryStream -> memoryStream.ToArray()
        | _ ->
            use memoryStream = new MemoryStream()
            memoryStream.ToArray()
        
        
        
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
    

type LoadPackageError =
    | UnexpectedError of Exception
    | UnsuccessfulResponse of HttpStatusCode
// A union to cover all known errors that can happen in the app
type AppError =
    | DeserializationError of DeserializationError
    | LoadPackageError of Exception
let loadFromCache (fileName : string) (client: HttpClient) =
    task {
          // Assume service worker loads this from cache
          use! result = client.GetAsync("/files/" + fileName)
          if result.IsSuccessStatusCode then
            use! stream = result.Content.ReadAsStreamAsync()
            use fileStream = File.Open(Path.GetRandomFileName(),FileMode.OpenOrCreate )
            do! stream.CopyToAsync fileStream
            return AsStream fileStream |> Ok 
          else
              return UnsuccessfulResponse result.StatusCode |> Error
    }
