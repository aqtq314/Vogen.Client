namespace Vogen.Synth

open Doaz.Reactive
open NAudio
open NAudio.MediaFoundation
open NAudio.Wave
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices


type SampleProvider(samples : _ []) =
    member val CurrOffset = 0 with get, set
    interface ISampleProvider with
        member x.WaveFormat = playbackWaveFormat
        member x.Read(buffer, offset, count) =
            let currOffset = x.CurrOffset
            let count = count |> min(samples.Length - currOffset)
            for i in 0 .. count - 1 do
                buffer.[offset + i] <- samples.[currOffset + i]
            x.CurrOffset <- currOffset + count
            count

module Audio =
    let sampleToTime(sampleTime : int) =
        TimeSpan.FromSeconds(float sampleTime / float fs)

    let timeToSample(time : TimeSpan) =
        int(time.TotalSeconds * float fs)

    let inline pulseToSample bpm0 pulses =
        pulses |> Midi.toTimeSpan bpm0 |> timeToSample

    let inline sampleToPulse bpm0 sampleTime =
        sampleTime |> sampleToTime |> Midi.ofTimeSpan bpm0

type AudioSamplesLazy = {
    FileBytes : byte []
    GetSamples : unit -> float32 [] }

type AudioSamples = {
    FileBytes : byte []
    Samples : float32 [] }
    with
    member x.AsLazy =
        { FileBytes = x.FileBytes; GetSamples = fun () -> x.Samples }

module AudioSamples =
    let inline create fileBytes samples = { FileBytes = fileBytes; Samples = samples }

    let empty = { FileBytes = Array.empty; Samples = Array.empty }
    let emptyLazy = empty.AsLazy

    let inline asLazy(audioSamples : AudioSamples) = audioSamples.AsLazy

    let decode(fileBytes : byte []) =
        use cacheStream = new MemoryStream(fileBytes)
        cacheStream.Position <- 0L

        MediaFoundationApi.Startup()
        use reader = new StreamMediaFoundationReader(cacheStream)
        use reader = new MediaFoundationResampler(reader, playbackWaveFormat)
        let sampleReader = reader.ToSampleProvider()

        let sampleChunks = List<_>()
        let buffer = Array.zeroCreate<float32>(fs * channels)
        let rec readBuffer() =
            let bytesRead = sampleReader.Read(buffer, 0, buffer.Length)
            if bytesRead > 0 then
                sampleChunks.Add buffer.[..bytesRead - 1]
                readBuffer()
        readBuffer()

        Array.concat sampleChunks

    let loadFromBytes(fileBytes : byte []) =
        let samplesLazy = lazy decode fileBytes
        { FileBytes = fileBytes; GetSamples = samplesLazy.Force }

    let loadFromStream(fileStream : Stream) =
        let fileBytes = fileStream.ReadAllBytes()
        loadFromBytes fileBytes

    let loadFromFile filePath =
        use fileStream = File.OpenRead filePath
        loadFromStream fileStream

    let validate(audioSamplesLazy : AudioSamplesLazy) =
        let fileBytes = audioSamplesLazy.FileBytes
        let samples = audioSamplesLazy.GetSamples()
        { FileBytes = fileBytes; Samples = samples }

    let tryValidate audioSamplesLazy =
        try Some(validate audioSamplesLazy)
        with ex ->
            None

    let render uttOffsetAndSamples =
        let outSampleDur =
            uttOffsetAndSamples
            |> Seq.map(fun (sampleOffset, samples : _ []) -> sampleOffset + samples.Length)
            |> Seq.appendItem 0
            |> Seq.max
        let outSamples = Array.zeroCreate outSampleDur
        for sampleOffset, samples in uttOffsetAndSamples do
            for i in max 0 sampleOffset .. sampleOffset + samples.Length - 1 do
                outSamples.[i] <- outSamples.[i] + samples.[i - sampleOffset]
        outSamples

    let renderToFile filePath uttOffsetAndSamples =
        File.Create(filePath).Dispose()
        let outSamples = render uttOffsetAndSamples
        if outSamples.Length > 0 then
            let sampleProvider = SampleProvider(outSamples)
            match (Path.GetExtension filePath).ToLower() with
            | ".wav" ->
                WaveFileWriter.CreateWaveFile16(filePath, sampleProvider)
            | ".m4a" ->
                MediaFoundationApi.Startup()
                MediaFoundationEncoder.EncodeToAac(sampleProvider.ToWaveProvider(), filePath, 192000)
            | fileExt ->
                raise(ArgumentException($"Unknown output file extension ({fileExt}) for rendering"))


