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

module Synth =
    //let baseUrl = Uri(@"http://localhost:24678")
    let baseUrl = Uri(@"http://doaz.tsinghua-csai.edu.cn:24678")
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

    //let requestPO(tUtt : TimeTable.TUtt) = async {
    //    use httpClient = new HttpClient()
    //    let! synthResult = httpClient.PostJsonAsync(poSynthUrl, dict [|
    //        "romScheme", box tUtt.RomScheme
    //        "uttDur", box tUtt.UttDur
    //        "chars", box tUtt.Chars |])
    //    let! resultBodyStr = synthResult.Content.ReadAsStringAsync() |> Async.AwaitTask
    //    return JsonConvert.DeserializeObject<ImmutableList<TimeTable.TChar>> resultBodyStr }

    let requestPO(synthActor : MailboxProcessor<_>)(tUtt : TimeTable.TUtt) = async {
        let romScheme, uttDur, chars = tUtt.RomScheme, tUtt.UttDur, tUtt.Chars
        let! synthResult = synthActor.PostAndAsyncReply(fun reply -> (romScheme, uttDur, chars), reply)
        let outChars =
            match synthResult with
            | Ok outChars -> outChars
            | Error ex -> raise ex
        return outChars }

    let requestF0(tUtt : TimeTable.TUtt)(tChars : ImmutableList<TimeTable.TChar>) = async {
        use httpClient = new HttpClient()
        let! synthResult = httpClient.PostJsonAsync(f0SynthUrl, dict [|
            "romScheme", box tUtt.RomScheme
            "chars", box tChars |])
        let! resultBodyStr = synthResult.Content.ReadAsStringAsync() |> Async.AwaitTask
        return JsonConvert.DeserializeObject<float32 []> resultBodyStr }

    let requestAc(tChars : ImmutableList<TimeTable.TChar>)(f0 : float32 [])(singerName : string)(sampleOffset : int) = async {
        use httpClient = new HttpClient()
        let! synthResult = httpClient.PostJsonAsync(acSynthUrl, dict [|
            "singerName", box singerName
            "sampleOffset", box sampleOffset
            "f0", box f0
            "chars", box tChars |])
        let! resultBodyByteStream = synthResult.Content.ReadAsStreamAsync() |> Async.AwaitTask
        return AudioSamples.loadFromStream resultBodyByteStream }


