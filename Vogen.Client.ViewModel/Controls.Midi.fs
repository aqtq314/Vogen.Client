namespace Vogen.Client.Controls

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Windows.Media


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

module TimeScale =
    let minMajorHopWidth = 80.0     // in screen pixels
    let minMinorHopWidth = 25.0

    let findHop(timeSig : TimeSignature) quarterWidth minHopWidth =
        seq {
            yield 1L
            yield 5L
            yield! Seq.initInfinite(fun i -> 15L <<< i)
                |> Seq.takeWhile(fun length -> length < timeSig.PulsesPerBeat)
            yield timeSig.PulsesPerBeat
            yield! Seq.initInfinite(fun i -> timeSig.PulsesPerMeasure <<< i) }
        |> Seq.find(fun hop ->
            pulseToPixel quarterWidth 0.0 (float hop) >= minHopWidth)

type WorkspaceProperties() =
    inherit DependencyObject()

    static member GetTimeSignature(d : DependencyObject) = d.GetValue WorkspaceProperties.TimeSignatureProperty :?> TimeSignature
    static member SetTimeSignature(d : DependencyObject, value : TimeSignature) = d.SetValue(WorkspaceProperties.TimeSignatureProperty, value)
    static member val TimeSignatureProperty =
        Dp.rega<TimeSignature, WorkspaceProperties> "TimeSignature"
            (Dp.Meta(timeSignature 4 4, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetQuarterWidth(d : DependencyObject) = d.GetValue WorkspaceProperties.QuarterWidthProperty :?> float
    static member SetQuarterWidth(d : DependencyObject, value : float) = d.SetValue(WorkspaceProperties.QuarterWidthProperty, value)
    static member val QuarterWidthProperty =
        Dp.rega<float, WorkspaceProperties> "QuarterWidth"
            (Dp.Meta(100.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetKeyHeight(d : DependencyObject) = d.GetValue WorkspaceProperties.KeyHeightProperty :?> float
    static member SetKeyHeight(d : DependencyObject, value : float) = d.SetValue(WorkspaceProperties.KeyHeightProperty, value)
    static member val KeyHeightProperty =
        Dp.rega<float, WorkspaceProperties> "KeyHeight"
            (Dp.Meta(12.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetMinKey(d : DependencyObject) = d.GetValue WorkspaceProperties.MinKeyProperty :?> int
    static member SetMinKey(d : DependencyObject, value : int) = d.SetValue(WorkspaceProperties.MinKeyProperty, value)
    static member val MinKeyProperty =
        Dp.rega<int, WorkspaceProperties> "MinKey"
            (Dp.Meta(23, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetMaxKey(d : DependencyObject) = d.GetValue WorkspaceProperties.MaxKeyProperty :?> int
    static member SetMaxKey(d : DependencyObject, value : int) = d.SetValue(WorkspaceProperties.MaxKeyProperty, value)
    static member val MaxKeyProperty =
        Dp.rega<int, WorkspaceProperties> "MaxKey"
            (Dp.Meta(105, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetHOffset(d : DependencyObject) = d.GetValue WorkspaceProperties.HOffsetProperty :?> float
    static member SetHOffset(d : DependencyObject, value : float) = d.SetValue(WorkspaceProperties.HOffsetProperty, value)
    static member val HOffsetProperty =
        Dp.rega<float, WorkspaceProperties> "HOffset"
            (Dp.Meta(0.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetVOffset(d : DependencyObject) = d.GetValue WorkspaceProperties.VOffsetProperty :?> float
    static member SetVOffset(d : DependencyObject, value : float) = d.SetValue(WorkspaceProperties.VOffsetProperty, value)
    static member val VOffsetProperty =
        Dp.rega<float, WorkspaceProperties> "VOffset"
            (Dp.Meta(57.0, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

type SideKeyboard() =
    inherit FrameworkElement()

    static let whiteKeyFill = Brushes.White
    static let whiteKeyPen : Pen = Pen(Brushes.Black, 0.6) |>! freeze
    static let blackKeyFill = Brushes.Black
    static let blackKeyPen : Pen = null

    static let keyOffsetLookup = [| -8; 0; -4; 0; 0; -9; 0; -6; 0; -3; 0; 0 |] |> Array.map float       // assuming key height is 12
    static let keyHeightLookup = [| 20; 12; 20; 12; 20; 21; 12; 21; 12; 21; 12; 21 |] |> Array.map float

    member x.BlackKeyLengthRatio
        with get() = x.GetValue SideKeyboard.BlackKeyLengthRatioProperty :?> float
        and set(v : float) = x.SetValue(SideKeyboard.BlackKeyLengthRatioProperty, box v)
    static member val BlackKeyLengthRatioProperty =
        Dp.reg<float, SideKeyboard> "BlackKeyLengthRatio"
            (Dp.Meta(0.6, Dp.MetaFlags.AffectsRender))

    override x.OnRender dc =
        let vOffset = WorkspaceProperties.GetVOffset x
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let keyHeight = WorkspaceProperties.GetKeyHeight x
        let minKey = WorkspaceProperties.GetMinKey x
        let maxKey = WorkspaceProperties.GetMaxKey x

        let whiteKeyWidth = actualWidth
        let blackKeyWidth = whiteKeyWidth * x.BlackKeyLengthRatio |> clamp 0.0 whiteKeyWidth
        let cornerRadius = 2.0 |> min(half keyHeight) |> min(half blackKeyWidth)

        let botPitch = int(pixelToPitch keyHeight actualHeight vOffset actualHeight)
        let topPitch = int(pixelToPitch keyHeight actualHeight vOffset 0.0) |> min maxKey

        // white keys
        for pitch in botPitch .. topPitch do
            if not(Midi.isBlackKey pitch) then
                let keyOffset = keyOffsetLookup.[pitch % 12] / 12.0
                let y = actualHeight - (float(pitch + 1) - vOffset - keyOffset) * keyHeight
                let height = keyHeightLookup.[pitch % 12] / 12.0 * keyHeight
                let x = if isNull whiteKeyPen then 0.0 else half whiteKeyPen.Thickness
                let width = max 0.0 (whiteKeyWidth - x * 2.0)
                dc.DrawRoundedRectangle(whiteKeyFill, whiteKeyPen, Rect(x, y, width, height), cornerRadius, cornerRadius)

        // black keys
        for pitch in botPitch .. topPitch do
            if Midi.isBlackKey pitch then
                let y = actualHeight - (float(pitch + 1) - vOffset) * keyHeight
                let height = keyHeight
                let x = if isNull blackKeyPen then 0.0 else half blackKeyPen.Thickness
                let width = max 0.0 (blackKeyWidth - x * 2.0)
                dc.DrawRoundedRectangle(blackKeyFill, blackKeyPen, Rect(0.0, y, width, height), cornerRadius, cornerRadius)

        // text labels
        for pitch in botPitch .. topPitch do
            if pitch % 12 = 0 then
                let ft = x |> makeFormattedText(sprintf "C%d" (pitch / 12 - 1))
                let x = whiteKeyWidth - 2.0 - ft.Width
                let y = actualHeight - (float(pitch + 1) - vOffset) * keyHeight + half(keyHeight - ft.Height)
                dc.DrawText(ft, Point(x, y))

type Ruler() =
    inherit FrameworkElement()

    static let majorTickHeight = 6.0
    static let minorTickHeight = 4.0

    static let tickPen = Pen(SolidColorBrush((0xFF000000u).AsColor()), 1.0) |>! freeze

    override x.MeasureOverride s =
        let fontFamily = TextBlock.GetFontFamily x
        let fontSize = TextBlock.GetFontSize x
        let height = fontSize * fontFamily.LineSpacing + max majorTickHeight minorTickHeight
        Size(zeroIfInf s.Width, height)

    override x.ArrangeOverride s =
        let fontFamily = TextBlock.GetFontFamily x
        let fontSize = TextBlock.GetFontSize x
        let height = fontSize * fontFamily.LineSpacing + max majorTickHeight minorTickHeight
        Size(s.Width, height)

    override x.OnRender dc =
        let hOffset = WorkspaceProperties.GetHOffset x
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterWidth = WorkspaceProperties.GetQuarterWidth x
        let timeSig = WorkspaceProperties.GetTimeSignature x

        let minPulse = int64(pixelToPulse quarterWidth hOffset 0.0)
        let maxPulse = int64(pixelToPulse quarterWidth hOffset actualWidth)

        let majorHop = TimeScale.findHop timeSig quarterWidth TimeScale.minMajorHopWidth
        let minorHop = TimeScale.findHop timeSig quarterWidth TimeScale.minMinorHopWidth

        // bottom border
        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, actualHeight - 0.5, actualWidth, 0.5))

        // tickmarks
        for currPulse in minPulse / minorHop * minorHop .. minorHop .. maxPulse do
            let isMajor = currPulse % majorHop = 0L
            let xPos = pulseToPixel quarterWidth hOffset (float currPulse)
            let height = if isMajor then majorTickHeight else minorTickHeight
            dc.DrawLine(tickPen, Point(xPos, actualHeight - height), Point(xPos, actualHeight))

            if isMajor then
                let textStr =
                    if majorHop % timeSig.PulsesPerMeasure = 0L then MidiTime.formatMeasures timeSig currPulse
                    elif majorHop % timeSig.PulsesPerBeat = 0L then MidiTime.formatMeasureBeats timeSig currPulse
                    else MidiTime.formatFull timeSig currPulse
                let ft = x |> makeFormattedText textStr
                let halfTextWidth = half ft.Width
                if xPos - halfTextWidth >= 0.0 && xPos + halfTextWidth <= actualWidth then
                    dc.DrawText(ft, new Point(xPos - halfTextWidth, 0.0))

type ChartGrid() =
    inherit BasicPanel()

    member val private MouseDownButton = None with get, set

    override x.OnMouseDown e =
        if x.CaptureMouse() then
            x.MouseDownButton <- Some e.ChangedButton
        base.OnMouseDown e

    override x.OnMouseUp e =
        x.MouseDownButton
        |> Option.filter((=) e.ChangedButton)
        |> Option.iter(fun mouseDownButton ->
            x.ReleaseMouseCapture())
        base.OnMouseUp e

    override x.OnLostMouseCapture e =
        x.MouseDownButton
        |> Option.iter(fun mouseDownButton ->
            x.MouseDownButton <- None)
        base.OnLostMouseCapture e

    override x.OnRender dc =
        let hOffset = WorkspaceProperties.GetHOffset x
        let vOffset = WorkspaceProperties.GetVOffset x
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterWidth = WorkspaceProperties.GetQuarterWidth x
        let keyHeight = WorkspaceProperties.GetKeyHeight x
        let timeSig = WorkspaceProperties.GetTimeSignature x
        let minKey = WorkspaceProperties.GetMinKey x
        let maxKey = WorkspaceProperties.GetMaxKey x

        let majorTickPen = Pen(SolidColorBrush((0x30000000u).AsColor()), 0.5) |>! freeze
        let minorTickPen = Pen(SolidColorBrush((0x20000000u).AsColor()), 0.5) |>! freeze
        let octavePen = Pen(SolidColorBrush((0x20000000u).AsColor()), 0.5) |>! freeze
        let blackKeyFill = SolidColorBrush((0x10000000u).AsColor()) |>! freeze

        // background
        dc.DrawRectangle(Brushes.Transparent, null, Rect(Size(actualWidth, actualHeight)))

        // time grids
        let minPulse = int64(pixelToPulse quarterWidth hOffset 0.0)
        let maxPulse = int64(pixelToPulse quarterWidth hOffset actualWidth)

        let majorHop = TimeScale.findHop timeSig quarterWidth TimeScale.minMajorHopWidth
        let minorHop = TimeScale.findHop timeSig quarterWidth TimeScale.minMinorHopWidth

        for currPulse in minPulse / minorHop * minorHop .. minorHop .. maxPulse do
            let x = pulseToPixel quarterWidth hOffset (float currPulse)
            let pen = if currPulse % majorHop = 0L then majorTickPen else minorTickPen
            dc.DrawLine(pen, Point(x, 0.0), Point(x, actualHeight))

        // pitch grids
        let botPitch = int(pixelToPitch keyHeight actualHeight vOffset actualHeight)
        let topPitch = int(pixelToPitch keyHeight actualHeight vOffset 0.0) |> min maxKey

        for pitch in botPitch .. topPitch do
            match pitch % 12 with
            | 0 | 5 ->
                let y = actualHeight - (float pitch - vOffset) * keyHeight - half octavePen.Thickness
                dc.DrawLine(octavePen, Point(0.0, y), Point(actualWidth, y))
            | _ -> ()

            if pitch |> Midi.isBlackKey then
                let y = actualHeight - (float(pitch + 1) - vOffset) * keyHeight
                dc.DrawRectangle(blackKeyFill, null, Rect(0.0, y, actualWidth, keyHeight))

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





