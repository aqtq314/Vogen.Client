namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Windows
open Vogen.Client.Controls


type WorkspaceModel() =
    let activeComp = rp(None : Composition option)
    let activeCompOrEmpty = activeComp |> Rpo.map(Option.defaultValue Composition.Empty)

    member x.ActiveComp = activeComp
    member x.ActiveCompOrEmpty = activeCompOrEmpty

    member x.Load comp =
        activeComp |> Rp.set(Some comp)


