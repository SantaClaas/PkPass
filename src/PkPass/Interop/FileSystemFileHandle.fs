namespace PkPass.Interop

open Microsoft.JSInterop

type FileSystemFileHandle = FileSystemFileHandle of IJSInProcessObjectReference
type File = File of IJSInProcessObjectReference

module FileSystemFileHandle =
    let getFile (FileSystemFileHandle handleReference) =
        task {
            let! fileReference = handleReference.InvokeAsync<IJSInProcessObjectReference> "getFile"
            return File fileReference
        }
