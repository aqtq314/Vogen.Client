module Vogen.Client.ViewModel.DesignerModels

open Doaz.Reactive
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Reflection
open System.Windows
open Vogen.Client.Controls
open Vogen.Client.Model


let comp, uttSynthCache =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream @"Vogen.Client.ViewModel.testComp.vog"
    FilePackage.read stream

let programModel = ProgramModel()
do  programModel.ActiveChart |> Rp.set(ChartState(comp, Some comp.Utts.[0], ImmutableHashSet.CreateRange [|
        yield! Seq.take 3 comp.Utts.[0].Notes
        yield! Seq.take 1 comp.Utts.[1].Notes |]))
    programModel.ActiveUttSynthCache |> Rp.set uttSynthCache
    programModel.CompIsSaved |> Rp.set false
    programModel.ManualSetCursorPos 1920L


