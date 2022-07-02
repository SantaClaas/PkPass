namespace PkPass.Interop

open Microsoft.JSInterop
type FileSystemFileHandle = FileSystemFileHandle of IJSInProcessObjectReference
type File = File of IJSObjectReference
