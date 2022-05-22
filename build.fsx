#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

// Target.initEnvironment ()
let directories =
    {| artifacts = "./artifacts"
       source = "./src" |}

let files =
    {| project = $"{directories.source}/PkPass/PkPass.fsproj" |}

// Constants for target names
let targets =
    {| clean = "Clean"
       publish = "Publish"
       all = "All" |}

Target.create targets.clean (fun _ -> !! "src/**/bin" ++ "src/**/obj" ++ "artifacts" |> Shell.cleanDirs)

let setParameters (options: DotNet.PublishOptions) =
    { options with OutputPath = Some directories.artifacts }
let runPublish _ =
    try
        DotNet.publish setParameters files.project
    with
    | exception' ->
        Trace.traceError (exception'.ToString())
        reraise()
        
Target.create targets.publish runPublish

Target.create targets.all ignore

targets.clean ==> targets.publish ==> targets.all

Target.runOrDefault targets.all
