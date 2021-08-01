namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Runtime.InteropServices
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.ViewModel
open Vogen.Synth
open Vogen.Synth.Romanization

#nowarn "40"


type ChartMouseEvent =
    | ChartMouseDown of e : MouseButtonEventArgs
    | ChartMouseMove of e : MouseEventArgs
    | ChartMouseRelease of e : MouseEventArgs
    | ChartMouseEnter of e : MouseEventArgs
    | ChartMouseLeave of e : MouseEventArgs

    static member BindEvents push (x : NoteChartEditBase) =
        x.MouseDown.Add(fun e ->
            push(ChartMouseDown e)
            e.Handled <- true)

        x.MouseMove.Add(fun e ->
            push(ChartMouseMove e)
            e.Handled <- true)

        x.LostMouseCapture.Add(fun e ->
            push(ChartMouseRelease e)
            e.Handled <- true)

        x.MouseEnter.Add(fun e ->
            push(ChartMouseEnter e)
            e.Handled <- true)

        x.MouseLeave.Add(fun e ->
            push(ChartMouseLeave e)
            e.Handled <- true)

type NoteChartEditPanelBase() =
    inherit UserControl()

    member x.Quantization
        with get() = x.GetValue NoteChartEditPanelBase.QuantizationProperty :?> int64
        and set(v : int64) = x.SetValue(NoteChartEditPanelBase.QuantizationProperty, box v)
    static member val QuantizationProperty =
        Dp.reg<int64, NoteChartEditPanelBase> "Quantization"
            (Dp.Meta(Midi.ppqn / 2L, Dp.MetaFlags.AffectsRender))

    member x.Snap
        with get() = x.GetValue NoteChartEditPanelBase.SnapProperty :?> bool
        and set(v : bool) = x.SetValue(NoteChartEditPanelBase.SnapProperty, box v)
    static member val SnapProperty =
        Dp.reg<bool, NoteChartEditPanelBase> "Snap"
            (Dp.Meta(true, Dp.MetaFlags.AffectsRender))

    member x.ProgramModel = x.DataContext :?> ProgramModel

    abstract ChartEditor : ChartEditor
    default x.ChartEditor = Unchecked.defaultof<_>
    abstract ChartEditorAdornerLayer : ChartEditorAdornerLayer
    default x.ChartEditorAdornerLayer = Unchecked.defaultof<_>
    abstract RulerGrid : RulerGrid
    default x.RulerGrid = Unchecked.defaultof<_>
    abstract SideKeyboard : SideKeyboard
    default x.SideKeyboard = Unchecked.defaultof<_>
    abstract BgAudioDisplay : BgAudioDisplay
    default x.BgAudioDisplay = Unchecked.defaultof<_>
    abstract HScrollZoom : ChartScrollZoomKitBase
    default x.HScrollZoom = Unchecked.defaultof<_>
    abstract VScrollZoom : ChartScrollZoomKitBase
    default x.VScrollZoom = Unchecked.defaultof<_>
    abstract LyricPopup : TextBoxPopupBase
    default x.LyricPopup = Unchecked.defaultof<_>
    abstract ChartEditorContextMenu : ContextMenu
    default x.ChartEditorContextMenu = Unchecked.defaultof<_>

    member x.PixelToPulse xPos =
        let quarterWidth = x.ChartEditor.QuarterWidth
        let hOffset = x.ChartEditor.HOffsetAnimated
        pixelToPulse quarterWidth hOffset xPos

    member x.PixelToPitch yPos =
        let actualHeight = x.ChartEditor.ActualHeight
        let keyHeight = x.ChartEditor.KeyHeight
        let vOffset = x.ChartEditor.VOffsetAnimated
        pixelToPitch keyHeight actualHeight vOffset yPos

    member x.PulseToPixel pulses =
        let quarterWidth = x.ChartEditor.QuarterWidth
        let hOffset = x.ChartEditor.HOffsetAnimated
        pulseToPixel quarterWidth hOffset pulses

    member x.PitchToPixel pitch =
        let actualHeight = x.ChartEditor.ActualHeight
        let keyHeight = x.ChartEditor.KeyHeight
        let vOffset = x.ChartEditor.VOffsetAnimated
        pitchToPixel keyHeight actualHeight vOffset pitch

    member x.Quantize timeSig pulses =
        let quantization = x.Quantization
        let snap = x.Snap
        quantize snap quantization timeSig pulses

    member x.QuantizeCeil timeSig pulses =
        let quantization = x.Quantization
        let snap = x.Snap
        quantizeCeil snap quantization timeSig pulses

    member x.EditSelectedNoteLyrics() =
        let chart = !!x.ProgramModel.ActiveChart
        match chart.ActiveUtt with
        | None -> ()
        | Some utt ->
            let uttSelectedNotes = utt.Notes |> Seq.filter chart.GetIsNoteSelected |> ImmutableArray.CreateRange
            let uttSelectedLyricNotes = uttSelectedNotes.RemoveAll(fun note -> note.IsHyphen)
            if uttSelectedLyricNotes.Length > 0 then
                let keyHeight = x.ChartEditor.KeyHeight
                let minPulse = uttSelectedLyricNotes.[0].On
                let maxPulse = uttSelectedLyricNotes.[^0].Off
                let minPitch = uttSelectedLyricNotes |> Seq.map(fun note -> note.Pitch) |> Seq.min
                let maxPitch = uttSelectedLyricNotes |> Seq.map(fun note -> note.Pitch) |> Seq.max

                let xMin = x.PulseToPixel(float minPulse)
                let xMax = x.PulseToPixel(float maxPulse)
                let yMin = x.PitchToPixel(float maxPitch) - half keyHeight
                let yMax = x.PitchToPixel(float minPitch) + half keyHeight
                x.LyricPopup.PlacementRectangle <- Rect(xMin, yMin, xMax - xMin, yMax - yMin)
                x.LyricPopup.MinWidth <- xMax - xMin

                let initLyricText =
                    uttSelectedLyricNotes
                    |> Seq.map(fun note ->
                        let lyricOrSpace = if String.IsNullOrEmpty note.Lyric then " " else note.Lyric
                        lyricOrSpace + note.Rom)
                    |> String.concat ""
                    |> fun s -> s.TrimStart()

                let chart = chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.CreateRange uttSelectedLyricNotes)
                x.ProgramModel.ActiveChart |> Rp.set chart

                let undoWriter = x.ProgramModel.UndoRedoStack.BeginPushUndo(EditNoteLyric, chart)

                let candidateLyricNotes =
                    ImmutableArray.CreateRange(seq {
                        yield! uttSelectedLyricNotes
                        yield! utt.Notes |> Seq.skipWhile((<>) uttSelectedLyricNotes.[^0]) |> Seq.skip 1
                            |> Seq.filter(fun note -> not note.IsHyphen) })

                Task.Run(fun () -> Romanizer.get utt.RomScheme) |> ignore

                let revertChanges() =
                    x.ProgramModel.ActiveChart |> Rp.set chart
                    undoWriter.UnpushUndo()

                x.LyricPopup.Open initLyricText revertChanges <| fun lyricText ->
                    let lyricText = lyricText.Trim()

                    let pChar = @"[\u3400-\u4DBF\u4E00-\u9FFF]"
                    let pRom = @"([a-z\-]+:)?[A-Za-z0-9]+"
                    let pattern = Regex($@"\G\s*(?<ch>{pChar})(?<rom>({pRom})?)|\G\s*(?<ch>)(?<rom>{pRom})")
                    let matches = pattern.Matches lyricText

                    let matchedCharCount =
                        if matches.Count = 0 then 0 else
                            let lastCapture = matches.[matches.Count - 1]
                            lastCapture.Index + lastCapture.Length
                    if matchedCharCount <> lyricText.Length then
                        // TODO: Error prompt
                        Error()

                    else
                        let noteLyrics = matches |> Seq.map(fun m -> m.Groups.["ch"].Value) |> Array.ofSeq
                        let noteRoms = matches |> Seq.map(fun m -> m.Groups.["rom"].Value) |> Array.ofSeq
                        let noteRoms =
                            (Romanizer.get utt.RomScheme).Convert noteLyrics noteRoms
                            |> Array.map(fun roms -> roms.[0])

                        // DiffDict: no existance -> no modification
                        let noteDiffDict =
                            Seq.zip3 candidateLyricNotes noteLyrics noteRoms
                            |> Seq.filter(fun (note, newLyric, newRom) ->
                                note.Lyric <> newLyric.ToString() || note.Rom <> newRom)
                            |> Seq.map(fun (note, newLyric, newRom) ->
                                KeyValuePair(note, note.SetText(newLyric.ToString(), newRom)))
                            |> ImmutableDictionary.CreateRange

                        if noteDiffDict.Count = 0 then
                            x.ProgramModel.ActiveChart |> Rp.set(
                                chart.SetSelectedNotes(chart.Comp,
                                    ImmutableHashSet.CreateRange(
                                        candidateLyricNotes
                                        |> Seq.take(min matches.Count candidateLyricNotes.Length))))
                            undoWriter.UnpushUndo()

                        else
                            let newUtt =
                                utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note ->
                                    noteDiffDict.GetOrDefault note note))
                            let newSelectedNotes =
                                ImmutableHashSet.CreateRange(
                                    candidateLyricNotes
                                    |> Seq.take(min matches.Count candidateLyricNotes.Length)
                                    |> Seq.map(fun note -> noteDiffDict.GetOrDefault note note))

                            x.ProgramModel.ActiveChart |> Rp.set(
                                ChartState(
                                    chart.Comp.SetUtts(chart.Comp.Utts.Replace(utt, newUtt)),
                                    Some newUtt,
                                    newSelectedNotes))

                            undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                            x.ProgramModel.CompIsSaved |> Rp.set false

                        Ok()

    member x.CutSelectedNotes() = result {
        do! x.CopySelectedNotes()
        x.DeleteSelectedNotes CutNote }

    member x.CopySelectedNotes() = result {
        let chart = !!x.ProgramModel.ActiveChart
        if chart.SelectedNotes.Count > 0 then
            try let minNoteOn = chart.SelectedNotes |> Seq.map(fun note -> note.On) |> Seq.min

                let getClipUtt(utt : Utterance) =
                    let selectedNotes = utt.Notes |> Seq.filter chart.GetIsNoteSelected |> ImmutableArray.CreateRange
                    if selectedNotes.Length = 0 then None
                    else Some(utt.SetNotes selectedNotes)

                let activeClipUttOp = chart.ActiveUtt |> Option.bind getClipUtt

                let otherClipUtts =
                    match chart.ActiveUtt with
                    | None -> chart.Comp.Utts
                    | Some activeUtt -> chart.Comp.Utts.Remove activeUtt
                    |> Seq.choose getClipUtt

                FilePackage.toClipboardText minNoteOn activeClipUttOp otherClipUtts
                |> Clipboard.SetText

            with ex ->
                MessageBox.Show(
                    $"{ex.Message}\r\n\r\n{ex.StackTrace}", "复制失败",
                    MessageBoxButton.OK, MessageBoxImage.Error) |> ignore

                return! Error() }

    member x.Paste() =
        try let hScrollValue = x.HScrollZoom.ScrollValue
            let chart = !!x.ProgramModel.ActiveChart
            let chart = chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Empty)

            let clipboardText = Clipboard.GetText()
            let minNoteOn = x.QuantizeCeil chart.Comp.TimeSig0 (int64 hScrollValue)
            let activeClipUttOp, otherClipUtts = FilePackage.ofClipboardText chart.Comp.Bpm0 minNoteOn clipboardText
            let newSelectedNotes =
                ImmutableHashSet.CreateRange(
                    Seq.append(Option.toArray activeClipUttOp) otherClipUtts
                    |> Seq.collect(fun utt -> utt.Notes))

            let newChart =
                match activeClipUttOp, chart.ActiveUtt with
                | Some activeClipUtt, Some activeUtt ->
                    let newActiveUtt = activeUtt.UpdateNotes(fun notes -> notes.AddRange activeClipUtt.Notes)
                    ChartState(
                        chart.Comp.UpdateUtts(fun utts -> utts.Replace(activeUtt, newActiveUtt).AddRange otherClipUtts),
                        Some newActiveUtt, newSelectedNotes)
                | Some activeClipUtt, None ->
                    ChartState(
                        chart.Comp.UpdateUtts(fun utts -> utts.AddRange(Seq.prependItem activeClipUtt otherClipUtts)),
                        Some activeClipUtt, newSelectedNotes)
                | None, _ ->
                    ChartState(
                        chart.Comp.UpdateUtts(fun utts -> utts.AddRange otherClipUtts),
                        None, newSelectedNotes)

            x.ProgramModel.ActiveChart |> Rp.set newChart

            x.ProgramModel.UndoRedoStack.PushUndo(PasteNote, chart, !!x.ProgramModel.ActiveChart)
            x.ProgramModel.CompIsSaved |> Rp.set false

        with | ex ->
            System.Media.SystemSounds.Exclamation.Play()

    member x.DeleteSelectedNotes undoDesc =
        let chart = !!x.ProgramModel.ActiveChart
        if not chart.SelectedNotes.IsEmpty then
            // DelDict: no existance -> deletion
            let uttDelDict =
                chart.Comp.Utts
                |> Seq.choose(fun utt ->
                    let newNotes = utt.Notes.RemoveAll(Predicate(chart.GetIsNoteSelected))
                    if newNotes.Length = 0 then None
                    elif newNotes.Length = utt.Notes.Length then Some(KeyValuePair(utt, utt))
                    else Some(KeyValuePair(utt, utt.SetNotes newNotes)))
                |> ImmutableDictionary.CreateRange
            let newActiveUtt = chart.ActiveUtt |> Option.bind(uttDelDict.TryGetValue >> Option.ofByRef)
            x.ProgramModel.ActiveChart |> Rp.set(
                ChartState(
                    chart.Comp.SetUtts(ImmutableArray.CreateRange uttDelDict.Values),
                    newActiveUtt, ImmutableHashSet.Empty))

            x.ProgramModel.UndoRedoStack.PushUndo(
                undoDesc, chart, !!x.ProgramModel.ActiveChart)
            x.ProgramModel.CompIsSaved |> Rp.set false

    member x.DeleteSelectedNotes() =
        x.DeleteSelectedNotes DeleteNote

    member x.SelectAll() =
        let chart = !!x.ProgramModel.ActiveChart
        x.ProgramModel.ActiveChart |> Rp.set(
            chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.CreateRange chart.Comp.AllNotes))

    member x.BlurUtt() =
        let chart = !!x.ProgramModel.ActiveChart
        match chart.ActiveUtt with
        | None -> ()
        | Some _ -> x.ProgramModel.ActiveChart |> Rp.set(chart.SetActiveUtt(chart.Comp, None))

    member x.BindBehaviors() =
        let rec mouseMidDownDragging(prevMousePos : Point, idle)(edit : NoteChartEditBase) = behavior {
            match! () with
            | ChartMouseMove e ->
                let hOffset = edit.HOffsetAnimated
                let vOffset = edit.VOffsetAnimated
                let quarterWidth = edit.QuarterWidth
                let keyHeight = edit.KeyHeight

                let mousePos = e.GetPosition edit
                if edit.CanScrollH then
                    let xDelta = pixelToPulse quarterWidth 0.0 (mousePos.X - prevMousePos.X)
                    x.HScrollZoom.EnableAnimation <- false
                    x.HScrollZoom.ScrollValue <- hOffset - xDelta
                    x.HScrollZoom.EnableAnimation <- true
                if edit.CanScrollV then
                    let yDelta = pixelToPitch keyHeight 0.0 0.0 (mousePos.Y - prevMousePos.Y)
                    x.VScrollZoom.EnableAnimation <- false
                    x.VScrollZoom.ScrollValue <- vOffset - yDelta
                    x.VScrollZoom.EnableAnimation <- true

                return! edit |> mouseMidDownDragging(mousePos, idle)

            | ChartMouseRelease e -> return! idle()

            | _ -> return! edit |> mouseMidDownDragging(prevMousePos, idle) }

        let enumerateUttsByDepth(activeUtt, utts : ImmutableArray<Utterance>) =
            match activeUtt with
            | Some activeUtt -> seq {
                yield activeUtt
                for uttIndex in utts.Length - 1 .. -1 .. 0 do
                    if utts.[uttIndex] <> activeUtt then
                        yield utts.[uttIndex] }
            | None -> seq {
                for uttIndex in utts.Length - 1 .. -1 .. 0 do
                    yield utts.[uttIndex] }

        let findMouseOverNote(mousePos : Point) activeUtt utts (edit : ChartEditor) =
            let mousePulse = x.PixelToPulse mousePos.X |> int64
            let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

            let uttsReordered = enumerateUttsByDepth(activeUtt, utts)
            Seq.tryHead <| seq {
                for utt in uttsReordered do
                    for noteIndex in utt.Notes.Length - 1 .. -1 .. 0 do
                        let note = utt.Notes.[noteIndex]
                        if mousePulse |> between note.On note.Off && mousePitch = note.Pitch then
                            yield utt, note }
            |> Option.map(fun (utt, note) ->
                let x0 = x.PulseToPixel (float note.On)
                let x1 = x.PulseToPixel (float note.Off)
                let noteDragType =
                    if   mousePos.X <= min(x0 + 10.0)(lerp x0 x1 0.2) then NoteDragResizeLeft
                    elif mousePos.X >= max(x1 - 10.0)(lerp x0 x1 0.8) then NoteDragResizeRight
                    else NoteDragMove
                utt, note, noteDragType)

        let mouseToCursorPos(mousePos : Point) =
            int64(x.PixelToPulse mousePos.X) |> NoteChartEditBase.CoerceCursorPosition x.RulerGrid

        let hintSetNone() =
            x.ChartEditorAdornerLayer.EditorHint <- None

        let hintSetGhostCursor mousePos =
            let chart = !!x.ProgramModel.ActiveChart
            let cursorPos = mouseToCursorPos mousePos |> x.Quantize chart.Comp.TimeSig0
            x.ChartEditorAdornerLayer.EditorHint <- Some(GhostCursor cursorPos)

        let hintSetMouseOverNote mousePos =
            let mouseOverNoteOp =
                let chart = !!x.ProgramModel.ActiveChart
                findMouseOverNote mousePos chart.ActiveUtt chart.Comp.Utts x.ChartEditor

            x.ChartEditorAdornerLayer.EditorHint <- Option.map HoverNote mouseOverNoteOp

        let hintSetGhostNote mousePos =
            let edit = x.ChartEditor
            let minKey = edit.MinKey
            let maxKey = edit.MaxKey
            let chart = !!x.ProgramModel.ActiveChart
            let mouseDownNoteOp = findMouseOverNote mousePos chart.ActiveUtt ImmutableArray.Empty edit
            let note =
                match mouseDownNoteOp with
                | None ->
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int
                    let noteOn = mousePulse |> x.Quantize chart.Comp.TimeSig0 |> max 0L
                    let noteOff = noteOn + 1L |> x.QuantizeCeil chart.Comp.TimeSig0
                    let notePitch = mousePitch |> clamp minKey maxKey
                    Note(notePitch, "", "du", noteOn, noteOff - noteOn)

                | Some(mouseDownUtt, mouseDownNote, noteDragType) ->
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let noteOn =
                        mousePulse
                        |> min(mouseDownNote.Off - 1L)
                        |> x.Quantize chart.Comp.TimeSig0
                        |> max mouseDownNote.On
                    Note(mouseDownNote.Pitch, "-", "-", noteOn, mouseDownNote.Off - noteOn)

            x.ChartEditorAdornerLayer.EditorHint <- Some(GhostNote note)

        x.ChartEditor |> ChartMouseEvent.BindEvents(
            let edit = x.ChartEditor

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    let keyboardModifiers = Keyboard.Modifiers

                    match e.ChangedButton with
                    | MouseButton.Left when keyboardModifiers.IsAlt ->
                        hintSetNone()
                        let chart = !!x.ProgramModel.ActiveChart

                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos chart.ActiveUtt ImmutableArray.Empty edit
                        match mouseDownNoteOp with
                        | None ->
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    WriteNote, chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Empty))

                            let minKey = edit.MinKey
                            let maxKey = edit.MaxKey
                            let mousePulse = x.PixelToPulse mousePos.X |> int64
                            let mousePitch = x.PixelToPitch mousePos.Y |> round |> int
                            let maxNoteOn = mousePulse |> x.Quantize chart.Comp.TimeSig0 |> max 0L
                            let minNoteOff = maxNoteOn + 1L |> x.QuantizeCeil chart.Comp.TimeSig0

                            let buildNewNote mousePulse mousePitch =
                                let noteOn = min maxNoteOn (mousePulse |> x.Quantize chart.Comp.TimeSig0) |> max 0L
                                let noteOff = max minNoteOff (mousePulse |> x.QuantizeCeil chart.Comp.TimeSig0)
                                let notePitch = mousePitch |> clamp minKey maxKey
                                Note(notePitch, "", "du", noteOn, noteOff - noteOn)

                            let buildNewComp =
                                match chart.ActiveUtt with
                                | None ->
                                    let singerId = !!x.ProgramModel.UttPanelSingerId
                                    let romScheme = !!x.ProgramModel.UttPanelRomScheme
                                    fun note ->
                                        let utt = Utterance(singerId, romScheme, chart.Comp.Bpm0, ImmutableArray.Create(note : Note))
                                        ChartState(
                                            chart.Comp.UpdateUtts(fun utts -> utts.Add utt),
                                            Some utt, ImmutableHashSet.Create note)
                                | Some activeUtt ->
                                    fun note ->
                                        let utt = activeUtt.UpdateNotes(fun notes -> notes.Add note)
                                        ChartState(
                                            chart.Comp.UpdateUtts(fun utts -> utts.Replace(activeUtt, utt)),
                                            Some utt, ImmutableHashSet.Create note)

                            let note = buildNewNote mousePulse mousePitch
                            let chart = buildNewComp note
                            x.ProgramModel.ActiveChart |> Rp.set chart

                            MidiPlayback.playPitch note.Pitch
                            undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                            x.ProgramModel.CompIsSaved |> Rp.set false

                            let writeNoteArgs = buildNewNote, buildNewComp, note, undoWriter
                            return! writingNote writeNoteArgs

                        | Some(mouseDownUtt, mouseDownNote, noteDragType) ->
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    WriteHyphenNote, chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Create mouseDownNote))

                            let minKey = edit.MinKey
                            let maxKey = edit.MaxKey
                            let mousePulse = x.PixelToPulse mousePos.X |> int64

                            let buildNewNote mousePulse mousePitch =
                                let noteOn =
                                    mousePulse
                                    |> min(mouseDownNote.Off - 1L)
                                    |> x.Quantize chart.Comp.TimeSig0
                                    |> max mouseDownNote.On
                                let notePitch = mousePitch |> clamp minKey maxKey
                                Note(notePitch, "-", "-", noteOn, mouseDownNote.Off - noteOn)

                            let buildNewComp(note : Note) =
                                let utt =
                                    if note.On = mouseDownNote.On then
                                        mouseDownUtt.UpdateNotes(fun notes -> notes.Replace(mouseDownNote, note))
                                    else
                                        mouseDownUtt.UpdateNotes(fun notes ->
                                            notes.Remove(mouseDownNote).AddRange([| mouseDownNote.SetOff note.On; note |]))
                                ChartState(
                                    chart.Comp.UpdateUtts(fun utts -> utts.Replace(mouseDownUtt, utt)),
                                    Some utt, ImmutableHashSet.Create note)

                            let note = buildNewNote mousePulse mouseDownNote.Pitch
                            let chart = buildNewComp note
                            x.ProgramModel.ActiveChart |> Rp.set chart

                            MidiPlayback.playPitch note.Pitch
                            undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                            x.ProgramModel.CompIsSaved |> Rp.set false

                            let writeNoteArgs = buildNewNote, buildNewComp, note, undoWriter
                            return! writingNote writeNoteArgs

                    | MouseButton.Left ->
                        hintSetNone()
                        let chart = !!x.ProgramModel.ActiveChart
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos chart.ActiveUtt chart.Comp.Utts edit
                        match mouseDownNoteOp with
                        | None ->
                            if e.ClickCount = 2 && chart.ActiveUtt <> None then
                                x.ProgramModel.ActiveChart |> Rp.modify(fun chart ->
                                    chart.SetActiveUtt(chart.Comp, None))

                            elif e.ClickCount = 1 && keyboardModifiers <> ModifierKeys.Control then
                                x.ProgramModel.ActiveChart |> Rp.modify(fun chart ->
                                    chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Empty))

                            let chart = !!x.ProgramModel.ActiveChart
                            return! draggingSelBox chart mousePos

                        | Some(utt, note, noteDragType) ->
                            let mousePulse = x.PixelToPulse mousePos.X |> int64
                            let mouseDownPulse =
                                match noteDragType with
                                | NoteDragResizeLeft
                                | NoteDragMove ->
                                    let noteGridDeviation = note.On - (note.On |> x.Quantize chart.Comp.TimeSig0)
                                    (mousePulse - noteGridDeviation |> x.Quantize chart.Comp.TimeSig0) + noteGridDeviation
                                | NoteDragResizeRight ->
                                    let noteGridDeviation = note.Off - (note.Off |> x.QuantizeCeil chart.Comp.TimeSig0)
                                    (mousePulse - noteGridDeviation |> x.QuantizeCeil chart.Comp.TimeSig0) + noteGridDeviation

                            let pendingDeselectNotes, chart =
                                if e.ClickCount >= 2 then
                                    let targetNotes =
                                        if e.ClickCount >= 3 then utt.Notes :> seq<_> else
                                            utt.Notes
                                            |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen)
                                            |> Seq.find(fun notes -> notes |> Array.contains note) :> seq<_>
                                    let pendingDeselectNotes = if chart.GetIsNoteSelected note then Seq.empty else targetNotes
                                    pendingDeselectNotes, chart.UpdateSelectedNotes(chart.Comp, fun selectedNotes -> selectedNotes.Union targetNotes)

                                else
                                    match chart.GetIsNoteSelected note, keyboardModifiers with
                                    | true, ModifierKeys.Control ->
                                        seq { note }, chart
                                    | true, _ ->
                                        Seq.empty, chart
                                    | false, ModifierKeys.Control ->
                                        Seq.empty, chart.UpdateSelectedNotes(chart.Comp, fun selectedNotes -> selectedNotes.Add note)
                                    | false, _ ->
                                        Seq.empty, chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Create note)

                            let chart = chart.SetActiveUtt(chart.Comp, Some utt)
                            x.ProgramModel.ActiveChart |> Rp.set chart

                            MidiPlayback.playPitch note.Pitch
                            let undoWriter = x.ProgramModel.UndoRedoStack.BeginPushUndo(MouseDragNote noteDragType, chart)

                            let dragNoteArgs = note, chart, mouseDownPulse, note, noteDragType, undoWriter
                            return! mouseDownNotePendingDeselect pendingDeselectNotes dragNoteArgs

                    | MouseButton.Middle ->
                        hintSetNone()
                        let chart = !!x.ProgramModel.ActiveChart
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos chart.ActiveUtt chart.Comp.Utts edit
                        match mouseDownNoteOp with
                        | None when e.ClickCount = 2 ->
                            x.ProgramModel.ActiveChart |> Rp.set(chart.SetActiveUtt(chart.Comp, None))

                        | Some(utt, note, noteDragType) ->
                            x.ProgramModel.ActiveChart |> Rp.set(chart.SetActiveUtt(chart.Comp, Some utt))

                        | _ -> ()

                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)

                    | MouseButton.Right ->
                        // Context menu preparations
                        let chart = !!x.ProgramModel.ActiveChart
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos chart.ActiveUtt chart.Comp.Utts edit
                        match mouseDownNoteOp with
                        | None ->
                            match keyboardModifiers with
                            | ModifierKeys.Control -> ()
                            | _ ->
                                x.ProgramModel.ActiveChart |> Rp.set(
                                    chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Empty))

                        | Some(utt, note, noteDragType) ->
                            let chart =
                                match chart.GetIsNoteSelected note, keyboardModifiers with
                                | true, _ ->
                                    chart
                                | false, ModifierKeys.Control ->
                                    chart.UpdateSelectedNotes(chart.Comp, fun selectedNotes -> selectedNotes.Add note)
                                | false, _ ->
                                    chart.SetSelectedNotes(chart.Comp, ImmutableHashSet.Create note)

                            let chart = chart.SetActiveUtt(chart.Comp, Some utt)
                            x.ProgramModel.ActiveChart |> Rp.set chart

                            if not note.IsHyphen then
                                let romMenuItems = List()
                                let moreRoms =
                                    (Romanizer.get utt.RomScheme).Convert [| note.Lyric |] [| "" |]
                                    |> Array.exactlyOne
                                    |> Array.filter((<>) note.Rom)
                                do for i in 0 .. moreRoms.Length - 1 do
                                    let rom = moreRoms.[i]
                                    let romMenuItem = MenuItem(Header = TextResources.getContextMenuSetRom note.Lyric rom)
                                    romMenuItem.Click.Add(fun e ->
                                        let newNote = note.SetText(note.Lyric, rom)
                                        let newUtt = utt.SetNotes(utt.Notes.Replace(note, newNote))

                                        let undoWriter =
                                            x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                                SetNoteRomContextMenu,
                                                ChartState(chart.Comp, Some utt, ImmutableHashSet.Create note))

                                        x.ProgramModel.ActiveChart |> Rp.set(
                                            ChartState(
                                                chart.Comp.SetUtts(chart.Comp.Utts.Replace(utt, newUtt)),
                                                Some newUtt, ImmutableHashSet.Create newNote))

                                        undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                                        x.ProgramModel.CompIsSaved |> Rp.set false)

                                    x.ChartEditorContextMenu.Items.Insert(i, romMenuItem)
                                    romMenuItems.Add romMenuItem

                                let rec eventUnsubscriber =
                                    [| contextMenuClosedSubscriber |] 
                                    |> Disposable.join id

                                and contextMenuClosedSubscriber = x.ChartEditorContextMenu.Closed.Subscribe(fun e ->
                                    for romMenuItem in romMenuItems do
                                        x.ChartEditorContextMenu.Items.Remove romMenuItem
                                    eventUnsubscriber |> Disposable.dispose)

                                ()

                        return! idle()

                    | _ -> return! idle()

                | ChartMouseMove e ->
                    let keyboardModifiers = Keyboard.Modifiers

                    if keyboardModifiers.IsAlt then
                        hintSetGhostNote(e.GetPosition edit)
                    else
                        hintSetMouseOverNote(e.GetPosition edit)

                    return! idle()

                | _ -> return! idle() }

            and mouseDownNotePendingDeselect pendingDeselectNotes dragNoteArgs = behavior {
                let mouseDownNote, chart, mouseDownPulse, prevNote, noteDragType, undoWriter = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    return! (draggingNote dragNoteArgs : _ BehaviorAction).Run(ChartMouseMove e)

                | ChartMouseRelease e ->
                    x.ProgramModel.ActiveChart |> Rp.set(
                        chart.UpdateSelectedNotes(chart.Comp, fun selectedNotes ->
                            selectedNotes.Except pendingDeselectNotes))
                    return! (draggingNote dragNoteArgs).Run(ChartMouseRelease e)

                | _ -> return! mouseDownNotePendingDeselect pendingDeselectNotes dragNoteArgs }

            and writingNote(buildNewNote, buildNewComp, prevNote, undoWriter as writeNoteArgs) = behavior {
                match! () with
                | ChartMouseMove e ->
                    let mousePos = e.GetPosition edit
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

                    let note = buildNewNote mousePulse mousePitch

                    if (prevNote.On, prevNote.Off, prevNote.Pitch) <> (note.On, note.Off, note.Pitch) then
                        let chart = buildNewComp note
                        x.ProgramModel.ActiveChart |> Rp.set chart

                        MidiPlayback.switchPitch prevNote.Pitch note.Pitch
                        undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                        x.ProgramModel.CompIsSaved |> Rp.set false

                        return! writingNote(buildNewNote, buildNewComp, note, undoWriter)

                    else
                        return! writingNote writeNoteArgs

                | ChartMouseRelease e ->
                    MidiPlayback.stopPitch prevNote.Pitch
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! writingNote writeNoteArgs }

            and draggingNote dragNoteArgs = behavior {
                let mouseDownNote, chart, mouseDownPulse, prevNote, noteDragType, undoWriter = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    let minKey = edit.MinKey
                    let maxKey = edit.MaxKey
                    let mousePos = e.GetPosition edit
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

                    let newNoteOn =
                        match noteDragType with
                        | NoteDragResizeLeft
                        | NoteDragMove ->
                            mouseDownNote.On + mousePulse - mouseDownPulse |> x.Quantize chart.Comp.TimeSig0
                        | NoteDragResizeRight ->
                            mouseDownNote.Off + mousePulse - mouseDownPulse |> x.QuantizeCeil chart.Comp.TimeSig0

                    let deltaPulse, deltaDur =
                        let selMinPulse = chart.SelectedNotes |> Seq.map(fun note -> note.On) |> Seq.min
                        let selMinDur   = chart.SelectedNotes |> Seq.map(fun note -> note.Dur) |> Seq.min
                        match noteDragType with
                        | NoteDragResizeLeft ->
                            let minOn = mouseDownNote.On - selMinPulse
                            let maxOn = mouseDownNote.On + selMinDur - 1L |> x.Quantize chart.Comp.TimeSig0
                            let deltaPulse = (newNoteOn |> clamp minOn maxOn) - mouseDownNote.On
                            deltaPulse, -deltaPulse
                        | NoteDragMove ->
                            let minOn = mouseDownNote.On - selMinPulse
                            (newNoteOn |> max minOn) - mouseDownNote.On, 0L
                        | NoteDragResizeRight ->
                            let minOff = mouseDownNote.Off - selMinDur + 1L |> x.QuantizeCeil chart.Comp.TimeSig0
                            0L, (newNoteOn |> max minOff) - mouseDownNote.Off

                    let deltaPitch =
                        match noteDragType with
                        | NoteDragResizeLeft
                        | NoteDragResizeRight -> 0
                        | NoteDragMove ->
                            let mouseDownSelMinPitch = chart.SelectedNotes |> Seq.map(fun note -> note.Pitch) |> Seq.min
                            let mouseDownSelMaxPitch = chart.SelectedNotes |> Seq.map(fun note -> note.Pitch) |> Seq.max
                            mousePitch - mouseDownNote.Pitch |> clamp(minKey - mouseDownSelMinPitch)(maxKey - mouseDownSelMaxPitch)

                    let note = mouseDownNote.MoveDelta(deltaPitch, deltaPulse, deltaDur)

                    if (prevNote.On, prevNote.Off, prevNote.Pitch) <> (note.On, note.Off, note.Pitch) then
                        if deltaPulse = 0L && deltaDur = 0L && deltaPitch = 0 then
                            x.ProgramModel.ActiveChart |> Rp.set chart
                            MidiPlayback.switchPitch prevNote.Pitch note.Pitch
                            undoWriter.UnpushUndo()

                        else
                            // DiffDict: no existance -> no modification
                            let noteDiffDict = chart.SelectedNotes.ToImmutableDictionary(id, fun (note : Note) ->
                                note.MoveDelta(deltaPitch, deltaPulse, deltaDur))

                            let uttDiffDict = chart.Comp.Utts.ToImmutableDictionary(id, fun (utt : Utterance) ->
                                if utt.Notes |> Seq.forall(fun note -> not(noteDiffDict.ContainsKey note)) then utt else
                                    utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> noteDiffDict.GetOrDefault note note)))

                            let newComp = chart.Comp.SetUtts(ImmutableArray.CreateRange(chart.Comp.Utts, fun utt -> uttDiffDict.GetOrDefault utt utt))
                            let activeUtt = chart.ActiveUtt |> Option.map(fun utt -> uttDiffDict.GetOrDefault utt utt)
                            let selectedNotes = ImmutableHashSet.CreateRange noteDiffDict.Values
                            x.ProgramModel.ActiveChart |> Rp.set(ChartState(newComp, activeUtt, selectedNotes))

                            MidiPlayback.switchPitch prevNote.Pitch note.Pitch
                            undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                            x.ProgramModel.CompIsSaved |> Rp.set false

                        return! draggingNote(mouseDownNote, chart, mouseDownPulse, note, noteDragType, undoWriter)

                    else
                        return! draggingNote dragNoteArgs

                | ChartMouseRelease e ->
                    MidiPlayback.stopPitch prevNote.Pitch
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! draggingNote dragNoteArgs }

            and draggingSelBox chart mouseDownPos = behavior {
                match! () with
                | ChartMouseMove e ->
                    let mousePos = e.GetPosition edit

                    let selMinPulse = x.PixelToPulse (min mousePos.X mouseDownPos.X) |> int64
                    let selMaxPulse = x.PixelToPulse (max mousePos.X mouseDownPos.X) |> int64
                    let selMinPitch = x.PixelToPitch (max mousePos.Y mouseDownPos.Y) |> round |> int
                    let selMaxPitch = x.PixelToPitch (min mousePos.Y mouseDownPos.Y) |> round |> int
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- Some(selMinPulse, selMaxPulse, selMinPitch, selMaxPitch)

                    let newChart =
                        chart.SetSelectedNotes(
                            chart.Comp,
                            chart.Comp.AllNotes
                            |> Seq.filter(fun note ->
                                let noteHasIntersection =
                                    note.On <= selMaxPulse && note.Off >= selMinPulse && note.Pitch |> betweenInc selMinPitch selMaxPitch
                                noteHasIntersection <> chart.GetIsNoteSelected note)
                            |> ImmutableHashSet.CreateRange)
                    x.ProgramModel.ActiveChart |> Rp.set newChart

                    return! draggingSelBox chart mouseDownPos

                | ChartMouseRelease e ->
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- None
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! draggingSelBox chart mouseDownPos }

            Behavior.agent(idle()))

        x.RulerGrid |> ChartMouseEvent.BindEvents(
            let edit = x.RulerGrid

            let playNoteMidi(note : Note) = MidiPlayback.playPitch note.Pitch
            let stopNoteMidi(note : Note) = MidiPlayback.stopPitch note.Pitch

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        hintSetNone()
                        let chart = !!x.ProgramModel.ActiveChart
                        let playbackPos = mouseToCursorPos(e.GetPosition edit)

                        match Keyboard.Modifiers with
                        | ModifierKeys.Control when not !!x.ProgramModel.IsPlaying ->
                            let scrubNotes =
                                chart.Comp.AllNotes
                                |> Seq.filter(fun note -> note.On <= playbackPos && note.Off > playbackPos)
                                |> ImmutableHashSet.CreateRange

                            scrubNotes |> Seq.iter playNoteMidi
                            x.ProgramModel.ManualSetCursorPos playbackPos
                            return! mouseLeftDown true (Some scrubNotes)

                        | ModifierKeys.Control ->
                            x.ProgramModel.ManualSetCursorPos playbackPos
                            return! mouseLeftDown true None

                        | _ ->
                            let playbackPos = playbackPos |> x.Quantize chart.Comp.TimeSig0

                            x.ProgramModel.ManualSetCursorPos playbackPos
                            return! mouseLeftDown false None

                    | MouseButton.Middle ->
                        hintSetNone()
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)

                    | _ -> return! idle()

                | ChartMouseMove e ->
                    hintSetGhostCursor(e.GetPosition edit)
                    return! idle()

                | ChartMouseLeave e ->
                    hintSetNone()
                    return! idle()

                | _ -> return! idle() }

            and mouseLeftDown forceNoQuantize scrubNotesOp = behavior {
                match! () with
                | ChartMouseMove e ->
                    let chart = !!x.ProgramModel.ActiveChart
                    let playbackPos = mouseToCursorPos(e.GetPosition edit)
                    match scrubNotesOp with
                    | Some prevScrubPitches ->
                        let scrubNotes =
                            chart.Comp.AllNotes
                            |> Seq.filter(fun note -> note.On <= playbackPos && note.Off > playbackPos)
                            |> ImmutableHashSet.CreateRange

                        prevScrubPitches.Except scrubNotes |> Seq.iter stopNoteMidi
                        scrubNotes.Except prevScrubPitches |> Seq.iter playNoteMidi
                        x.ProgramModel.ManualSetCursorPos playbackPos
                        return! mouseLeftDown forceNoQuantize (Some scrubNotes)

                    | None ->
                        let playbackPos =
                            if forceNoQuantize then playbackPos else
                                playbackPos |> x.Quantize chart.Comp.TimeSig0

                        x.ProgramModel.ManualSetCursorPos playbackPos
                        return! mouseLeftDown forceNoQuantize scrubNotesOp

                | ChartMouseRelease e ->
                    match scrubNotesOp with
                    | Some scrubNotes ->
                        scrubNotes |> Seq.iter stopNoteMidi
                    | None -> ()

                    hintSetGhostCursor(e.GetPosition edit)
                    return! idle()

                | _ -> return! mouseLeftDown forceNoQuantize scrubNotesOp }

            Behavior.agent(idle()))

        x.SideKeyboard |> ChartMouseEvent.BindEvents(
            let edit = x.SideKeyboard

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Middle ->
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)
                    | _ -> return! idle()
                | _ -> return! idle() }

            Behavior.agent(idle()))

        x.BgAudioDisplay |> ChartMouseEvent.BindEvents(
            let edit = x.BgAudioDisplay

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        let chart = !!x.ProgramModel.ActiveChart
                        let mouseDownPos = e.GetPosition edit

                        let undoWriter = x.ProgramModel.UndoRedoStack.BeginPushUndo(MoveBgAudio, chart)

                        return! mouseDragging mouseDownPos chart undoWriter

                    | _ -> return! idle()

                | _ -> return! idle() }

            and mouseDragging mouseDownPos chart undoWriter = behavior {
                match! () with
                | ChartMouseMove e ->
                    let mousePos = e.GetPosition edit
                    let mouseDownSamplePos = mouseDownPos.X |> x.PixelToPulse |> Audio.pulseToSample chart.Comp.Bpm0
                    let mouseSamplePos     = mousePos.X     |> x.PixelToPulse |> Audio.pulseToSample chart.Comp.Bpm0

                    let newComp = chart.Comp.UpdateBgAudioOffset(fun sampleOffset ->
                        sampleOffset - mouseDownSamplePos + mouseSamplePos)
                    x.ProgramModel.ActiveChart |> Rp.set(chart.SetComp newComp)

                    undoWriter.PutRedo(!!x.ProgramModel.ActiveChart)
                    x.ProgramModel.CompIsSaved |> Rp.set false

                    return! mouseDragging mouseDownPos chart undoWriter

                | ChartMouseRelease e ->
                    return! idle()

                | _ ->
                    return! mouseDragging mouseDownPos chart undoWriter }

            Behavior.agent(idle()))

        // mouse wheel events
        let onMouseWheel(edit : NoteChartEditBase)(e : MouseWheelEventArgs) =
            if edit.CanScrollH then
                let zoomDelta = float(sign e.Delta) * 0.2       // TODO Use Slider.SmallChange
                let log2Zoom = x.HScrollZoom.Log2ZoomValue
                let log2ZoomMin = x.HScrollZoom.Log2ZoomMinimum
                let log2ZoomMax = x.HScrollZoom.Log2ZoomMaximum
                let newLog2Zoom = log2Zoom + zoomDelta |> clamp log2ZoomMin log2ZoomMax
                let mousePos = e.GetPosition edit
                let xPos = mousePos.X
                let hScrollValue = x.HScrollZoom.ScrollValue
                let quarterWidth = 2.0 ** log2Zoom
                let newQuarterWidth = 2.0 ** newLog2Zoom
                let currPulse = pixelToPulse quarterWidth hScrollValue xPos
                let nextPulse = pixelToPulse newQuarterWidth hScrollValue xPos
                let offsetDelta = nextPulse - currPulse

                x.HScrollZoom.Log2ZoomValue <- newLog2Zoom
                x.HScrollZoom.ScrollValue <- hScrollValue - offsetDelta

            elif edit.CanScrollV then
                let zoomDelta = float(sign e.Delta) * 0.1       // TODO Use Slider.SmallChange
                let log2Zoom = x.VScrollZoom.Log2ZoomValue
                let log2ZoomMin = x.VScrollZoom.Log2ZoomMinimum
                let log2ZoomMax = x.VScrollZoom.Log2ZoomMaximum
                let newLog2Zoom = log2Zoom + zoomDelta |> clamp log2ZoomMin log2ZoomMax
                let mousePos = e.GetPosition edit
                let yPos = mousePos.Y
                let vScrollValue = x.VScrollZoom.ScrollValue
                let keyHeight = 2.0 ** log2Zoom
                let newKeyHeight = 2.0 ** newLog2Zoom
                let actualHeight = x.ChartEditor.ActualHeight
                let currPitch = pixelToPitch keyHeight actualHeight vScrollValue yPos
                let nextPitch = pixelToPitch newKeyHeight actualHeight vScrollValue yPos
                let offsetDelta = nextPitch - currPitch

                x.VScrollZoom.Log2ZoomValue <- newLog2Zoom
                x.VScrollZoom.ScrollValue <- vScrollValue - offsetDelta

        x.ChartEditor.MouseWheel.Add(onMouseWheel x.ChartEditor)
        x.RulerGrid.MouseWheel.Add(onMouseWheel x.RulerGrid)
        x.SideKeyboard.MouseWheel.Add(onMouseWheel x.SideKeyboard)
        x.BgAudioDisplay.MouseWheel.Add(onMouseWheel x.BgAudioDisplay)

        // playback cursor
        x.ChartEditor.CursorPositionChanged.Add <| fun (prevPlayPos, playPos) ->
            let edit = x.ChartEditor
            if edit.IsPlaying then
                let quarterWidth = edit.QuarterWidth
                let hOffset = x.HScrollZoom.ScrollValue
                let actualWidth = edit.ActualWidth
                let hRightOffset = pixelToPulse quarterWidth hOffset actualWidth
                if float prevPlayPos < hRightOffset && float playPos >= hRightOffset then
                    x.HScrollZoom.ScrollValue <- hOffset + (hRightOffset - hOffset) * 0.9

        // key events
        x.KeyDown.Add <| fun e ->
            if not e.IsRepeat then
                match e.Key with
                | Key.System ->
                    match e.SystemKey with
                    | Key.LeftAlt | Key.RightAlt ->
                        hintSetGhostNote(Mouse.GetPosition x.ChartEditor)
                        e.Handled <- true

                    | _ -> ()

                | _ -> ()

                x.ChartEditorAdornerLayer.InvalidateVisual()

        x.KeyUp.Add <| fun e ->
            hintSetMouseOverNote(Mouse.GetPosition x.ChartEditor)
            x.ChartEditorAdornerLayer.InvalidateVisual()


