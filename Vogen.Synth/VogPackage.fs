namespace Vogen.Synth

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


module VogPackage =
    type TimeSignatureConverter() =
        inherit JsonConverter()

        override x.CanConvert objectType = typeof<TimeSignature>.IsAssignableFrom objectType

        override x.ReadJson(reader, objectType, existingValue, serializer) =
            let timeSigStr = reader.Value :?> string
            match timeSigStr with
            | null | "" -> timeSignature 4 4
            | _ -> TimeSignature.Parse timeSigStr
            |> box

        override x.WriteJson(writer, value, serializer) =
            let timeSigStr = (value :?> TimeSignature).ToString()
            writer.WriteValue timeSigStr

    [<NoComparison; ReferenceEquality>]
    type FNote = {
        [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
        [<JsonProperty("lyric", Required=Required.Always)>] Lyric : string
        [<JsonProperty("rom", Required=Required.Always)>]   Rom : string
        [<JsonProperty("on", Required=Required.Always)>]    On : int64
        [<JsonProperty("dur", Required=Required.Always)>]   Dur : int64 }
        with
        [<JsonIgnore>] member x.Off = x.On + x.Dur
        [<JsonIgnore>] member x.IsHyphen = x.Lyric = "-"

    [<NoComparison; ReferenceEquality>]
    type FUtt = {
        [<JsonProperty("name", Required=Required.Always)>]      Name : string
        [<JsonProperty("singerId", Required=Required.Default)>] SingerId : string
        [<JsonProperty("romScheme", Required=Required.Always)>] RomScheme : string
        [<JsonProperty("notes", Required=Required.Always)>]     Notes : FNote [] }

    [<NoComparison; ReferenceEquality>]
    type FClip = {
        [<JsonProperty("utts", Required=Required.Always)>] Utts : FUtt [] }

    [<NoComparison; ReferenceEquality>]
    type FPh = {
        [<JsonProperty("ph", Required=Required.Always)>]  Ph : string
        [<JsonProperty("on", Required=Required.Always)>]  On : int
        [<JsonProperty("off", Required=Required.Always)>] Off : int }

    [<NoComparison; ReferenceEquality>]
    type FChGrid = {
        [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
        [<JsonProperty("phs", Required=Required.Always)>]   Phs : FPh [] }

    [<NoComparison; ReferenceEquality>]
    type FComp = {
        [<JsonProperty("timeSig0", Required=Required.Default)>]
        [<JsonConverter(typeof<TimeSignatureConverter>)>]          TimeSig0 : TimeSignature
        [<JsonProperty("bpm0", Required=Required.Always)>]         Bpm0 : float
        [<JsonProperty("accomOffset", Required=Required.Default)>] AccomOffset : int
        [<JsonProperty("utts", Required=Required.Always)>]         Utts : FUtt [] }

    type VogPackage = {
        Chart : FComp
        AccomPath : string
        AccomAudio : AudioSamplesLazy option
        UttCharGrids : Dictionary<string, FChGrid []>
        UttF0s : Dictionary<string, float32 []>
        UttAudios : Dictionary<string, AudioSamplesLazy> }

    let read stream filePath =
        use zipFile = new ZipArchive(stream, ZipArchiveMode.Read)
        use chartStream = (zipFile.GetEntry "chart.json").Open()
        use chartReader = new StreamReader(chartStream)
        let fCompStr = chartReader.ReadToEnd()
        let fComp = JsonConvert.DeserializeObject<FComp> fCompStr

        let zipEntryDict = zipFile.Entries.ToDictionary(fun entry -> entry.Name)
        let accomPath, accomAudio =
            try match zipEntryDict.TryGetValue "accom.path" |> Option.ofByRef with
                | None -> "", None
                | Some audioPathEntry ->
                    use fileStream = audioPathEntry.Open()
                    use textReader = new StreamReader(fileStream, Encoding.UTF8)
                    let audioFilePathRel = textReader.ReadToEnd()
                    let audioFilePath = Path.GetFullPath(audioFilePathRel, Path.GetDirectoryName(filePath : string))
                    try use fileStream = File.OpenRead audioFilePath
                        let fileBytes = fileStream.ReadAllBytes()
                        audioFilePath, Some(AudioSamples.loadFromBytes fileBytes)
                    with ex ->
                        audioFilePath, None
            with ex ->
                "", None

        let uttCharGrids =
            Dictionary(fComp.Utts |> Seq.choose(fun utt ->
                zipEntryDict.TryGetValue $"{utt.Name}.cg" |> Option.ofByRef
                |> Option.map(fun cgEntry ->
                    use fileStream = cgEntry.Open()
                    use fileReader = new StreamReader(fileStream)
                    let fChGridsStr = fileReader.ReadToEnd()
                    let fChGrids = JsonConvert.DeserializeObject<FChGrid []> fChGridsStr
                    KeyValuePair(utt.Name, fChGrids))))
        let uttF0s =
            Dictionary(fComp.Utts |> Seq.choose(fun utt ->
                zipEntryDict.TryGetValue $"{utt.Name}.f0" |> Option.ofByRef
                |> Option.map(fun f0Entry ->
                    use fileStream = f0Entry.Open()
                    let f0Bytes = fileStream.ReadAllBytes()
                    let f0Samples = Array.zeroCreate(f0Bytes.Length / sizeof<float32>)
                    Buffer.BlockCopy(f0Bytes, 0, f0Samples, 0, f0Samples.Length * sizeof<float32>)
                    KeyValuePair(utt.Name, f0Samples))))
        let uttAudios =
            Dictionary(fComp.Utts |> Seq.choose(fun utt ->
                zipEntryDict.TryGetValue $"{utt.Name}.m4a" |> Option.ofByRef
                |> Option.map(fun audioEntry ->
                    use fileStream = audioEntry.Open()
                    let fileBytes = fileStream.ReadAllBytes()
                    let audioSamples = AudioSamples.loadFromBytes fileBytes
                    KeyValuePair(utt.Name, audioSamples))))

        {   Chart = fComp
            AccomPath = accomPath
            AccomAudio = accomAudio
            UttCharGrids = uttCharGrids
            UttF0s = uttF0s
            UttAudios = uttAudios }

    let readFromFile filePath =
        use stream = File.OpenRead filePath
        read stream filePath

    let save stream filePath vogPackage =
        let {
            Chart = fComp
            AccomPath = accomPath
            AccomAudio = accomAudio
            UttCharGrids = uttCharGrids
            UttF0s = uttF0s
            UttAudios = uttAudios } = vogPackage

        use zipFile = new ZipArchive(stream, ZipArchiveMode.Create)
        do  use chartStream = zipFile.CreateEntry("chart.json", CompressionLevel.Optimal).Open()
            use chartWriter = new StreamWriter(chartStream)
            let fCompStr = JsonConvert.SerializeObjectFormatted fComp
            chartWriter.Write fCompStr

        if not(String.IsNullOrEmpty accomPath) then
            use fileStream = zipFile.CreateEntry("accom.path", CompressionLevel.Optimal).Open()
            use textWriter = new StreamWriter(fileStream, Encoding.UTF8)
            let audioFilePathRel = Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(filePath : string)), accomPath)
            textWriter.Write audioFilePathRel

        fComp.Utts |> Seq.iter(fun utt ->
            match uttCharGrids.TryGetValue utt.Name |> Option.ofByRef with
            | None -> ()
            | Some fChGrids ->
                use fileStream = zipFile.CreateEntry($"{utt.Name}.cg", CompressionLevel.Optimal).Open()
                use fileWriter = new StreamWriter(fileStream)
                let fChGridsStr = JsonConvert.SerializeObjectFormatted fChGrids
                fileWriter.Write fChGridsStr
            match uttF0s.TryGetValue utt.Name |> Option.ofByRef with
            | None -> ()
            | Some f0Samples ->
                use fileStream = zipFile.CreateEntry($"{utt.Name}.f0", CompressionLevel.Optimal).Open()
                let f0Bytes = Array.zeroCreate(f0Samples.Length * sizeof<float32>)
                Buffer.BlockCopy(f0Samples, 0, f0Bytes, 0, f0Samples.Length * sizeof<float32>)
                fileStream.Write(f0Bytes, 0, f0Bytes.Length)
            match uttAudios.TryGetValue utt.Name |> Option.ofByRef with
            | None -> ()
            | Some audioSamples ->
                use fileStream = zipFile.CreateEntry($"{utt.Name}.m4a", CompressionLevel.Fastest).Open()
                fileStream.Write(audioSamples.FileBytes, 0, audioSamples.FileBytes.Length))

    let saveToFile filePath vogPackage =
        use stream = File.Open(filePath, FileMode.Create)
        save stream filePath vogPackage


