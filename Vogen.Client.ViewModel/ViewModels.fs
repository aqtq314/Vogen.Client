namespace Vogen.Client.ViewModel

open Doaz.Reactive
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
    let activeComp = rp Composition.Empty
    let activeUttSynthCache = rp UttSynthCache.Empty
    let activeSelection = rp CompSelection.Empty
    let undoRedoStack = UndoRedoStack()

    let compFilePathOp = rp None
    let compFileName = rp "Untitled.vog"
    let compIsSaved = rp true

    static let latency = 80
    static let latencyTimeSpan = TimeSpan.FromMilliseconds(float latency)
    let isPlaying = rp false
    let cursorPos = rp 0L
    do  CompositionTarget.Rendering.Add <| fun e ->
            if isPlaying.Value then
                x.PlaybackSyncCursorPos()

    let audioEngine = AudioPlaybackEngine()
    let waveOut = new DirectSoundOut(latency)
    do  waveOut.Init audioEngine
    do  activeComp |> Rpo.leaf(fun comp ->
            audioEngine.Comp <- comp)
    do  activeUttSynthCache |> Rpo.leaf(fun uttSynthCache ->
            audioEngine.UttSynthCache <- uttSynthCache)

    member val CompFilePathOp = compFilePathOp |> Rpo.map id
    member val CompFileName = compFileName |> Rpo.map id
    member x.CompIsSaved = compIsSaved
    member x.ActiveComp = activeComp
    member x.ActiveUttSynthCache = activeUttSynthCache
    member x.ActiveSelection = activeSelection
    member x.UndoRedoStack = undoRedoStack
    member val IsPlaying = isPlaying |> Rpo.map id
    member val CursorPosition = cursorPos |> Rpo.map id

    member x.OpenOrNew filePathOp =
        if isPlaying.Value then x.Stop()
        let fileName, comp, uttSynthCache =
            match filePathOp with
            | None -> "Untitled.vog", Composition.Empty, UttSynthCache.Empty
            | Some filePath ->
                use fileStream = File.OpenRead filePath
                let comp, uttSynthCache = FilePackage.read fileStream
                Path.GetFileName filePath, comp, uttSynthCache
        activeComp |> Rp.set comp
        activeUttSynthCache |> Rp.set uttSynthCache
        activeSelection |> Rp.set CompSelection.Empty
        undoRedoStack.Clear()
        compFilePathOp |> Rp.set filePathOp
        compFileName |> Rp.set fileName
        compIsSaved |> Rp.set true

    member x.New() =
        x.OpenOrNew None

    member x.Open filePath =
        x.OpenOrNew(Some filePath)

    member x.Import filePath =
        let prevComp = !!activeComp
        let prevSelection = !!activeSelection
        if isPlaying.Value then x.Stop()

        let comp, uttSynthCache =
            match Path.GetExtension(filePath : string).ToLower() with
            | ".vog" ->
                use stream = File.OpenRead filePath
                FilePackage.read stream
            | ".vpr" ->
                use stream = File.OpenRead filePath
                let comp = External.loadVpr "man" stream
                comp, UttSynthCache.Create comp.Bpm0
            | ext ->
                raise(KeyNotFoundException($"Unknwon file extension {ext}"))
        let selection =
            CompSelection(None, ImmutableHashSet.CreateRange comp.AllNotes)

        activeComp |> Rp.set comp
        activeUttSynthCache |> Rp.set uttSynthCache
        activeSelection |> Rp.set selection
        undoRedoStack.Clear()
        compFilePathOp |> Rp.set None
        compFileName |> Rp.set(Path.GetFileNameWithoutExtension filePath + ".vog")
        compIsSaved |> Rp.set false

    member x.Save outFilePath =
        use outFileStream = File.Open(outFilePath, FileMode.Create)
        (!!x.ActiveComp, !!x.ActiveUttSynthCache) ||> FilePackage.save outFileStream
        compFilePathOp |> Rp.set(Some outFilePath)
        compFileName |> Rp.set(Path.GetFileName outFilePath)
        compIsSaved |> Rp.set true

    member x.Export outFilePath =
        (!!x.ActiveComp, !!x.ActiveUttSynthCache) ||> AudioSamples.renderToFile outFilePath

    member x.Undo() =
        match undoRedoStack.TryPopUndo() with
        | None -> ()
        | Some(comp, selection) ->
            activeComp |> Rp.set comp
            activeSelection |> Rp.set selection
            compIsSaved |> Rp.set false

    member x.Redo() =
        match undoRedoStack.TryPopRedo() with
        | None -> ()
        | Some(comp, selection) ->
            activeComp |> Rp.set comp
            activeSelection |> Rp.set selection
            compIsSaved |> Rp.set false

    member x.ManualSetCursorPos newCursorPos =
        audioEngine.ManualSetPlaybackSamplePosition(
            float newCursorPos
            |> Midi.toTimeSpan (!!activeComp).Bpm0
            |> Audio.timeToSample)
        cursorPos |> Rp.set newCursorPos

    member x.PlaybackSyncCursorPos() =
        let newCursorPos =
            audioEngine.PlaybackSamplePosition
            |> Audio.sampleToTime
            |> (+)(TimeSpan.FromTicks(Stopwatch.GetTimestamp() - audioEngine.PlaybackPositionRefTicks))
            |> (+)(if isPlaying.Value then -latencyTimeSpan else TimeSpan.Zero)
            |> Midi.ofTimeSpan((!!activeComp).Bpm0) |> round |> int64
        cursorPos |> Rp.set newCursorPos

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
        activeUttSynthCache |> Rp.modify(fun uttSynthCache -> uttSynthCache.Clear())
        compIsSaved |> Rp.set false

    member x.SynthUtt(dispatcher : Dispatcher, singerName, utt) =
        activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
            utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                uttSynthResult.SetIsSynthing true))
        compIsSaved |> Rp.set false

        let bpm0 = (!!activeComp).Bpm0
        Async.Start <| async {
            try try let tUtt = TimeTable.ofUtt bpm0 utt
                    let! tChars = Synth.requestPO tUtt
                    let charGrids = TimeTable.toCharGrids tChars
                    dispatcher.BeginInvoke(fun () ->
                        activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                            utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                                if not uttSynthResult.IsSynthing then uttSynthResult else
                                    uttSynthResult.SetCharGrids charGrids))
                        compIsSaved |> Rp.set false) |> ignore
                    let! f0Samples = Synth.requestF0 tUtt tChars
                    dispatcher.BeginInvoke(fun () ->
                        activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                            utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                                if not uttSynthResult.IsSynthing then uttSynthResult else
                                    uttSynthResult.SetF0Samples f0Samples))
                        compIsSaved |> Rp.set false) |> ignore
                    let! audioContent = Synth.requestAc tChars f0Samples singerName
                    dispatcher.BeginInvoke(fun () ->
                        activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                            utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                                if not uttSynthResult.IsSynthing then uttSynthResult else
                                    uttSynthResult.SetAudio audioContent))
                        compIsSaved |> Rp.set false) |> ignore
                with ex ->
                    Trace.WriteLine ex
            finally
                dispatcher.BeginInvoke(fun () ->
                    activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                            uttSynthResult.SetIsSynthing false))
                    compIsSaved |> Rp.set false) |> ignore }

    member x.Synth(dispatcher, singerName) =
        let comp = !!x.ActiveComp
        x.ActiveUttSynthCache |> Rp.modify(fun uttSynthCache -> uttSynthCache.SlimWith comp)
        let uttSynthCache = !!x.ActiveUttSynthCache
        for utt in comp.Utts do
            let uttSynthResult = uttSynthCache.GetOrDefault utt
            if not uttSynthResult.IsSynthing && not uttSynthResult.HasAudio then
                x.SynthUtt(dispatcher, singerName, utt)


