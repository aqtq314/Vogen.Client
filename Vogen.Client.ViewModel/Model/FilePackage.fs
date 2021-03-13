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
open System.Text.RegularExpressions


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
        [<JsonProperty("notes", Required=Required.Always)>]     Notes : FNote [] }
        with
        static member toUtt x =
            let { Name = name; RomScheme = romScheme; Notes = fNotes } = x
            let notes = ImmutableList.CreateRange(Seq.map FNote.toNote fNotes)
            Utterance(romScheme, notes), name

        static member ofUtt uttName (utt : Utterance) =
            let fNotes = Array.ofSeq(Seq.map FNote.ofNote utt.Notes)
            { Name = uttName; RomScheme = utt.RomScheme; Notes = fNotes }

    [<NoComparison; ReferenceEquality>]
    type FPh = {
        [<JsonProperty("ph", Required=Required.Always)>]  Ph : string
        [<JsonProperty("on", Required=Required.Always)>]  On : int
        [<JsonProperty("off", Required=Required.Always)>] Off : int }
        with
        static member toPh x =
            let { Ph = ph; On = on; Off = off } = x
            PhonemeInterval(ph, on, off)

        static member ofPh(ph : PhonemeInterval) =
            { Ph = ph.Ph; On = ph.On; Off = ph.Off }

    [<NoComparison; ReferenceEquality>]
    type FChGrid = {
        [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
        [<JsonProperty("phs", Required=Required.Always)>]   Phs : FPh [] }
        with
        static member toCharGrid x =
            let { Pitch = pitch; Phs = fPhs } = x
            let phs = fPhs |> Array.map FPh.toPh
            CharGrid(pitch, phs)

        static member ofCharGrid(charGrid : CharGrid) =
            let fPhs = charGrid.Phs |> Array.map FPh.ofPh
            { Pitch = charGrid.Pitch; Phs = fPhs }

    [<NoComparison; ReferenceEquality>]
    type FComp = {
        [<JsonProperty("bpm0", Required=Required.Always)>]  Bpm0 : float
        [<JsonProperty("utts", Required=Required.Always)>]  Utts : FUtt [] }
        with
        static member toComp x =
            let { Bpm0 = bpm0; Utts = fUtts } = x
            let uttsByNameDict = dict(Seq.map FUtt.toUtt fUtts)
            let utts = ImmutableList.CreateRange uttsByNameDict.Keys
            let getUttName utt = uttsByNameDict.[utt]
            Composition(bpm0, utts), getUttName

        static member ofComp getUttName (comp : Composition) =
            let uttNames = Seq.map getUttName comp.Utts
            let fUtts = Array.ofSeq(Seq.map2 FUtt.ofUtt uttNames comp.Utts)
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
            let comp =
                match zipEntryDict.TryGetValue $"{uttName}.cg" |> Option.ofByRef with
                | None -> comp
                | Some cgEntry ->
                    use fileStream = cgEntry.Open()
                    use fileReader = new StreamReader(fileStream)
                    let fChGridsStr = fileReader.ReadToEnd()
                    let fChGrids = JsonConvert.DeserializeObject<FChGrid []> fChGridsStr
                    let charGrids = Array.map FChGrid.toCharGrid fChGrids
                    utt |> comp.SetUttSynthResult(fun uttSynthResult -> uttSynthResult.SetCharGrids charGrids)
            let comp =
                match zipEntryDict.TryGetValue $"{uttName}.f0" |> Option.ofByRef with
                | None -> comp
                | Some f0Entry ->
                    use fileStream = f0Entry.Open()
                    use byteStream = new MemoryStream()
                    fileStream.CopyTo byteStream
                    let f0Bytes = byteStream.ToArray()
                    let f0Samples = Array.zeroCreate(f0Bytes.Length / sizeof<float32>)
                    Buffer.BlockCopy(f0Bytes, 0, f0Samples, 0, f0Samples.Length * sizeof<float32>)
                    utt |> comp.SetUttSynthResult(fun uttSynthResult -> uttSynthResult.SetF0Samples f0Samples)
            let comp =
                match zipEntryDict.TryGetValue $"{uttName}.m4a" |> Option.ofByRef with
                | None -> comp
                | Some audioEntry ->
                    use fileStream = audioEntry.Open()
                    let audioContent = AudioSamples.loadFromStream fileStream
                    utt |> comp.SetUttSynthResult(fun uttSynthResult -> uttSynthResult.SetAudio audioContent)
            comp)

    let save stream comp =
        let getUttName =
            let uttsByNameDict = dict((comp : Composition).Utts |> Seq.mapi(fun i utt -> utt, $"utt-{i}"))
            fun utt -> uttsByNameDict.[utt]

        use zipFile = new ZipArchive(stream, ZipArchiveMode.Create)
        do  use chartStream = zipFile.CreateEntry("chart.json", CompressionLevel.Optimal).Open()
            use chartWriter = new StreamWriter(chartStream)
            let fComp = FComp.ofComp getUttName comp
            let fCompStr = JsonConvert.SerializeObjectFormatted fComp
            chartWriter.Write fCompStr

        comp.Utts |> Seq.iter(fun utt ->
            let uttSynthResult = comp.GetUttSynthResult utt
            let uttName = getUttName utt
            if uttSynthResult.HasCharGrids then
                use fileStream = zipFile.CreateEntry($"{uttName}.cg", CompressionLevel.Optimal).Open()
                use fileWriter = new StreamWriter(fileStream)
                let charGrids = uttSynthResult.CharGrids
                let fChGrids = Array.map FChGrid.ofCharGrid charGrids
                let fChGridsStr = JsonConvert.SerializeObjectFormatted fChGrids
                fileWriter.Write fChGridsStr
            if uttSynthResult.HasF0Samples then
                use fileStream = zipFile.CreateEntry($"{uttName}.f0", CompressionLevel.Optimal).Open()
                let f0Samples = uttSynthResult.F0Samples
                let f0Bytes = Array.zeroCreate(f0Samples.Length * sizeof<float32>)
                Buffer.BlockCopy(f0Samples, 0, f0Bytes, 0, f0Samples.Length * sizeof<float32>)
                fileStream.Write(f0Bytes, 0, f0Bytes.Length)
            if uttSynthResult.HasAudio then
                use fileStream = zipFile.CreateEntry($"{uttName}.m4a", CompressionLevel.Fastest).Open()
                fileStream.Write(uttSynthResult.AudioFileBytes, 0, uttSynthResult.AudioFileBytes.Length))


