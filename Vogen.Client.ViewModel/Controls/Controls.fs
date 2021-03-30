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
open Vogen.Client.Model
open type Doaz.Reactive.ColorConv


type NoteChartEditBase() =
    inherit FrameworkElement()

    member val private MouseDownButton = None with get, set

    override x.OnMouseDown e =
        match x.MouseDownButton with
        | Some _ -> ()
        | None ->
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

    abstract CanScrollH : bool
    abstract CanScrollV : bool
    default x.CanScrollH = true
    default x.CanScrollV = true

    member x.TimeSignature
        with get() = x.GetValue NoteChartEditBase.TimeSignatureProperty :?> TimeSignature
        and set(v : TimeSignature) = x.SetValue(NoteChartEditBase.TimeSignatureProperty, box v)
    static member val TimeSignatureProperty =
        Dp.reg<TimeSignature, NoteChartEditBase> "TimeSignature"
            (Dp.Meta(timeSignature 4 4, Dp.MetaFlags.AffectsRender))

    member x.Quantization
        with get() = x.GetValue NoteChartEditBase.QuantizationProperty :?> int64
        and set(v : int64) = x.SetValue(NoteChartEditBase.QuantizationProperty, box v)
    static member val QuantizationProperty =
        Dp.reg<int64, NoteChartEditBase> "Quantization"
            (Dp.Meta(Midi.ppqn, Dp.MetaFlags.AffectsRender))

    member x.QuarterWidth
        with get() = x.GetValue NoteChartEditBase.QuarterWidthProperty :?> float
        and set(v : float) = x.SetValue(NoteChartEditBase.QuarterWidthProperty, box v)
    static member DefaultQuarterWidth = 100.0
    static member val QuarterWidthProperty =
        Dp.reg<float, NoteChartEditBase> "QuarterWidth"
            (Dp.Meta(NoteChartEditBase.DefaultQuarterWidth, Dp.MetaFlags.AffectsRender))

    member x.KeyHeight
        with get() = x.GetValue NoteChartEditBase.KeyHeightProperty :?> float
        and set(v : float) = x.SetValue(NoteChartEditBase.KeyHeightProperty, box v)
    static member DefaultKeyHeight = 12.0
    static member val KeyHeightProperty =
        Dp.reg<float, NoteChartEditBase> "KeyHeight"
            (Dp.Meta(NoteChartEditBase.DefaultKeyHeight, Dp.MetaFlags.AffectsRender))

    member x.MinKey
        with get() = x.GetValue NoteChartEditBase.MinKeyProperty :?> int
        and set(v : int) = x.SetValue(NoteChartEditBase.MinKeyProperty, box v)
    static member val MinKeyProperty =
        Dp.reg<int, NoteChartEditBase> "MinKey"
            (Dp.Meta(33, Dp.MetaFlags.AffectsRender, (fun x _ ->
                x.CoerceValue NoteChartEditBase.VOffsetAnimatedProperty)))

    member x.MaxKey
        with get() = x.GetValue NoteChartEditBase.MaxKeyProperty :?> int
        and set(v : int) = x.SetValue(NoteChartEditBase.MaxKeyProperty, box v)
    static member val MaxKeyProperty =
        Dp.reg<int, NoteChartEditBase> "MaxKey"
            (Dp.Meta(93, Dp.MetaFlags.AffectsRender, (fun x _ ->
                x.CoerceValue NoteChartEditBase.VOffsetAnimatedProperty)))

    member x.HOffsetAnimated
        with get() = x.GetValue NoteChartEditBase.HOffsetAnimatedProperty :?> float
        and set(v : float) = x.SetValue(NoteChartEditBase.HOffsetAnimatedProperty, box v)
    static member CoerceHOffsetAnimated x v = max 0.0 v
    static member val HOffsetAnimatedProperty =
        Dp.reg<float, NoteChartEditBase> "HOffsetAnimated"
            (Dp.Meta(0.0, Dp.MetaFlags.AffectsRender, (fun _ _ -> ()), NoteChartEditBase.CoerceHOffsetAnimated))

    member x.VOffsetAnimated
        with get() = x.GetValue NoteChartEditBase.VOffsetAnimatedProperty :?> float
        and set(v : float) = x.SetValue(NoteChartEditBase.VOffsetAnimatedProperty, box v)
    static member CoerceVOffsetAnimated x v = v |> min(float x.MaxKey) |> max(float x.MinKey)
    static member val VOffsetAnimatedProperty =
        Dp.reg<float, NoteChartEditBase> "VOffsetAnimated"
            (Dp.Meta(60.0, Dp.MetaFlags.AffectsRender, (fun _ _ -> ()), NoteChartEditBase.CoerceVOffsetAnimated))

    member x.CursorPosition
        with get() = x.GetValue NoteChartEditBase.CursorPositionProperty :?> int64
        and set(v : int64) = x.SetValue(NoteChartEditBase.CursorPositionProperty, box v)
    member val OnCursorPositionChangedEvent : Event<_> = Event<_>()
    [<CLIEvent>] member x.OnCursorPositionChanged = x.OnCursorPositionChangedEvent.Publish
    static member CoerceCursorPosition x v = max 0L v
    static member val CursorPositionProperty =
        Dp.reg<int64, NoteChartEditBase> "CursorPosition"
            (Dp.Meta(0L,
                (fun (x : NoteChartEditBase)(oldValue, newValue) ->
                    x.OnCursorPositionChangedEvent.Trigger((oldValue, NoteChartEditBase.CoerceCursorPosition x newValue))),
                NoteChartEditBase.CoerceCursorPosition))

    member x.IsPlaying
        with get() = x.GetValue NoteChartEditBase.IsPlayingProperty :?> bool
        and set(v : bool) = x.SetValue(NoteChartEditBase.IsPlayingProperty, box v)
    member val OnIsPlayingChangedEvent : Event<_> = Event<_>()
    [<CLIEvent>] member x.OnIsPlayingChanged = x.OnIsPlayingChangedEvent.Publish
    static member val IsPlayingProperty =
        Dp.reg<bool, NoteChartEditBase> "IsPlaying"
            (Dp.Meta(false, Dp.MetaFlags.AffectsRender,
                (fun (x : NoteChartEditBase)(oldValue, newValue) -> x.OnIsPlayingChangedEvent.Trigger(oldValue, newValue))))

    member x.Composition
        with get() = x.GetValue NoteChartEditBase.CompositionProperty :?> Composition
        and set(v : Composition) = x.SetValue(NoteChartEditBase.CompositionProperty, box v)
    member val OnCompositionChangedEvent : Event<_> = Event<_>()
    [<CLIEvent>] member x.OnCompositionChanged = x.OnCompositionChangedEvent.Publish
    static member val CompositionProperty =
        Dp.reg<Composition, NoteChartEditBase> "Composition"
            (Dp.Meta(Composition.Empty, Dp.MetaFlags.AffectsRender,
                (fun (x : NoteChartEditBase)(oldValue, newValue) -> x.OnCompositionChangedEvent.Trigger(oldValue, newValue))))

type SideKeyboard() =
    inherit NoteChartEditBase()

    static let whiteKeyFill = Brushes.White
    static let whiteKeyPen : Pen = Pen(Brushes.Black, 0.6) |>! freeze
    static let blackKeyFill = Brushes.Black
    static let blackKeyPen : Pen = null

    static let defaultKeyHeight = 12.0
    static let keyOffsetLookup = [| -8; 0; -4; 0; 0; -9; 0; -6; 0; -3; 0; 0 |] |> Array.map float
    static let keyHeightLookup = [| 20; 12; 20; 12; 20; 21; 12; 21; 12; 21; 12; 21 |] |> Array.map float

    member x.BlackKeyLengthRatio
        with get() = x.GetValue SideKeyboard.BlackKeyLengthRatioProperty :?> float
        and set(v : float) = x.SetValue(SideKeyboard.BlackKeyLengthRatioProperty, box v)
    static member val BlackKeyLengthRatioProperty =
        Dp.reg<float, SideKeyboard> "BlackKeyLengthRatio"
            (Dp.Meta(0.6, Dp.MetaFlags.AffectsRender))

    override x.CanScrollH = false

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let keyHeight = x.KeyHeight
        let minKey = x.MinKey
        let maxKey = x.MaxKey
        let vOffset = x.VOffsetAnimated

        let whiteKeyWidth = actualWidth
        let blackKeyWidth = whiteKeyWidth * x.BlackKeyLengthRatio |> clamp 0.0 whiteKeyWidth
        let cornerRadius = 2.0 |> min(half keyHeight) |> min(half blackKeyWidth)

        let botPitch = pixelToPitch keyHeight actualHeight vOffset actualHeight |> int |> max minKey
        let topPitch = pixelToPitch keyHeight actualHeight vOffset 0.0 |> ceil |> int |> min maxKey

        dc.PushClip(RectangleGeometry(Rect(Size(actualWidth, actualHeight))))

        // background
        dc.DrawRectangle(Brushes.Transparent, null, Rect(Size(actualWidth, actualHeight)))

        // white keys
        for pitch in botPitch .. topPitch do
            if not(Midi.isBlackKey pitch) then
                let keyOffset = keyOffsetLookup.[pitch % 12] / defaultKeyHeight
                let y = pitchToPixel keyHeight actualHeight vOffset (float pitch + 0.5 - keyOffset)
                let height = keyHeightLookup.[pitch % 12] / defaultKeyHeight * keyHeight
                let x = if isNull whiteKeyPen then 0.0 else half whiteKeyPen.Thickness
                let width = max 0.0 (whiteKeyWidth - x * 2.0)
                dc.DrawRoundedRectangle(whiteKeyFill, whiteKeyPen, Rect(x, y, width, height), cornerRadius, cornerRadius)

        // black keys
        for pitch in botPitch .. topPitch do
            if Midi.isBlackKey pitch then
                let y = pitchToPixel keyHeight actualHeight vOffset (float pitch + 0.5)
                let height = keyHeight
                let x = if isNull blackKeyPen then 0.0 else half blackKeyPen.Thickness
                let width = max 0.0 (blackKeyWidth - x * 2.0)
                dc.DrawRoundedRectangle(blackKeyFill, blackKeyPen, Rect(0.0, y, width, height), cornerRadius, cornerRadius)

        // text labels
        for pitch in botPitch .. topPitch do
            if pitch % 12 = 0 then
                let ft = x |> makeFormattedText(sprintf "C%d" (pitch / 12 - 1))
                let x = whiteKeyWidth - 2.0 - ft.Width
                let y = pitchToPixel keyHeight actualHeight vOffset (float pitch + 0.5) + half(keyHeight - ft.Height)
                dc.DrawText(ft, Point(x, y))

type RulerGrid() =
    inherit NoteChartEditBase()

    static let majorTickHeight = 6.0
    static let minorTickHeight = 4.0

    static let tickPen = Pen(SolidColorBrush(rgb 0), 1.0) |>! freeze

    static member Quantizations = [|
        1920L; 960L; 480L; 240L; 120L; 60L; 30L; 15L; 1L
        320L; 160L; 80L; 40L; 20L |]

    static member QuantizationsSorted = Array.sort RulerGrid.Quantizations

    static member MinMajorTickHop = 70.0    // in screen pixels
    static member MinMinorTickHop = 25.0

    static member FindTickHop(timeSig : TimeSignature) quarterWidth minTickHop =
        seq {
            yield 1L
            yield 5L
            yield! Seq.initInfinite(fun i -> 15L <<< i)
                |> Seq.takeWhile(fun length -> length < timeSig.PulsesPerBeat)
            yield timeSig.PulsesPerBeat
            yield! Seq.initInfinite(fun i -> timeSig.PulsesPerMeasure >>> (i + 1))
                |> Seq.takeWhile(fun length -> length > timeSig.PulsesPerBeat && length % timeSig.PulsesPerBeat = 0L)
                |> Seq.rev
            yield! Seq.initInfinite(fun i -> timeSig.PulsesPerMeasure <<< i) }
        |> Seq.find(fun hop ->
            pulseToPixel quarterWidth 0.0 (float hop) >= minTickHop)
    
    override x.CanScrollV = false

    //override x.MeasureOverride s =
    //    let fontFamily = TextBlock.GetFontFamily x
    //    let fontSize = TextBlock.GetFontSize x
    //    let height = fontSize * fontFamily.LineSpacing + max majorTickHeight minorTickHeight
    //    Size(zeroIfInf s.Width, height)

    //override x.ArrangeOverride s =
    //    let fontFamily = TextBlock.GetFontFamily x
    //    let fontSize = TextBlock.GetFontSize x
    //    let height = fontSize * fontFamily.LineSpacing + max majorTickHeight minorTickHeight
    //    Size(s.Width, height)

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let timeSig = x.TimeSignature
        let quarterWidth = x.QuarterWidth
        let hOffset = x.HOffsetAnimated

        let minPulse = int64(pixelToPulse quarterWidth hOffset 0.0)
        let maxPulse = int64(pixelToPulse quarterWidth hOffset actualWidth)

        let majorHop = RulerGrid.FindTickHop timeSig quarterWidth RulerGrid.MinMajorTickHop
        let minorHop = RulerGrid.FindTickHop timeSig quarterWidth RulerGrid.MinMinorTickHop

        dc.PushClip(RectangleGeometry(Rect(Size(actualWidth, actualHeight))))

        // background
        dc.DrawRectangle(Brushes.Transparent, null, Rect(Size(actualWidth, actualHeight)))

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
                    if majorHop % timeSig.PulsesPerMeasure = 0L then Midi.formatMeasures timeSig currPulse
                    elif majorHop % timeSig.PulsesPerBeat = 0L then Midi.formatMeasureBeats timeSig currPulse
                    else Midi.formatFull timeSig currPulse
                let ft = x |> makeFormattedText textStr
                let halfTextWidth = half ft.Width
                if xPos - halfTextWidth >= 0.0 && xPos + halfTextWidth <= actualWidth then
                    dc.DrawText(ft, new Point(xPos - halfTextWidth, actualHeight - ft.Height - majorTickHeight))

type ChartEditor() as x =
    inherit NoteChartEditBase()

    static let makeDiamondGeometry radius (center : Point) =
        let geometry = StreamGeometry()
        do  use ctx = geometry.Open()
            ctx.BeginFigure(
                Point(center.X, center.Y - radius), true, true)
            ctx.PolyLineTo([|
                Point(center.X + radius, center.Y)
                Point(center.X, center.Y + radius)
                Point(center.X - radius, center.Y) |], true, false)
        geometry |>! freeze

    static let makeWaveformGeometry samples pixelToSampleIndex x0 x1 yMid xRes hOffset hScale =
        let waveformUpperContourPoints = List()
        let waveformLowerContourPoints = List()
        for xi0 in x0 .. xRes .. x1 do
            let si0 = pixelToSampleIndex xi0 |> max 0
            let si1 = pixelToSampleIndex(xi0 + xRes) |> min(samples |> Array.length)
            let mutable sMax, sMin = samples.[si0], samples.[si0]
            for si in si0 + 1 .. si1 - 1 do
                sMax <- max sMax samples.[si]
                sMin <- min sMin samples.[si]
            waveformUpperContourPoints.Add(Point(xi0, yMid - float sMax * hScale - hOffset))
            waveformLowerContourPoints.Add(Point(xi0, yMid - float sMin * hScale + hOffset))
        waveformLowerContourPoints.Reverse()
        pointsToGeometry true (Seq.append waveformUpperContourPoints waveformLowerContourPoints)

    let mutable uttToCharsDict = ImmutableDictionary.Empty
    let updateUttToCharsDict comp =
        uttToCharsDict <-
            (comp : Composition).Utts
            |> Seq.map(fun utt ->
                let chars =
                    utt.Notes
                    |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen)
                    |> Seq.map(fun notes -> {| Ch = notes.[0].Lyric; Notes = notes |})
                    |> ImmutableArray.CreateRange
                KeyValuePair(utt, chars))
            |> ImmutableDictionary.CreateRange

    let mutable activeNotes = HashSet()
    let updateActiveNotes playbackPos isPlaying comp =
        let newActiveNotes =
            (comp : Composition).Utts
            |> Seq.collect(fun utt -> utt.Notes)
            |> Seq.filter(
                if isPlaying then (fun note -> note.On <= playbackPos && note.Off > playbackPos)
                else (fun note -> note.IsSelected))
            |> HashSet
        if not(activeNotes.SetEquals newActiveNotes) then
            activeNotes <- newActiveNotes
            x.InvalidateVisual()

    do x.OnCursorPositionChanged.Add <| fun (_, playbackPos) ->
        updateActiveNotes playbackPos x.IsPlaying x.Composition

    do x.OnIsPlayingChanged.Add <| fun (_, isPlaying) ->
        updateActiveNotes x.CursorPosition isPlaying x.Composition

    do x.OnCompositionChanged.Add <| fun (_, comp) ->
        updateUttToCharsDict comp
        updateActiveNotes x.CursorPosition x.IsPlaying comp

    static let minGridScreenHop = 5.0
    static let majorGridPen = Pen(SolidColorBrush(aRgb 0x80 0), 0.5) |>! freeze
    static let minorGridPen = Pen(SolidColorBrush(aRgb 0x40 0), 0.5) |>! freeze

    static let noteBaseColor = rgb 0xFF8040
    static let hyphBaseColor = rgb 0xFF6080

    static let noteBrush = SolidColorBrush(lerpColor noteBaseColor (rgb -1) 0.6) |>! freeze
    static let hyphBrush = SolidColorBrush(lerpColor hyphBaseColor (rgb -1) 0.6) |>! freeze
    static let noteBrushInvalid = SolidColorBrush(rgb -1) |>! freeze
    static let notePen = Pen(SolidColorBrush(lerpColor noteBaseColor (rgb -1) 0.2), 1.0) |>! freeze
    static let hyphPen = Pen(SolidColorBrush(lerpColor hyphBaseColor (rgb -1) 0.2), 1.0) |>! freeze
    static let restPen = Pen(SolidColorBrush(lerpColor noteBaseColor (rgb -1) 0.2), 1.0, DashStyle = DashStyle([| 2.0; 4.0 |], 0.0)) |>! freeze

    static let waveBrush = SolidColorBrush(aRgb 0x20 0) |>! freeze
    static let wavePen = Pen(SolidColorBrush(aFRgb 0.25 -1), 1.0) |>! freeze

    static let selPitchBrush = SolidColorBrush(aFRgb 0.1 0x000080) |>! freeze
    static let selNoteBrush = SolidColorBrush(aRgb 0x40 0x000080) |>! freeze
    static let selNotePen = Pen(SolidColorBrush(aRgb 0x80 0x000080), 2.0) |>! freeze

    static let f0Pen = Pen(SolidColorBrush(aRgb 0x40 0x800000), 2.0) |>! freeze

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let timeSig = x.TimeSignature
        let quantization = x.Quantization
        let quarterWidth = x.QuarterWidth
        let keyHeight = x.KeyHeight
        let minKey = x.MinKey
        let maxKey = x.MaxKey
        let hOffset = x.HOffsetAnimated
        let vOffset = x.VOffsetAnimated
        let playbackPos = x.CursorPosition
        let comp = x.Composition

        let minPulse = int64(pixelToPulse quarterWidth hOffset 0.0)
        let maxPulse = int64(pixelToPulse quarterWidth hOffset actualWidth |> ceil)

        let botPitch = pixelToPitch keyHeight actualHeight vOffset actualHeight |> int |> max minKey
        let topPitch = pixelToPitch keyHeight actualHeight vOffset 0.0 |> ceil |> int |> min maxKey

        // background
        dc.PushClip(RectangleGeometry(Rect(Size(actualWidth, actualHeight))))
        dc.DrawRectangle(Brushes.Transparent, null, Rect(Size(actualWidth, actualHeight)))

        // time grids
        let measureHop =
            Seq.initInfinite(fun i -> timeSig.PulsesPerMeasure <<< i)
            |> Seq.find(fun pulses -> pulseToPixel quarterWidth 0.0 (float pulses) >= minGridScreenHop)
        let beatHop =
            Seq.initInfinite(fun i -> timeSig.PulsesPerBeat <<< i)
            |> Seq.skipWhile(fun pulses -> pulses < quantization)
            |> Seq.takeWhile(fun pulses -> pulses < timeSig.PulsesPerMeasure)
            |> Seq.tryFind(fun pulses -> pulseToPixel quarterWidth 0.0 (float pulses) >= minGridScreenHop)
            |> Option.defaultValue measureHop
        let gridHop =
            if quantization = 1L then beatHop else
            Seq.initInfinite(fun i -> quantization <<< i)
            |> Seq.takeWhile(fun pulses -> pulses < timeSig.PulsesPerBeat)
            |> Seq.tryFind(fun pulses -> pulseToPixel quarterWidth 0.0 (float pulses) >= minGridScreenHop)
            |> Option.defaultValue beatHop

        for measurePulse in minPulse / measureHop * measureHop .. measureHop .. maxPulse do
            if measurePulse > minPulse then
                let x0 = pulseToPixel quarterWidth hOffset (float measurePulse)
                dc.DrawLine(majorGridPen, Point(x0, 0.0), Point(x0, actualHeight))

            for beatPulse in measurePulse .. beatHop .. min maxPulse (measurePulse + timeSig.PulsesPerMeasure - 1L) do
                if beatPulse > max minPulse measurePulse then
                    let x0 = pulseToPixel quarterWidth hOffset (float beatPulse)
                    for pitch in botPitch .. topPitch do
                        let y = pitchToPixel keyHeight actualHeight vOffset (float pitch)
                        if pitch |> Midi.isBlackKey then
                            dc.DrawLine(majorGridPen, Point(x0, y - (half keyHeight + 1.0)), Point(x0, y + (half keyHeight + 1.0)))

                for gridPulse in beatPulse + gridHop .. gridHop .. min maxPulse (beatPulse + timeSig.PulsesPerBeat - 1L) do
                    if gridPulse > max minPulse beatPulse then
                        let x0 = pulseToPixel quarterWidth hOffset (float gridPulse)
                        for pitch in botPitch .. topPitch do
                            let y = pitchToPixel keyHeight actualHeight vOffset (float pitch)
                            if pitch |> Midi.isBlackKey then
                                dc.DrawLine(minorGridPen, Point(x0, y - max 0.0 (half keyHeight - 1.0)), Point(x0, y + max 0.0 (half keyHeight - 1.0)))

        // pitch grids
        for pitch in botPitch .. topPitch do
            let y = pitchToPixel keyHeight actualHeight vOffset (float pitch)
            match pitch % 12 with
            | 0 ->
                let y = y + half keyHeight - half majorGridPen.Thickness
                dc.DrawLine(majorGridPen, Point(0.0, y), Point(actualWidth, y))
            | 5 ->
                let y = y + half keyHeight - half majorGridPen.Thickness
                dc.DrawLine(minorGridPen, Point(0.0, y), Point(actualWidth, y))
            | _ -> ()

        // active note pitches
        let activePitches = HashSet()
        for note in activeNotes do
            if note.Pitch >= botPitch && note.Pitch <= topPitch then
                activePitches.Add note.Pitch |> ignore

        for pitch in activePitches do
            let yMid = pitchToPixel keyHeight actualHeight vOffset (float pitch)
            dc.DrawRectangle(selPitchBrush, null, Rect(0.0, yMid - half keyHeight, actualWidth, keyHeight))

        // utt start decor
        for utt in comp.Utts do
            let uttSynthResult = comp.GetUttSynthResult utt
            if utt.On >= minPulse && utt.On <= maxPulse then
                let x0 = pulseToPixel quarterWidth hOffset (float utt.On)
                let yMid = pitchToPixel keyHeight actualHeight vOffset (float utt.Notes.[0].Pitch)
                dc.DrawLine(notePen, Point(x0 - 0.0, yMid - half keyHeight - 8.0), Point(x0 - 0.0, yMid + half keyHeight + 8.0))
                dc.DrawLine(notePen, Point(x0 - 3.0, yMid - half keyHeight - 8.0), Point(x0 - 3.0, yMid + half keyHeight + 8.0))
                dc.DrawLine(notePen, Point(x0 - 6.0, yMid - half keyHeight - 8.0), Point(x0 - 6.0, yMid + half keyHeight + 8.0))

                let ft =
                    let text = String.concat Environment.NewLine [|
                        $"Gloria"
                        $"({TextResources.getRomSchemeChar utt.RomScheme})" |]
                    x |> makeFormattedText text
                ft.TextAlignment <- TextAlignment.Right
                ft.SetFontSize(0.75 * TextBlock.GetFontSize x)
                ft.SetForegroundBrush notePen.Brush
                dc.DrawText(ft, Point(x0 - 8.0, yMid - half ft.Height))

        // notes
        let bpm0 = comp.Bpm0
        for utt in comp.Utts do
            let uttSynthResult = comp.GetUttSynthResult utt
            let chars = uttToCharsDict.[utt]

            //let charActiveNotes = HashSet()
            //for charIndex in 0 .. chars.Length - 1 do
            //    let ch = chars.[charIndex]
            //    let charActive = ch.Notes.[0].On <= playbackPos && ch.Notes.[^0].Off > playbackPos
            //    if charActive then
            //        charActiveNotes.UnionWith ch.Notes

            for noteIndex in 0 .. utt.Notes.Count - 1 do
                let note = utt.Notes.[noteIndex]
                if note.Off >= minPulse && note.On <= maxPulse && note.Pitch |> betweenInc botPitch topPitch then
                    let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                    let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                    let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)

                    // note connection
                    let hasNextNote = noteIndex < utt.Notes.Count - 1
                    if hasNextNote then
                        let nextNote = utt.Notes.[noteIndex + 1]
                        if nextNote.On <= maxPulse then
                            let n1x0 = pulseToPixel quarterWidth hOffset (float nextNote.On)
                            let n1yMid = pitchToPixel keyHeight actualHeight vOffset (float nextNote.Pitch)
                            let currNotePen = if nextNote.IsHyphen then hyphPen else notePen
                            let yMidMin = min yMid n1yMid
                            let yMidMax = max yMid n1yMid
                            dc.DrawLine(currNotePen, Point(n1x0, yMidMin - half keyHeight - 8.0), Point(n1x0, yMidMax + half keyHeight + 8.0))

                    // waveform
                    let n1x0 =
                        if not hasNextNote then x1 else
                            let nextNote = utt.Notes.[noteIndex + 1]
                            pulseToPixel quarterWidth hOffset (float nextNote.On)
                    if uttSynthResult.HasAudio then
                        let samples = uttSynthResult.AudioSamples
                        let sampleOffset = uttSynthResult.SampleOffset
                        let x0 =
                            if noteIndex > 0 then x0 |> max 0.0 else
                                sampleOffset
                                |> Audio.sampleToPulse bpm0 |> pulseToPixel quarterWidth hOffset |> max 0.0
                        let n1x0 =
                            if noteIndex < utt.Notes.Count - 1 then n1x0 |> min actualWidth else
                                sampleOffset + samples.Length
                                |> Audio.sampleToPulse bpm0 |> pulseToPixel quarterWidth hOffset |> min actualWidth
                        let pixelToSampleIndex x =
                            (x |> pixelToPulse quarterWidth hOffset |> Audio.pulseToSample bpm0) - sampleOffset
                        let xRes = 1.0
                        let waveformGeometry = makeWaveformGeometry samples pixelToSampleIndex x0 n1x0 yMid xRes 0.5 50.0
                        dc.DrawGeometry(waveBrush, null, waveformGeometry)

                    // note
                    let currNoteBrush = if note.IsHyphen then hyphBrush else noteBrush
                    let currNotePen = if note.IsHyphen then hyphPen else notePen
                    if hasNextNote then
                        let nextNote = utt.Notes.[noteIndex + 1]
                        if x1 > n1x0 then
                            dc.DrawRectangle(noteBrushInvalid, currNotePen, Rect(n1x0, yMid - half keyHeight, x1 - n1x0, keyHeight))
                        elif x1 < n1x0 then
                            if nextNote.IsHyphen then
                                dc.DrawLine(hyphPen, Point(x1, yMid), Point(n1x0, yMid))
                            else
                                dc.DrawLine(restPen, Point(x1, yMid), Point(n1x0, yMid))

                    let noteRect      = Rect(x0, yMid - half keyHeight, x1 - x0, keyHeight)
                    let noteValidRect = Rect(x0, yMid - half keyHeight, min x1 n1x0 - x0, keyHeight)
                    dc.DrawRectangle(currNoteBrush, currNotePen, noteValidRect)

                    // selection
                    if activeNotes.Contains note then
                        dc.DrawRectangle(selNoteBrush, selNotePen, Rect.Inflate(noteRect, 1.5, 1.5))

                    // text
                    if not note.IsHyphen then
                        let ft = x |> makeFormattedText note.Rom
                        ft.SetFontSize(1.0 * TextBlock.GetFontSize x)
                        dc.DrawText(ft, Point(x0, yMid - half ft.Height))

        // utt ph bounds
        //for utt in comp.Utts do
        //    let uttSynthResult = comp.GetUttSynthResult utt
        //    let uttTimeOffset = uttSynthResult.SampleOffset |> Audio.sampleToTime
        //    for charGrid in uttSynthResult.CharGrids do
        //        let pitch = charGrid.Pitch
        //        let y = pitchToPixel keyHeight actualHeight vOffset (float pitch)
        //        charGrid.Phs |> Array.iteri(fun i ph ->
        //            let x0 = pulseToPixel quarterWidth hOffset (Midi.ofTimeSpan bpm0 (uttTimeOffset + TimeTable.frameToTime(float ph.On)))
        //            let x1 = pulseToPixel quarterWidth hOffset (Midi.ofTimeSpan bpm0 (uttTimeOffset + TimeTable.frameToTime(float ph.Off)))
        //            if x1 >= 0.0 && x0 <= actualWidth then
        //                let ft = x |> makeFormattedText ph.Ph
        //                ft.SetForegroundBrush phPen.Brush
        //                ft.SetFontSize(0.75 * TextBlock.GetFontSize x)
        //                dc.DrawLine(phPen, Point(x0, y), Point(x1, y))
        //                let fillBrush = if i = 0 then phPen.Brush else Brushes.White :> _
        //                dc.DrawEllipse(fillBrush, phPen, Point(x0, y), 2.0, 2.0)
        //                dc.DrawText(ft, Point(x0, y)))

        // utt f0 samples
        for utt in comp.Utts do
            let uttSynthResult = comp.GetUttSynthResult utt
            let f0Samples = uttSynthResult.F0Samples
            let uttTimeOffset = uttSynthResult.SampleOffset |> Audio.sampleToTime
            let startSampleIndex =
                TimeTable.timeToFrame(Midi.toTimeSpan bpm0 (pixelToPulse quarterWidth hOffset 0.0) - uttTimeOffset) - 1.0
                |> int |> max 0
            let endSampleIndex =
                TimeTable.timeToFrame(Midi.toTimeSpan bpm0 (pixelToPulse quarterWidth hOffset actualWidth) - uttTimeOffset) + 1.0
                |> ceil |> int |> min f0Samples.Length
            let f0Geometry = drawGeometry <| fun sgc ->
                let mutable prevVuv = false
                for sampleIndex in startSampleIndex .. endSampleIndex - 1 do
                    let freq = f0Samples.[sampleIndex]
                    if freq > 0f then
                        let x = pulseToPixel quarterWidth hOffset (Midi.ofTimeSpan bpm0 (uttTimeOffset + TimeTable.frameToTime(float sampleIndex)))
                        let y = pitchToPixel keyHeight actualHeight vOffset (Midi.ofFreq(float freq))
                        if not prevVuv then
                            sgc.BeginFigure(Point(x, y), false, false)
                            prevVuv <- true
                        else
                            sgc.LineTo(Point(x, y), true, false)
                    else
                        prevVuv <- false
            dc.DrawGeometry(null, f0Pen, f0Geometry)

type ChartEditorAdornerLayer() =
    inherit NoteChartEditBase()

    static do
        let baseMeta = NoteChartEditBase.CursorPositionProperty.DefaultMetadata
        NoteChartEditBase.CursorPositionProperty.OverrideMetadata(
            typeof<ChartEditorAdornerLayer>, FrameworkPropertyMetadata(
                0L, Dp.MetaFlags.AffectsRender, baseMeta.PropertyChangedCallback, baseMeta.CoerceValueCallback))

    static let hoverNoteBrush = SolidColorBrush(aRgb 0x80 -1) |>! freeze
    static let hoverCursorPen    = Pen(SolidColorBrush(aFRgb 0.25 0), 0.5) |>! freeze
    static let playbackCursorPen = Pen(SolidColorBrush(rgb 0xFF0000), 0.75) |>! freeze

    static let getCursorHeadGeometry xPos =
        pointsToGeometry true [|
            Point(xPos, 0.0)
            Point(xPos - 5.0, -5.0)
            Point(xPos - 5.0, -10.0)
            Point(xPos + 5.0, -10.0)
            Point(xPos + 5.0, -5.0) |]

    member x.MouseOverCursorPositionOp
        with get() = x.GetValue ChartEditorAdornerLayer.MouseOverCursorPositionOpProperty :?> int64 option
        and set(v : int64 option) = x.SetValue(ChartEditorAdornerLayer.MouseOverCursorPositionOpProperty, box v)
    static member val MouseOverCursorPositionOpProperty =
        Dp.reg<int64 option, ChartEditorAdornerLayer> "MouseOverCursorPositionOp"
            (Dp.Meta(None, Dp.MetaFlags.AffectsRender, (fun (x : ChartEditorAdornerLayer) -> x.OnMouseOverCursorPositionOpChanged)))
    member x.OnMouseOverCursorPositionOpChanged(prevMouseOverCursorPosOp, mouseOverCursorPosOp) = ()

    member x.MouseOverNoteOp
        with get() = x.GetValue ChartEditorAdornerLayer.MouseOverNoteOpProperty :?> Note option
        and set(v : Note option) = x.SetValue(ChartEditorAdornerLayer.MouseOverNoteOpProperty, box v)
    static member val MouseOverNoteOpProperty =
        Dp.reg<Note option, ChartEditorAdornerLayer> "MouseOverNoteOp"
            (Dp.Meta(None, Dp.MetaFlags.AffectsRender, (fun (x : ChartEditorAdornerLayer) -> x.OnMouseOverNoteOpChanged)))
    member x.OnMouseOverNoteOpChanged(prevMouseOverNoteOp, mouseOverNoteOp) = ()

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterWidth = x.QuarterWidth
        let keyHeight = x.KeyHeight
        let hOffset = x.HOffsetAnimated
        let vOffset = x.VOffsetAnimated
        let playbackPos = x.CursorPosition
        let comp = x.Composition
        let mouseOverCursorPosOp = x.MouseOverCursorPositionOp
        let mouseOverNoteOp = x.MouseOverNoteOp

        // mouse over note
        match mouseOverNoteOp with
        | Some note ->
            let x0 = pulseToPixel quarterWidth hOffset (float note.On)
            let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
            let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)

            let noteRect = Rect(x0, yMid - half keyHeight, x1 - x0, keyHeight)
            dc.DrawRectangle(hoverNoteBrush, null, noteRect)
        | _ -> ()

        // playback cursor
        let xPos = pulseToPixel quarterWidth hOffset (float playbackPos)
        if xPos >= 0.0 && xPos <= actualWidth then
            dc.DrawLine(playbackCursorPen, Point(xPos, 0.0), Point(xPos, actualHeight))
            dc.DrawGeometry(Brushes.White, playbackCursorPen, getCursorHeadGeometry xPos)

        // mouse over cursor
        match mouseOverCursorPosOp with
        | Some mouseOverCursorPos ->
            let xPos = pulseToPixel quarterWidth hOffset (float mouseOverCursorPos)
            if xPos >= 0.0 && xPos <= actualWidth then
                dc.DrawLine(hoverCursorPen, Point(xPos, 0.0), Point(xPos, actualHeight))
                dc.DrawGeometry(Brushes.White, hoverCursorPen, getCursorHeadGeometry xPos)
        | None -> ()


