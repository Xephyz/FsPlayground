#i "/Users/xephyz/dev/Playground/NuGetPackages"
#r "nuget: Moq"

open Moq

type IThing = abstract Foo: unit -> bool
let m = Mock<IThing>()
m.Setup(fun x -> x.Foo()).Returns(true)
printfn $"{m.Object.Foo()}"
