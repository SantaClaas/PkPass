// Extending standard result module
module Result 
let partition results =
    let folder ((values, errors)) result =
        match result with
        | Ok value -> (value :: values, errors)
        | Error error -> (values, error :: errors)

    Seq.fold folder ([], []) results
