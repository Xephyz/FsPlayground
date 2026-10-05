#r "nuget: FSharp.Analyzers.SDK"

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

/// Detect any `System.*` open statements after non System ones
let private topologicallySortedOpenStatementsAnalyzer
    (sourceText: ISourceText)
    (untypedTree: ParsedInput)
    (checkResults: FSharpCheckFileResults)
    : Async<Message list>
    =
    async {
        let allOpenStatements =
            let openStmts = ResizeArray<string list * range>()

            let (|LongIdentAsString|) (lid: SynLongIdent) =
                lid.LongIdent
                |> List.map _.idText

            let walker =
                { new SyntaxCollectorBase() with
                    override _.WalkSynModuleSigDecl(_, decl: SynModuleSigDecl) =
                        match decl with
                        | SynModuleSigDecl.Open(
                            target = SynOpenDeclTarget.ModuleOrNamespace(
                                longId = LongIdentAsString value; range = mOpen)) ->
                            openStmts.Add(value, mOpen)
                        | _ -> ()

                    override _.WalkSynModuleDecl(_, decl: SynModuleDecl) =
                        match decl with
                        | SynModuleDecl.Open(
                            target = SynOpenDeclTarget.ModuleOrNamespace(
                                longId = LongIdentAsString value; range = mOpen)) ->
                            openStmts.Add(value, mOpen)
                        | _ -> ()
                }

            ASTCollecting.walkAst walker untypedTree

            openStmts
            |> Seq.toList

        let isSystemOpenStatement (openStatement: string list, mOpen: range) =
            let isFromBCL () =
                let line = sourceText.GetLineString (mOpen.EndLine - 1)

                match
                    checkResults.GetSymbolUseAtLocation(
                        mOpen.EndLine,
                        mOpen.EndColumn,
                        line,
                        openStatement
                    )
                with
                | Some symbolUse ->
                    match symbolUse.Symbol.Assembly.FileName with
                    | None -> false
                    | Some assemblyPath ->
                        // Probably not airtight :))
                        assemblyPath.ToLower().Contains "microsoft.netcore.app.ref"
                | _ -> false

            openStatement[0].StartsWith "System"
            && isFromBCL ()

        let nonSystemOpens =
            allOpenStatements
            |> List.skipWhile isSystemOpenStatement

        return
            nonSystemOpens
            |> List.filter isSystemOpenStatement
            |> List.map (fun (openStmt, mOpen) ->
                let openStmtText =
                    openStmt
                    |> String.concat "."

                {
                    Type = "Unsorted System open statement"
                    Message = $"%s{openStmtText} was found after non System namespaces were opened"
                    Code = "SOT-001"
                    Severity = Severity.Warning
                    Range = mOpen
                    Fixes = []
                }
            )
    }

[<CliAnalyzer "Topologically sorted open statements">]
let cliAnalyzer (ctx: CliContext) : Async<Message list> =
    topologicallySortedOpenStatementsAnalyzer
        ctx.SourceText
        ctx.ParseFileResults.ParseTree
        ctx.CheckFileResults

[<EditorAnalyzer "Topologically sorted open statements">]
let editorAnalyzer (ctx: EditorContext) : Async<Message list> =
    match ctx.CheckFileResults with
    // Editor might not have any check results for a given file.
    | None -> async.Return []
    | Some checkResults ->
        topologicallySortedOpenStatementsAnalyzer
            ctx.SourceText
            ctx.ParseFileResults.ParseTree
            checkResults
