namespace Doaz.Reactive.Controls

open Doaz.Reactive
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Media
open System.Windows.Media.Animation


type AnimatedSlider() =
    inherit Slider()

    member x.AnimatedValue
        with get() = x.GetValue AnimatedSlider.AnimatedValueProperty :?> float
        and set(v : float) = x.SetValue(AnimatedSlider.AnimatedValueProperty, box v)
    static member val AnimatedValueProperty =
        Dp.reg<float, AnimatedSlider> "AnimatedValue"
            (Dp.Meta(0.0))

    override x.OnValueChanged(oldValue, newValue) =
        base.OnValueChanged(oldValue, newValue)
        let animation = DoubleAnimation(newValue, Duration(TimeSpan.FromSeconds 0.2))
        animation.EasingFunction <- QuadraticEase(EasingMode = EasingMode.EaseOut)
        x.BeginAnimation(AnimatedSlider.AnimatedValueProperty, animation)



