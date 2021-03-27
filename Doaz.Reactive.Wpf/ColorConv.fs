namespace Doaz.Reactive

open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Linq
open System.Windows
open System.Windows.Media


type ColorConv =
    static member argb(colorCode : int) =
        Color.FromArgb(
            byte(colorCode >>> 24),
            byte(colorCode >>> 16),
            byte(colorCode >>> 8),
            byte(colorCode >>> 0))

    static member rgb(colorCode : int) =
        Color.FromRgb(
            byte(colorCode >>> 16),
            byte(colorCode >>> 8),
            byte(colorCode >>> 0))

    static member aRgb(alpha : int)(colorCode : int) =
        Color.FromArgb(
            byte alpha,
            byte(colorCode >>> 16),
            byte(colorCode >>> 8),
            byte(colorCode >>> 0))

    static member aFRgb(alpha : float)(colorCode : int) =
        ColorConv.aRgb(int(alpha * 255.0)) colorCode

    static member withAlpha alpha (x : Color) =
        Color.FromArgb(alpha, x.R, x.G, x.B)

    static member withAlphaF alpha (x : Color) =
        x |> ColorConv.withAlpha(byte(alpha * 255.0))

    static member lerpColor(x : Color)(y : Color) amount =
        let lerpByte a b amount =
            if a = b then a else lerp(float a)(float b) amount |> clamp 0.0 255.0 |> byte
        Color.FromArgb(
            lerpByte x.A y.A amount,
            lerpByte x.R y.R amount,
            lerpByte x.G y.G amount,
            lerpByte x.B y.B amount)

    // https://en.wikipedia.org/wiki/HSL_and_HSV#HSL_to_RGB_alternative
    static member ahsl(alpha, hue, sat, lig) =
        let alpha = alpha |> clamp 0.0 1.0
        let hue =
            let hue = hue % 1.0
            if hue < 0.0 then hue + 1.0 else hue
        let sat = sat |> clamp 0.0 1.0
        let lig = lig |> clamp 0.0 1.0
        let a = sat * min lig (1.0 - lig)
        let inline f n =
            let k = (n + hue * 12.0) % 12.0
            lig - a * min(k - 3.0)(9.0 - k) |> clamp -1.0 1.0
        let r, g, b = f 0.0, f 8.0, f 4.0
        Color.FromArgb(
            byte(alpha * 255.0),
            byte(r * 255.0),
            byte(g * 255.0),
            byte(b * 255.0))


