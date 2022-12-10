/// <summary>
/// Commonly used useful extensions to the base library
/// </summary>
module PkPass.Extensions

    module Option =
        let fromTry = function
            | true, value -> Some value
            | false, _ -> None