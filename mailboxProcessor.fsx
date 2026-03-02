

module Simple =
    let counter = lazy(
        MailboxProcessor.Start(fun inbox ->
            let rec loop n =
                async { do printfn $"n = %d{n}, waiting..."
                        let! msg = inbox.Receive()
                        return! loop (n+msg) }

            loop 0)
        )

    let run () =
        let counter = counter.Force()
        counter.Post(5)
        counter.Post(20)
        counter.Post(11)
        counter.Post(63)

module TwoWay =
    type msg =
        | Incr of int
        | Fetch of AsyncReplyChannel<int>

    let counter =
        MailboxProcessor.Start(fun inbox ->
            let rec loop n =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Incr x -> return! loop(n + x)
                    | Fetch replyChannel ->
                        replyChannel.Reply n
                        return! loop n
                }
            loop 0)

    let run () =
        counter.Post(Incr 8)
        counter.PostAndReply(fun rc -> Fetch rc)
        |> printfn "After Incr 8: %d"
        counter.Post(Incr 50)
        counter.PostAndReply(fun rc -> Fetch rc)
        |> printfn "After Incr 50: %d"

module Encapsulated =
    type countMsg =
        | Die
        | Incr of int
        | Fetch of AsyncReplyChannel<int>

    type counter() =
        let innerCounter =
            MailboxProcessor.Start(fun inbox ->
                let rec loop n =
                    async { let! msg = inbox.Receive()
                            match msg with
                            | Die -> return ()
                            | Incr x -> return! loop(n + x)
                            | Fetch reply ->
                                reply.Reply n;
                                return! loop n }
                loop 0)

        member this.Incr x = innerCounter.Post(Incr x)
        member this.Fetch() = innerCounter.PostAndReply((fun reply -> Fetch reply), timeout = 2000)
        member this.Die() = innerCounter.Post Die

    let run () =
        let c = counter()
        c.Incr 8
        c.Fetch() |> printfn "After incr 8: %d"
        c.Incr 50
        c.Fetch() |> printfn "After incr 50: %d"
        c.Die()
        printfn "Killed"


Encapsulated.run()
