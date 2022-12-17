module PkPass.LoadPass

open System
open System.IO.Compression
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.FSharp.Core
open Microsoft.JSInterop
open PkPass.Interop
open PkPass.Extensions
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Images
open PkPass.PassKit.Package
open PkPass.PassKit.Package.Images

type RequiredImageMissingError = RequiredImageMissingError of imageName: string

type LoadPassError =
    | LoadCacheUrlError
    | LoadPassDataFromCacheError of Exception
    | RequiredImageMissing of RequiredImageMissingError
    | RequiredImagesMissing of imageNames: string list
    /// <summary>
    /// The deserialization failed while loading the pass
    /// </summary>
    | DeserializationError of DeserializationError
    /// <summary>
    /// Error because ticket is an event ticket and has a strip image specified but also a background or thumbnail which
    /// is not allowed.
    /// https://developer.apple.com/library/archive/documentation/UserExperience/Conceptual/PassKit_PG/Creating.html
    /// </summary>
    | EventTicketWithInvalidImages
    /// <summary>
    /// The pass.json file could not be loaded probably because it does not exist
    /// </summary>
    | NoPassJsonFile

module LoadPassError =
    // None -> ImageRequiredError, Some -> Ok
    let requireImage<'TImage> (option: 'TImage option) =
        match option with
        | Some image -> Result.Ok image
        | None -> RequiredImageMissingError nameof<'TImage> |> Error

// Load passes
// 1. Load cache urls
// Inefficient way to get a property from a JS object reference
let private getProperty<'T> (reference: IJSObjectReference) (properties: string array) (jsRuntime: IJSRuntime) =
    task {
        let! asString = jsRuntime.InvokeAsync<string>("JSON.stringify", reference, properties)
        return JsonSerializer.Deserialize<'T>(asString)
    }

let private loadCacheUrls jsRuntime =
    task {
        let! cache = jsRuntime |> CacheStorage.open' "files"

        let! requests = Cache.getKeys cache

        let getRequestUrl request =
            match request with
            | Request reference -> getProperty<{| url: string |}> reference [| "url" |] jsRuntime

        let getUrlTasks = requests |> Seq.map getRequestUrl

        let! urls = Task.WhenAll getUrlTasks
        return urls |> Array.map (fun request -> request.url)
    }

let guessFileNameFromUri (uri: Uri) = uri.Segments |> Array.last

let private getFileName (response: HttpResponseMessage) =
    response.Content.Headers.ContentDisposition
    |> Option.ofObj
    |> Option.map (fun header -> header.FileName)
    // Else guess from last segment
    |> Option.defaultWith (fun _ -> response.RequestMessage.RequestUri |> guessFileNameFromUri)
// 2. Load passes from urls
let private loadPassDataFromCacheUrl (client: HttpClient) (url: string) =
    task {
        try
            use! response = client.GetAsync url
            let fileName = getFileName response
            // Pass name for now is saved in
            let! stream = client.GetStreamAsync url
            let archive = new ZipArchive(stream)
            return (archive, fileName) |> AsZip |> Result.Ok
        with
        | exception' -> return LoadPassDataFromCacheError exception' |> Error
    }

let private loadPassesDataFromCacheUrls (client: HttpClient) (urls: string array) =
    task {
        let tasks = Array.map (loadPassDataFromCacheUrl client) urls

        return! Task.WhenAll tasks
    }


// 3. Parse passes
// We load all the data of the passes at once (for now) instead of a more complex lazy approach that can be implemented later
let private loadPassPackage (package: PassPackageData) : Result<PassPackage, LoadPassError> =
    let (PassPackageData.AsZip (_, name)) = package
    let commonImagesResult = getCommonImages package |> LoadPassError.requireImage
    /// <summary>
    /// Helper function to load common images and pass specific images
    /// </summary>
    /// <param name="mapping">To map to the pass specific images type</param>
    /// <param name="getOtherImage">A function to get other images that are needed for the pass specific images</param>
    let loadImages mapping getOtherImage =
        match (getOtherImage package |> LoadPassError.requireImage, commonImagesResult) with
        | Result.Ok otherImage, Result.Ok commonImages -> (commonImages, otherImage) |> mapping |> Result.Ok
        | Error (RequiredImageMissingError commonImages), Error (RequiredImageMissingError otherImage) ->
            [ commonImages; otherImage ] |> LoadPassError.RequiredImagesMissing |> Error
        | Error imageMissing, Result.Ok _
        | Result.Ok _, Error imageMissing -> imageMissing |> LoadPassError.RequiredImageMissing |> Error

    let constructPassPackage pass =
        match pass with
        | PassStyle.BoardingPass boardingPass ->
            loadImages BoardingPassImages getFooterImage
            |> Result.map (fun images -> PassPackage.BoardingPass(name, boardingPass, images))
        | PassStyle.Coupon coupon ->
            loadImages CouponImages getStripImage
            |> Result.map (fun images -> PassPackage.Coupon(name, coupon, images))
        | PassStyle.EventTicket eventTicket ->
            // Event ticket image loading is a bit more complicated since it has extra requirements
            match getStripImage package, getBackground package, getThumbnail package with
            // The cases where there are too many images for event ticket
            | Some _, _, Some _
            | Some _, Some _, Some _
            | Some _, Some _, _ -> LoadPassError.EventTicketWithInvalidImages |> Error
            // Only strip image
            | Some stripImage, _, _ -> EventTicketImageOption.StripImage stripImage |> Result.Ok
            // Only the other images 
            | _, Some background, Some thumbnail -> EventTicketImageOption.Other(background, thumbnail) |> Result.Ok
            // Images are missing or none are provided
            | _, Some _, _ ->
                nameof (Thumbnail)
                |> RequiredImageMissingError.RequiredImageMissingError
                |> LoadPassError.RequiredImageMissing
                |> Error
            | _, _, Some _ ->
                nameof (BackgroundImage)
                |> RequiredImageMissingError
                |> LoadPassError.RequiredImageMissing
                |> Error
            | _, _, _ ->
                [ nameof (StripImage); nameof (BackgroundImage); nameof (Thumbnail) ]
                |> RequiredImagesMissing
                |> Error
            |> Result.bind (fun images ->
                match commonImagesResult with
                | Error requiredImageMissingError ->
                    requiredImageMissingError |> LoadPassError.RequiredImageMissing |> Error
                | Result.Ok commonImages ->
                    let ticketImages = EventTicketImages(commonImages, images)
                    PassPackage.EventTicket(name, eventTicket, ticketImages) |> Result.Ok)
        | PassStyle.Generic genericPass ->
            loadImages GenericPassImages getThumbnail
            |> Result.map (fun images -> PassPackage.GenericPass(name, genericPass, images))
        | PassStyle.StoreCard storeCard -> 
            loadImages StoreCardImages getStripImage
            |> Result.map (fun images -> PassPackage.StoreCard(name, storeCard, images))
            
    match getPass package with
    | Some (Recovered (pass, errors)) ->
        //TODO remove temporary debug side effect
        let builder = StringBuilder()

        builder.AppendLine "Encountered the following errors but could recover"
        |> ignore

        for error in errors do
            builder.AppendLine $"Encountered error {error}" |> ignore

        Console.WriteLine(builder.ToString())
        
        constructPassPackage pass
    | Some (Ok pass) -> constructPassPackage pass
    | Some (Failed error) -> LoadPassError.DeserializationError error |> Error
    | None -> LoadPassError.NoPassJsonFile |> Error
        
let completelyLoadPasses jsRuntime client =
    task {

        // 1. Load cache urls
        let! cacheUrls = loadCacheUrls jsRuntime
        // 2. Load passes from cache urls
        let! packagesDataLoadResults = cacheUrls |> loadPassesDataFromCacheUrls client

        let toPassPackage = Result.bind loadPassPackage

        // 3. Extract data from pass files
        return packagesDataLoadResults |> Array.map toPassPackage 
    }
