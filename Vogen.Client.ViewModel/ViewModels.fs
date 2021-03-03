namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Windows
open Vogen.Client.Controls
open Vogen.Client.Model


type ProgramModel() =
    let activeComp = rp Composition.Empty
    let activeAudioLib = rp AudioLibrary.Empty

    let hScrollMax = activeComp |> Rpo.map(fun comp ->
        15360L + (comp.Utts
            |> Seq.collect(fun utt -> utt.Notes)
            |> Seq.map(fun note -> note.Off)
            |> Seq.appendItem 0L
            |> Seq.max))

    member x.ActiveComp = activeComp
    member x.ActiveAudioLib = activeAudioLib :> ReactiveProperty<_>

    member x.HScrollMax = hScrollMax

    member x.Load comp audioLib =
        activeComp |> Rp.set comp
        activeAudioLib |> Rp.set audioLib


