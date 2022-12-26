module PkPass.PassKit.Package

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Text.Json
open Microsoft.FSharp.Core
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Errors
open PkPass.PassKit.Images

//TODO make package hold the zip archive instead of the location to avoid opening it for every operation
// Describes in what state the raw data of a package can be in.
// These are dumb and do not know what the pass includes or if it is even valid
type PassPackageData =
    | AsZip of ZipArchive * fileName : string
    
type FileName = FileName of string
type BoardingPassPackage = {
    fileName: FileName
    pass: BoardingPass
    images: BoardingPassImages
}
type CouponPassPackage = {
    fileName: FileName
    pass: Coupon
    images: CouponImages
}
type EventTicketPassPackage = {
    fileName: FileName
    pass: EventTicket
    images: EventTicketImages   
}
type GenericPassPackage = {
    fileName: FileName
    pass: GenericPass
    images: GenericPassImages
    
}
type StoreCardPassPackage ={
    fileName: FileName
    pass: StoreCard
    images: StoreCardImages
    
}

[<RequireQualifiedAccess>]
type PassPackage =
    | BoardingPass of BoardingPassPackage
    | Coupon of CouponPassPackage
    | EventTicket of EventTicketPassPackage
    | GenericPass of GenericPassPackage
    | StoreCard of StoreCardPassPackage

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
        deserializePass &reader)
    

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
