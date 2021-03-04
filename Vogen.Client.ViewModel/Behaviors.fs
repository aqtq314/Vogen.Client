module Vogen.Client.ViewModel.Behaviors

open Doaz.Reactive
open Doaz.Reactive.Controls
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open Vogen.Client.Controls
open Vogen.Client.Model


type ChartMouseEvent =
    | ChartMouseDown of e : MouseButtonEventArgs
    | ChartMouseMove of e : MouseEventArgs
    | ChartMouseRelease of e : MouseEventArgs

let bindWorkspace(workspaceRoot : FrameworkElement, programModel : ProgramModel, chart : Chart, ruler : Ruler, sideKeyboard : SideKeyboard) =
    let rec mouseMidDownDragging(prevMousePos : Point, idle, moveX, moveY) x = behavior {
        match! () with
        | ChartMouseMove e ->
            let hOffset = ChartProperties.GetHOffsetAnimated x
            let vOffset = ChartProperties.GetVOffset x
            let quarterWidth = ChartProperties.GetQuarterWidth x
            let keyHeight = ChartProperties.GetKeyHeight x

            let mousePos = e.GetPosition x
            if moveX then
                let xDelta = pixelToPulse quarterWidth 0.0 (mousePos.X - prevMousePos.X)
                ChartProperties.SetHOffset(workspaceRoot, hOffset - xDelta)
            if moveY then
                let yDelta = pixelToPitch keyHeight 0.0 0.0 (mousePos.Y - prevMousePos.Y)
                ChartProperties.SetVOffset(workspaceRoot, vOffset - yDelta)

            return! x |> mouseMidDownDragging(mousePos, idle, moveX, moveY)

        | ChartMouseRelease e -> return! idle

        | _ -> return! x |> mouseMidDownDragging(prevMousePos, idle, moveX, moveY) }

    let updateCursorPos(e : MouseEventArgs) x =
        let hOffset = ChartProperties.GetHOffsetAnimated x
        let quarterWidth = ChartProperties.GetQuarterWidth x

        let mousePos = e.GetPosition x
        let newCursorPos = int64(pixelToPulse quarterWidth hOffset mousePos.X)
        programModel.ManualSetCursorPos newCursorPos

    let pushChartMouseEvent =
        let x = chart

        let rec idle() = behavior {
            match! () with
            | ChartMouseDown e ->
                match e.ChangedButton with
                | MouseButton.Left ->
                    x |> updateCursorPos e
                    return! mouseLeftDown updateCursorPos

                | MouseButton.Middle ->
                    return! x |> mouseMidDownDragging(e.GetPosition x, idle(), true, true)

                | _ -> return! idle()

            | _ -> return! idle() }

        and mouseLeftDown updateCursorPos = behavior {
            match! () with
            | ChartMouseMove e ->
                x |> updateCursorPos e
                return! mouseLeftDown updateCursorPos

            | ChartMouseRelease e -> return! idle()

            | _ -> return! mouseLeftDown updateCursorPos }

        Behavior.agent(idle())

    chart.MouseDown.Add(fun e ->
        pushChartMouseEvent(ChartMouseDown e)
        e.Handled <- true)

    chart.MouseMove.Add(fun e ->
        pushChartMouseEvent(ChartMouseMove e)
        e.Handled <- true)

    chart.LostMouseCapture.Add(fun e ->
        pushChartMouseEvent(ChartMouseRelease e)
        e.Handled <- true)

    let pushRulerMouseEvent =
        let x = ruler

        let rec idle() = behavior {
            match! () with
            | ChartMouseDown e ->
                match e.ChangedButton with
                | MouseButton.Left ->
                    x |> updateCursorPos e
                    return! mouseLeftDown updateCursorPos

                | MouseButton.Middle ->
                    return! x |> mouseMidDownDragging(e.GetPosition x, idle(), true, false)

                | _ -> return! idle()

            | _ -> return! idle() }

        and mouseLeftDown updateCursorPos = behavior {
            match! () with
            | ChartMouseMove e ->
                x |> updateCursorPos e
                return! mouseLeftDown updateCursorPos

            | ChartMouseRelease e -> return! idle()

            | _ -> return! mouseLeftDown updateCursorPos }

        Behavior.agent(idle())

    ruler.MouseDown.Add(fun e ->
        pushRulerMouseEvent(ChartMouseDown e)
        e.Handled <- true)

    ruler.MouseMove.Add(fun e ->
        pushRulerMouseEvent(ChartMouseMove e)
        e.Handled <- true)

    ruler.LostMouseCapture.Add(fun e ->
        pushRulerMouseEvent(ChartMouseRelease e)
        e.Handled <- true)

    let pushSideKeyboardMouseEvent =
        let x = sideKeyboard

        let rec idle() = behavior {
            match! () with
            | ChartMouseDown e ->
                match e.ChangedButton with
                | MouseButton.Middle ->
                    return! x |> mouseMidDownDragging(e.GetPosition x, idle(), false, true)

                | _ -> return! idle()

            | _ -> return! idle() }

        Behavior.agent(idle())

    sideKeyboard.MouseDown.Add(fun e ->
        pushSideKeyboardMouseEvent(ChartMouseDown e)
        e.Handled <- true)

    sideKeyboard.MouseMove.Add(fun e ->
        pushSideKeyboardMouseEvent(ChartMouseMove e)
        e.Handled <- true)

    sideKeyboard.LostMouseCapture.Add(fun e ->
        pushSideKeyboardMouseEvent(ChartMouseRelease e)
        e.Handled <- true)


