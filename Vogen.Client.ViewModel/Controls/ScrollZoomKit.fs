namespace Vogen.Client.Controls

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Animation
open Vogen.Client.Model


type ChartScrollZoomKitBase() =
    inherit Control()

    member x.EnableAnimation
        with get() = x.GetValue ChartScrollZoomKitBase.EnableAnimationProperty :?> bool
        and set(v : bool) = x.SetValue(ChartScrollZoomKitBase.EnableAnimationProperty, box v)
    static member val EnableAnimationProperty =
        Dp.reg<bool, ChartScrollZoomKitBase> "EnableAnimation"
            (Dp.Meta(true))

    member x.ScrollMinimum
        with get() = x.GetValue ChartScrollZoomKitBase.ScrollMinimumProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.ScrollMinimumProperty, box v)
    static member val ScrollMinimumProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "ScrollMinimum"
            (Dp.Meta(0.0, (fun x _ ->
                x.CoerceValue ChartScrollZoomKitBase.ScrollValueProperty)))

    member x.ScrollMaximum
        with get() = x.GetValue ChartScrollZoomKitBase.ScrollMaximumProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.ScrollMaximumProperty, box v)
    static member val ScrollMaximumProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "ScrollMaximum"
            (Dp.Meta(1.0, (fun x _ ->
                x.CoerceValue ChartScrollZoomKitBase.ScrollValueProperty)))

    member x.ScrollViewport
        with get() = x.GetValue ChartScrollZoomKitBase.ScrollViewportProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.ScrollViewportProperty, box v)
    static member val ScrollViewportProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "ScrollViewport"
            (Dp.Meta(0.0))

    member x.ScrollValue
        with get() = x.GetValue ChartScrollZoomKitBase.ScrollValueProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.ScrollValueProperty, box v)
    static member OnScrollValueChanged x (oldValue, newValue) =
        let newValue = ChartScrollZoomKitBase.CoerceScrollValue x newValue
        let animation = DoubleAnimation(newValue, Duration(TimeSpan.Zero))
        if x.EnableAnimation then
            animation.Duration <- Duration(TimeSpan.FromSeconds 0.2)
            animation.EasingFunction <- QuadraticEase(EasingMode = EasingMode.EaseOut)
        x.BeginAnimation(ChartScrollZoomKitBase.ScrollValueAnimatedProperty, animation)
    static member CoerceScrollValue x v = v |> min x.ScrollMaximum |> max x.ScrollMinimum
    static member val ScrollValueProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "ScrollValue"
            (Dp.Meta(0.0, ChartScrollZoomKitBase.OnScrollValueChanged, ChartScrollZoomKitBase.CoerceScrollValue))

    member x.ScrollValueAnimated
        with get() = x.GetValue ChartScrollZoomKitBase.ScrollValueAnimatedProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.ScrollValueAnimatedProperty, box v)
    static member val ScrollValueAnimatedProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "ScrollValueAnimated"
            (Dp.Meta(0.0))

    member x.Log2ZoomMinimum
        with get() = x.GetValue ChartScrollZoomKitBase.Log2ZoomMinimumProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.Log2ZoomMinimumProperty, box v)
    static member val Log2ZoomMinimumProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "Log2ZoomMinimum"
            (Dp.Meta(0.0, (fun x _ ->
                x.CoerceValue ChartScrollZoomKitBase.Log2ZoomValueProperty)))

    member x.Log2ZoomMaximum
        with get() = x.GetValue ChartScrollZoomKitBase.Log2ZoomMaximumProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.Log2ZoomMaximumProperty, box v)
    static member val Log2ZoomMaximumProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "Log2ZoomMaximum"
            (Dp.Meta(1.0, (fun x _ ->
                x.CoerceValue ChartScrollZoomKitBase.Log2ZoomValueProperty)))

    member x.Log2ZoomValue
        with get() = x.GetValue ChartScrollZoomKitBase.Log2ZoomValueProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.Log2ZoomValueProperty, box v)
    static member OnLog2ZoomValueChanged x (oldValue, newValue) =
        let newValue = ChartScrollZoomKitBase.CoerceLog2ZoomValue x newValue
        let animation = DoubleAnimation(newValue, Duration(TimeSpan.Zero))
        if x.EnableAnimation then
            animation.Duration <- Duration(TimeSpan.FromSeconds 0.2)
            animation.EasingFunction <- QuadraticEase(EasingMode = EasingMode.EaseOut)
        x.BeginAnimation(ChartScrollZoomKitBase.Log2ZoomValueAnimatedProperty, animation)
    static member CoerceLog2ZoomValue x v = v |> min x.Log2ZoomMaximum |> max x.Log2ZoomMinimum
    static member val Log2ZoomValueProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "Log2ZoomValue"
            (Dp.Meta(0.0, ChartScrollZoomKitBase.OnLog2ZoomValueChanged, ChartScrollZoomKitBase.CoerceLog2ZoomValue))

    member x.Log2ZoomValueAnimated
        with get() = x.GetValue ChartScrollZoomKitBase.Log2ZoomValueAnimatedProperty :?> float
        and set(v : float) = x.SetValue(ChartScrollZoomKitBase.Log2ZoomValueAnimatedProperty, box v)
    static member val Log2ZoomValueAnimatedProperty =
        Dp.reg<float, ChartScrollZoomKitBase> "Log2ZoomValueAnimated"
            (Dp.Meta(0.0))


