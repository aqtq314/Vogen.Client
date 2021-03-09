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
        [<JsonProperty("name", Required=Required.Always)>]      Name : string
        [<JsonProperty("romScheme", Required=Required.Always)>] RomScheme : string
        [<JsonProperty("notes", Required=Required.Always)>]     Notes : ImmutableArray<FNote> }
        with
        static member toUtt x =
            let { Name = name; RomScheme = romScheme; Notes = fNotes } = x
            let notes = ImmutableList.CreateRange(Seq.map FNote.toNote fNotes)
            Utterance(romScheme, notes), name

        static member ofUtt uttName (utt : Utterance) =
            let fNotes = ImmutableArray.CreateRange(Seq.map FNote.ofNote utt.Notes)
            { Name = uttName; RomScheme = utt.RomScheme; Notes = fNotes }

    [<NoComparison; ReferenceEquality>]
    type FComp = {
        [<JsonProperty("bpm0", Required=Required.Always)>]  Bpm0 : float
        [<JsonProperty("utts", Required=Required.Always)>]  Utts : ImmutableArray<FUtt> }
        with
        static member toComp x =
            let { Bpm0 = bpm0; Utts = fUtts } = x
            let uttsByNameDict = dict(Seq.map FUtt.toUtt fUtts)
            let utts = ImmutableList.CreateRange uttsByNameDict.Keys
            let getUttName utt = uttsByNameDict.[utt]
            Composition(bpm0, utts), getUttName

        static member ofComp getUttName (comp : Composition) =
            let uttNames = Seq.map getUttName comp.Utts
            let fUtts = ImmutableArray.CreateRange(Seq.map2 FUtt.ofUtt uttNames comp.Utts)
            { Bpm0 = comp.Bpm0; Utts = fUtts }

    let read stream =
        use zipFile = new ZipArchive(stream, ZipArchiveMode.Read)
        use chartStream = (zipFile.GetEntry "chart.json").Open()
        use chartReader = new StreamReader(chartStream)
        let fCompStr = chartReader.ReadToEnd()
        let fComp = JsonConvert.DeserializeObject<FComp> fCompStr
        let comp, getUttName = FComp.toComp fComp

        let zipEntryDict = zipFile.Entries.ToDictionary(fun entry -> entry.Name)
        (comp, comp.Utts)
        ||> Seq.fold(fun comp utt ->
            let uttName = getUttName utt
            zipEntryDict.TryGetValue $"{uttName}.m4a" |> Option.ofByRef
            |> Option.map(fun zipEntry ->
                use fileStream = zipEntry.Open()
                let audioContent = AudioSamples.loadFromStream fileStream
                utt |> comp.SetUttAudioSynthed audioContent)
            |> Option.defaultValue comp)

    let save stream comp =
        let getUttName =
            let uttsByNameDict = dict((comp : Composition).Utts |> Seq.mapi(fun i utt -> utt, $"utt-{i}"))
            fun utt -> uttsByNameDict.[utt]

        use zipFile = new ZipArchive(stream, ZipArchiveMode.Create)
        do  use chartStream = (zipFile.CreateEntry "chart.json").Open()
            use chartWriter = new StreamWriter(chartStream)
            let fComp = FComp.ofComp getUttName comp
            let fCompStr = JsonConvert.SerializeObject fComp
            chartWriter.Write fCompStr

        comp.Utts |> Seq.iter(fun utt ->
            let uttAudio = comp.GetUttAudio utt
            let uttName = getUttName utt
            if uttAudio.IsSynthed then
                use fileStream = (zipFile.CreateEntry $"{uttName}.m4a").Open()
                fileStream.Write(uttAudio.FileBytes, 0, uttAudio.FileBytes.Length))





