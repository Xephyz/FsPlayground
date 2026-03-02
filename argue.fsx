// Try out how Argu works
// Tutorial from https://fsprojects.github.io/Argu/tutorial.html

#r "nuget: Argu"
open Argu

type CliArgs =
    | [<Mandatory>][<Unique>] Working_Directory of path: string
    | [<Unique>] Listener of host: string * port: uint
    | Data of base64: byte[]
    | Port of tcp_port: uint
    | Log_Level of level: uint
    | Detach

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Working_Directory _ -> "specify a working directory."
            | Listener _ -> "specify a listener (hostname : port)."
            | Data _ -> "binary data in base64 encoding."
            | Port _ -> "Specify a primary port."
            | Log_Level _ -> "Set the log level."
            | Detach -> "Detach daemon from console."

let parser = ArgumentParser.Create<CliArgs>(programName = "gadget.exe")
let results = parser.Parse (Array.tail fsi.CommandLineArgs)
