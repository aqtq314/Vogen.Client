namespace Vogen.Client.Controls

open Doaz.Reactive
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Input
open System.Windows.Media
open Vogen.Client.Model


[<AutoOpen>]
module ChartUnitConversion =
    let pulseToPixel quarterWidth hOffset pulses =
        (pulses - hOffset) / float Midi.ppqn * quarterWidth

    let pixelToPulse quarterWidth hOffset xPos =
        hOffset + xPos * float Midi.ppqn / quarterWidth

    let pitchToPixel keyHeight actualHeight vOffset pitch : float =
        actualHeight - (pitch - vOffset) * keyHeight

    let pixelToPitch keyHeight actualHeight vOffset yPos : float =
        vOffset + (actualHeight - yPos) / keyHeight

module ChartConverters =
    let hScrollSpanConverter = ValueConverter.CreateMulti(fun vs p ->       // unit in midi pulses
        match vs with
        | [| quarterWidth; chartWidth |] ->
            let quarterWidth = Convert.ToDouble quarterWidth
            let chartWidth = Convert.ToDouble chartWidth
            let scale = if isNull p then 1.0 else Convert.ToDouble p
            scale * pixelToPulse quarterWidth 0.0 chartWidth
        | _ ->
            raise(ArgumentException()))

    let vScrollViewportConverter = ValueConverter.CreateMulti(fun vs ->     // unit in key indices
        match vs with
        | [| keyHeight; chartHeight |] ->
            let keyHeight = Convert.ToDouble keyHeight
            let chartHeight = Convert.ToDouble chartHeight
            chartHeight / keyHeight
        | _ ->
            raise(ArgumentException()))

    let vScrollValueConverter =
        ValueConverter.Create(
            (fun vOffset -> -Convert.ToDouble(vOffset : obj) |> box),
            (fun sliderValue -> -Convert.ToDouble(sliderValue : obj) |> box))

    let vScrollMinimumConverter = ValueConverter.Create(fun minKey ->
        let minKey = Convert.ToDouble(minKey : obj)
        -minKey)

    let vScrollMaximumConverter = ValueConverter.CreateMulti(fun vs ->      // unit in key indices
        match vs with
        | [| maxKey; keyHeight; chartHeight |] ->
            let maxKey = Convert.ToInt32 maxKey
            let keyHeight = Convert.ToDouble keyHeight
            let chartHeight = Convert.ToDouble chartHeight
            let extent = float(maxKey + 1) - chartHeight / keyHeight
            -(max extent 0.0)
        | _ ->
            raise(ArgumentException()))

    let hZoomToQuarterLength =
        ValueConverter.Create(
            (fun sliderValue -> exp(sliderValue * log 2.0) * 240.0),
            (fun quarterWidth -> log(quarterWidth / 240.0) / log 2.0))

    let vZoomToQuarterLength =
        ValueConverter.Create(
            (fun sliderValue -> exp(sliderValue * log 2.0) * 12.0),
            (fun quarterWidth -> log(quarterWidth / 12.0) / log 2.0))


