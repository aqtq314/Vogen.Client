namespace Doaz.Reactive.Controls

open Doaz.Reactive
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Windows.Media


type Chart() =
    inherit DependencyObject()

    static member GetKeyHeight(d : DependencyObject) = d.GetValue Chart.KeyHeightProperty :?> float
    static member SetKeyHeight(d : DependencyObject, value : float) = d.SetValue(Chart.KeyHeightProperty, value)
    static member val KeyHeightProperty =
        Dp.rega<float, Chart> "KeyHeight"
            (Dp.Meta(12.0, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetMinKey(d : DependencyObject) = d.GetValue Chart.MinKeyProperty :?> int
    static member SetMinKey(d : DependencyObject, value : int) = d.SetValue(Chart.MinKeyProperty, value)
    static member val MinKeyProperty =
        Dp.rega<int, Chart> "MinKey"
            (Dp.Meta(48, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetMaxKey(d : DependencyObject) = d.GetValue Chart.MaxKeyProperty :?> int
    static member SetMaxKey(d : DependencyObject, value : int) = d.SetValue(Chart.MaxKeyProperty, value)
    static member val MaxKeyProperty =
        Dp.rega<int, Chart> "MaxKey"
            (Dp.Meta(72, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetQuarterLength(d : DependencyObject) = d.GetValue Chart.QuarterLengthProperty :?> float
    static member SetQuarterLength(d : DependencyObject, value : float) = d.SetValue(Chart.QuarterLengthProperty, value)
    static member val QuarterLengthProperty =
        Dp.rega<float, Chart> "QuarterLength"
            (Dp.Meta(100.0, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetTimeSignature(d : DependencyObject) = d.GetValue Chart.TimeSignatureProperty :?> TimeSignature
    static member SetTimeSignature(d : DependencyObject, value : TimeSignature) = d.SetValue(Chart.TimeSignatureProperty, value)
    static member val TimeSignatureProperty =
        Dp.rega<TimeSignature, Chart> "TimeSignature"
            (Dp.Meta(timeSignature 4 4, Dp.MetaFlags.AffectsRender ||| Dp.MetaFlags.Inherits))

    static member GetHOffset(d : DependencyObject) = d.GetValue Chart.HOffsetProperty :?> float
    static member SetHOffset(d : DependencyObject, value : float) = d.SetValue(Chart.HOffsetProperty, value)
    static member val HOffsetProperty =
        Dp.rega<float, Chart> "HOffset"
            (Dp.Meta(0.0, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.AffectsRender))

    static member GetVOffset(d : DependencyObject) = d.GetValue Chart.VOffsetProperty :?> float
    static member SetVOffset(d : DependencyObject, value : float) = d.SetValue(Chart.VOffsetProperty, value)
    static member val VOffsetProperty =
        Dp.rega<float, Chart> "VOffset"
            (Dp.Meta(0.0, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.AffectsRender))


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
        let vOffset = Chart.GetVOffset x
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let keyHeight = Chart.GetKeyHeight x
        let minKey = Chart.GetMinKey x
        let maxKey = Chart.GetMaxKey x

        let whiteKeyWidth = actualWidth
        let blackKeyWidth = whiteKeyWidth * x.BlackKeyLengthRatio |> clamp 0.0 whiteKeyWidth
        let cornerRadius = 2.0 |> min(half keyHeight) |> min(half blackKeyWidth)

        let botPitch = minKey + int vOffset
        let topPitch = min maxKey (minKey + int(vOffset + actualHeight / keyHeight))
        for pitch in botPitch .. topPitch do
            if not(Midi.isBlackKey pitch) then
                let keyOffset = keyOffsetLookup.[pitch % 12] / 12.0
                let y = actualHeight - (float(pitch - minKey + 1) - vOffset - keyOffset) * keyHeight
                let height = keyHeightLookup.[pitch % 12] / 12.0 * keyHeight
                let x = if isNull whiteKeyPen then 0.0 else half whiteKeyPen.Thickness
                let width = max 0.0 (whiteKeyWidth - x * 2.0)
                dc.DrawRoundedRectangle(whiteKeyFill, whiteKeyPen, Rect(x, y, width, height), cornerRadius, cornerRadius)

        for pitch in botPitch .. topPitch do
            if Midi.isBlackKey pitch then
                let y = actualHeight - (float(pitch - minKey + 1) - vOffset) * keyHeight
                let height = keyHeight
                let x = if isNull blackKeyPen then 0.0 else half blackKeyPen.Thickness
                let width = max 0.0 (blackKeyWidth - x * 2.0)
                dc.DrawRoundedRectangle(blackKeyFill, blackKeyPen, Rect(0.0, y, width, height), cornerRadius, cornerRadius)

        let typeface =
            Typeface(
                TextBlock.GetFontFamily x,
                TextBlock.GetFontStyle x,
                TextBlock.GetFontWeight x,
                TextBlock.GetFontStretch x)
        for pitch in botPitch .. topPitch do
            if pitch % 12 = 0 then
                let ft =
                    FormattedText(
                        sprintf "C%d" (pitch / 12 - 1),
                        Globalization.CultureInfo.CurrentUICulture,
                        TextBlock.GetFlowDirection x,
                        typeface,
                        TextBlock.GetFontSize x,
                        TextBlock.GetForeground x,
                        (let dpi = VisualTreeHelper.GetDpi x in dpi.PixelsPerDip))
                let x = whiteKeyWidth - 2.0 - ft.Width
                let y = actualHeight - (float(pitch - minKey + 1) - vOffset) * keyHeight + half(keyHeight - ft.Height)
                dc.DrawText(ft, Point(x, y))


module TimeScale =
    let minMajorHop = 80.0
    let minMinorHop = 25.0

    let findHop(timeSig : TimeSignature) quarterLength minHop =
        seq {
            yield 1L
            yield 5L
            yield! Seq.initInfinite(fun i -> 15L <<< i)
                |> Seq.takeWhile(fun length -> length < timeSig.PulsesPerBeat)
            yield timeSig.PulsesPerBeat
            yield! Seq.initInfinite(fun i -> timeSig.PulsesPerMeasure <<< i) }
        |> Seq.find(fun hop ->
            float hop / float Midi.ppqn * quarterLength >= minHop)

type Ruler() =
    inherit FrameworkElement()

    static let majorTickHeight = 6.0
    static let minorTickHeight = 4.0

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
        let hOffset = Chart.GetHOffset x
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterLength = Chart.GetQuarterLength x
        let timeSig = Chart.GetTimeSignature x

        let tickPen = Pen(SolidColorBrush((0xFF000000u).AsColor()), 1.0) |>! freeze

        let minTick = int64 hOffset
        let maxTick = int64(hOffset + actualWidth / quarterLength * float Midi.ppqn)
        let typeface =
            Typeface(
                TextBlock.GetFontFamily x,
                TextBlock.GetFontStyle x,
                TextBlock.GetFontWeight x,
                TextBlock.GetFontStretch x)
        let makeText text =
            FormattedText(
                text,
                Globalization.CultureInfo.CurrentUICulture,
                TextBlock.GetFlowDirection x,
                typeface,
                TextBlock.GetFontSize x,
                TextBlock.GetForeground x,
                (let dpi = VisualTreeHelper.GetDpi x in dpi.PixelsPerDip))

        let majorHop = TimeScale.findHop timeSig quarterLength TimeScale.minMajorHop
        let minorHop = TimeScale.findHop timeSig quarterLength TimeScale.minMinorHop

        for currTick in minTick / minorHop * minorHop .. minorHop .. maxTick do
            let isMajor = currTick % majorHop = 0L
            let x = (float currTick - hOffset) / float Midi.ppqn * quarterLength
            let height = if isMajor then majorTickHeight else minorTickHeight
            dc.DrawLine(tickPen, Point(x, actualHeight - height), Point(x, actualHeight))

            if isMajor then
                let textStr =
                    if majorHop % timeSig.PulsesPerMeasure = 0L then MidiTime.formatMeasures timeSig currTick
                    elif majorHop % timeSig.PulsesPerBeat = 0L then MidiTime.formatMeasureBeats timeSig currTick
                    else MidiTime.formatFull timeSig currTick
                let ft = makeText textStr
                let halfTextWidth = half ft.Width
                if x - halfTextWidth >= 0.0 && x + halfTextWidth <= actualWidth then
                    dc.DrawText(ft, new Point(x - halfTextWidth, 0.0))


type ChartGrid() =
    inherit BasicPanel()

    override x.OnRender dc =
        let hOffset = Chart.GetHOffset x
        let vOffset = Chart.GetVOffset x
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterLength = Chart.GetQuarterLength x
        let keyHeight = Chart.GetKeyHeight x
        let timeSig = Chart.GetTimeSignature x
        let minKey = Chart.GetMinKey x
        let maxKey = Chart.GetMaxKey x

        let majorTickPen = Pen(SolidColorBrush((0x30000000u).AsColor()), 0.5) |>! freeze
        let minorTickPen = Pen(SolidColorBrush((0x20000000u).AsColor()), 0.5) |>! freeze
        let octavePen = Pen(SolidColorBrush((0x20000000u).AsColor()), 0.5) |>! freeze
        let blackKeyFill = SolidColorBrush((0x10000000u).AsColor()) |>! freeze

        // background
        dc.DrawRectangle(Brushes.Transparent, null, Rect(Size(actualWidth, actualHeight)))

        // time grids
        let minTick = int64 hOffset
        let maxTick = int64(hOffset + actualWidth / quarterLength * float Midi.ppqn)

        let majorHop = TimeScale.findHop timeSig quarterLength TimeScale.minMajorHop
        let minorHop = TimeScale.findHop timeSig quarterLength TimeScale.minMinorHop

        for currTick in minTick / minorHop * minorHop .. minorHop .. maxTick do
            let x = (float currTick - hOffset) / float Midi.ppqn * quarterLength
            let pen = if currTick % majorHop = 0L then majorTickPen else minorTickPen
            dc.DrawLine(pen, Point(x, 0.0), Point(x, actualHeight))

        // pitch grids
        let botPitch = minKey + int vOffset
        let topPitch = min maxKey (minKey + int(vOffset + actualHeight / keyHeight))
        for pitch in botPitch .. topPitch do
            match pitch % 12 with
            | 0 | 5 ->
                let y = actualHeight - (float(pitch - minKey) - vOffset) * keyHeight - half octavePen.Thickness
                dc.DrawLine(octavePen, Point(0.0, y), Point(actualWidth, y))
            | _ -> ()

            if pitch |> Midi.isBlackKey then
                let y = actualHeight - (float(pitch - minKey + 1) - vOffset) * keyHeight
                dc.DrawRectangle(blackKeyFill, null, Rect(0.0, y, actualWidth, keyHeight))


type ChartPanel() =
    inherit Panel()

    static member GetPitch(d : DependencyObject) = d.GetValue ChartPanel.PitchProperty :?> WpfOption<int>
    static member SetPitch(d : DependencyObject, value : WpfOption<int>) = d.SetValue(ChartPanel.PitchProperty, value)
    static member val PitchProperty =
        Dp.rega<WpfOption<int>, ChartPanel> "Pitch"
            (Dp.Meta(WpfSome 60, Dp.MetaFlags.AffectsParentArrange))

    static member GetSecondaryPitch(d : DependencyObject) = d.GetValue ChartPanel.SecondaryPitchProperty :?> WpfOption<int>
    static member SetSecondaryPitch(d : DependencyObject, value : WpfOption<int>) = d.SetValue(ChartPanel.SecondaryPitchProperty, value)
    static member val SecondaryPitchProperty =
        Dp.rega<WpfOption<int>, ChartPanel> "SecondaryPitch"
            (Dp.Meta(WpfNone, Dp.MetaFlags.AffectsParentArrange))

    static member GetOnTime(d : DependencyObject) = d.GetValue ChartPanel.OnTimeProperty :?> int64
    static member SetOnTime(d : DependencyObject, value : int64) = d.SetValue(ChartPanel.OnTimeProperty, value)
    static member val OnTimeProperty =
        Dp.rega<int64, ChartPanel> "OnTime"
            (Dp.Meta(0L, Dp.MetaFlags.AffectsParentArrange))

    static member GetDuration(d : DependencyObject) = d.GetValue ChartPanel.DurationProperty :?> int64
    static member SetDuration(d : DependencyObject, value : int64) = d.SetValue(ChartPanel.DurationProperty, value)
    static member val DurationProperty =
        Dp.rega<int64, ChartPanel> "Duration"
            (Dp.Meta(480L, Dp.MetaFlags.AffectsParentArrange))

    static member GetStretchChildren(d : DependencyObject) = d.GetValue ChartPanel.StretchChildrenProperty :?> bool
    static member SetStretchChildren(d : DependencyObject, value : bool) = d.SetValue(ChartPanel.StretchChildrenProperty, value)
    static member val StretchChildrenProperty =
        Dp.rega<bool, ChartPanel> "StretchChildren"
            (Dp.Meta(false, Dp.MetaFlags.AffectsMeasure ||| Dp.MetaFlags.Inherits))

    override x.MeasureOverride s =
        let quarterLength = Chart.GetQuarterLength x
        let keyHeight = Chart.GetKeyHeight x
        let minKey = Chart.GetMinKey x
        let maxKey = Chart.GetMaxKey x

        let stretchChildren = ChartPanel.GetStretchChildren x
        let getValidPitch pitchOp =
            match pitchOp with
            | WpfSome pitch when pitch >= minKey && pitch <= maxKey -> Some pitch
            | _ -> None

        for child in x.InternalChildren do
            let childWidth =
                let childDuration = ChartPanel.GetDuration child
                float childDuration / float Midi.ppqn * quarterLength
            let childHOp =
                if stretchChildren then
                    Some s.Height
                else
                    let p1 = ChartPanel.GetPitch child
                    let p2 = ChartPanel.GetSecondaryPitch child
                    match getValidPitch p1, getValidPitch p2 with
                    | Some p1, Some p2 -> Some(float(abs(p1 - p2) + 1) * keyHeight)
                    | Some p, _ | _, Some p -> Some keyHeight
                    | _, _ -> None
            match childHOp with
            | Some childHeight ->
                child.Measure(Size(childWidth, childHeight))
            | _ -> ()

        let height =
            if stretchChildren then
                x.InternalChildren
                |> Seq.cast<UIElement>
                |> Seq.map(fun child -> let childDesiredSize = child.DesiredSize in childDesiredSize.Height)
                |> Seq.appendItem 0.0
                |> Seq.max
            else
                zeroIfInf s.Height
        Size(zeroIfInf s.Width, height)

    override x.ArrangeOverride s =
        let hOffset = Chart.GetHOffset x
        let vOffset = Chart.GetVOffset x
        let actualWidth = s.Width
        let actualHeight = s.Height
        let quarterLength = Chart.GetQuarterLength x
        let keyHeight = Chart.GetKeyHeight x
        let minKey = Chart.GetMinKey x
        let maxKey = Chart.GetMaxKey x

        let minTick = int64 hOffset
        let maxTick = int64(hOffset + actualWidth / quarterLength * float Midi.ppqn)
        let botPitch = minKey + int vOffset
        let topPitch = min maxKey (minKey + int(vOffset + actualHeight / keyHeight))

        let stretchChildren = ChartPanel.GetStretchChildren x
        let getValidPitch pitchOp =
            match pitchOp with
            | WpfSome pitch when pitch >= minKey && pitch <= maxKey -> Some pitch
            | _ -> None
        let isVisiblePitch pitch =
            pitch >= botPitch && pitch <= topPitch

        for child in x.InternalChildren do
            let childXWOp =
                let childOnTime = ChartPanel.GetOnTime child
                let childDuration = ChartPanel.GetDuration child
                if childOnTime <= maxTick && childOnTime + childDuration >= minTick then
                    let childX = (float childOnTime - hOffset) / float Midi.ppqn * quarterLength
                    let childWidth = float childDuration / float Midi.ppqn * quarterLength
                    Some(childX, childWidth)
                else
                    None
            let childYHOp =
                if stretchChildren then
                    Some(0.0, actualHeight)
                else
                    let p1 = ChartPanel.GetPitch child
                    let p2 = ChartPanel.GetSecondaryPitch child
                    match getValidPitch p1, getValidPitch p2 with
                    | Some p1, Some p2 when isVisiblePitch p1 || isVisiblePitch p2 ->
                        let childY = actualHeight - (float(max p1 p2 - minKey + 1) - vOffset) * keyHeight
                        let childHeight = float(abs(p1 - p2) + 1) * keyHeight
                        Some(childY, childHeight)
                    | Some p, _ | _, Some p when isVisiblePitch p ->
                        let childY = actualHeight - (float(p - minKey + 1) - vOffset) * keyHeight
                        Some(childY, keyHeight)
                    | _, _ ->
                        None
            match childXWOp, childYHOp with
            | Some(childX, childWidth), Some(childY, childHeight) ->
                child.Visibility <- Visibility.Visible
                child.Arrange(Rect(childX, childY, childWidth, childHeight))
            | _, _ ->
                child.Visibility <- Visibility.Collapsed

        s


module ChartConverters =
    let hScrollSpanConverter = ValueConverter.CreateMulti(fun vs p ->       // unit in midi ticks
        match vs with
        | [| quarterLength; chartWidth |] ->
            let quarterLength = Convert.ToDouble quarterLength
            let chartWidth = Convert.ToDouble chartWidth
            let scale = if isNull p then 1.0 else Convert.ToDouble p
            chartWidth * scale / quarterLength * float Midi.ppqn
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

    let vScrollExtentConverter = ValueConverter.CreateMulti(fun vs ->       // unit in key indices
        match vs with
        | [| minKey; maxKey; keyHeight; chartHeight |] ->
            let minKey = Convert.ToInt32 minKey
            let maxKey = Convert.ToInt32 maxKey
            let keyHeight = Convert.ToDouble keyHeight
            let chartHeight = Convert.ToDouble chartHeight
            let extent = float(abs(maxKey - minKey) + 1) - chartHeight / keyHeight
            max extent 0.0
        | _ ->
            raise(ArgumentException()))

    let hZoomToQuarterLength =
        ValueConverter.Create(
            (fun sliderValue -> exp(sliderValue * log 2.0) * 240.0),
            (fun quarterLength -> log(quarterLength / 240.0) / log 2.0))

    let vZoomToQuarterLength =
        ValueConverter.Create(
            (fun sliderValue -> exp(sliderValue * log 2.0) * 12.0),
            (fun quarterLength -> log(quarterLength / 12.0) / log 2.0))


