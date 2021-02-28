namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Windows
open Vogen.Client.JsonModels


[<AutoOpen>]
module Utils =
    let inline ensureSorted projection (itemsList : MutableReactiveList<_>) =
        // assuming itemsList is in final unlocking stage
        for i in 1 .. itemsList.Count - 1 do
            if projection itemsList.[i - 1] > projection itemsList.[i] then
                seq { i - 1 .. -1 .. 1 }
                |> Seq.tryFind(fun j -> projection itemsList.[j - 1] <= projection itemsList.[i])
                |> Option.defaultValue 0
                |> itemsList.Move i

type NoteType =
    | NTNote
    | NTLegato

type Note(pitch, lyric, rom, startTime, duration) =
    let pitch = rp(pitch : int)
    let lyric = rp(lyric : string)
    let rom = rp(rom : string)
    let startTime = rp(startTime : int)
    let duration = rp(duration : int)

    let hasOverlap = rp false

    let isHyphen = lyric |> Rpo.map((=) "-")
    let endTime = (startTime, duration) |> Rpo.map2((+))

    member x.Pitch = pitch
    member x.Lyric = lyric
    member x.Rom = rom
    member x.StartTime = startTime
    member x.Duration = duration

    member x.HasOverlap = hasOverlap

    member x.IsHyphen = isHyphen
    member x.EndTime = endTime

type Utterance(notes) =
    let notes = rl(notes |> Seq.sortBy(fun (n : Note) -> !!n.StartTime))

    let startTime = notes |> Rpo.aggregate(fun n -> n.StartTime) Seq.head
    let endTime = notes |> Rpo.aggregate(fun n -> n.EndTime) Seq.max
    let duration = (startTime, endTime) |> Rpo.map2(fun startTime endTime -> endTime - startTime)

    let checkOverlap() =
        use setter = Rp.bulkSetter()
        for n1, n2 in Seq.pairwise notes do
            n1.HasOverlap |> setter.LockSetPropIfDiff(!!n1.EndTime > !!n2.StartTime)
        if notes.Count > 0 then
            notes.[^0].HasOverlap |> setter.LockSetPropIfDiff false

    do notes.Subscribe(
        let ops = List()
        ListSubscriber.leaf ops (fun () ->
            notes |> ensureSorted(fun n -> !!n.StartTime)
            checkOverlap()
            ops.Clear()))

    member x.Notes = notes

    member x.StartTime = startTime
    member x.EndTime = endTime
    member x.Duration = duration

    static member ofJsonModel jUtt =
        let { Chars = chars } = jUtt
        Utterance(chars |> Seq.collect(fun jCh ->
            let { Ch = chSym; Rom = rom; Ipa = phs; Notes = notes } = jCh
            if chSym <> null then
                notes |> Seq.mapi(fun i jN ->
                    let { Pitch = pitch; On = on; Off = off } = jN
                    let chSym, rom = if i = 0 then chSym, rom else "-", ""
                    Note(pitch, chSym, rom, on, off - on))
            else Seq.empty))

type Composition(utts) =
    let utts = rl(utts |> Seq.sortBy(fun (utt : Utterance) -> !!utt.StartTime))

    let startTime = utts |> Rpo.aggregate(fun utt -> utt.StartTime) Seq.head
    let endTime = utts |> Rpo.aggregate(fun utt -> utt.EndTime) Seq.max
    let duration = (startTime, endTime) |> Rpo.map2(fun startTime endTime -> endTime - startTime)

    member x.Utts = utts

    member x.StartTime = startTime
    member x.EndTime = endTime
    member x.Duration = duration

    static member ofJsonModel jComp =
        let { Utts = utts } = jComp
        Composition(Seq.map Utterance.ofJsonModel utts)

type ProgramModel() =
    let activeComp = rp(None : Composition option)
    let activeCompOrNull = activeComp |> Rpo.map(Option.defaultValue Unchecked.defaultof<_>)

    member x.ActiveComp = activeComp
    member x.ActiveCompOrNull = activeCompOrNull

    member x.LoadFromJson jComp =
        let comp = Composition.ofJsonModel jComp
        activeComp |> Rp.set(Some comp)


