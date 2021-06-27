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
        [<JsonProperty("singerId", Required=Required.Default)>] SingerId : string
        [<JsonProperty("romScheme", Required=Required.Always)>] RomScheme : string
        [<JsonProperty("notes", Required=Required.Always)>]     Notes : FNote [] }
        with
        static member toUtt bpm0 x =
            let { Name = name; SingerId = singerId; RomScheme = romScheme; Notes = fNotes } = x
            let singerId = if String.IsNullOrEmpty singerId then "gloria" else singerId
            let notes = ImmutableArray.CreateRange(Seq.map FNote.toNote fNotes)
            Utterance(singerId, romScheme, bpm0, notes), name

        static member ofUtt uttName (utt : Utterance) =
            let fNotes = Array.ofSeq(Seq.map FNote.ofNote utt.Notes)
            { Name = uttName; SingerId = utt.SingerId; RomScheme = utt.RomScheme; Notes = fNotes }

    [<NoComparison; ReferenceEquality>]
    type FClip = {
        [<JsonProperty("utts", Required=Required.Always)>] Utts : FUtt [] }
        with
        static member toUtts bpm0 refNoteOn x =
            let { Utts = fUtts } = x
            let uttAndNames = fUtts |> Array.map(fun fUtt ->
                let utt, name = FUtt.toUtt bpm0 fUtt
                let utt = utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> note.SetOn(note.On + refNoteOn)))
                utt, name)
            let activeUttOp = uttAndNames |> Array.tryPick(fun (utt, name) -> if name = "active" then Some utt else None)
            let otherUtts = uttAndNames |> Seq.filter(fun (utt, name) -> name <> "active") |> Seq.map(fun (utt, name) -> utt)
            activeUttOp, ImmutableArray.CreateRange otherUtts

        static member ofUtts refNoteOn activeUttOp otherUtts =
            let uttAndNames =
                Seq.append
                    (activeUttOp |> Option.map(fun utt -> utt, "active") |> Option.toArray)
                    (otherUtts |> Seq.mapi(fun i utt -> utt, $"utt-{i}"))
            let fUtts =
                uttAndNames
                |> Seq.map(fun (utt : Utterance, name) ->
                    let utt = utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> note.SetOn(note.On - refNoteOn)))
                    FUtt.ofUtt name utt)
                |> Array.ofSeq
            { Utts = fUtts }

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
        [<JsonProperty("timeSig0", Required=Required.Default)>]    TimeSig0 : string
        [<JsonProperty("bpm0", Required=Required.Always)>]         Bpm0 : float
        [<JsonProperty("accomOffset", Required=Required.Default)>] AccomOffset : int
        [<JsonProperty("utts", Required=Required.Always)>]         Utts : FUtt [] }
        with
        static member toComp x =
            let { TimeSig0 = timeSig0Str; Bpm0 = bpm0; AccomOffset = accomOffset; Utts = fUtts } = x
            let timeSig0 =
                match timeSig0Str with
                | null | "" -> timeSignature 4 4
                | _ -> TimeSignature.Parse timeSig0Str
            let uttsByNameDict = dict(Seq.map(FUtt.toUtt bpm0)fUtts)
            let utts = ImmutableArray.CreateRange uttsByNameDict.Keys
            let getUttName utt = uttsByNameDict.[utt]
            Composition(timeSig0, bpm0, utts).SetBgAudioOffset accomOffset, getUttName

        static member ofComp getUttName (comp : Composition) =
            let timeSig0Str = comp.TimeSig0.ToString()
            let accomOffset = comp.BgAudio.SampleOffset
            let uttNames = Seq.map getUttName comp.Utts
            let fUtts = Array.ofSeq(Seq.map2 FUtt.ofUtt uttNames comp.Utts)
            { TimeSig0 = timeSig0Str; Bpm0 = comp.Bpm0; AccomOffset = accomOffset; Utts = fUtts }

    let read stream =
        use zipFile = new ZipArchive(stream, ZipArchiveMode.Read)
        use chartStream = (zipFile.GetEntry "chart.json").Open()
        use chartReader = new StreamReader(chartStream)
        let fCompStr = chartReader.ReadToEnd()
        let fComp = JsonConvert.DeserializeObject<FComp> fCompStr
        let comp, getUttName = FComp.toComp fComp

        let zipEntryDict = zipFile.Entries.ToDictionary(fun entry -> entry.Name)
        let comp =
            match zipEntryDict.TryGetValue "accom.bin" |> Option.ofByRef with
            | None -> comp
            | Some audioEntry ->
                use fileStream = audioEntry.Open()
                let audioFileBytes, audioSamples = AudioSamples.loadFromStream fileStream
                comp.SetBgAudio(AudioTrack(comp.BgAudio.SampleOffset, audioFileBytes, audioSamples))

        let uttSynthCache =
            (UttSynthCache.Create comp.Bpm0, comp.Utts)
            ||> Seq.fold(fun uttSynthCache utt ->
                let uttName = getUttName utt
                let uttSynthCache =
                    match zipEntryDict.TryGetValue $"{uttName}.cg" |> Option.ofByRef with
                    | None -> uttSynthCache
                    | Some cgEntry ->
                        use fileStream = cgEntry.Open()
                        use fileReader = new StreamReader(fileStream)
                        let fChGridsStr = fileReader.ReadToEnd()
                        let fChGrids = JsonConvert.DeserializeObject<FChGrid []> fChGridsStr
                        let charGrids = Array.map FChGrid.toCharGrid fChGrids
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult -> uttSynthResult.SetCharGrids charGrids)
                let uttSynthCache =
                    match zipEntryDict.TryGetValue $"{uttName}.f0" |> Option.ofByRef with
                    | None -> uttSynthCache
                    | Some f0Entry ->
                        use fileStream = f0Entry.Open()
                        use byteStream = new MemoryStream()
                        fileStream.CopyTo byteStream
                        let f0Bytes = byteStream.ToArray()
                        let f0Samples = Array.zeroCreate(f0Bytes.Length / sizeof<float32>)
                        Buffer.BlockCopy(f0Bytes, 0, f0Samples, 0, f0Samples.Length * sizeof<float32>)
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult -> uttSynthResult.SetF0Samples f0Samples)
                let uttSynthCache =
                    match zipEntryDict.TryGetValue $"{uttName}.m4a" |> Option.ofByRef with
                    | None -> uttSynthCache
                    | Some audioEntry ->
                        use fileStream = audioEntry.Open()
                        let audioContent = AudioSamples.loadFromStream fileStream
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult -> uttSynthResult.SetAudio audioContent)
                uttSynthCache)

        comp, uttSynthCache

    let save stream comp uttSynthCache =
        let getUttName =
            let uttsByNameDict = dict((comp : Composition).Utts |> Seq.mapi(fun i utt -> utt, $"utt-{i}"))
            fun utt -> uttsByNameDict.[utt]

        use zipFile = new ZipArchive(stream, ZipArchiveMode.Create)
        do  use chartStream = zipFile.CreateEntry("chart.json", CompressionLevel.Optimal).Open()
            use chartWriter = new StreamWriter(chartStream)
            let fComp = FComp.ofComp getUttName comp
            let fCompStr = JsonConvert.SerializeObjectFormatted fComp
            chartWriter.Write fCompStr

        if comp.BgAudio.HasAudio then
            use fileStream = zipFile.CreateEntry("accom.bin", CompressionLevel.Fastest).Open()
            fileStream.Write(comp.BgAudio.AudioFileBytes, 0, comp.BgAudio.AudioFileBytes.Length)

        comp.Utts |> Seq.iter(fun utt ->
            let uttSynthResult = (uttSynthCache : UttSynthCache).GetOrDefault utt
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
            if uttSynthResult.HasAudio && uttSynthResult.AudioFileBytes.Length > 0 then
                use fileStream = zipFile.CreateEntry($"{uttName}.m4a", CompressionLevel.Fastest).Open()
                fileStream.Write(uttSynthResult.AudioFileBytes, 0, uttSynthResult.AudioFileBytes.Length))

    let toClipboardText refNoteOn activeUttOp otherUtts =
        let fClip = FClip.ofUtts refNoteOn activeUttOp otherUtts
        JsonConvert.SerializeObjectFormatted fClip

    let ofClipboardText bpm0 refNoteOn jStr =
        let fClip = JsonConvert.DeserializeObject<_> jStr
        FClip.toUtts bpm0 refNoteOn fClip


