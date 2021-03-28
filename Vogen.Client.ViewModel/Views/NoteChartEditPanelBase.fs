namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open System
open System.Collections.Generic
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

[<AbstractClass>]
type NoteChartEditPanelBase() =
    inherit UserControl()

    member x.ProgramModel = x.DataContext :?> ProgramModel

    abstract ChartEditor : ChartEditor
    abstract ChartEditorAdornerLayer : ChartEditorAdornerLayer
    abstract RulerGrid : RulerGrid
    abstract SideKeyboard : SideKeyboard
    abstract HScrollZoom : ChartScrollZoomKitBase
    abstract VScrollZoom : ChartScrollZoomKitBase

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
                for uttIndex in comp.Utts.Count - 1 .. -1 .. 0 do
                    let utt = comp.Utts.[uttIndex]
                    for noteIndex in utt.Notes.Count - 1 .. -1 .. 0 do
                        let note = utt.Notes.[noteIndex]
                        if mousePulse |> between note.On note.Off && mousePitch = note.Pitch then
                            yield note }

        let updatePlaybackCursorPos(e : MouseEventArgs)(edit : NoteChartEditBase) =
            let hOffset = edit.HOffsetAnimated
            let quarterWidth = edit.QuarterWidth

            let mousePos = e.GetPosition edit
            let newCursorPos = int64(pixelToPulse quarterWidth hOffset mousePos.X) |> NoteChartEditBase.CoerceCursorPosition edit
            x.ProgramModel.ManualSetCursorPos newCursorPos

        x.ChartEditor |> ChartMouseEvent.BindEvents(
            let edit = x.ChartEditor

            let updateMouseOverNote mousePosOp =
                let mouseOverNoteOp = mousePosOp |> Option.bind(fun mousePos -> findMouseOverNote mousePos edit)
                x.ChartEditorAdornerLayer.MouseOverNoteOp <- mouseOverNoteOp

                match mouseOverNoteOp with
                | None ->
                    edit.Cursor <- Cursors.Arrow
                | Some note ->
                    edit.Cursor <- Cursors.Hand

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        updateMouseOverNote None
                        edit |> updatePlaybackCursorPos e
                        return! mouseLeftDown()
                    | MouseButton.Middle ->
                        updateMouseOverNote None
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)
                    | _ -> return! idle()
                | ChartMouseMove e ->
                    updateMouseOverNote(Some(e.GetPosition edit))
                    return! idle()
                | _ -> return! idle() }

            and mouseLeftDown() = behavior {
                match! () with
                | ChartMouseMove e ->
                    edit |> updatePlaybackCursorPos e
                    return! mouseLeftDown()
                | ChartMouseRelease e ->
                    updateMouseOverNote(Some(e.GetPosition edit))
                    return! idle()
                | _ -> return! mouseLeftDown() }

            Behavior.agent(idle()))

        x.RulerGrid |> ChartMouseEvent.BindEvents(
            let edit = x.RulerGrid

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        edit |> updatePlaybackCursorPos e
                        return! mouseLeftDown()
                    | MouseButton.Middle ->
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)
                    | _ -> return! idle()
                | _ -> return! idle() }

            and mouseLeftDown() = behavior {
                match! () with
                | ChartMouseMove e ->
                    edit |> updatePlaybackCursorPos e
                    return! mouseLeftDown()
                | ChartMouseRelease e -> return! idle()
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
                let currPulse = pixelToPitch keyHeight actualHeight vOffset yPos
                let nextPulse = pixelToPitch newKeyHeight actualHeight vOffset yPos
                let offsetDelta = nextPulse - currPulse

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

