#r "nuget:FSharp.SystemTextJson"

open System.Text.Json
open System.Text.Json.Serialization
open System
open System.IO

module JsonElement =
    let getInt32 (element: JsonElement) = element.GetInt32()
    let getString (element: JsonElement) = element.GetString()

type PackageDefinition =
    PackageDefinition of description: string
type PackageDefinitionJsonConverter () =
     inherit  JsonConverter<PackageDefinition>()

     override this.Read(reader, typeToConvert, options) =
         match reader.TokenType with
            | JsonTokenType.Null -> 
                failwith "todo"
     override this.Write(writer, value, options) = failwith "Not yet implemented"


let options = JsonSerializerOptions();
options.Converters.Add <| PackageDefinitionJsonConverter()
use stream = File.OpenRead "manifest.json"
let o = JsonSerializer.Deserialize<PackageDefinition>(stream, options)
//type PackageDefinition =
//    | PackageDefinition of document: JsonDocument
//
//    member this.GetProperty(property: string) =
//        match this with
//        | PackageDefinition document -> document.RootElement.GetProperty property
//
//
//    member this.Description =
//        this.GetProperty "description"
//        |> JsonElement.getString
//
//    member this.FormatVersion =
//        this.GetProperty "formatVersion"
//        |> JsonElement.getString
//
//    member this.OrganizationName =
//        this.GetProperty "organizationName"
//        |> JsonElement.getString
//
//    member this.PassTypeIdentifier =
//        this.GetProperty "passTypeIdentifier"
//        |> JsonElement.getString
//
//    member this.SerialNumber =
//        this.GetProperty "serialNumber"
//        |> JsonElement.getString
//
//    member this.TeamIdentifier =
//        this.GetProperty "teamIdentifier"
//        |> JsonElement.getString
