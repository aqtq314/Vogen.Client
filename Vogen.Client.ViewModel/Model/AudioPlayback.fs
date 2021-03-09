namespace Vogen.Client.Model

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


module AudioSamples =
    let loadFromStream(fileStream : Stream) =
        use cacheStream = new MemoryStream()
        fileStream.CopyTo cacheStream
        let fileBytes = cacheStream.ToArray()
        cacheStream.Position <- 0L

        use reader = new StreamMediaFoundationReader(cacheStream)
        use reader = new MediaFoundationResampler(reader, waveFormat)
        let sampleReader = reader.ToSampleProvider()

        let sampleChunks = List<_>()
        let buffer = Array.zeroCreate<float32>(fs * channels)
        let rec readBuffer() =
            let bytesRead = sampleReader.Read(buffer, 0, buffer.Length)
            if bytesRead > 0 then
                sampleChunks.Add buffer.[..bytesRead]
                readBuffer()
        readBuffer()

        let outSamples = Array.concat sampleChunks
        outSamples.[^0] <- 0f

        fileBytes, outSamples

    let loadFromFile filePath =
        use fileStream = File.OpenRead filePath
        loadFromStream fileStream

module AudioPlayback =
    let fillBuffer(playbackSamplePos, comp : Composition, buffer : float32 [], bufferOffset, bufferLength) =
        Array.Clear(buffer, bufferOffset, bufferLength * sizeof<float32>)
        for utt in comp.Utts do
            let uttAudio = comp.GetUttAudio utt
            if uttAudio.IsSynthed then
                let samples = uttAudio.Samples
                let sampleOffset = uttAudio.SampleOffset
                let startIndex = max playbackSamplePos sampleOffset - playbackSamplePos
                let endIndex = min(playbackSamplePos + bufferLength)(sampleOffset + samples.Length) - playbackSamplePos
                for i in startIndex .. endIndex - 1 do
                    buffer.[i + bufferOffset] <- buffer.[i + bufferOffset] + samples.[i + playbackSamplePos - sampleOffset]

type AudioPlaybackEngine() =
    let mutable playbackSamplePos = 0

    member val Comp = Composition.Empty with get, set

    member val PlaybackPositionRefTicks = 0L with get, set
    member x.PlaybackSamplePosition = playbackSamplePos

    member x.ManualSetPlaybackSamplePosition newPos =
        lock x <| fun () ->
            playbackSamplePos <- newPos
            x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()

    interface ISampleProvider with
        member x.WaveFormat = Audio.waveFormat
        member x.Read(buffer, offset, count) =
            lock x <| fun () ->
                AudioPlayback.fillBuffer(playbackSamplePos, x.Comp, buffer, offset, count)
                playbackSamplePos <- playbackSamplePos + count
                x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()
            count


