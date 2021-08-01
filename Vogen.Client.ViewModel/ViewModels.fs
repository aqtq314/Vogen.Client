namespace rec Vogen.Client.ViewModel

open Doaz.Reactive
open NAudio.Wave
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Windows
open System.Windows.Media
open System.Windows.Threading
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Synth
open Vogen.Synth.Romanization

#nowarn "40"


type ProgramModel() as x =
    let activeChart = rp ChartState.Empty
    let activeUttSynthCache = rp UttSynthCache.Empty
    let undoRedoStack = UndoRedoStack()

    let rec synthActorOpRef = ref None
    and getSynthActor(dispatcher : Dispatcher) =
        match !synthActorOpRef with
        | None ->
            let synthActor = SynthActor.create()
            synthActor.Error.Add(fun ex ->
                dispatcher.BeginInvoke(fun () ->
                    MessageBox.Show($"{ex.Message}\r\n{ex.StackTrace}", "SynthActor Error", MessageBoxButton.OK, MessageBoxImage.Error) |> ignore
                    synthActorOpRef := None) |> ignore)
            synthActorOpRef := Some synthActor
            synthActor
        | Some synthActor -> synthActor

    let mutable suspendUttPanelSync = false
    let uttPanelSingerId = rp(if Acoustics.voiceLibs.ContainsKey "Gloria" then "Gloria" else (Seq.tryHead Acoustics.voiceLibIds |> Option.defaultValue ""))
    let uttPanelRomScheme = rp Romanizer.defaultId
    do  activeChart |> Rpo.leaf(fun chart -> x.UpdateUttPanelValues chart)
        uttPanelSingerId |> Rpo.leaf(fun singerId ->
            x.SyncUttPanelEdits(singerId, !!uttPanelRomScheme))
        uttPanelRomScheme |> Rpo.leaf(fun romScheme ->
            x.SyncUttPanelEdits(!!uttPanelSingerId, romScheme))

    let compFilePathOp = rp None
    let compFileName = rp "Untitled.vog"
    let compIsSaved = rp true

    let isPlaying = rp false
    let cursorPos = rp 0L
    do  CompositionTarget.Rendering.Add <| fun e ->
            if isPlaying.Value then
                x.PlaybackSyncCursorPos()

    static let latency = 80
    static let latencyTimeSpan = TimeSpan.FromMilliseconds(float latency)
    let audioEngine = AudioPlaybackEngine()
    let waveOut = new DirectSoundOut(latency)
    do  waveOut.Init audioEngine
    do  activeChart |> Rpo.leaf(fun chart ->
            audioEngine.Comp <- chart.Comp)
    do  activeUttSynthCache |> Rpo.leaf(fun uttSynthCache ->
            audioEngine.UttSynthCache <- uttSynthCache)

    member x.ActiveChart = activeChart
    member x.ActiveUttSynthCache = activeUttSynthCache
    member x.UndoRedoStack = undoRedoStack

    member x.UttPanelSingerId = uttPanelSingerId
    member x.UttPanelRomScheme = uttPanelRomScheme
    member val UttPanelSingerIdWpf = MutableReactivePropertyWpfView(uttPanelSingerId)
    member val UttPanelRomSchemeWpf = MutableReactivePropertyWpfView(uttPanelRomScheme)

    member val CompFilePathOp = compFilePathOp |> Rpo.map id
    member val CompFileName = compFileName |> Rpo.map id
    member x.CompIsSaved = compIsSaved

    member val IsPlaying = isPlaying |> Rpo.map id
    member val CursorPosition = cursorPos |> Rpo.map id

    member x.OpenOrNew filePathOp =
        if isPlaying.Value then x.Stop()
        let fileName, comp, uttSynthCache =
            match filePathOp with
            | None -> "Untitled.vog", Composition.Empty, UttSynthCache.Empty
            | Some filePath ->
                let comp, uttSynthCache = FilePackage.readFromFile filePath
                Path.GetFileName filePath, comp, uttSynthCache
        x.ActiveChart |> Rp.set(ChartState(comp))
        activeUttSynthCache |> Rp.set uttSynthCache
        undoRedoStack.Clear()
        compFilePathOp |> Rp.set filePathOp
        compFileName |> Rp.set fileName
        compIsSaved |> Rp.set true

    member x.New() =
        x.OpenOrNew None

    member x.Open filePath =
        x.OpenOrNew(Some filePath)

    member x.Import filePath =
        let prevChart = !!activeChart
        if isPlaying.Value then x.Stop()

        let comp, uttSynthCache =
            match Path.GetExtension(filePath : string).ToLower() with
            | ".vog" ->
                FilePackage.readFromFile filePath
            | ".vpr" ->
                use stream = File.OpenRead filePath
                let singerId = !!x.UttPanelSingerId
                let romScheme = !!x.UttPanelRomScheme
                let comp = External.loadVpr singerId romScheme stream
                comp, UttSynthCache.Create comp.Bpm0
            | ext ->
                raise(KeyNotFoundException($"Unknwon file extension {ext}"))
        let chart =
            ChartState(comp, None, ImmutableHashSet.CreateRange comp.AllNotes)

        x.ActiveChart |> Rp.set chart
        activeUttSynthCache |> Rp.set uttSynthCache
        undoRedoStack.Clear()
        compFilePathOp |> Rp.set None
        compFileName |> Rp.set(Path.GetFileNameWithoutExtension filePath + ".vog")
        compIsSaved |> Rp.set false

    member x.SaveACopy outFilePath =
        ((!!x.ActiveChart).Comp, !!x.ActiveUttSynthCache) ||> FilePackage.saveToFile outFilePath

    member x.Save outFilePath =
        x.SaveACopy outFilePath
        compFilePathOp |> Rp.set(Some outFilePath)
        compFileName |> Rp.set(Path.GetFileName outFilePath)
        compIsSaved |> Rp.set true

    member x.ExportAudio outFilePath =
        let comp = (!!x.ActiveChart).Comp
        let uttSynthCache = !!x.ActiveUttSynthCache
        let uttSynthResults = [|
            for utt in comp.Utts do
                let synthResult = uttSynthCache.GetOrDefault utt
                match synthResult.Audio with
                | None -> ()
                | Some audio -> yield synthResult.SampleOffset, audio.Samples |]

        AudioSamples.renderToFile outFilePath uttSynthResults

    member x.Undo() =
        match undoRedoStack.TryPopUndo() with
        | None -> ()
        | Some chart ->
            x.ActiveChart |> Rp.set chart
            compIsSaved |> Rp.set false

    member x.Redo() =
        match undoRedoStack.TryPopRedo() with
        | None -> ()
        | Some chart ->
            x.ActiveChart |> Rp.set chart
            compIsSaved |> Rp.set false

    member x.ManualSetCursorPos newCursorPos =
        audioEngine.ManualSetPlaybackSamplePosition(
            float newCursorPos
            |> Midi.toTimeSpan (!!activeChart).Comp.Bpm0
            |> Audio.timeToSample)
        cursorPos |> Rp.set newCursorPos

    member x.PlaybackSyncCursorPos() =
        let newCursorPos =
            audioEngine.PlaybackSamplePosition
            |> Audio.sampleToTime
            |> (+)(TimeSpan.FromTicks(Stopwatch.GetTimestamp() - audioEngine.PlaybackPositionRefTicks))
            |> (+)(if isPlaying.Value then -latencyTimeSpan else TimeSpan.Zero)
            |> Midi.ofTimeSpan((!!activeChart).Comp.Bpm0) |> round |> int64
        cursorPos |> Rp.set newCursorPos

    member x.LoadAccom audioFile =
        let audioSamplesLazy = AudioSamples.loadFromFile audioFile
        let audioSamples = AudioSamples.validate audioSamplesLazy
        let chart = !!x.ActiveChart
        x.ActiveChart |> Rp.set(
            let comp = chart.Comp
            chart.SetComp(comp.SetBgAudio(AudioTrack(0, audioFile, audioSamples))))

        x.UndoRedoStack.PushUndo(LoadBgAudio, chart, !!x.ActiveChart)
        x.CompIsSaved |> Rp.set false

    member x.ClearAccom() =
        let chart = !!x.ActiveChart
        if chart.Comp.BgAudio.HasAudio then
            x.ActiveChart |> Rp.set(chart.SetComp(chart.Comp.SetBgAudio(AudioTrack(0))))

            x.UndoRedoStack.PushUndo(ClearBgAudio, chart, !!x.ActiveChart)
            x.CompIsSaved |> Rp.set false

    member x.Play() =
        waveOut.Play()
        isPlaying |> Rp.set true
        audioEngine.PlaybackPositionRefTicks <- Stopwatch.GetTimestamp()
        x.PlaybackSyncCursorPos()

    member x.Stop() =
        waveOut.Stop()
        isPlaying |> Rp.set false
        x.PlaybackSyncCursorPos()

    member x.PlayOrStop() =
        if not !!isPlaying then
            x.Play()
        else
            x.Stop()

    member x.ClearAllSynth() =
        activeUttSynthCache |> Rp.modify(fun uttSynthCache -> uttSynthCache.Clear())
        compIsSaved |> Rp.set false

    member x.SynthUtt(dispatcher : Dispatcher) utt = async {
        try try dispatcher.Invoke(fun () ->
                    activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                            uttSynthResult.SetIsSynthing true))
                    compIsSaved |> Rp.set false) |> ignore

                let updateSynthResultInCache updateUttSynthResult utt =
                    let success = dispatcher.Invoke(fun () ->
                        let uttSynthCache = !!activeUttSynthCache
                        let uttSynthResult = uttSynthCache.GetOrDefault utt
                        if uttSynthResult.IsSynthing then
                            compIsSaved |> Rp.set false
                            activeUttSynthCache |> Rp.set(
                                utt |> uttSynthCache.UpdateUttSynthResult updateUttSynthResult)
                            true
                        else
                            false)
                    if not success then
                        raise(OperationCanceledException("Utt requested for synth has already be changed."))

                let synthActor = getSynthActor dispatcher
                let tUtt = TimeTable.ofUtt utt
                let uttDurTimeSpan = TimeTable.frameToTime(float tUtt.UttDur)
                if uttDurTimeSpan >= TimeSpan.FromSeconds 30.0 then
                    let msgboxResult = dispatcher.Invoke(fun () ->
                        MessageBox.Show(
                            $"当前合成乐句过长（{uttDurTimeSpan.TotalSeconds}秒），可能会消耗大量内存。是否继续合成？",
                            $"合成乐句过长", MessageBoxButton.YesNo, MessageBoxImage.Exclamation))
                    if msgboxResult <> MessageBoxResult.Yes then
                        raise(OperationCanceledException("Utt synth request has been cancelled by user."))

                let! tChars = synthActor |> SynthActor.post(fun reply -> Choice1Of3(reply, tUtt.RomScheme, tUtt.UttDur, tUtt.Chars))
                let charGrids = TimeTable.toCharGrids tChars
                utt |> updateSynthResultInCache(fun uttSynthResult -> uttSynthResult.SetCharGrids charGrids)

                let! f0 = synthActor |> SynthActor.post(fun reply -> Choice2Of3(reply, tUtt.RomScheme, tChars))
                utt |> updateSynthResultInCache(fun uttSynthResult -> uttSynthResult.SetF0Samples f0)

                let sampleOffset = UttSynthResult.GetSampleOffset utt
                let! mgc, bap = synthActor |> SynthActor.post(fun reply -> Choice3Of3(reply, tUtt.RomScheme, utt.SingerId, f0, tChars))
                let audioSamples = World.synthesize32 f0 mgc bap
                let audioFileBytes = Array.empty
                utt |> updateSynthResultInCache(fun uttSynthResult -> uttSynthResult.SetAudio(AudioSamples.create audioFileBytes audioSamples))
                return true

            with ex ->
                Trace.WriteLine ex
                dispatcher.BeginInvoke(fun () ->
                    MessageBox.Show(
                        $"合成失败: {ex.Message}\r\n\r\n{ex.StackTrace}", "合成失败",
                        MessageBoxButton.OK, MessageBoxImage.Error) |> ignore) |> ignore
                return false

        finally
            dispatcher.BeginInvoke(fun () ->
                activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                    utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                        uttSynthResult.SetIsSynthing false))
                compIsSaved |> Rp.set false) |> ignore }

    // TODO: cancellation and multiple worker prevention
    member x.Synth(dispatcher : Dispatcher) =
        let chart = !!x.ActiveChart
        let uttSynthCache = (!!x.ActiveUttSynthCache).SlimWith chart.Comp
        x.ActiveUttSynthCache |> Rp.set uttSynthCache

        List.ofSeq chart.Comp.Utts
        |> fix(fun synthNext uttList -> async {
            match uttList with
            | [] -> ()
            | utt :: uttListCont ->
                let needSynth = dispatcher.Invoke(fun () ->
                    let uttSynthCache = !!x.ActiveUttSynthCache
                    let uttSynthResult = uttSynthCache.GetOrDefault utt
                    not uttSynthResult.IsSynthing && not uttSynthResult.HasAudio)
                if needSynth then
                    let! success = utt |> x.SynthUtt dispatcher
                    if success then
                        return! synthNext uttListCont
                else
                    return! synthNext uttListCont })
        |> Async.Start

    member x.Resynth dispatcher =
        x.ClearAllSynth()
        x.Synth dispatcher

    // TODO: ugly two-way data sync
    member val private PrevNonNullSingerId = !!uttPanelSingerId with get, set
    member val private PrevNonNullRomScheme = !!uttPanelRomScheme with get, set
    member x.UpdateUttPanelValues chart =
        if not suspendUttPanelSync then
            suspendUttPanelSync <- true

            let getDisplayValue fallback (values : seq<_>) =
                let iter = values.GetEnumerator()
                if not(iter.MoveNext()) then fallback   // 0 value
                else
                    let head = iter.Current
                    if not(iter.MoveNext()) then head   // 1 value
                    else null                           // 2+ values

            let selectedUtts = if chart.SelectedNotes.IsEmpty then Seq.empty else chart.UttsWithSelection :> _
            let editingUtts = if not(Seq.isEmpty selectedUtts) then selectedUtts else chart.ActiveUtt |> Option.toArray :> _

            do  use bulk = Rp.bulkSetter()
                let newSingerId = editingUtts |> Seq.map(fun utt -> utt.SingerId) |> Seq.distinct |> getDisplayValue x.PrevNonNullSingerId
                let newRomScheme = editingUtts |> Seq.map(fun utt -> utt.RomScheme) |> Seq.distinct |> getDisplayValue x.PrevNonNullRomScheme
                uttPanelSingerId |> bulk.LockSetPropIfDiff newSingerId
                uttPanelRomScheme |> bulk.LockSetPropIfDiff newRomScheme
                if newSingerId <> null then x.PrevNonNullSingerId <- newSingerId
                if newRomScheme <> null then x.PrevNonNullRomScheme <- newRomScheme

            suspendUttPanelSync <- false

    member x.SyncUttPanelEdits(newSingerId, newRomScheme) =
        if not suspendUttPanelSync then
            suspendUttPanelSync <- true

            let chart = !!activeChart
            let selectedUtts = if chart.SelectedNotes.IsEmpty then Seq.empty else chart.UttsWithSelection :> _
            let editingUtts = if not(Seq.isEmpty selectedUtts) then selectedUtts else chart.ActiveUtt |> Option.toArray :> _

            let uttDiffDict =
                ImmutableDictionary.CreateRange(
                    editingUtts
                    |> Seq.map(fun utt ->
                        let newUtt =
                            utt
                            |> fun utt -> if newSingerId = null || utt.SingerId = newSingerId then utt else utt.SetSingerId newSingerId
                            |> fun utt -> if newRomScheme = null || utt.RomScheme = newRomScheme then utt else utt.SetRomScheme newRomScheme
                        KeyValuePair(utt, newUtt))
                    |> Seq.filter(fun (KeyValue(utt, newUtt)) -> utt <> newUtt))

            if uttDiffDict.Count > 0 then
                let newChart =
                    let newComp = chart.Comp.SetUtts(ImmutableArray.CreateRange(chart.Comp.Utts, fun utt -> uttDiffDict.GetOrDefault utt utt))
                    let activeUtt = chart.ActiveUtt |> Option.map(fun utt -> uttDiffDict.GetOrDefault utt utt)
                    chart.SetActiveUtt(newComp, activeUtt)
                x.ActiveChart |> Rp.set newChart

                x.UndoRedoStack.PushUndo(EditUttPanelValue, chart, !!x.ActiveChart)
                x.CompIsSaved |> Rp.set false

                if newSingerId <> null then x.PrevNonNullSingerId <- newSingerId
                if newRomScheme <> null then x.PrevNonNullRomScheme <- newRomScheme

            suspendUttPanelSync <- false


