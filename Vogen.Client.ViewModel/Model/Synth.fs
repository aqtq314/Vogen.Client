namespace Vogen.Client.Model

open Doaz.Reactive
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.IO.Compression
open System.Linq
open System.Net
open System.Net.Http
open System.Text
open System.Text.Encodings
open System.Web
open Vogen.Synth


module TimeTable =
    open Vogen.Synth.TimeTable

    let toCharGrids tChars =
        tChars
        |> Seq.filter(fun { TChar.Ch = ch } -> ch <> null)
        |> Seq.map(fun { Notes = notes; Ipa = ipa } ->
            let pitch = notes.[0].Pitch
            let phs = ipa |> Seq.map(fun { Ph = ph; On = on; Off = off } ->
                PhonemeInterval(ph, on, off)) |> Array.ofSeq
            CharGrid(pitch, phs))
        |> Array.ofSeq

    let ofUtt(utt : Utterance) =
        let allNotes = utt.Notes.ToList()
        let uttStart = (float allNotes.[0].On |> Midi.toTimeSpan utt.Bpm0) - headSil
        let uttEnd = (float allNotes.[^0].Off |> Midi.toTimeSpan utt.Bpm0) + tailSil
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
                allNotes.[i] <- note.SetOff nextNote.On

        // remove in-char sils
        let charNotes = allNotes |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen) |> Array.ofSeq
        for notes in charNotes do
            for i in 0 .. notes.Length - 2 do
                let note, nextNote = notes.[i], notes.[i + 1]
                if note.Off <> nextNote.On then
                    notes.[i] <- note.SetOff nextNote.On

        // convert to tchars
        let chars =
            charNotes
            |> Array.map(fun notes ->
                let outNotes = ImmutableList.CreateRange(notes |> Seq.map(fun note ->
                    let on  = (float note.On  |> Midi.toTimeSpan utt.Bpm0) - uttStart |> timeToFrame |> round |> int
                    let off = (float note.Off |> Midi.toTimeSpan utt.Bpm0) - uttStart |> timeToFrame |> round |> int
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


