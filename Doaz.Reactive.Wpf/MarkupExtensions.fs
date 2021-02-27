namespace Doaz.Reactive

open Microsoft.FSharp.Core
open Microsoft.FSharp.Reflection
open System
open System.Collections.Generic
open System.Linq
open System.Reflection
open System.Text
open System.Threading.Tasks
open System.Windows.Markup


type FSharpUnionExtension (unionType) =
    inherit MarkupExtension ()

    override x.ProvideValue serviceProvider =
        let caseInfos = FSharpType.GetUnionCases unionType
        let cases = caseInfos |> Array.map (fun caseInfo -> FSharpValue.MakeUnion (caseInfo, Array.empty))
        box cases

