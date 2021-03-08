namespace Vogen.Client.Model

open Doaz.Reactive
open NAudio
open NAudio.Wave
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.IO.Compression
open System.Linq
open System.Text
open System.Text.Encodings


module FilePackage =
    [<NoComparison; ReferenceEquality>]
    type FNote = {
        [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
        [<JsonProperty("lyric", Required=Required.Always)>] Lyric : string
        [<JsonProperty("rom", Required=Required.Always)>]   Rom : string
        [<JsonProperty("on", Required=Required.Always)>]    On : int64
        [<JsonProperty("dur", Required=Required.Always)>]   Dur : int64 }
        with
        static member toNote x =
            let { Pitch = pitch; Lyric = lyric; Rom = rom; On = on; Dur = dur } = x
            Note(pitch, lyric, rom, on, dur)

        static member ofNote(note : Note) =
            { Pitch = note.Pitch; Lyric = note.Lyric; Rom = note.Rom; On = note.On; Dur = note.Dur }

    [<NoComparison; ReferenceEquality>]
    type FUtt = {
        [<JsonProperty("name", Required=Required.Always)>]  Name : string
        [<JsonProperty("notes", Required=Required.Always)>] Notes : ImmutableArray<FNote> }
        with
        static member toUtt x =
            let { Name = name; Notes = fNotes } = x
            let notes = ImmutableList.CreateRange(Seq.map FNote.toNote fNotes)
            Utterance(name, notes)

        static member ofUtt(utt : Utterance) =
            let fNotes = ImmutableArray.CreateRange(Seq.map FNote.ofNote utt.Notes)
            { Name = utt.Name; Notes = fNotes }

    [<NoComparison; ReferenceEquality>]
    type FComp = {
        [<JsonProperty("bpm0", Required=Required.Always)>]  Bpm0 : float
        [<JsonProperty("utts", Required=Required.Always)>]  Utts : ImmutableArray<FUtt> }
        with
        static member toComp x =
            let { Bpm0 = bpm0; Utts = fUtts } = x
            let utts = ImmutableList.CreateRange(Seq.map FUtt.toUtt fUtts)
            Composition(bpm0, utts)

        static member ofComp(comp : Composition) =
            let utts = ImmutableArray.CreateRange(Seq.map FUtt.ofUtt comp.Utts)
            { Bpm0 = comp.Bpm0; Utts = utts }

    let read stream =
        use zipFile = new ZipArchive(stream, ZipArchiveMode.Read)
        use chartStream = (zipFile.GetEntry "chart.json").Open()
        use chartReader = new StreamReader(chartStream)
        let fCompStr = chartReader.ReadToEnd()
        let fComp = JsonConvert.DeserializeObject<FComp> fCompStr
        let comp = FComp.toComp fComp

        let zipEntryDict = zipFile.Entries.ToDictionary(fun entry -> entry.Name)
        let audioSegments =
            comp.Utts
            |> Seq.choose(fun utt ->
                zipEntryDict.TryGetValue $"{utt.Name}.flac"
                |> Option.ofByRef
                |> Option.map(fun zipEntry -> utt, zipEntry))
            |> Seq.map(fun (utt, zipEntry) ->
                use fileStream = zipEntry.Open()
                use cachedStream = fileStream.CacheAsMemoryStream()
                let samples = AudioSamples.loadFromStream cachedStream
                KeyValuePair(utt.Name, samples))
            |> ImmutableDictionary.CreateRange

        let comp = comp.SetAudioSegments audioSegments
        comp


