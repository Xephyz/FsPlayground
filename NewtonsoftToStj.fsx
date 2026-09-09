

#r "nuget: Newtonsoft.Json"

type NewtonsoftToStj<'T>() =
    inherit Newtonsoft.Json.JsonConverter()

    override _.CanConvert t =
        t = typeof<'T>

    override _.WriteJson (writer: Newtonsoft.Json.JsonWriter, value: obj, _) =
        value :?> 'T
        |> System.Text.Json.JsonSerializer.Serialize
        |> Newtonsoft.Json.Linq.JRaw
        |> fun jraw -> jraw.WriteTo writer

    override _.ReadJson (reader: Newtonsoft.Json.JsonReader, _, _, _) =
        match reader.TokenType with
        | Newtonsoft.Json.JsonToken.Null -> null
        | _ ->
            Newtonsoft.Json.Linq.JToken.ReadFrom reader
            |> _.ToString()
            |> System.Text.Json.JsonSerializer.Deserialize<'T>
            |> box

[<Newtonsoft.Json.JsonConverter(typeof<NewtonsoftToStj<Foo>>)>]
type Foo = {
    x: int
    y: int option
    z: string option
}

let foo = {x=1;y=None;z=Some "hi"}

let stjSerialized = System.Text.Json.JsonSerializer.Serialize foo
let nSerialized = Newtonsoft.Json.JsonConvert.SerializeObject foo

printfn "== SERIALIZATION =="
printfn $"STJ:\n%s{stjSerialized}\n"
printfn $"Newtonsoft using STJ:\n%s{nSerialized}\n"

let fooStr = """{"x":1,"y":2,"z":"hi"}"""

let stjDe = System.Text.Json.JsonSerializer.Deserialize<Foo> fooStr
let nDe = Newtonsoft.Json.JsonConvert.DeserializeObject<Foo>(fooStr)

printfn "== DESERIALIZATION =="
printfn $"STJ:\n{stjDe}\n"
printfn $"Newt:\n{nDe}"
