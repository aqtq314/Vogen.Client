namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.ViewModel


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
    abstract HScrollZoom : ChartScrollZoomKitBase
    default x.HScrollZoom = Unchecked.defaultof<_>
    abstract VScrollZoom : ChartScrollZoomKitBase
    default x.VScrollZoom = Unchecked.defaultof<_>
    abstract LyricPopup : Popup
    default x.LyricPopup = Unchecked.defaultof<_>
    abstract LyricTextBox : TextBox
    default x.LyricTextBox = Unchecked.defaultof<_>

    member x.PixelToPulse xPos =
        let quarterWidth = x.ChartEditor.QuarterWidth
        let hOffset = x.ChartEditor.HOffsetAnimated
        pixelToPulse quarterWidth hOffset xPos

    member x.PixelToPitch yPos =
        let actualHeight = x.ChartEditor.ActualHeight
        let keyHeight = x.ChartEditor.KeyHeight
        let vOffset = x.ChartEditor.VOffsetAnimated
        pixelToPitch keyHeight actualHeight vOffset yPos

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
            let actualHeight = edit.ActualHeight
            let quarterWidth = edit.QuarterWidth
            let keyHeight = edit.KeyHeight
            let hOffset = edit.HOffsetAnimated
            let vOffset = edit.VOffsetAnimated
            let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64
            let mousePitch = pixelToPitch keyHeight actualHeight vOffset mousePos.Y |> round |> int

            let comp = !!x.ProgramModel.ActiveComp
            let selection = !!x.ProgramModel.ActiveSelection

            let uttsReordered = enumerateUttsByDepth(activeUtt, utts)
            Seq.tryHead <| seq {
                for utt in uttsReordered do
                    for noteIndex in utt.Notes.Length - 1 .. -1 .. 0 do
                        let note = utt.Notes.[noteIndex]
                        if mousePulse |> between note.On note.Off && mousePitch = note.Pitch then
                            yield utt, note }
            |> Option.map(fun (utt, note) ->
                let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                let noteDragType =
                    if   mousePos.X <= min(x0 + 6.0)(lerp x0 x1 0.2) then NoteDragResizeLeft
                    elif mousePos.X >= max(x1 - 6.0)(lerp x0 x1 0.8) then NoteDragResizeRight
                    else NoteDragMove
                utt, note, noteDragType)

        let quantize snap quantization (timeSig : TimeSignature) pulses =
            if not snap then pulses else
                let pulsesMeasureQuantized = pulses / timeSig.PulsesPerMeasure * timeSig.PulsesPerMeasure
                pulsesMeasureQuantized + (pulses - pulsesMeasureQuantized) / quantization * quantization

        let quantizeCeil snap quantization (timeSig : TimeSignature) pulses =
            if not snap then pulses else
                let pulsesMeasureQuantized = pulses / timeSig.PulsesPerMeasure * timeSig.PulsesPerMeasure
                pulsesMeasureQuantized + ((pulses - pulsesMeasureQuantized) /^ quantization * quantization |> min timeSig.PulsesPerMeasure)

        let getPlaybackCursorPos(mousePos : Point) =
            let hOffset = x.RulerGrid.HOffsetAnimated
            let quarterWidth = x.RulerGrid.QuarterWidth
            let comp = !!x.ProgramModel.ActiveComp
            let quantization = x.Quantization
            let snap = x.Snap

            let newCursorPos = int64(pixelToPulse quarterWidth hOffset mousePos.X) |> NoteChartEditBase.CoerceCursorPosition x.RulerGrid
            newCursorPos |> quantize snap quantization comp.TimeSig0

        let updatePlaybackCursorPos mousePos =
            let newCursorPos = getPlaybackCursorPos mousePos
            x.ProgramModel.ManualSetCursorPos newCursorPos

        let hintSetNone() =
            x.ChartEditorAdornerLayer.EditorHint <- None

        let hintSetGhostCursor mousePos =
            let cursorPos = getPlaybackCursorPos mousePos
            x.ChartEditorAdornerLayer.EditorHint <- Some(GhostCursor cursorPos)

        let hintSetMouseOverNote mousePos =
            let mouseOverNoteOp =
                let comp = !!x.ProgramModel.ActiveComp
                let selection = !!x.ProgramModel.ActiveSelection
                findMouseOverNote mousePos selection.ActiveUtt comp.Utts x.ChartEditor

            x.ChartEditorAdornerLayer.EditorHint <- Option.map HoverNote mouseOverNoteOp

        let hintSetGhostNote mousePos =
            let edit = x.ChartEditor
            let actualHeight = edit.ActualHeight
            let quarterWidth = edit.QuarterWidth
            let keyHeight = edit.KeyHeight
            let minKey = edit.MinKey
            let maxKey = edit.MaxKey
            let hOffset = edit.HOffsetAnimated
            let vOffset = edit.VOffsetAnimated
            let comp = !!x.ProgramModel.ActiveComp
            let selection = !!x.ProgramModel.ActiveSelection
            let quantization = x.Quantization
            let snap = x.Snap
            let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt ImmutableArray.Empty edit
            let note =
                match mouseDownNoteOp with
                | None ->
                    let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64
                    let mousePitch = pixelToPitch keyHeight actualHeight vOffset mousePos.Y |> round |> int
                    let noteOn = mousePulse |> quantize snap quantization comp.TimeSig0 |> max 0L
                    let noteOff = noteOn + 1L |> quantizeCeil snap quantization comp.TimeSig0
                    let notePitch = mousePitch |> clamp minKey maxKey
                    Note(notePitch, "", "du", noteOn, noteOff - noteOn)

                | Some(mouseDownUtt, mouseDownNote, noteDragType) ->
                    let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64
                    let noteOn =
                        mousePulse
                        |> min(mouseDownNote.Off - 1L)
                        |> quantize snap quantization comp.TimeSig0
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
                        let actualHeight = edit.ActualHeight
                        let quarterWidth = edit.QuarterWidth
                        let keyHeight = edit.KeyHeight
                        let hOffset = edit.HOffsetAnimated
                        let vOffset = edit.VOffsetAnimated
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let quantization = x.Quantization
                        let snap = x.Snap

                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt ImmutableArray.Empty edit
                        match mouseDownNoteOp with
                        | None ->
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    WriteNote, (comp, selection.SetSelectedNotes ImmutableHashSet.Empty))

                            let minKey = edit.MinKey
                            let maxKey = edit.MaxKey
                            let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64
                            let mousePitch = pixelToPitch keyHeight actualHeight vOffset mousePos.Y |> round |> int
                            let maxNoteOn = mousePulse |> quantize snap quantization comp.TimeSig0 |> max 0L
                            let minNoteOff = maxNoteOn + 1L |> quantizeCeil snap quantization comp.TimeSig0

                            let buildNewNote mousePulse mousePitch =
                                let noteOn = min maxNoteOn (mousePulse |> quantize snap quantization comp.TimeSig0) |> max 0L
                                let noteOff = max minNoteOff (mousePulse |> quantizeCeil snap quantization comp.TimeSig0)
                                let notePitch = mousePitch |> clamp minKey maxKey
                                Note(notePitch, "", "du", noteOn, noteOff - noteOn)

                            let buildNewComp =
                                match selection.ActiveUtt with
                                | None -> fun note ->
                                    let utt = Utterance("man", ImmutableArray.Create(note : Note))
                                    utt, comp.UpdateUtts(fun utts -> utts.Add utt)
                                | Some activeUtt -> fun note ->
                                    let utt = activeUtt.UpdateNotes(fun notes -> notes.Add note)
                                    utt, comp.UpdateUtts(fun utts -> utts.Replace(activeUtt, utt))

                            let note = buildNewNote mousePulse mousePitch
                            let utt, comp = buildNewComp note
                            x.ProgramModel.ActiveComp |> Rp.set comp
                            x.ProgramModel.ActiveSelection |> Rp.set(
                                CompSelection(Some utt, ImmutableHashSet.Create note))

                            undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                            x.ProgramModel.CompIsSaved |> Rp.set false

                            let writeNoteArgs = buildNewNote, buildNewComp, note, undoWriter
                            return! writingNote writeNoteArgs

                        | Some(mouseDownUtt, mouseDownNote, noteDragType) ->
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    WriteHyphenNote, (comp, selection.SetSelectedNotes(ImmutableHashSet.Create mouseDownNote)))

                            let minKey = edit.MinKey
                            let maxKey = edit.MaxKey
                            let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64

                            let buildNewNote mousePulse mousePitch =
                                let noteOn =
                                    mousePulse
                                    |> min(mouseDownNote.Off - 1L)
                                    |> quantize snap quantization comp.TimeSig0
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
                                utt, comp.UpdateUtts(fun utts -> utts.Replace(mouseDownUtt, utt))

                            let note = buildNewNote mousePulse mouseDownNote.Pitch
                            let utt, comp = buildNewComp note
                            x.ProgramModel.ActiveComp |> Rp.set comp
                            x.ProgramModel.ActiveSelection |> Rp.set(
                                CompSelection(Some utt, ImmutableHashSet.Create note))

                            undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                            x.ProgramModel.CompIsSaved |> Rp.set false

                            let writeNoteArgs = buildNewNote, buildNewComp, note, undoWriter
                            return! writingNote writeNoteArgs

                    | MouseButton.Left ->
                        hintSetNone()
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt comp.Utts edit
                        match mouseDownNoteOp with
                        | None ->
                            if e.ClickCount = 2 then
                                x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                    selection.SetActiveUtt None)

                            match keyboardModifiers with
                            | ModifierKeys.Control -> ()
                            | _ ->
                                x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                    selection.SetSelectedNotes ImmutableHashSet.Empty)

                            let mouseDownSelection = !!x.ProgramModel.ActiveSelection
                            return! draggingSelBox mouseDownSelection mousePos

                        | Some(utt, note, noteDragType) when e.ClickCount >= 2 ->
                            let actualHeight = edit.ActualHeight
                            let quarterWidth = edit.QuarterWidth
                            let keyHeight = edit.KeyHeight
                            let hOffset = edit.HOffsetAnimated
                            let vOffset = edit.VOffsetAnimated
                            let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                            let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                            let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)
                            x.LyricPopup.PlacementRectangle <- Rect(x0, yMid - half keyHeight, x1 - x0, keyHeight)
                            x.LyricPopup.IsOpen <- true
                            x.LyricTextBox.Text <- note.Rom + $" - {e.ClickCount}"
                            x.LyricTextBox.SelectAll()
                            x.LyricTextBox.Focus() |> ignore
                            return! idle()

                        | Some(utt, note, noteDragType) ->
                            let quarterWidth = edit.QuarterWidth
                            let hOffset = edit.HOffsetAnimated
                            let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64

                            let quantization = x.Quantization
                            let snap = x.Snap
                            let mouseDownPulse =
                                match noteDragType with
                                | NoteDragResizeLeft
                                | NoteDragMove ->
                                    let noteGridDeviation = note.On - (note.On |> quantize snap quantization comp.TimeSig0)
                                    (mousePulse - noteGridDeviation |> quantize snap quantization comp.TimeSig0) + noteGridDeviation
                                | NoteDragResizeRight ->
                                    let noteGridDeviation = note.Off - (note.Off |> quantizeCeil snap quantization comp.TimeSig0)
                                    (mousePulse - noteGridDeviation |> quantizeCeil snap quantization comp.TimeSig0) + noteGridDeviation

                            //MidiPlayback.playPitch note.Pitch
                            x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                selection.SetActiveUtt(Some utt))

                            if not(selection.GetIsNoteSelected note) then
                                if keyboardModifiers = ModifierKeys.Control then
                                    x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                        selection.UpdateSelectedNotes(fun selectedNotes ->
                                            selectedNotes.Add note))
                                else
                                    x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                        selection.SetSelectedNotes(
                                            ImmutableHashSet.Create note))

                            let mouseDownSelection = !!x.ProgramModel.ActiveSelection
                            let mouseDownSelection = mouseDownSelection.EnsureIntersectionWith comp
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    MouseDragNote noteDragType, (comp, mouseDownSelection))

                            let dragNoteArgs = note, comp, mouseDownSelection, mouseDownPulse, noteDragType, undoWriter
                            let isPendingDeselect = selection.GetIsNoteSelected note && keyboardModifiers = ModifierKeys.Control
                            if isPendingDeselect then
                                return! mouseDownNotePendingDeselect dragNoteArgs
                            else
                                return! draggingNote dragNoteArgs

                    | MouseButton.Middle ->
                        hintSetNone()
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt comp.Utts edit
                        match mouseDownNoteOp with
                        | None when e.ClickCount = 2 ->
                            x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                selection.SetActiveUtt None)

                        | Some(utt, note, noteDragType) ->
                            x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                selection.SetActiveUtt(Some utt))

                        | _ -> ()

                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)

                    | _ -> return! idle()

                | ChartMouseMove e ->
                    let keyboardModifiers = Keyboard.Modifiers

                    if keyboardModifiers.IsAlt then
                        hintSetGhostNote(e.GetPosition edit)
                    else
                        hintSetMouseOverNote(e.GetPosition edit)

                    return! idle()

                | _ -> return! idle() }

            and mouseDownNotePendingDeselect dragNoteArgs = behavior {
                let mouseDownNote, mouseDownComp, mouseDownSelection, mouseDownPulse, noteDragType, undoWriter = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    return! (draggingNote dragNoteArgs).Run(ChartMouseMove e)

                | ChartMouseRelease e ->
                    x.ProgramModel.ActiveSelection |> Rp.set(
                        mouseDownSelection.UpdateSelectedNotes(fun selectedNotes ->
                            selectedNotes.Remove mouseDownNote))
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! mouseDownNotePendingDeselect dragNoteArgs }

            and writingNote(buildNewNote, buildNewComp, prevNote, undoWriter as writeNoteArgs) = behavior {
                match! () with
                | ChartMouseMove e ->
                    let minKey = edit.MinKey
                    let maxKey = edit.MaxKey
                    let comp = !!x.ProgramModel.ActiveComp
                    let mousePos = e.GetPosition edit
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

                    let quantization = x.Quantization
                    let snap = x.Snap
                    let note = buildNewNote mousePulse mousePitch

                    if (prevNote.On, prevNote.Off, prevNote.Pitch) <> (note.On, note.Off, note.Pitch) then
                        let utt, comp = buildNewComp note

                        x.ProgramModel.ActiveComp |> Rp.set comp
                        x.ProgramModel.ActiveSelection |> Rp.set(
                            CompSelection(Some utt, ImmutableHashSet.Create note))

                        undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                        x.ProgramModel.CompIsSaved |> Rp.set false

                        return! writingNote(buildNewNote, buildNewComp, note, undoWriter)

                    else
                        return! writingNote writeNoteArgs

                | ChartMouseRelease e ->
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! writingNote writeNoteArgs }

            and draggingNote dragNoteArgs = behavior {
                let mouseDownNote, comp, mouseDownSelection, mouseDownPulse, noteDragType, undoWriter = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    let actualHeight = edit.ActualHeight
                    let quarterWidth = edit.QuarterWidth
                    let keyHeight = edit.KeyHeight
                    let minKey = edit.MinKey
                    let maxKey = edit.MaxKey
                    let hOffset = edit.HOffsetAnimated
                    let vOffset = edit.VOffsetAnimated
                    let mousePos = e.GetPosition edit
                    let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64
                    let mousePitch = pixelToPitch keyHeight actualHeight vOffset mousePos.Y |> round |> int

                    let quantization = x.Quantization
                    let snap = x.Snap
                    let newNoteOn =
                        match noteDragType with
                        | NoteDragResizeLeft
                        | NoteDragMove ->
                            mouseDownNote.On + mousePulse - mouseDownPulse |> quantize snap quantization comp.TimeSig0
                        | NoteDragResizeRight ->
                            mouseDownNote.Off + mousePulse - mouseDownPulse |> quantizeCeil snap quantization comp.TimeSig0

                    let deltaPulse, deltaDur =
                        let selMinPulse = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.On) |> Seq.min
                        let selMinDur   = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.Dur) |> Seq.min
                        match noteDragType with
                        | NoteDragResizeLeft ->
                            let minOn = mouseDownNote.On - selMinPulse
                            let maxOn = mouseDownNote.On + selMinDur - 1L |> quantize snap quantization comp.TimeSig0
                            let deltaPulse = (newNoteOn |> clamp minOn maxOn) - mouseDownNote.On
                            deltaPulse, -deltaPulse
                        | NoteDragMove ->
                            let minOn = mouseDownNote.On - selMinPulse
                            (newNoteOn |> max minOn) - mouseDownNote.On, 0L
                        | NoteDragResizeRight ->
                            let minOff = mouseDownNote.Off - selMinDur + 1L |> quantizeCeil snap quantization comp.TimeSig0
                            0L, (newNoteOn |> max minOff) - mouseDownNote.Off

                    let deltaPitch =
                        match noteDragType with
                        | NoteDragResizeLeft
                        | NoteDragResizeRight -> 0
                        | NoteDragMove ->
                            let mouseDownSelMinPitch = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.Pitch) |> Seq.min
                            let mouseDownSelMaxPitch = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.Pitch) |> Seq.max
                            mousePitch - mouseDownNote.Pitch |> clamp(minKey - mouseDownSelMinPitch)(maxKey - mouseDownSelMaxPitch)

                    if deltaPulse = 0L && deltaDur = 0L && deltaPitch = 0 then
                        x.ProgramModel.ActiveComp |> Rp.set comp
                        x.ProgramModel.ActiveSelection |> Rp.set mouseDownSelection
                        undoWriter.UnpushUndo()

                    else
                        // DiffDict: no existance -> no modification
                        let noteDiffDict = mouseDownSelection.SelectedNotes.ToImmutableDictionary(id, fun (note : Note) ->
                            note.Move(note.Pitch + deltaPitch, note.On + deltaPulse, note.Dur + deltaDur))

                        let uttDiffDict = comp.Utts.ToImmutableDictionary(id, fun (utt : Utterance) ->
                            if utt.Notes |> Seq.forall(fun note -> not(noteDiffDict.ContainsKey note)) then utt else
                                utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> noteDiffDict.GetOrDefault note note)))

                        x.ProgramModel.ActiveComp |> Rp.set(
                            comp.SetUtts(ImmutableArray.CreateRange(comp.Utts, fun utt -> uttDiffDict.GetOrDefault utt utt)))
                        x.ProgramModel.ActiveSelection |> Rp.set(
                            let activeUtt = mouseDownSelection.ActiveUtt |> Option.map(fun utt -> uttDiffDict.GetOrDefault utt utt)
                            let selectedNotes = ImmutableHashSet.CreateRange noteDiffDict.Values
                            CompSelection(activeUtt, selectedNotes))

                        undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                        x.ProgramModel.CompIsSaved |> Rp.set false

                    return! draggingNote dragNoteArgs

                | ChartMouseRelease e ->
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! draggingNote dragNoteArgs }

            and draggingSelBox mouseDownSelection mouseDownPos = behavior {
                match! () with
                | ChartMouseMove e ->
                    let actualHeight = edit.ActualHeight
                    let quarterWidth = edit.QuarterWidth
                    let keyHeight = edit.KeyHeight
                    let hOffset = edit.HOffsetAnimated
                    let vOffset = edit.VOffsetAnimated
                    let comp = !!x.ProgramModel.ActiveComp
                    let mousePos = e.GetPosition edit

                    let selMinPulse = pixelToPulse quarterWidth hOffset (min mousePos.X mouseDownPos.X) |> int64
                    let selMaxPulse = pixelToPulse quarterWidth hOffset (max mousePos.X mouseDownPos.X) |> int64
                    let selMinPitch = pixelToPitch keyHeight actualHeight vOffset (max mousePos.Y mouseDownPos.Y) |> round |> int
                    let selMaxPitch = pixelToPitch keyHeight actualHeight vOffset (min mousePos.Y mouseDownPos.Y) |> round |> int
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- Some(selMinPulse, selMaxPulse, selMinPitch, selMaxPitch)

                    let selection =
                        mouseDownSelection.SetSelectedNotes(
                            comp.AllNotes
                            |> Seq.filter(fun note ->
                                let noteHasIntersection =
                                    note.On <= selMaxPulse && note.Off >= selMinPulse && note.Pitch |> betweenInc selMinPitch selMaxPitch
                                noteHasIntersection <> mouseDownSelection.GetIsNoteSelected note)
                            |> ImmutableHashSet.CreateRange)
                    x.ProgramModel.ActiveSelection |> Rp.set selection

                    return! draggingSelBox mouseDownSelection mouseDownPos

                | ChartMouseRelease e ->
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- None
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! draggingSelBox mouseDownSelection mouseDownPos }

            Behavior.agent(idle()))

        x.RulerGrid |> ChartMouseEvent.BindEvents(
            let edit = x.RulerGrid

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        hintSetNone()
                        updatePlaybackCursorPos(e.GetPosition edit)
                        return! mouseLeftDown()
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

            and mouseLeftDown() = behavior {
                match! () with
                | ChartMouseMove e ->
                    updatePlaybackCursorPos(e.GetPosition edit)
                    return! mouseLeftDown()
                | ChartMouseRelease e ->
                    hintSetGhostCursor(e.GetPosition edit)
                    return! idle()
                | _ -> return! mouseLeftDown() }

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
                let hOffset = x.HScrollZoom.ScrollValue
                let quarterWidth = 2.0 ** log2Zoom
                let newQuarterWidth = 2.0 ** newLog2Zoom
                let currPulse = pixelToPulse quarterWidth hOffset xPos
                let nextPulse = pixelToPulse newQuarterWidth hOffset xPos
                let offsetDelta = nextPulse - currPulse

                x.HScrollZoom.Log2ZoomValue <- newLog2Zoom
                x.HScrollZoom.ScrollValue <- hOffset - offsetDelta

            elif edit.CanScrollV then
                let zoomDelta = float(sign e.Delta) * 0.1       // TODO Use Slider.SmallChange
                let log2Zoom = x.VScrollZoom.Log2ZoomValue
                let log2ZoomMin = x.VScrollZoom.Log2ZoomMinimum
                let log2ZoomMax = x.VScrollZoom.Log2ZoomMaximum
                let newLog2Zoom = log2Zoom + zoomDelta |> clamp log2ZoomMin log2ZoomMax
                let mousePos = e.GetPosition edit
                let yPos = mousePos.Y
                let vOffset = x.VScrollZoom.ScrollValue
                let keyHeight = 2.0 ** log2Zoom
                let newKeyHeight = 2.0 ** newLog2Zoom
                let actualHeight = x.ChartEditor.ActualHeight
                let currPitch = pixelToPitch keyHeight actualHeight vOffset yPos
                let nextPitch = pixelToPitch newKeyHeight actualHeight vOffset yPos
                let offsetDelta = nextPitch - currPitch

                x.VScrollZoom.Log2ZoomValue <- newLog2Zoom
                x.VScrollZoom.ScrollValue <- vOffset - offsetDelta

        x.ChartEditor.MouseWheel.Add(onMouseWheel x.ChartEditor)
        x.RulerGrid.MouseWheel.Add(onMouseWheel x.RulerGrid)
        x.SideKeyboard.MouseWheel.Add(onMouseWheel x.SideKeyboard)

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
            let keyboardModifiers = Keyboard.Modifiers

            match e.Key with
            | Key.Space ->
                let programModel = x.ProgramModel
                if not !!programModel.IsPlaying then
                    programModel.Play()
                else
                    programModel.Stop()

            | Key.Delete ->
                let comp = !!x.ProgramModel.ActiveComp
                let selection = !!x.ProgramModel.ActiveSelection
                let mouseDownSelection = selection.SelectedNotes.Intersect comp.AllNotes
                if not mouseDownSelection.IsEmpty then
                    // DelDict: no existance -> deletion
                    let uttDelDict =
                        comp.Utts
                        |> Seq.choose(fun utt ->
                            let newNotes = utt.Notes.RemoveAll(Predicate(selection.GetIsNoteSelected))
                            if newNotes.Length = 0 then None
                            elif newNotes.Length = utt.Notes.Length then Some(KeyValuePair(utt, utt))
                            else Some(KeyValuePair(utt, utt.SetNotes newNotes)))
                        |> ImmutableDictionary.CreateRange
                    x.ProgramModel.ActiveComp |> Rp.set(
                        comp.SetUtts(ImmutableArray.CreateRange uttDelDict.Values))
                    x.ProgramModel.ActiveSelection |> Rp.set(
                        let activeUtt = selection.ActiveUtt |> Option.bind(uttDelDict.TryGetValue >> Option.ofByRef)
                        CompSelection(activeUtt, ImmutableHashSet.Empty))

                    x.ProgramModel.UndoRedoStack.PushUndo(
                        DeleteNote, (comp, selection), (!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                    x.ProgramModel.CompIsSaved |> Rp.set false

            | Key.Z when keyboardModifiers.IsCtrl ->
                x.ProgramModel.Undo()

            | Key.Y when keyboardModifiers.IsCtrl ->
                x.ProgramModel.Redo()

            | Key.Z when keyboardModifiers = (ModifierKeys.Control ||| ModifierKeys.Shift) ->
                x.ProgramModel.Redo()

            | _ -> ()

            x.ChartEditorAdornerLayer.InvalidateVisual()


