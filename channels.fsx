// Turns out, F# has no built-in way to consume `IAsyncEnumerable<_>` types.
// So far only doable with this library or annoying manual extension methods
#r "nuget: FSharp.Control.TaskSeq"

open System
open System.Threading.Tasks
open System.Threading.Channels

open FSharp.Control

let basic() = task {
    let ints = Channel.CreateBounded<int>(capacity = 5)

    let producer =
        backgroundTask {
            try
                for i in 0..10000-1 do
                    do! ints.Writer.WriteAsync(i)

                ints.Writer.TryComplete() |> ignore
            with e -> ints.Writer.TryComplete(e) |> ignore
        }

    let consumer =
        backgroundTask {
            let mutable count = 0

            do!
                ints.Reader.ReadAllAsync()
                |> TaskSeq.iter (fun _ -> count <- count + 1)

            printfn $"{count}"
        }

    return! consumer
}

let complex() = task {
    let ints = Channel.CreateBounded<int>(capacity = 5)
    let strings = Channel.CreateBounded<string>(capacity = 5)

    let producer = backgroundTask {
        try
            for i in 0..10000-1 do
                do! ints.Writer.WriteAsync(i)

            ints.Writer.TryComplete() |> ignore
        with e -> ints.Writer.TryComplete(e) |> ignore
    }

    let transformer = backgroundTask {
        try
            do!
                ints.Reader.ReadAllAsync()
                |> TaskSeq.iterAsync (fun item -> task {do! strings.Writer.WriteAsync(item.ToString())})
            strings.Writer.TryComplete() |> ignore
        with e -> strings.Writer.TryComplete e |> ignore
    }

    let consumer = backgroundTask {
        let mutable count = 0
        do!
            strings.Reader.ReadAllAsync()
            |> TaskSeq.iter (fun _ -> count <- count + 1)

        printfn $"count: %d{count}"
    }

    return! consumer
}

let main _ =
    async {
        do! Async.SwitchToThreadPool()
        return! complex() |> Async.ofTask
    }
    |> Async.RunSynchronously

main ()
