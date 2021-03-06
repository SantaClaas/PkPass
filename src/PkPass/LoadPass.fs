module PkPass.LoadPass

open System
open System.IO.Compression
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Microsoft.JSInterop
open PkPass.Interop
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Package

type LoadPassError =
    | LoadCacheUrlError
    | LoadPassDataFromCacheError of Exception
    | DeserializationError of DeserializationError

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

        let getUrlTasks =
            requests |> Seq.map getRequestUrl

        let! urls = Task.WhenAll getUrlTasks
        return urls |> Array.map (fun request -> request.url)
    }

let guessFileNameFromUri (uri: Uri) = uri.Segments |> Array.last

let private getFileName (response: HttpResponseMessage) =
    response.Content.Headers.ContentDisposition
    |> Option.ofObj
    |> Option.map (fun header -> header.FileName)
    // Else guess from last segment
    |> Option.defaultWith (fun _ ->
        response.RequestMessage.RequestUri
        |> guessFileNameFromUri)
// 2. Load passes from urls
let private loadPassDataFromCacheUrl (client: HttpClient) (url: string) =
    task {
        try
            use! response = client.GetAsync url
            let fileName = getFileName response
            // Pass name for now is saved in
            let! stream = client.GetStreamAsync url
            let archive = new ZipArchive(stream)
            return (archive, fileName) |> AsZip |> Ok
        with
        | exception' -> return LoadPassDataFromCacheError exception' |> Error
    }

let private loadPassesDataFromCacheUrls (client: HttpClient) (urls: string array) =
    task {
        let tasks =
            Array.map (loadPassDataFromCacheUrl client) urls

        return! Task.WhenAll tasks
    }

// 3. Parse passes
// We load all the data of the passes at once (for now) instead of a more complex lazy approach that can be implemented later
let private loadPass (package: PassPackageData) : Result<PassPackage, DeserializationError> =
    let pass = getPass package

    match pass with
    | Error error -> Error error
    | Ok pass ->
        let (PassPackageData.AsZip (_, name)) =
            package

        { fileName = name
          pass = pass
          background = getBackground package
          thumbnail = getThumbnail package
          logo = getLogo package }
        |> Ok

let private loadPasses = Array.map loadPass

let completelyLoadPasses jsRuntime client =
    task {

        // 1. Load cache urls
        let! cacheUrls = loadCacheUrls jsRuntime
        // 2. Load passes from cache urls
        let! packagesDataLoadResults = cacheUrls |> loadPassesDataFromCacheUrls client

        let toPassPackage (result: Result<PassPackageData, LoadPassError>) : Result<PassPackage, LoadPassError> =
            match result with
            | Error error -> Error error
            | Ok package ->
                match loadPass package with
                | Error error -> LoadPassError.DeserializationError error |> Error
                | Ok pass -> Ok pass

        // 3. Extract data from pass files
        let passPackageResults =
            Array.map toPassPackage packagesDataLoadResults

        return passPackageResults
    }
