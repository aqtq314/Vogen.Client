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

type NoteChartEditPanelBase() =
    inherit UserControl()

    static member BindBehaviors(x : NoteChartEditPanelBase, chartEditor, rulerGrid, sideKeyboard, hScrollZoom, vScrollZoom) =
        let getProgramModel() = x.DataContext :?> ProgramModel
        let chartEditor : ChartEditor = chartEditor
        let rulerGrid : RulerGrid = rulerGrid
        let sideKeyboard : SideKeyboard = sideKeyboard
        let hScrollZoom : ChartScrollZoomKitBase = hScrollZoom
        let vScrollZoom : ChartScrollZoomKitBase = vScrollZoom

        let rec mouseMidDownDragging(prevMousePos : Point, idle)(x : NoteChartEditBase) = behavior {
            match! () with
            | ChartMouseMove e ->
                let hOffset = x.HOffsetAnimated
                let vOffset = x.VOffsetAnimated
                let quarterWidth = x.QuarterWidth
                let keyHeight = x.KeyHeight

                let mousePos = e.GetPosition x
                if x.CanScrollH then
                    let xDelta = pixelToPulse quarterWidth 0.0 (mousePos.X - prevMousePos.X)
                    hScrollZoom.EnableAnimation <- false
                    hScrollZoom.ScrollValue <- hOffset - xDelta
                    hScrollZoom.EnableAnimation <- true
                if x.CanScrollV then
                    let yDelta = pixelToPitch keyHeight 0.0 0.0 (mousePos.Y - prevMousePos.Y)
                    vScrollZoom.EnableAnimation <- false
                    vScrollZoom.ScrollValue <- vOffset - yDelta
                    vScrollZoom.EnableAnimation <- true

                return! x |> mouseMidDownDragging(mousePos, idle)

            | ChartMouseRelease e -> return! idle()

            | _ -> return! x |> mouseMidDownDragging(prevMousePos, idle) }

        let updateCursorPos(e : MouseEventArgs)(x : NoteChartEditBase) =
            let hOffset = x.HOffsetAnimated
            let quarterWidth = x.QuarterWidth

            let mousePos = e.GetPosition x
            let newCursorPos = int64(pixelToPulse quarterWidth hOffset mousePos.X) |> NoteChartEditBase.CoerceCursorPosition x
            getProgramModel().ManualSetCursorPos newCursorPos

        chartEditor |> ChartMouseEvent.BindEvents(
            let x = chartEditor

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        x |> updateCursorPos e
                        return! mouseLeftDown updateCursorPos
                    | MouseButton.Middle ->
                        return! x |> mouseMidDownDragging(e.GetPosition x, idle)
                    | _ -> return! idle()
                | _ -> return! idle() }

            and mouseLeftDown updateCursorPos = behavior {
                match! () with
                | ChartMouseMove e ->
                    x |> updateCursorPos e
                    return! mouseLeftDown updateCursorPos
                | ChartMouseRelease e -> return! idle()
                | _ -> return! mouseLeftDown updateCursorPos }

            Behavior.agent(idle()))

        rulerGrid |> ChartMouseEvent.BindEvents(
            let x = rulerGrid

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        x |> updateCursorPos e
                        return! mouseLeftDown updateCursorPos
                    | MouseButton.Middle ->
                        return! x |> mouseMidDownDragging(e.GetPosition x, idle)
                    | _ -> return! idle()
                | _ -> return! idle() }

            and mouseLeftDown updateCursorPos = behavior {
                match! () with
                | ChartMouseMove e ->
                    x |> updateCursorPos e
                    return! mouseLeftDown updateCursorPos
                | ChartMouseRelease e -> return! idle()
                | _ -> return! mouseLeftDown updateCursorPos }

            Behavior.agent(idle()))

        sideKeyboard |> ChartMouseEvent.BindEvents(
            let x = sideKeyboard

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Middle ->
                        return! x |> mouseMidDownDragging(e.GetPosition x, idle)
                    | _ -> return! idle()
                | _ -> return! idle() }

            Behavior.agent(idle()))

        let onMouseWheel(x : NoteChartEditBase)(e : MouseWheelEventArgs) =
            if x.CanScrollH then
                let zoomDelta = float(sign e.Delta) * 0.1       // TODO Use Slider.SmallChange
                let log2Zoom = hScrollZoom.Log2ZoomValue
                let mousePos = e.GetPosition x
                let xPos = mousePos.X
                let hOffset = hScrollZoom.ScrollValue
                let quarterWidth = 2.0 ** log2Zoom
                let newQuarterWidth = 2.0 ** (log2Zoom + zoomDelta)
                let currPulse = pixelToPulse quarterWidth hOffset xPos
                let nextPulse = pixelToPulse newQuarterWidth hOffset xPos
                let offsetDelta = nextPulse - currPulse

                hScrollZoom.Log2ZoomValue <- log2Zoom + zoomDelta
                hScrollZoom.ScrollValue <- hOffset - offsetDelta

            elif x.CanScrollV then
                let zoomDelta = float(sign e.Delta) * 0.04      // TODO Use Slider.SmallChange
                let log2Zoom = vScrollZoom.Log2ZoomValue
                let mousePos = e.GetPosition x
                let yPos = mousePos.Y
                let vOffset = vScrollZoom.ScrollValue
                let keyHeight = 2.0 ** log2Zoom
                let newKeyHeight = 2.0 ** (log2Zoom + zoomDelta)
                let actualHeight = chartEditor.ActualHeight
                let currPulse = pixelToPitch keyHeight actualHeight vOffset yPos
                let nextPulse = pixelToPitch newKeyHeight actualHeight vOffset yPos
                let offsetDelta = nextPulse - currPulse

                vScrollZoom.Log2ZoomValue <- log2Zoom + zoomDelta
                vScrollZoom.ScrollValue <- vOffset - offsetDelta

        chartEditor.MouseWheel.Add(onMouseWheel chartEditor)
        rulerGrid.MouseWheel.Add(onMouseWheel rulerGrid)
        sideKeyboard.MouseWheel.Add(onMouseWheel sideKeyboard)

        chartEditor.OnCursorPositionChanged.Add <| fun (prevPlayPos, playPos) ->
            let x = chartEditor
            if x.IsPlaying then
                let quarterWidth = x.QuarterWidth
                let hOffset = hScrollZoom.ScrollValue
                let actualWidth = x.ActualWidth
                let hRightOffset = pixelToPulse quarterWidth hOffset actualWidth
                if float prevPlayPos < hRightOffset && float playPos >= hRightOffset then
                    hScrollZoom.ScrollValue <- hRightOffset

        x.KeyDown.Add <| fun e ->
            match e.Key with
            | Key.Space ->
                let programModel = getProgramModel()
                if not programModel.IsPlaying.Value then
                    programModel.Play()
                else
                    programModel.Stop()

            | _ -> ()

