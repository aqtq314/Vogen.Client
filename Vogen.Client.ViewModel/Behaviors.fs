module Vogen.Client.ViewModel.Behaviors

open Doaz.Reactive
open Doaz.Reactive.Controls
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Input
open Vogen.Client.Controls

#nowarn "40"


type ChartMouseEvent =
    | ChartMouseDown of e : MouseButtonEventArgs
    | ChartMouseMove of e : MouseEventArgs
    | ChartMouseRelease of e : MouseEventArgs

let bindWorkspace(workspaceRoot : FrameworkElement, chartGrid : ChartGrid) =
    let pushChartMouseEvent =
        let x = chartGrid

        let rec idle() = behavior {
            match! () with
            | ChartMouseDown e ->
                match e.ChangedButton with
                | MouseButton.Middle ->
                    return! midMouseDown(e.GetPosition x)

                | _ -> return! idle()

            | _ -> return! idle() }

        and midMouseDown prevMousePos = behavior {
            match! () with
            | ChartMouseMove e ->
                let hOffset = WorkspaceProperties.GetHOffset x
                let vOffset = WorkspaceProperties.GetVOffset x
                let quarterWidth = WorkspaceProperties.GetQuarterWidth x
                let keyHeight = WorkspaceProperties.GetKeyHeight x

                let mousePos = e.GetPosition x
                let xDelta = pixelToPulse quarterWidth 0.0 (mousePos.X - prevMousePos.X)
                let yDelta = pixelToPitch keyHeight 0.0 0.0 (mousePos.Y - prevMousePos.Y)
                WorkspaceProperties.SetHOffset(workspaceRoot, hOffset - xDelta)
                WorkspaceProperties.SetVOffset(workspaceRoot, vOffset - yDelta)

                return! midMouseDown mousePos

            | ChartMouseRelease e -> return! idle()

            | _ -> return! midMouseDown prevMousePos }

        Behavior.agent(idle())

    chartGrid.MouseDown.AddHandler(MouseButtonEventHandler(fun sender e ->
        pushChartMouseEvent(ChartMouseDown e)
        e.Handled <- true))

    chartGrid.MouseMove.AddHandler(MouseEventHandler(fun sender e ->
        pushChartMouseEvent(ChartMouseMove e)
        e.Handled <- true))

    chartGrid.LostMouseCapture.AddHandler(MouseEventHandler(fun sender e ->
        pushChartMouseEvent(ChartMouseRelease e)
        e.Handled <- true))


