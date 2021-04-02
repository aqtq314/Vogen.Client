namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Windows
open System.Windows.Controls
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
            (Dp.Meta(Midi.ppqn, Dp.MetaFlags.AffectsRender))

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

        let findMouseOverNote(mousePos : Point)(edit : ChartEditor) =
            let actualHeight = edit.ActualHeight
            let quarterWidth = edit.QuarterWidth
            let keyHeight = edit.KeyHeight
            let hOffset = edit.HOffsetAnimated
            let vOffset = edit.VOffsetAnimated
            let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64
            let mousePitch = pixelToPitch keyHeight actualHeight vOffset mousePos.Y |> round |> int

            let comp = !!x.ProgramModel.ActiveComp

            Seq.tryHead <| seq {
                for uttIndex in comp.Utts.Length - 1 .. -1 .. 0 do
                    let utt = comp.Utts.[uttIndex]
                    for noteIndex in utt.Notes.Length - 1 .. -1 .. 0 do
                        let note = utt.Notes.[noteIndex]
                        if mousePulse |> between note.On note.Off && mousePitch = note.Pitch then
                            yield note }
            |> Option.map(fun note ->
                let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                let noteDragType =
                    if   mousePos.X <= min(x0 + 6.0)(lerp x0 x1 0.2) then NoteDragResizeLeft
                    elif mousePos.X >= max(x1 - 6.0)(lerp x0 x1 0.8) then NoteDragResizeRight
                    else NoteDragMove
                note, noteDragType)

        let findMouseOverNoteOp mousePosOp edit =
            mousePosOp |> Option.bind(fun mousePos -> findMouseOverNote mousePos edit)

        let quantize snap quantization (timeSig : TimeSignature) pulses =
            if not snap then pulses else
                let pulsesMeasureQuantized = pulses / timeSig.PulsesPerMeasure * timeSig.PulsesPerMeasure
                pulsesMeasureQuantized + (pulses - pulsesMeasureQuantized) / quantization * quantization

        let quantizeCeil snap quantization (timeSig : TimeSignature) pulses =
            if not snap then pulses else
                let pulsesMeasureQuantized = pulses / timeSig.PulsesPerMeasure * timeSig.PulsesPerMeasure
                pulsesMeasureQuantized + ((pulses - pulsesMeasureQuantized) /^ quantization * quantization |> min timeSig.PulsesPerMeasure)

        let getPlaybackCursorPos(mousePos : Point)(edit : NoteChartEditBase) =
            let hOffset = edit.HOffsetAnimated
            let quarterWidth = edit.QuarterWidth
            let comp = !!x.ProgramModel.ActiveComp
            let quantization = x.Quantization
            let snap = x.Snap

            let newCursorPos = int64(pixelToPulse quarterWidth hOffset mousePos.X) |> NoteChartEditBase.CoerceCursorPosition edit
            newCursorPos |> quantize snap quantization comp.TimeSig0

        let updatePlaybackCursorPos(e : MouseEventArgs)(edit : NoteChartEditBase) =
            let mousePos = e.GetPosition edit
            let newCursorPos = getPlaybackCursorPos mousePos edit
            x.ProgramModel.ManualSetCursorPos newCursorPos

        x.ChartEditor |> ChartMouseEvent.BindEvents(
            let edit = x.ChartEditor

            let updateMouseOverNote mousePosOp =
                let mouseOverNoteOp = findMouseOverNoteOp mousePosOp edit
                x.ChartEditorAdornerLayer.MouseOverNoteOp <- mouseOverNoteOp

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        updateMouseOverNote None
                        let comp = !!x.ProgramModel.ActiveComp
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos edit
                        match mouseDownNoteOp with
                        | None ->
                            let mouseDownIsNoteSelected =
                                match Keyboard.Modifiers with
                                | ModifierKeys.Control ->
                                    comp.GetIsNoteSelected
                                | _ ->
                                    x.ProgramModel.UpdateComp(fun comp ->
                                        comp.UpdateSelectedNotes(fun _ -> ImmutableHashSet.Empty))
                                    (fun _ -> false)

                            return! draggingSelBox mouseDownIsNoteSelected mousePos

                        | Some(note, noteDragType) ->
                            let quarterWidth = edit.QuarterWidth
                            let hOffset = edit.HOffsetAnimated
                            let mousePulse = pixelToPulse quarterWidth hOffset mousePos.X |> int64

                            let quantization = x.Quantization
                            let snap = x.Snap
                            let mouseDownPulse =
                                match noteDragType with
                                | NoteDragResizeLeft
                                | NoteDragMove ->
                                    let noteGridOffset = note.On - (note.On |> quantize snap quantization comp.TimeSig0)
                                    (mousePulse - noteGridOffset |> quantize snap quantization comp.TimeSig0) + noteGridOffset
                                | NoteDragResizeRight ->
                                    let noteGridOffset = note.Off - (note.Off |> quantizeCeil snap quantization comp.TimeSig0)
                                    (mousePulse - noteGridOffset |> quantizeCeil snap quantization comp.TimeSig0) + noteGridOffset

                            //MidiPlayback.playPitch note.Pitch
                            let comp, isPendingDeselect =
                                match comp.GetIsNoteSelected note, Keyboard.Modifiers with
                                | false, ModifierKeys.Control ->
                                    let comp =
                                        x.ProgramModel.UpdateCompReturn(fun comp ->
                                            comp.UpdateSelectedNotes(fun selectedNotes -> selectedNotes.Add note))
                                    comp, false

                                | true, ModifierKeys.Control ->
                                    comp, true

                                | false, _ ->
                                    let comp =
                                        x.ProgramModel.UpdateCompReturn(fun comp ->
                                            comp.UpdateSelectedNotes(fun _ -> ImmutableHashSet.Create note))
                                    comp, false

                                | true, _ ->
                                    comp, false

                            let mouseDownSelection =
                                comp.Utts
                                |> Seq.collect(fun utt -> utt.Notes)
                                |> Seq.filter comp.GetIsNoteSelected
                                |> ImmutableHashSet.CreateRange

                            let dragNoteArgs = note, comp, mouseDownSelection, mouseDownPulse, noteDragType
                            if isPendingDeselect then
                                return! mouseDownNotePendingDeselect dragNoteArgs
                            else
                                return! draggingNote dragNoteArgs

                    | MouseButton.Middle ->
                        updateMouseOverNote None
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)

                    | _ -> return! idle()

                | ChartMouseMove e ->
                    updateMouseOverNote(Some(e.GetPosition edit))
                    return! idle()

                | _ -> return! idle() }

            and mouseDownNotePendingDeselect dragNoteArgs = behavior {
                let mouseDownNote, mouseDownComp, mouseDownSelection, mouseDownPulse, noteDragType = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    return! (draggingNote dragNoteArgs).Run(ChartMouseMove e)

                | ChartMouseRelease e ->
                    x.ProgramModel.UpdateComp(fun comp ->
                        comp.UpdateSelectedNotes(fun selectedNotes -> selectedNotes.Remove mouseDownNote))
                    updateMouseOverNote(Some(e.GetPosition edit))
                    return! idle()

                | _ -> return! mouseDownNotePendingDeselect dragNoteArgs }

            and draggingNote dragNoteArgs = behavior {
                let mouseDownNote, mouseDownComp, mouseDownSelection, mouseDownPulse, noteDragType = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    let actualHeight = edit.ActualHeight
                    let quarterWidth = edit.QuarterWidth
                    let keyHeight = edit.KeyHeight
                    let minKey = edit.MinKey
                    let maxKey = edit.MaxKey
                    let hOffset = edit.HOffsetAnimated
                    let vOffset = edit.VOffsetAnimated
                    let comp = !!x.ProgramModel.ActiveComp
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
                        let selMinPulse = mouseDownSelection |> Seq.map(fun note -> note.On) |> Seq.min
                        let selMinDur   = mouseDownSelection |> Seq.map(fun note -> note.Dur) |> Seq.min
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
                            let mouseDownSelMinPitch = mouseDownSelection |> Seq.map(fun note -> note.Pitch) |> Seq.min
                            let mouseDownSelMaxPitch = mouseDownSelection |> Seq.map(fun note -> note.Pitch) |> Seq.max
                            mousePitch - mouseDownNote.Pitch |> clamp(minKey - mouseDownSelMinPitch)(maxKey - mouseDownSelMaxPitch)

                    if deltaPulse = 0L && deltaDur = 0L && deltaPitch = 0 then
                        x.ProgramModel.UpdateComp(fun _ -> mouseDownComp)

                    else
                        let selectedNotesToNew = mouseDownSelection.ToImmutableDictionary(id, fun (note : Note) ->
                            note.Move(note.Pitch + deltaPitch, note.On + deltaPulse, note.Dur + deltaDur))

                        let newUtts = ImmutableArray.CreateRange(mouseDownComp.Utts |> Seq.map(fun utt ->
                            if utt.Notes |> Seq.forall(fun note -> not(selectedNotesToNew.ContainsKey note)) then utt else
                                utt.SetNotes(ImmutableArray.CreateRange(utt.Notes |> Seq.map(fun note ->
                                    selectedNotesToNew.TryGetValue note
                                    |> Option.ofByRef
                                    |> Option.defaultValue note)))))

                        x.ProgramModel.UpdateComp(fun comp ->
                            comp.SetUtts(newUtts).UpdateSelectedNotes(fun _ -> ImmutableHashSet.CreateRange selectedNotesToNew.Values))

                    return! draggingNote dragNoteArgs

                | ChartMouseRelease e ->
                    updateMouseOverNote(Some(e.GetPosition edit))
                    return! idle()

                | _ -> return! draggingNote dragNoteArgs }

            and draggingSelBox mouseDownIsNoteSelected mouseDownPos = behavior {
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
                        comp.Utts
                        |> Seq.collect(fun utt -> utt.Notes)
                        |> Seq.filter(fun note ->
                            let noteHasIntersection =
                                note.On <= selMaxPulse && note.Off >= selMinPulse && note.Pitch |> betweenInc selMinPitch selMaxPitch
                            noteHasIntersection <> mouseDownIsNoteSelected note)
                        |> ImmutableHashSet.CreateRange
                    x.ProgramModel.UpdateComp(fun comp -> comp.UpdateSelectedNotes(fun _ -> selection))

                    return! draggingSelBox mouseDownIsNoteSelected mouseDownPos

                | ChartMouseRelease e ->
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- None
                    updateMouseOverNote(Some(e.GetPosition edit))
                    return! idle()

                | _ -> return! draggingSelBox mouseDownIsNoteSelected mouseDownPos }

            Behavior.agent(idle()))

        let updateMouseOverCursorPos mousePosOp (edit : NoteChartEditBase) =
            let mouseOverCursorPosOp = mousePosOp |> Option.map(fun (mousePos : Point) -> getPlaybackCursorPos mousePos edit)
            x.ChartEditorAdornerLayer.MouseOverCursorPositionOp <- mouseOverCursorPosOp

        x.RulerGrid |> ChartMouseEvent.BindEvents(
            let edit = x.RulerGrid

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        edit |> updateMouseOverCursorPos None
                        edit |> updatePlaybackCursorPos e
                        return! mouseLeftDown()
                    | MouseButton.Middle ->
                        edit |> updateMouseOverCursorPos None
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)
                    | _ -> return! idle()
                | ChartMouseMove e ->
                    edit |> updateMouseOverCursorPos(Some(e.GetPosition edit))
                    return! idle()
                | ChartMouseLeave e ->
                    edit |> updateMouseOverCursorPos None
                    return! idle()
                | _ -> return! idle() }

            and mouseLeftDown() = behavior {
                match! () with
                | ChartMouseMove e ->
                    edit |> updatePlaybackCursorPos e
                    return! mouseLeftDown()
                | ChartMouseRelease e ->
                    edit |> updateMouseOverCursorPos(Some(e.GetPosition edit))
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
        x.ChartEditor.OnCursorPositionChanged.Add <| fun (prevPlayPos, playPos) ->
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
            match e.Key with
            | Key.Space ->
                let programModel = x.ProgramModel
                if not programModel.IsPlaying.Value then
                    programModel.Play()
                else
                    programModel.Stop()

            | _ -> ()

