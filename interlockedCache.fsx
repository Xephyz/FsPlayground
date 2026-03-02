open System
open System.Threading

type ExpiringAccessToken = abstract ExpiresIn: TimeSpan

type DefaultAccessToken =
    { access_token: string
      expires_in: uint }

    interface ExpiringAccessToken with
        member this.ExpiresIn = TimeSpan.FromSeconds (int64 this.expires_in)

type private AccessTokenCacheEntry<'T when 'T :> ExpiringAccessToken> =
    { Contents: 'T
      CreatedAt: DateTimeOffset }

type AccessTokenCache<'T, 'Error when 'T :> ExpiringAccessToken>() =
    let mutable cacheEntry: AccessTokenCacheEntry<'T> option = None

    member _.Get() =
        cacheEntry
        |> Option.map _.Contents

    member _.Update(newVal: 'T) =
        let newValue = { Contents = newVal; CreatedAt = DateTimeOffset.UtcNow }
        Interlocked.Exchange(&cacheEntry, Some newValue) |> ignore

        cacheEntry |> Option.map _.Contents

type AppError =
    | TokenExpired of DateTimeOffset
    | NetworkError of string

let main _ =
    let cache = AccessTokenCache<DefaultAccessToken, AppError>()
    printfn $"Cache has: %A{cache.Get()}"
    let newVal = { access_token = "token"; expires_in = 3600u }
    printfn $"updating cache with: %A{newVal}"
    let updated = cache.Update newVal
    printfn $"Cache.Update returned: %A{updated}"
    printfn $"Cache has: {cache.Get()}"
    0

main ()
