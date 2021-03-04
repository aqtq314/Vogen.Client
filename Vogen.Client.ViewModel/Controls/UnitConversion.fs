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
        half actualHeight - (pitch - vOffset) * keyHeight

    let pixelToPitch keyHeight actualHeight vOffset yPos : float =
        vOffset + (half actualHeight - yPos) / keyHeight

module ChartConverters =
    let hScrollMaxConverter = ValueConverter.Create(fun comp trailingSil ->
        let comp : Composition = comp
        let trailingSil = if isNull trailingSil then 0.0 else Convert.ToDouble(trailingSil : obj)
        let compDur =
            comp.Utts
            |> Seq.collect(fun utt -> utt.Notes)
            |> Seq.map(fun note -> note.Off)
            |> Seq.appendItem 0L
            |> Seq.max
            |> float
        compDur + trailingSil)

    let hScrollSpanConverter = ValueConverter.CreateMulti(fun vs p ->       // unit in midi pulses
        match vs with
        | [| log2QuarterWidth; chartWidth |] ->
            let log2QuarterWidth = Convert.ToDouble log2QuarterWidth
            let chartWidth = Convert.ToDouble chartWidth
            let scale = if isNull p then 1.0 else Convert.ToDouble p
            scale * pixelToPulse (2.0 ** log2QuarterWidth) 0.0 chartWidth
        | _ ->
            raise(ArgumentException()))

    let vScrollSpanConverter = ValueConverter.CreateMulti(fun vs p ->       // unit in key indices
        match vs with
        | [| log2KeyHeight; chartHeight |] ->
            let log2KeyHeight = Convert.ToDouble log2KeyHeight
            let chartHeight = Convert.ToDouble chartHeight
            let scale = if isNull p then 1.0 else Convert.ToDouble p
            scale * pixelToPitch (2.0 ** log2KeyHeight) 0.0 0.0 -chartHeight
        | _ ->
            raise(ArgumentException()))

    let vScrollValueConverter =
        ValueConverter.Create(
            (fun vOffset -> -Convert.ToDouble(vOffset : obj) |> box),
            (fun sliderValue -> -Convert.ToDouble(sliderValue : obj) |> box))

    let hZoomToQuarterLength =
        ValueConverter.Create(
            (fun sliderValue -> exp(sliderValue * log 2.0) * 240.0),
            (fun quarterWidth -> log(quarterWidth / 240.0) / log 2.0))

    let vZoomToQuarterLength =
        ValueConverter.Create(
            (fun sliderValue -> exp(sliderValue * log 2.0) * 12.0),
            (fun quarterWidth -> log(quarterWidth / 12.0) / log 2.0))

    let log2Converter =
        ValueConverter.Create(
            (fun linearValue (p : obj) ->
                let p = if isNull p then 0.0 else Convert.ToDouble p
                log2 linearValue + p),
            (fun log2Value (p : obj) ->
                let p = if isNull p then 0.0 else Convert.ToDouble p
                2.0 ** (log2Value + p)))

    let exp2Converter =
        ValueConverter.Create(
            (fun log2Value (p : obj) ->
                let p = if isNull p then 0.0 else Convert.ToDouble p
                2.0 ** (log2Value + p)),
            (fun linearValue (p : obj) ->
                let p = if isNull p then 0.0 else Convert.ToDouble p
                log2 linearValue + p))


