/// Some kind of generic type magic to perform null coalescing
/// but also make it work on options and <c>Nullable<T></c> types
type NullCoalesce =
    static member Coalesce(a: 'a option, b: 'a Lazy) =
        match a with
        | Some a -> a
        | _ -> b.Value

    static member Coalesce(a: System.Nullable<'a>, b: 'a Lazy) =
        if a.HasValue then a.Value else b.Value

    static member Coalesce(a: 'a when 'a: null, b: 'a Lazy) =
        match a with
        | null -> b.Value
        | _ -> a

let inline nullCoalesceHelper<^t, ^a, ^b, ^c
        when (^t or ^a) : (static member Coalesce : ^a * ^b -> ^c)>
    a b =
        ((^t or ^a) : (static member Coalesce : ^a * ^b -> ^c) (a, b))

let inline (|??) a b = nullCoalesceHelper<NullCoalesce, _, _, _> a b

printfn $"Some 5 |?? 7 = %A{Some 5 |?? lazy 7}"
printfn $"None |?? 9 = %A{None |?? lazy 9}"
printfn $"null |?? 11 = %A{(null :> obj) |?? lazy (box 11)}"
