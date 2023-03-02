open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open System.Text

[<EntryPoint>]
let build arguments =
    arguments
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    // Target.initEnvironment ()
    let directories =
        {| artifacts = Path.getFullName "../artifacts"
           source = Path.getFullName "../src" |}

    let files = {| project = Path.combine directories.source "PkPass/PkPass.fsproj" |}

    if files.project |> File.exists |> not then
        let projects = !! "../**/*.fsproj"
        let builder = StringBuilder()
        builder.Append "Could not find project file at " |> ignore
        builder.AppendLine files.project |> ignore
        builder.AppendLine "Found projects at:" |> ignore

        for project in projects do
            builder.AppendLine project |> ignore

        builder |> string |> FileNotFoundException |> raise



    // Constants for target names
    let targets =
        {| clean = "Clean"
           restore = "Restore"
           publish = "Publish"
           all = "All" |}

    Target.create targets.clean (fun _ -> !! "../src/**/bin" ++ "../src/**/obj" ++ "artifacts" |> Shell.cleanDirs)
    // Had difficulties finding the project on
    !! "../**/*.fsproj" |> Seq.iter (Trace.logfn "Found project at 👉 %s")

    let restore _ =
        !! "../**/*.fsproj" |> Seq.iter (DotNet.restore id)

    Target.create targets.restore restore

    let setParameters (options: DotNet.PublishOptions) =
        { options with
            OutputPath = Some directories.artifacts
            Configuration = DotNet.BuildConfiguration.Release }

    let runPublish _ =
        try
            DotNet.publish setParameters files.project
        with
        | exception' ->
            Trace.traceError (exception'.ToString())
            reraise ()

    Target.create targets.publish runPublish

    Target.create targets.all ignore

    targets.clean ==> targets.restore ==> targets.publish ==> targets.all |> ignore

    Target.runOrDefault targets.all
    0
