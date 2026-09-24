// Comparing failure output of Swensen.Unquote vs FsUnit.Xunit (and raw xUnit Assert).
// Motivation: `test <@ actual = expected @>` often prints a big reduction trace that ends in
// "Expected: True / Actual: False", which tells you nothing about *where* the values differ.
//
// Run with: dotnet fsi assertionOutput.fsx

#r "nuget: FsUnit.xUnit"
#r "nuget: Unquote"

open Swensen.Unquote
open FsUnit.Xunit

let private line = String.replicate 100 "-"

/// Runs an assertion that is expected to fail and prints whatever the framework produced.
let show (label: string) (assertion: unit -> unit) =
    printfn "%s" line
    printfn "## %s" label
    printfn "%s" line
    try
        assertion ()
        printfn "(no failure - assertion passed)"
    with e ->
        printfn "%s" (e.Message.TrimEnd())
    printfn ""

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

type Address = { Street: string; ZipCode: string; Country: string }

type Claim =
    | Nnin of string
    | Name of first: string * last: string
    | Age of int

type User =
    { Id: int
      Email: string
      Address: Address
      Claims: Claim list
      Nickname: string option }

let expectedUser =
    { Id = 42
      Email = "anders@idura.eu"
      Address = { Street = "Hovedgaden 1"; ZipCode = "2100"; Country = "DK" }
      Claims = [ Nnin "0101801234"; Name("Anders", "Michaelsen"); Age 45 ]
      Nickname = Some "xephyz" }

// One single nested field differs (ZipCode) plus one list element (Age).
let actualUser =
    { expectedUser with
        Address = { expectedUser.Address with ZipCode = "2200" }
        Claims = [ Nnin "0101801234"; Name("Anders", "Michaelsen"); Age 46 ] }

// ---------------------------------------------------------------------------
// 1. Plain ints
// ---------------------------------------------------------------------------

show "ints - Unquote: test <@ actual = expected @>" (fun () ->
    let actual = 1 + 1
    let expected = 3
    test <@ actual = expected @>)

show "ints - Unquote: actual =! expected" (fun () ->
    let actual = 1 + 1
    let expected = 3
    actual =! expected)

show "ints - FsUnit: actual |> should equal expected" (fun () ->
    let actual = 1 + 1
    let expected = 3
    actual |> should equal expected)

// ---------------------------------------------------------------------------
// 2. Strings (long-ish, differ in the middle)
// ---------------------------------------------------------------------------

let expectedStr = "https://idp.idura.eu/oauth2/authorize?client_id=urn:my:app&acr_values=urn:grn:authn:dk:mitid:substantial"
let actualStr   = "https://idp.idura.eu/oauth2/authorize?client_id=urn:my:app&acr_values=urn:grn:authn:dk:mitid:low"

show "strings - Unquote: test <@ actual = expected @>" (fun () ->
    test <@ actualStr = expectedStr @>)

show "strings - FsUnit: actual |> should equal expected" (fun () ->
    actualStr |> should equal expectedStr)

show "strings - xUnit: Assert.Equal" (fun () ->
    Xunit.Assert.Equal(expectedStr, actualStr))

// ---------------------------------------------------------------------------
// 3. Records (nested difference)
// ---------------------------------------------------------------------------

show "records - Unquote: test <@ actual = expected @>" (fun () ->
    test <@ actualUser = expectedUser @>)

show "records - Unquote: actual =! expected" (fun () ->
    actualUser =! expectedUser)

show "records - FsUnit: actual |> should equal expected" (fun () ->
    actualUser |> should equal expectedUser)

// ---------------------------------------------------------------------------
// 4. Lists / collections
// ---------------------------------------------------------------------------

let expectedList = [ 1; 2; 3; 4; 5 ]
let actualList   = [ 1; 2; 9; 4; 5 ]

show "lists - Unquote: test <@ actual = expected @>" (fun () ->
    test <@ actualList = expectedList @>)

show "lists - FsUnit: actual |> should equal expected" (fun () ->
    actualList |> should equal expectedList)

show "lists - FsUnit: actual |> should equalSeq expected" (fun () ->
    actualList |> should equalSeq expectedList)

// ---------------------------------------------------------------------------
// 5. Options / DUs
// ---------------------------------------------------------------------------

show "option - Unquote: test <@ actual = expected @>" (fun () ->
    let actual: string option = None
    test <@ actual = Some "xephyz" @>)

show "option - FsUnit: actual |> should equal expected" (fun () ->
    let actual: string option = None
    actual |> should equal (Some "xephyz"))

show "DU - Unquote: test <@ actual = expected @>" (fun () ->
    test <@ Age 46 = Age 45 @>)

show "DU - FsUnit: actual |> should equal expected" (fun () ->
    Age 46 |> should equal (Age 45))

// ---------------------------------------------------------------------------
// 6. The classic trap: a predicate inside the quotation
//    Unquote reduces the whole expression; if the final step is a bool you get
//    "Expected: True / Actual: False" with no values at all.
// ---------------------------------------------------------------------------

show "predicate - Unquote: test <@ List.forall ... @>" (fun () ->
    test <@ actualList |> List.forall (fun x -> x < 5) @>)

show "predicate - FsUnit: should be True" (fun () ->
    actualList |> List.forall (fun x -> x < 5) |> should be True)

// Better: keep the comparison *inside* the quotation so Unquote has values to show.
show "predicate - Unquote: filtered values compared instead of a bool" (fun () ->
    test <@ actualList |> List.filter (fun x -> x >= 5) = [] @>)

// ---------------------------------------------------------------------------
// 7. Function call on the left-hand side - Unquote's actual strength:
//    it shows every reduction step, so you see the intermediate values.
// ---------------------------------------------------------------------------

let normalizeAcr (s: string) = s.Split(':') |> Array.last |> _.ToUpperInvariant()

show "reduction - Unquote: test <@ f x = expected @>" (fun () ->
    test <@ normalizeAcr "urn:grn:authn:dk:mitid:low" = "SUBSTANTIAL" @>)

show "reduction - FsUnit: f x |> should equal expected" (fun () ->
    normalizeAcr "urn:grn:authn:dk:mitid:low" |> should equal "SUBSTANTIAL")

// ---------------------------------------------------------------------------
// 8. Type mismatch that compares unequal (int vs boxed int etc.)
// ---------------------------------------------------------------------------

show "obj - FsUnit: boxed values" (fun () ->
    let actual: obj = box 1
    let expected: obj = box "1"
    actual |> should equal expected)

show "obj - Unquote: boxed values" (fun () ->
    let actual: obj = box 1
    let expected: obj = box "1"
    test <@ actual = expected @>)

printfn "%s" line
printfn "Done."

// ---------------------------------------------------------------------------
// FINDINGS (dotnet 10.0.401, FsUnit.xUnit 7.x, Unquote 7.x, xunit.assert 2.9.x)
// ---------------------------------------------------------------------------
//
// 1. Unquote does NOT actually say "expected True, got False" for `test <@ a = b @>`.
//    It prints the reduction steps and the final `false`:
//
//        actual = expected
//        2 = 3
//        false
//
//    The "useless True/False" feeling comes from two places:
//      a) the last line is always a bare `false`, and
//      b) when the compared values are big, step 2 dumps both values into ONE line,
//         so the split between actual and expected is buried mid-line:
//             ... Nickname = Some "xephyz" } = { Id = 42 ...
//         That `} = {` is the only separator. That is the unreadable part.
//
// 2. FsUnit's `should equal` delegates to xUnit's Assert.Equal with a constraint object,
//    so you get the standard two-block layout with aligned indentation:
//
//        Assert.Equal() Failure: Values differ
//        Expected: Equals { Id = 42
//                    ... }
//        Actual:   { Id = 42
//                    ... }
//
//    For records / DUs / lists this is clearly easier to scan than Unquote, because
//    actual and expected never share a line. Neither tool does a structural diff though -
//    you still eyeball which field differs.
//
// 3. Unquote wins when the left side is a computation. It shows intermediates:
//        normalizeAcr "urn:grn:authn:dk:mitid:low" = "SUBSTANTIAL"
//        "LOW" = "SUBSTANTIAL"
//    FsUnit only shows the final "LOW" vs "SUBSTANTIAL", not the input that produced it.
//
// 4. Real "expected True / got False" comes from asserting on a bool:
//        FsUnit:  ... |> should be True   ->  Expected: True / Actual: false
//        Unquote: test <@ xs |> List.forall p @>  ->  prints the list, then `false`
//    Fix in either framework: never assert a bare predicate. Compare values instead,
//    e.g. `test <@ xs |> List.filter (not << p) = [] @>` which prints `[9; 5] = []`.
//
// 5. Best string failures come from raw xUnit `Assert.Equal(expected, actual)` on
//    string-typed args - it prints a caret at the first differing position:
//                                            ↓ (pos 93)
//        Expected: ···"values=urn:grn:authn:dk:mitid:substantial"
//        Actual:   ···"app&acr_values=urn:grn:authn:dk:mitid:low"
//    FsUnit's `should equal` boxes to obj and loses this. Unquote never had it.
//
// 6. Gotcha: `None |> should equal (Some "x")` reports Actual as `<null>` (boxed None).
//    Unquote prints `None` correctly.
//
// RULE OF THUMB
//   - comparing structured values (records, DUs, lists)  -> FsUnit `should equal`
//   - comparing strings                                  -> xUnit `Assert.Equal(exp, act)`
//   - asserting the result of a pipeline/function chain  -> Unquote `=!`
//   - never assert a bare bool in either framework
