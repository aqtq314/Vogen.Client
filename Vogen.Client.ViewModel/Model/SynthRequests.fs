﻿namespace Vogen.Client.Model

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
type TUtterance = {
    [<JsonProperty("uttStartSec", Required=Required.Always)>] UttStartSec : float
    [<JsonProperty("uttDur", Required=Required.Always)>]      UttDur : int
    [<JsonProperty("chars", Required=Required.Always)>]       Chars : ImmutableList<TChar> }

module TimeTable =
    let hopSize = TimeSpan.FromMilliseconds 10.0
    let headSil = TimeSpan.FromSeconds 0.5
    let tailSil = TimeSpan.FromSeconds 0.5

    let timeToFrame(timeSpan : TimeSpan) = timeSpan / hopSize
    let frameToTime(frames : float) = frames * hopSize

    let ofUtt bpm0 utt =
        let { Name = name; Notes = allNotes } = utt
        let allNotes = allNotes.ToList()
        let uttStart = (allNotes.[0].On |> Midi.toTimeSpan bpm0) - headSil
        let uttEnd = (allNotes.[^0].Off |> Midi.toTimeSpan bpm0) + tailSil
        let uttDur = uttEnd - uttStart

        // remove note with same onset-time
        for i in allNotes.Count - 1 .. -1 .. 1 do
            let note, prevNote = allNotes.[i], allNotes.[i - 1]
            if note.On = prevNote.On then
                allNotes.RemoveAt i

        // remove note overlapping
        for i in 0 .. allNotes.Count - 2 do
            let note, nextNote = allNotes.[i], allNotes.[i + 1]
            if note.Off <> nextNote.On then
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
                let outNotes = notes |> Seq.map(fun note ->
                    let on = (note.On |> Midi.toTimeSpan bpm0) - uttStart |> timeToFrame |> int
                    let off = (note.Off |> Midi.toTimeSpan bpm0) - uttStart |> timeToFrame |> int
                    { Pitch = note.Pitch; On = on; Off = off })
                { Ch = notes.[0].Lyric; Rom = notes.[0].Rom; Notes = ImmutableList.CreateRange outNotes; Ipa = null })

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
            Chars = ImmutableList.CreateRange chars }
        utt

module Synth =
    let baseUrl = Uri(@"http://localhost:24678")
    let poSynthUrl = Uri(baseUrl, "poSynth")
    let f0SynthUrl = Uri(baseUrl, "f0Synth")
    let acSynthUrl = Uri(baseUrl, "acSynth")

    type HttpClient with
        member x.PostJsonAsync(requestUri : Uri, jsonObject) = async {
            let requestBodyStr = JsonConvert.SerializeObject jsonObject
            use content = new StringContent(requestBodyStr, Encoding.UTF8, "application/json")
            let! result = x.PostAsync(requestUri, content) |> Async.AwaitTask
            GC.KeepAlive content
            return result.EnsureSuccessStatusCode() }

    let request(romScheme : string)(singerName : string)(tUtt : TUtterance) =
        async {
            use httpClient = new HttpClient()

            let! synthResult = httpClient.PostJsonAsync(poSynthUrl, dict([|
                "chars", box tUtt.Chars
                "uttDur", box tUtt.UttDur
                "romScheme", box romScheme |]))
            let! resultBodyStr = synthResult.Content.ReadAsStringAsync() |> Async.AwaitTask
            let chars = JsonConvert.DeserializeObject<ImmutableList<TChar>> resultBodyStr

            let! synthResult = httpClient.PostJsonAsync(f0SynthUrl, dict([|
                "chars", box chars
                "romScheme", box romScheme |]))
            let! resultBodyStr = synthResult.Content.ReadAsStringAsync() |> Async.AwaitTask
            let f0 = JsonConvert.DeserializeObject<float []> resultBodyStr

            let! synthResult = httpClient.PostJsonAsync(acSynthUrl, dict([|
                "chars", box chars
                "f0", box f0
                "singerName", box singerName |]))
            let! resultBodyByteStream = synthResult.Content.ReadAsStreamAsync() |> Async.AwaitTask

            return Audio.loadFromStream resultBodyByteStream }
    
