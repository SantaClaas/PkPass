module FileSystemFileHandle

open Microsoft.JSInterop
open Module

type FileSystemFileHandle = FileSystemFileHanddle of IJSInProcessObjectReference

type File = File of IJSObjectReference