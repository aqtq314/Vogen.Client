namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Windows
open Vogen.Client.Controls


type WorkspaceModel() =
    let activeComp = rp(None : Composition option)
    let activeCompOrNull = activeComp |> Rpo.map(Option.defaultValue Unchecked.defaultof<_>)

    member x.ActiveComp = activeComp
    member x.ActiveCompOrNull = activeCompOrNull

    member x.Load comp =
        activeComp |> Rp.set(Some comp)


