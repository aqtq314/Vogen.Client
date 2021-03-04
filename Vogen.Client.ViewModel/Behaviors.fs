namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
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

    static member BindControl push (x : NoteChartEditBase) =
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
            let newCursorPos = int64(pixelToPulse quarterWidth hOffset mousePos.X)
            getProgramModel().ManualSetCursorPos newCursorPos

        chartEditor |> ChartMouseEvent.BindControl(
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

        rulerGrid |> ChartMouseEvent.BindControl(
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

        rulerGrid |> ChartMouseEvent.BindControl(
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


