namespace Vogen.Client.ViewModel

open Doaz.Reactive
open NAudio
open NAudio.Wave
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Diagnostics
open System.Windows
open System.Windows.Media
open System.Windows.Threading
open Vogen.Client.Controls
open Vogen.Client.Model


type ProgramModel() as x =
    let activeComp = rp Composition.Empty
    let audioEngine = AudioPlaybackEngine()

    static let latency = 40
    static let latencyTimeSpan = TimeSpan.FromMilliseconds(float latency)
    let isPlaying = rp false
    let cursorPos = rp 0L
    do  CompositionTarget.Rendering.Add <| fun e ->
            if isPlaying.Value then
                x.PlaybackSyncCursorPos()

    let waveOut = new DirectSoundOut(latency)
    do  waveOut.Init audioEngine

    member val ActiveComp = activeComp |> Rpo.map id
    member val IsPlaying = isPlaying |> Rpo.map id
    member val CursorPosition = cursorPos |> Rpo.map id

    member x.Load comp =
        activeComp |> Rp.set comp
        audioEngine.Comp <- comp

    member x.UpdateComp update =
        lock x <| fun () ->
            update !!activeComp
            |>! x.Load

    member x.ManualSetCursorPos newCursorPos =
        audioEngine.ManualSetPlaybackSamplePosition(
            newCursorPos
            |> Midi.toTimeSpan (!!activeComp).Bpm0
            |> Audio.timeToSample)
        cursorPos |> Rp.set newCursorPos

    member x.PlaybackSyncCursorPos() =
        cursorPos |> Rp.set(
            audioEngine.PlaybackSamplePosition
            |> Audio.sampleToTime
            |> (+)(TimeSpan.FromTicks(Stopwatch.GetTimestamp() - audioEngine.PlaybackPositionRefTicks))
            |> (+)(if isPlaying.Value then -latencyTimeSpan else TimeSpan.Zero)
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

    member x.ClearAllSynth() =
        x.UpdateComp <| fun comp ->
            (comp, comp.Utts)
            ||> Seq.fold(fun comp utt ->
                comp.SetUttAudioNoSynth utt)

    member x.SynthUtt(dispatcher : Dispatcher, utt) =
        let comp = x.UpdateComp <| fun comp ->
            comp.SetUttAudioSynthing utt
        Async.Start <| async {
            let! audioSamples = Synth.request "gloria" comp.Bpm0 utt
            dispatcher.BeginInvoke(fun () ->
                x.UpdateComp <| fun comp ->
                    comp.SetUttAudioSynthed utt audioSamples
                |> ignore) |> ignore }

    member x.Synth dispatcher =
        let comp = !!x.ActiveComp
        for KeyValue(utt, uttAudio) in comp.UttAudios do
            match uttAudio.SynthState with
            | NoSynth -> x.SynthUtt(dispatcher, utt)
            | Synthing | Synthed -> ()


