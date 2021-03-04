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


module Audio =
    //let [<Literal>] fs = 44100
    let [<Literal>] fs = 32000
    let [<Literal>] channels = 1
    let waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(fs, channels)

    let sampleToTime(sampleTime : int) =
        TimeSpan.FromSeconds(float sampleTime / float fs)

    let timeToSample(time : TimeSpan) =
        int(time.TotalSeconds * float fs)

    let loadFromStream(fileStream : Stream) =
        use reader = new StreamMediaFoundationReader(fileStream)
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
        Array.concat sampleChunks

    let loadFromFile filePath =
        use fileStream = File.OpenRead filePath
        loadFromStream fileStream

type AudioSegment = {
    SampleOffset : int
    Samples : float32 [] }

type AudioLibrary = {
    Segments : ImmutableDictionary<string, AudioSegment> } with

    static member Empty = {
        Segments = ImmutableDictionary.Empty }

module AudioPlayback =
    let fillBuffer(playbackSamplePos, audioLib, buffer : float32 [], bufferOffset, bufferLength) =
        let { Segments = audioSegs } = audioLib
        Array.Clear(buffer, bufferOffset, bufferLength * sizeof<float32>)
        for audioSeg in audioSegs.Values do
            let { SampleOffset = sampleOffset; Samples = samples } = audioSeg
            let startIndex = max playbackSamplePos sampleOffset - playbackSamplePos
            let endIndex = min(playbackSamplePos + bufferLength)(sampleOffset + samples.Length) - playbackSamplePos
            for i in startIndex .. endIndex - 1 do
                buffer.[i + bufferOffset] <- buffer.[i + bufferOffset] + samples.[i + playbackSamplePos - sampleOffset]

type AudioPlaybackEngine() =
    let mutable playbackSamplePos = 0

    member val AudioLib = AudioLibrary.Empty with get, set

    member val PlaybackPositionRefTicks = 0L with get, set
    member x.PlaybackSamplePosition
        with get() = playbackSamplePos
        and set value = lock x <| fun () ->
            playbackSamplePos <- value
            x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()

    interface ISampleProvider with
        member x.WaveFormat = Audio.waveFormat
        member x.Read(buffer, offset, count) =
            lock x <| fun () ->
                AudioPlayback.fillBuffer(playbackSamplePos, x.AudioLib, buffer, offset, count)
                playbackSamplePos <- playbackSamplePos + count
                x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()
            count


