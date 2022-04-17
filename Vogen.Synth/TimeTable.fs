namespace Vogen.Synth

open Doaz.Reactive
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq
open VogPackage


module TimeTable =
    let timeToFrame(timeSpan : TimeSpan) = timeSpan / hopSize
    let frameToTime(frames : float) = frames * hopSize

    [<NoComparison; ReferenceEquality>]
    type TPhoneme = {
        [<JsonProperty("ph", Required=Required.AllowNull)>] Ph : string
        [<JsonProperty("on", Required=Required.Always)>]    On : int
        [<JsonProperty("off", Required=Required.Always)>]   Off : int }

    [<NoComparison; ReferenceEquality>]
    type TNote = {
        [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
        [<JsonProperty("on", Required=Required.Always)>]    On : int
        [<JsonProperty("off", Required=Required.Always)>]   Off : int }

    [<NoComparison; ReferenceEquality>]
    type TChar = {
        [<JsonProperty("ch", Required=Required.AllowNull)>]                   Ch : string
        [<JsonProperty("rom", Required=Required.AllowNull)>]                  Rom : string
        [<JsonProperty("notes", NullValueHandling=NullValueHandling.Ignore)>] Notes : ImmutableList<TNote>
        [<JsonProperty("ipa", NullValueHandling=NullValueHandling.Ignore)>]   Ipa : ImmutableList<TPhoneme> }

    [<NoComparison; ReferenceEquality>]
    type TUtt = {
        [<JsonProperty("uttStartSec", Required=Required.Always)>] UttStartSec : float
        [<JsonProperty("uttDur", Required=Required.Always)>]      UttDur : int
        [<JsonProperty("romScheme", Required=Required.Always)>]   RomScheme : string
        [<JsonProperty("chars", Required=Required.Always)>]       Chars : ImmutableList<TChar> }

    let toCharGrids tChars =
        tChars
        |> Seq.filter(fun { TChar.Ch = ch } -> ch <> null)
        |> Seq.map(fun { Notes = notes; Ipa = ipa } ->
            let pitch = notes.[0].Pitch
            let phs = ipa |> Seq.map(fun { Ph = ph; On = on; Off = off } ->
                { FPh.Ph = ph; On = on; Off = off }) |> Array.ofSeq
            { Pitch = pitch; Phs = phs })
        |> Array.ofSeq

    let ofUtt bpm0 (utt : FUtt) =
        let allNotes = utt.Notes.ToList()
        let uttStart = (MidiClock(allNotes.[0].On) |> MidiClock.ToTimeSpan bpm0) - headSil
        let uttEnd = (MidiClock(allNotes.[^0].Off) |> MidiClock.ToTimeSpan bpm0) + tailSil
        let uttDur = uttEnd - uttStart

        // check leading hyphen note
        if allNotes.Count > 0 && allNotes.[0].IsHyphen then
            raise(ArgumentException("First note cannot be hyphen note"))

        // remove note with same onset-time
        for i in allNotes.Count - 1 .. -1 .. 1 do
            let note, prevNote = allNotes.[i], allNotes.[i - 1]
            if note.On = prevNote.On then
                allNotes.RemoveAt i

        // remove note overlapping
        for i in 0 .. allNotes.Count - 2 do
            let note, nextNote = allNotes.[i], allNotes.[i + 1]
            if note.Off > nextNote.On then
                allNotes.[i] <- { note with Dur = nextNote.On - note.On }

        // remove in-char sils
        let charNotes = allNotes |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen) |> Array.ofSeq
        for notes in charNotes do
            for i in 0 .. notes.Length - 2 do
                let note, nextNote = notes.[i], notes.[i + 1]
                if note.Off <> nextNote.On then
                    notes.[i] <- { note with Dur = nextNote.On - note.On }

        // convert to tchars
        let chars =
            charNotes
            |> Array.map(fun notes ->
                let outNotes = ImmutableList.CreateRange(notes |> Seq.map(fun note ->
                    let on  = (MidiClock(note.On)  |> MidiClock.ToTimeSpan bpm0) - uttStart |> timeToFrame |> round |> int
                    let off = (MidiClock(note.Off) |> MidiClock.ToTimeSpan bpm0) - uttStart |> timeToFrame |> round |> int
                    { Pitch = note.Pitch; On = on; Off = off }))
                let ch = notes.[0].Lyric
                let rom = notes.[0].Rom
                let ch = if String.IsNullOrEmpty ch then rom else ch
                { Ch = ch; Rom = rom; Notes = outNotes; Ipa = null })

        // insert sil between chars when needed
        let chars =
            chars
            |> Seq.partitionBetween(fun ch1 ch2 -> ch1.Notes.[^0].Off <> ch2.Notes.[0].On)
            |> Seq.join [|{ Ch = null; Rom = null; Notes = null; Ipa = null }|]
            |> Array.ofSeq

        // insert head and tail sils
        let chars = [|
            { Ch = null; Rom = null; Notes = null; Ipa = null }
            yield! chars
            { Ch = null; Rom = null; Notes = null; Ipa = null } |]

        // build utt
        let utt = {
            UttStartSec = uttStart.TotalSeconds
            UttDur = timeToFrame uttDur |> int
            RomScheme = utt.RomScheme
            Chars = ImmutableList.CreateRange chars }
        utt


