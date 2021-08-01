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
open Vogen.Client.ViewModel
open Vogen.Synth


module AudioPlayback =
    let fillBuffer(playbackSamplePos, comp : Composition, uttSynthCache, buffer : float32 [], bufferOffset, bufferLength) =
        Array.Clear(buffer, bufferOffset, bufferLength * sizeof<float32>)
        let inline fillBufferSamples volume (samples : _ []) sampleOffset =
            let startIndex = max playbackSamplePos sampleOffset - playbackSamplePos
            let endIndex = min(playbackSamplePos + bufferLength)(sampleOffset + samples.Length) - playbackSamplePos
            for i in startIndex .. endIndex - 1 do
                buffer.[i + bufferOffset] <- buffer.[i + bufferOffset] + samples.[i + playbackSamplePos - sampleOffset] * volume
        for utt in comp.Utts do
            let uttSynthResult : UttSynthResult = (uttSynthCache : UttSynthCache).GetOrDefault utt
            match uttSynthResult.Audio with
            | None -> ()
            | Some audio ->
                fillBufferSamples 1.0f audio.Samples uttSynthResult.SampleOffset
        if comp.BgAudio.HasAudio then
            fillBufferSamples 0.25f comp.BgAudio.Audio.Samples comp.BgAudio.SampleOffset

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
        member x.WaveFormat = playbackWaveFormat
        member x.Read(buffer, offset, count) =
            lock x <| fun () ->
                AudioPlayback.fillBuffer(playbackSamplePos, x.Comp, x.UttSynthCache, buffer, offset, count)
                playbackSamplePos <- playbackSamplePos + count
                x.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()
            count


