namespace FsLib

module Say =
    let hello name =
        printfn "Hello %s" name

    let useOptional opt =
        let result = Option.map string opt
        printfn $"Result: %A{result}"
        result
