namespace Vogen.Client.Model

open Doaz.Reactive
open NAudio
open NAudio.Wave
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.IO.Compression
open System.Linq
open System.Text
open System.Text.Encodings


module External =
    let loadVpr romScheme stream =
        use zipFile = new ZipArchive(stream, ZipArchiveMode.Read)
        use seqStream = (zipFile.GetEntry @"Project\sequence.json").Open()
        use seqReader = new StreamReader(seqStream)
        let seqStr = seqReader.ReadToEnd()
        let vpr = JsonConvert.DeserializeObject seqStr :?> JToken

        let masterTrack = vpr.["masterTrack"]
        let bpm0 =
            let vprTempo = masterTrack.["tempo"]
            if vprTempo.["global"].["isEnabled"].ToObject<bool>() then
                vprTempo.["global"].["value"].ToObject<float>() / 100.0
            else
                vprTempo.["events"].[0].["value"].ToObject<float>() / 100.0

        let vprParts =
            vpr.["tracks"]
            |> Seq.filter(fun vprTrack -> vprTrack.["type"].ToObject<int>() = 0)
            |> Seq.collect(fun vprTrack ->
                let vprParts = vprTrack.["parts"]
                if vprParts = null then Seq.empty else vprParts.Values<JToken>())

        let utts = vprParts |> Seq.map(fun vprPart ->
            let vprPartPos = vprPart.["pos"].ToObject<int64>()
            let vprNotes = vprPart.["notes"].Values<JToken>()
            let notes = vprNotes |> Seq.map(fun vprNote ->
                let pitch = vprNote.["number"].ToObject<int>()
                let rom = vprNote.["lyric"].ToObject<string>()
                let lyric = if rom = "-" then "-" else "〇"
                let on = vprNote.["pos"].ToObject<int64>() + vprPartPos
                let dur = vprNote.["duration"].ToObject<int64>()
                Note(pitch, lyric, rom, on, dur))
            Utterance(romScheme, ImmutableList.CreateRange notes))

        Composition(bpm0, ImmutableList.CreateRange utts)


