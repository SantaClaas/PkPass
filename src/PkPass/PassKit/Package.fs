module PkPass.PassKit.Package

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Text.Json
open Microsoft.FSharp.Core
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Images

//TODO make package hold the zip archive instead of the location to avoid opening it for every operation
// Describes in what state the raw data of a package can be in.
// These are dumb and do not know what the pass includes or if it is even valid
type PassPackageData =
    | AsZip of ZipArchive * fileName : string

[<RequireQualifiedAccess>]
type PassPackage =
    | BoardingPass of fileName: string * BoardingPass * BoardingPassImages
    | Coupon of fileName: string * Coupon * CouponImages
    | EventTicket of fileName: string * EventTicket * EventTicketImages
    | GenericPass of fileName: string * GenericPass * GenericPassImages
    | StoreCard of fileName: string * StoreCard * StoreCardImages

let extractFromArchive (zip: ZipArchive) fileName =
    zip.GetEntry fileName
    |> Option.ofObj
    |> Option.map (fun entry ->
        use entryStream = entry.Open()
        use memoryStream = new MemoryStream()
        entryStream.CopyTo memoryStream
        memoryStream.ToArray())
    
let private getFileFromPackage fileName (package: PassPackageData) =
    match package with
    | AsZip (zipArchive, _) -> extractFromArchive zipArchive fileName

let getPass (package: PassPackageData) =
    package
    |> getFileFromPackage "pass.json"
    |> Option.map (fun data ->
        let mutable reader = Utf8JsonReader data
        deserializePass &reader None PassDeserializationState.Default)
    

/// <summary>
/// Module containing functions for loading images from packages
/// </summary>
module Images =
    let private getImageAs transformer name package  =
        package
        |> getFileFromPackage name
        |> Option.map (Convert.ToBase64String >> Image.Base64 >> transformer)
    let getBackground = getImageAs BackgroundImage "background.png"
    let getLogo = getImageAs Logo "logo.png"
    let getThumbnail = getImageAs Thumbnail "thumbnail.png"
    let getIcon = getImageAs Icon "icon.png"
    let getStripImage = getImageAs StripImage "strip.png"
    let getFooterImage = getImageAs FooterImage "footer.png"

    /// <summary>
    /// Gets images common for every pass from the package
    /// </summary>
    /// <param name="package"></param>
    let getCommonImages package =
        (getLogo package, getIcon package)
        ||> Option.map2 (fun logo icon -> CommonImages (logo,icon))


    type LoadPackageError =
        | UnexpectedError of Exception
        | UnsuccessfulResponse of HttpStatusCode
    // A union to cover all known errors that can happen in the app
    type AppError =
        | DeserializationError of DeserializationError
        | LoadPackageError of Exception
