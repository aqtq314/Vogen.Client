namespace Vogen.Client.ViewModel

open Doaz.Reactive
open NAudio
open NAudio.Wave
open System
open System.Collections.Generic
open System.Diagnostics
open System.Windows
open System.Windows.Media
open Vogen.Client.Controls
open Vogen.Client.Model


type ProgramModel() as x =
    let activeComp = rp Composition.Empty
    let hScrollMax = activeComp |> Rpo.map(fun comp ->
        15360L + (comp.Utts
            |> Seq.collect(fun utt -> utt.Notes)
            |> Seq.map(fun note -> note.Off)
            |> Seq.appendItem 0L
            |> Seq.max))

    let activeAudioLib = rp AudioLibrary.Empty
    let audioEngine = AudioPlaybackEngine()
    do  activeAudioLib |> Rpo.leaf(fun activeAudioLib ->
            audioEngine.AudioLib <- activeAudioLib)

    static let latency = 40
    static let latencyTimeSpan = TimeSpan.FromMilliseconds(float latency)
    let isPlaying = rp false
    let cursorPos = rp 0L
    do  CompositionTarget.Rendering.Add <| fun e ->
            if isPlaying |> Rp.get then
                x.PlaybackSyncCursorPos()

    let waveOut = new DirectSoundOut(latency)
    do  waveOut.Init audioEngine

    member x.ActiveComp = activeComp
    member x.HScrollMax = hScrollMax
    member val IsPlaying = isPlaying |> Rpo.map id
    member val CursorPosition = cursorPos |> Rpo.map id
    member val ActiveAudioLib = activeAudioLib |> Rpo.map id

    member x.Load comp audioLib =
        activeComp |> Rp.set comp
        activeAudioLib |> Rp.set audioLib

    member x.ManualSetCursorPos newCursorPos =
        audioEngine.PlaybackSamplePosition <-
            newCursorPos
            |> Midi.toTimeSpan (!!activeComp).Bpm0
            |> Audio.timeToSample
        cursorPos |> Rp.set newCursorPos

    member x.PlaybackSyncCursorPos() =
        cursorPos |> Rp.set(
            audioEngine.PlaybackSamplePosition
            |> Audio.sampleToTime
            |> (+)(TimeSpan.FromTicks(Stopwatch.GetTimestamp() - audioEngine.PlaybackPositionRefTicks))
            |> (+)(if isPlaying |> Rp.get then -latencyTimeSpan else TimeSpan.Zero)
            |> Midi.ofTimeSpan((!!activeComp).Bpm0))

    member x.Play() =
        waveOut.Play()
        isPlaying |> Rp.set true
        audioEngine.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()
        x.PlaybackSyncCursorPos()

    member x.Stop() =
        waveOut.Stop()
        isPlaying |> Rp.set false
        x.PlaybackSyncCursorPos()


