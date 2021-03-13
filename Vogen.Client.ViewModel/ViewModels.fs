namespace Vogen.Client.ViewModel

open Doaz.Reactive
open NAudio
open NAudio.Wave
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Diagnostics
open System.IO
open System.Windows
open System.Windows.Media
open System.Windows.Threading
open Vogen.Client.Controls
open Vogen.Client.Model


type ProgramModel() as x =
    let compFilePathOp = rp None
    let compFileName = rp "Untitled.vog"
    let compIsSaved = rp true
    let activeComp = rp Composition.Empty
    let audioEngine = AudioPlaybackEngine()

    static let latency = 80
    static let latencyTimeSpan = TimeSpan.FromMilliseconds(float latency)
    let isPlaying = rp false
    let cursorPos = rp 0L
    do  CompositionTarget.Rendering.Add <| fun e ->
            if isPlaying.Value then
                x.PlaybackSyncCursorPos()

    let waveOut = new DirectSoundOut(latency)
    do  waveOut.Init audioEngine

    member val CompFilePathOp = compFilePathOp |> Rpo.map id
    member val CompFileName = compFileName |> Rpo.map id
    member val CompIsSaved = compIsSaved |> Rpo.map id
    member val ActiveComp = activeComp |> Rpo.map id
    member val IsPlaying = isPlaying |> Rpo.map id
    member val CursorPosition = cursorPos |> Rpo.map id

    member x.LoadComp(comp, ?isSaved) =
        let isSaved = defaultArg isSaved false
        activeComp |> Rp.set comp
        audioEngine.Comp <- comp
        compIsSaved |> Rp.set isSaved

    member x.LoadFromFile filePathOp =
        let fileName, comp =
            match filePathOp with
            | None -> "Untitled.vog", Composition.Empty
            | Some filePath ->
                use fileStream = File.OpenRead filePath
                Path.GetFileName filePath, FilePackage.read fileStream
        x.LoadComp(comp, isSaved = true)
        compFilePathOp |> Rp.set filePathOp
        compFileName |> Rp.set fileName

    member x.UpdateComp update =
        lock x <| fun () ->
            update !!activeComp
            |>! x.LoadComp

    member x.New() =
        x.Stop()
        x.LoadFromFile None

    member x.Open filePath =
        x.Stop()
        x.LoadFromFile(Some filePath)

    member x.Import filePath =
        x.Stop()
        let comp =
            match Path.GetExtension(filePath : string).ToLower() with
            | ".vog" ->
                use stream = File.OpenRead filePath
                FilePackage.read stream
            | ".vpr" ->
                use stream = File.OpenRead filePath
                External.loadVpr "man" stream
            | ext ->
                raise(KeyNotFoundException($"Unknwon file extension {ext}"))

        x.LoadComp comp
        compFilePathOp |> Rp.set None
        compFileName |> Rp.set(Path.GetFileNameWithoutExtension filePath + ".vog")

    member x.Save outFilePath =
        use outFileStream = File.Open(outFilePath, FileMode.Create)
        !!x.ActiveComp |> FilePackage.save outFileStream
        compIsSaved |> Rp.set true
        compFilePathOp |> Rp.set(Some outFilePath)
        compFileName |> Rp.set(Path.GetFileName outFilePath)

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
            try let! audioContent = Synth.request "gloria" comp.Bpm0 utt
                dispatcher.BeginInvoke(fun () ->
                    x.UpdateComp <| fun comp ->
                        utt |> comp.SetUttAudioSynthed audioContent
                    |> ignore) |> ignore
            with ex ->
                dispatcher.BeginInvoke(fun () ->
                    x.UpdateComp <| fun comp ->
                        utt |> comp.SetUttAudioNoSynth
                    |> ignore) |> ignore}

    member x.Synth dispatcher =
        let comp = !!x.ActiveComp
        for utt in comp.Utts do
            let uttAudio = comp.GetUttAudio utt
            match uttAudio.SynthState with
            | NoSynth -> x.SynthUtt(dispatcher, utt)
            | Synthing | Synthed -> ()


