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


module AlgorithmConfig =
    let [<Literal>] HopSize = 10.0

    let UttLeadingSil = TimeSpan.FromSeconds 0.5

type Note = {
    [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
    [<JsonProperty("lyric", Required=Required.Always)>] Lyric : string
    [<JsonProperty("rom", Required=Required.Always)>]   Rom : string
    [<JsonProperty("on", Required=Required.Always)>]    On : int64
    [<JsonProperty("dur", Required=Required.Always)>]   Dur : int64 }

type Utterance = {
    [<JsonProperty("name", Required=Required.Always)>]  Name : string
    [<JsonProperty("notes", Required=Required.Always)>] Notes : ImmutableList<Note> }

type Composition = {
    [<JsonProperty("bpm0", Required=Required.Always)>]  Bpm0 : float
    [<JsonProperty("utts", Required=Required.Always)>]  Utts : ImmutableList<Utterance> }

type Note with
    [<JsonIgnore>] member x.Off = x.On + x.Dur

type Composition with
    static member Empty = {
        Bpm0 = 120.0
        Utts = ImmutableList.Empty }

module FilePackage =
    let cacheAsMemoryStream(stream : Stream) =
        let cacheStream = new MemoryStream()
        stream.CopyTo cacheStream
        cacheStream.Position <- 0L
        cacheStream

    let read stream =
        use zipFile = new ZipArchive(stream, ZipArchiveMode.Read)
        use chartStream = (zipFile.GetEntry "chart.json").Open()
        use chartReader = new StreamReader(chartStream)
        let comp = chartReader.ReadToEnd() |> JsonConvert.DeserializeObject<Composition>

        let audioSegments =
            ImmutableDictionary.CreateRange(
                comp.Utts |> Seq.map(fun utt ->
                    let entry = zipFile.GetEntry $"{utt.Name}.flac"
                    use fileStream = entry.Open()
                    use cachedStream = cacheAsMemoryStream fileStream
                    let samples = Audio.loadFromStream cachedStream
                    let sampleOffset =
                        utt.Notes.[0].On
                        |> MidiTime.toTimeSpan comp.Bpm0
                        |> (+) -AlgorithmConfig.UttLeadingSil
                        |> Audio.timeToSample
                    let audioSeg = {
                        SampleOffset = sampleOffset
                        Samples = samples }
                    KeyValuePair(utt.Name, audioSeg)))
        let audioLib = {
            Segments = audioSegments }

        comp, audioLib

