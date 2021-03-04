namespace Vogen.Client.Controls

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Windows
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Animation
open Vogen.Client.Model


type ChartProperties() =
    inherit DependencyObject()

    static member GetTimeSignature(d : DependencyObject) = d.GetValue ChartProperties.TimeSignatureProperty :?> TimeSignature
    static member SetTimeSignature(d : DependencyObject, value : TimeSignature) = d.SetValue(ChartProperties.TimeSignatureProperty, value)
    static member val TimeSignatureProperty =
        Dp.rega<TimeSignature, ChartProperties> "TimeSignature"
            (Dp.Meta(timeSignature 4 4, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetQuarterWidth(d : DependencyObject) = d.GetValue ChartProperties.QuarterWidthProperty :?> float
    static member SetQuarterWidth(d : DependencyObject, value : float) = d.SetValue(ChartProperties.QuarterWidthProperty, value)
    static member val QuarterWidthProperty =
        Dp.rega<float, ChartProperties> "QuarterWidth"
            (Dp.Meta(100.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetHScrollMax(d : DependencyObject) = d.GetValue ChartProperties.HScrollMaxProperty :?> float
    static member SetHScrollMax(d : DependencyObject, value : float) = d.SetValue(ChartProperties.HScrollMaxProperty, value)
    static member val HScrollMaxProperty =
        Dp.rega<float, ChartProperties> "HScrollMax"
            (Dp.Meta(9600.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetKeyHeight(d : DependencyObject) = d.GetValue ChartProperties.KeyHeightProperty :?> float
    static member SetKeyHeight(d : DependencyObject, value : float) = d.SetValue(ChartProperties.KeyHeightProperty, value)
    static member val KeyHeightProperty =
        Dp.rega<float, ChartProperties> "KeyHeight"
            (Dp.Meta(12.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetMinKey(d : DependencyObject) = d.GetValue ChartProperties.MinKeyProperty :?> int
    static member SetMinKey(d : DependencyObject, value : int) = d.SetValue(ChartProperties.MinKeyProperty, value)
    static member val MinKeyProperty =
        Dp.rega<int, ChartProperties> "MinKey"
            (Dp.Meta(45, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun x (v0, v) ->
                x.CoerceValue ChartProperties.VOffsetProperty)))

    static member GetMaxKey(d : DependencyObject) = d.GetValue ChartProperties.MaxKeyProperty :?> int
    static member SetMaxKey(d : DependencyObject, value : int) = d.SetValue(ChartProperties.MaxKeyProperty, value)
    static member val MaxKeyProperty =
        Dp.rega<int, ChartProperties> "MaxKey"
            (Dp.Meta(93, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun x (v0, v) ->
                x.CoerceValue ChartProperties.VOffsetProperty)))

    static member GetHOffset(d : DependencyObject) = d.GetValue ChartProperties.HOffsetProperty :?> float
    static member SetHOffset(d : DependencyObject, value : float) = d.SetValue(ChartProperties.HOffsetProperty, value)
    static member CoerceHOffset x v = max 0.0 v
    static member val HOffsetProperty =
        Dp.rega<float, ChartProperties> "HOffset"
            (Dp.Meta(0.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun x (oldValue, newValue) ->
                match box x with
                | :? UIElement ->
                    let newValue = ChartProperties.CoerceHOffset x newValue
                    let animation = DoubleAnimation(newValue, Duration(TimeSpan.Zero))
                    if x |> ChartProperties.GetEnableAnimation then
                        animation.Duration <- Duration(TimeSpan.FromSeconds 0.2)
                    animation.EasingFunction <- QuadraticEase(EasingMode = EasingMode.EaseOut)
                    (box x :?> UIElement).BeginAnimation(ChartProperties.HOffsetAnimatedProperty, animation)
                | _ -> ()
            ), ChartProperties.CoerceHOffset))

    static member GetVOffset(d : DependencyObject) = d.GetValue ChartProperties.VOffsetProperty :?> float
    static member SetVOffset(d : DependencyObject, value : float) = d.SetValue(ChartProperties.VOffsetProperty, value)
    static member CoerceVOffset x v = v |> min(float(ChartProperties.GetMaxKey x)) |> max(float(ChartProperties.GetMinKey x))
    static member val VOffsetProperty =
        Dp.rega<float, ChartProperties> "VOffset"
            (Dp.Meta(69.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun _ _ -> ()), ChartProperties.CoerceVOffset))

    static member GetEnableAnimation(d : DependencyObject) = d.GetValue ChartProperties.EnableAnimationProperty :?> bool
    static member SetEnableAnimation(d : DependencyObject, value : bool) = d.SetValue(ChartProperties.EnableAnimationProperty, value)
    static member val EnableAnimationProperty =
        Dp.rega<bool, ChartProperties> "EnableAnimation"
            (Dp.Meta(true, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun _ _ -> ())))

    static member GetHOffsetAnimated(d : DependencyObject) = d.GetValue ChartProperties.HOffsetAnimatedProperty :?> float
    static member SetHOffsetAnimated(d : DependencyObject, value : float) = d.SetValue(ChartProperties.HOffsetAnimatedProperty, value)
    static member val HOffsetAnimatedProperty =
        Dp.rega<float, ChartProperties> "HOffsetAnimated"
            (Dp.Meta(0.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun _ _ -> ()), ChartProperties.CoerceHOffset))

    static member GetVOffsetAnimated(d : DependencyObject) = d.GetValue ChartProperties.VOffsetAnimatedProperty :?> float
    static member SetVOffsetAnimated(d : DependencyObject, value : float) = d.SetValue(ChartProperties.VOffsetAnimatedProperty, value)
    static member val VOffsetAnimatedProperty =
        Dp.rega<float, ChartProperties> "VOffsetAnimated"
            (Dp.Meta(69.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun _ _ -> ()), ChartProperties.CoerceVOffset))

    static member GetCursorPosition(d : DependencyObject) = d.GetValue ChartProperties.CursorPositionProperty :?> int64
    static member SetCursorPosition(d : DependencyObject, value : int64) = d.SetValue(ChartProperties.CursorPositionProperty, value)
    static member val CursorPositionProperty =
        Dp.rega<int64, ChartProperties> "CursorPosition"
            (Dp.Meta(0L, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits, (fun _ _ -> ()), (fun _ v -> max 0L v)))

    static member GetComposition(d : DependencyObject) = d.GetValue ChartProperties.CompositionProperty :?> Composition
    static member SetComposition(d : DependencyObject, value : Composition) = d.SetValue(ChartProperties.CompositionProperty, value)
    static member val CompositionProperty =
        Dp.rega<Composition, ChartProperties> "Composition"
            (Dp.Meta(Composition.Empty, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))


