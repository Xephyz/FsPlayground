// Just checking that json (de)serialization works with F# DU types

open System.Text.Json

type Foo = {
    someInt: int option
    noneInt: int option
    a: string option
    b: string
}

let foo = {
    someInt = Some 1
    noneInt = None
    a = Some "what"
    b = ""
}

let jstr = """
{
    "someInt":1,
    "a":"hi"
}
"""

let ser = JsonSerializer.Serialize foo
printfn $"%s{ser}\n\n"

let des = JsonSerializer.Deserialize<Foo> ser
printfn $"%A{des}"

printfn $"%A{JsonSerializer.Deserialize<Foo> jstr}"
