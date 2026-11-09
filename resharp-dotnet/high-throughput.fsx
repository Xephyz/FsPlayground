#r "nuget: resharp"

open System

// high-throughput matching on large files
// Uses ValueMatches to avoid string allocations

let engine =
    Resharp.Regex(@"\w+", Resharp.ResharpOptions.HighThroughputDefaults)

printfn $"fully compiled DFA: %b{engine.IsFullDFA}"

// simulate large input

let input = String.replicate 1000 "hello world foo bar baz quz "

// allocation-free counting with ValueMatches
let wordCount =
    let mutable count = 0
    use slices = engine.ValueMatches input
    for _ in slices do
        count <- count + 1
    count

printfn $"found %d{wordCount} words in %d{input.Length} chars"

// only extract text for specific slices
let engine2 =
    Resharp.Regex(@"\d{4}-\d{2}-\d{2}", Resharp.ResharpOptions.HighThroughputDefaults)

let log = "event at 2024-01-15 and an other at 2024-03-22 end"
do
    use dateSlices = engine2.ValueMatches log
    for s in dateSlices do
        let text = s.GetText log // Only allocates here
        printfn $"date: %s{text} at position %d{s.Index}"
