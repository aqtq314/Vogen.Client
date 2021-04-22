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
open Vogen.Client.Romanization


type ProgramModel() as x =
    let activeComp = rp Composition.Empty
    let activeSelection = rp CompSelection.Empty
    let activeUttSynthCache = rp UttSynthCache.Empty
    let undoRedoStack = UndoRedoStack()

    let mutable suspendUttPanelSync = false
    let uttPanelSingerId = rp Singer.defaultId
    let uttPanelRomScheme = rp Romanizer.defaultId
    do  (activeComp, activeSelection) |> Rpo.map2(fun comp selection -> comp, selection) |> Rpo.leaf(fun (comp, selection) ->
            x.UpdateUttPanelValues(comp, selection))
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
    do  activeComp |> Rpo.leaf(fun comp ->
            audioEngine.Comp <- comp)
    do  activeUttSynthCache |> Rpo.leaf(fun uttSynthCache ->
            audioEngine.UttSynthCache <- uttSynthCache)

    member x.ActiveComp = activeComp :> ReactiveProperty<_>
    member x.ActiveSelection = activeSelection
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

    member x.SetComp(comp, selection) =
        use bulk = Rp.bulkSetter()
        activeComp |> bulk.LockSetProp comp
        activeSelection |> bulk.LockSetProp selection

    member x.OpenOrNew filePathOp =
        if isPlaying.Value then x.Stop()
        let fileName, comp, uttSynthCache =
            match filePathOp with
            | None -> "Untitled.vog", Composition.Empty, UttSynthCache.Empty
            | Some filePath ->
                use fileStream = File.OpenRead filePath
                let comp, uttSynthCache = FilePackage.read fileStream
                Path.GetFileName filePath, comp, uttSynthCache
        x.SetComp(comp, CompSelection.Empty)
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
                let singerId = !!x.UttPanelSingerId
                let romScheme = !!x.UttPanelRomScheme
                let comp = External.loadVpr singerId romScheme stream
                comp, UttSynthCache.Create comp.Bpm0
            | ext ->
                raise(KeyNotFoundException($"Unknwon file extension {ext}"))
        let selection =
            CompSelection(None, ImmutableHashSet.CreateRange comp.AllNotes)

        x.SetComp(comp, selection)
        activeUttSynthCache |> Rp.set uttSynthCache
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
            x.SetComp(comp, selection)
            compIsSaved |> Rp.set false

    member x.Redo() =
        match undoRedoStack.TryPopRedo() with
        | None -> ()
        | Some(comp, selection) ->
            x.SetComp(comp, selection)
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

    member x.LoadAccom audioFile =
        let audioContent = AudioSamples.loadFromFile audioFile
        let comp = !!x.ActiveComp
        let selection = !!x.ActiveSelection
        x.SetComp(comp.SetBgAudio(comp.BgAudio.SetAudio(audioContent).SetSampleOffset 0), selection)

        x.UndoRedoStack.PushUndo(
            LoadBgAudio,
            (comp, selection),
            (!!x.ActiveComp, !!x.ActiveSelection))
        x.CompIsSaved |> Rp.set false

    member x.ClearAccom() =
        let comp = !!x.ActiveComp
        let selection = !!x.ActiveSelection
        if comp.BgAudio.HasAudio then
            x.SetComp(comp.SetBgAudio(comp.BgAudio.SetNoAudio().SetSampleOffset 0), selection)

            x.UndoRedoStack.PushUndo(
                ClearBgAudio,
                (comp, selection),
                (!!x.ActiveComp, !!x.ActiveSelection))
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

    member x.SynthUtt(dispatcher : Dispatcher, utt, [<Optional>] requestDelay) =
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
                                    compIsSaved |> Rp.set false
                                    uttSynthResult.SetCharGrids charGrids))) |> ignore
                    let! f0Samples = Synth.requestF0 tUtt tChars
                    dispatcher.BeginInvoke(fun () ->
                        activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                            utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                                if not uttSynthResult.IsSynthing then uttSynthResult else
                                    compIsSaved |> Rp.set false
                                    uttSynthResult.SetF0Samples f0Samples))) |> ignore
                    // TODO: Not the best way to boost synth order
                    do! Async.Sleep(requestDelay : TimeSpan)
                    let! audioContent = Synth.requestAc tChars f0Samples utt.SingerId
                    dispatcher.BeginInvoke(fun () ->
                        activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                            utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                                if not uttSynthResult.IsSynthing then uttSynthResult else
                                    compIsSaved |> Rp.set false
                                    uttSynthResult.SetAudio audioContent))) |> ignore
                with ex ->
                    Trace.WriteLine ex
            finally
                dispatcher.BeginInvoke(fun () ->
                    activeUttSynthCache |> Rp.modify(fun uttSynthCache ->
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult ->
                            uttSynthResult.SetIsSynthing false))
                    compIsSaved |> Rp.set false) |> ignore }

    member x.Synth dispatcher =
        let comp = !!x.ActiveComp
        x.ActiveUttSynthCache |> Rp.modify(fun uttSynthCache -> uttSynthCache.SlimWith comp)
        let uttSynthCache = !!x.ActiveUttSynthCache
        let mutable requestDelay = TimeSpan.Zero
        for utt in comp.Utts do
            let uttSynthResult = uttSynthCache.GetOrDefault utt
            if not uttSynthResult.IsSynthing && not uttSynthResult.HasAudio then
                x.SynthUtt(dispatcher, utt, requestDelay)
                requestDelay <- requestDelay + TimeSpan.FromSeconds 0.05

    member x.Resynth dispatcher =
        x.ClearAllSynth()
        x.Synth dispatcher

    // TODO: ugly two-way data sync
    member val private PrevNonNullSingerId = !!uttPanelSingerId with get, set
    member val private PrevNonNullRomScheme = !!uttPanelRomScheme with get, set
    member x.UpdateUttPanelValues(comp, selection) =
        if not suspendUttPanelSync then
            suspendUttPanelSync <- true

            let getDisplayValue fallback (values : seq<_>) =
                let iter = values.GetEnumerator()
                if not(iter.MoveNext()) then fallback   // 0 value
                else
                    let head = iter.Current
                    if not(iter.MoveNext()) then head   // 1 value
                    else null                           // 2+ values

            let selectedUtts =
                if selection.SelectedNotes.IsEmpty then Seq.empty else
                    comp.Utts |> Seq.filter(fun utt -> Seq.exists selection.GetIsNoteSelected utt.Notes) |> Seq.cache
            let editingUtts =
                if not(Seq.isEmpty selectedUtts) then selectedUtts else
                    selection.ActiveUtt |> Option.toArray :> _

            do  use bulk = Rp.bulkSetter()
                let newSingerId = editingUtts |> Seq.map(fun utt -> utt.SingerId) |> Seq.distinct |> getDisplayValue x.PrevNonNullSingerId
                let newRomScheme = editingUtts |> Seq.map(fun utt -> utt.RomScheme) |> Seq.distinct |> getDisplayValue x.PrevNonNullRomScheme
                uttPanelSingerId |> bulk.LockSetPropIfDiff newSingerId
                uttPanelRomScheme |> bulk.LockSetPropIfDiff newRomScheme
                if newSingerId <> null then x.PrevNonNullSingerId <- newSingerId
                if newRomScheme <> null then x.PrevNonNullRomScheme <- newRomScheme

            suspendUttPanelSync <- false

    member x.SyncUttPanelEdits(singerId, romScheme) =
        if not suspendUttPanelSync then
            suspendUttPanelSync <- true

            let comp = !!activeComp
            let selection = !!activeSelection
            let selectedUtts =
                if selection.SelectedNotes.IsEmpty then Seq.empty else
                    comp.Utts |> Seq.filter(fun utt -> Seq.exists selection.GetIsNoteSelected utt.Notes) |> Seq.cache
            let editingUtts =
                if not(Seq.isEmpty selectedUtts) then selectedUtts else
                    selection.ActiveUtt |> Option.toArray :> _

            let uttDiffDict =
                ImmutableDictionary.CreateRange(
                    editingUtts
                    |> Seq.map(fun utt ->
                        let newUtt =
                            utt
                            |> fun utt -> if singerId = null || utt.SingerId = singerId then utt else utt.SetSingerId singerId
                            |> fun utt -> if romScheme = null || utt.RomScheme = romScheme then utt else utt.SetRomScheme romScheme
                        KeyValuePair(utt, newUtt))
                    |> Seq.filter(fun (KeyValue(utt, newUtt)) -> utt <> newUtt))

            if uttDiffDict.Count > 0 then
                let newComp = comp.SetUtts(ImmutableArray.CreateRange(comp.Utts, fun utt -> uttDiffDict.GetOrDefault utt utt))
                let newSelection =
                    let activeUtt = selection.ActiveUtt |> Option.map(fun utt -> uttDiffDict.GetOrDefault utt utt)
                    selection.SetActiveUtt activeUtt
                x.SetComp(newComp, newSelection)

                x.UndoRedoStack.PushUndo(
                    EditUttPanelValue,
                    (comp, selection),
                    (!!x.ActiveComp, !!x.ActiveSelection))
                x.CompIsSaved |> Rp.set false
        
            suspendUttPanelSync <- false


