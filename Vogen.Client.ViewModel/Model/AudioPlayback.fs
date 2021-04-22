namespace Vogen.Client.Model

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
open Audio


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

module AudioSamples =
    let loadFromStream(fileStream : Stream) =
        use cacheStream = new MemoryStream()
        fileStream.CopyTo cacheStream
        let fileBytes = cacheStream.ToArray()
        cacheStream.Position <- 0L

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

        fileBytes, Array.concat sampleChunks

    let loadFromFile filePath =
        use fileStream = File.OpenRead filePath
        loadFromStream fileStream

    let renderComp(comp : Composition)(uttSynthCache : UttSynthCache) =
        let uttSynthResults =
            comp.Utts
            |> Seq.map(fun utt -> uttSynthCache.GetOrDefault utt)
            |> Seq.filter(fun (synthResult : UttSynthResult) -> synthResult.HasAudio)
            |> Array.ofSeq
        let outSampleDur =
            uttSynthResults
            |> Seq.map(fun synthResult -> synthResult.SampleOffset + synthResult.AudioSamples.Length)
            |> Seq.appendItem 0
            |> Seq.max
        let outSamples = Array.zeroCreate outSampleDur
        for synthResult in uttSynthResults do
            let samples = synthResult.AudioSamples
            let sampleOffset = synthResult.SampleOffset
            for i in sampleOffset .. sampleOffset + samples.Length - 1 do
                outSamples.[i] <- outSamples.[i] + samples.[i - sampleOffset]
        outSamples

    let renderToFile filePath comp uttSynthCache =
        let outSamples = renderComp comp uttSynthCache
        let sampleProvider = SampleProvider(outSamples)
        let waveProvider = sampleProvider.ToWaveProvider()
        MediaFoundationEncoder.EncodeToAac(waveProvider, filePath, 192000)

module AudioPlayback =
    let fillBuffer(playbackSamplePos, comp : Composition, uttSynthCache, buffer : float32 [], bufferOffset, bufferLength) =
        Array.Clear(buffer, bufferOffset, bufferLength * sizeof<float32>)
        let inline fillBufferSamples(samples : _ []) sampleOffset =
            let startIndex = max playbackSamplePos sampleOffset - playbackSamplePos
            let endIndex = min(playbackSamplePos + bufferLength)(sampleOffset + samples.Length) - playbackSamplePos
            for i in startIndex .. endIndex - 1 do
                buffer.[i + bufferOffset] <- buffer.[i + bufferOffset] + samples.[i + playbackSamplePos - sampleOffset]
        for utt in comp.Utts do
            let uttSynthResult : UttSynthResult = (uttSynthCache : UttSynthCache).GetOrDefault utt
            if uttSynthResult.HasAudio then
                fillBufferSamples uttSynthResult.AudioSamples uttSynthResult.SampleOffset
        if comp.BgAudio.HasAudio then
            fillBufferSamples comp.BgAudio.AudioSamples comp.BgAudio.SampleOffset

type AudioPlaybackEngine() =
    let mutable playbackSamplePos = 0

    member val Comp = Composition.Empty with get, set
    member val UttSynthCache = UttSynthCache.Empty with get, set

    member val PlaybackPositionRefTicks = Stopwatch.GetTimestamp() with get, set
    member x.PlaybackSamplePosition = playbackSamplePos

    member x.ManualSetPlaybackSamplePosition newPos =
        lock x <| fun () ->
            playbackSamplePos <- newPos
            x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()

    interface ISampleProvider with
        member x.WaveFormat = Audio.playbackWaveFormat
        member x.Read(buffer, offset, count) =
            lock x <| fun () ->
                AudioPlayback.fillBuffer(playbackSamplePos, x.Comp, x.UttSynthCache, buffer, offset, count)
                playbackSamplePos <- playbackSamplePos + count
                x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()
            count


