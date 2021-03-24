﻿namespace Vogen.Client.Controls

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
    static member val IsPlayingProperty =
        Dp.reg<bool, NoteChartEditBase> "IsPlaying"
            (Dp.Meta(false, Dp.MetaFlags.AffectsRender))

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

    static member MinMajorTickHop = 60.0    // in screen pixels
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
                    dc.DrawText(ft, new Point(xPos - halfTextWidth, 0.0))

type ChartEditor() as x =
    inherit NoteChartEditBase()

    static let makeDiamondGeometry radius (center : Point) =
        let geometry = StreamGeometry()
        do  use ctx = geometry.Open()
            ctx.BeginFigure(
                Point(center.X, center.Y), true, true)
            ctx.PolyLineTo([|
                Point(center.X + radius, center.Y)
                Point(center.X, center.Y + radius)
                Point(center.X - radius, center.Y) |], true, false)
        geometry |>! freeze

    let mutable uttGetChars = ImmutableDictionary.Empty
    do x.OnCompositionChanged.Add <| fun (_, comp) ->
        uttGetChars <-
            comp.Utts
            |> Seq.map(fun utt ->
                let chars =
                    utt.Notes
                    |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen)
                    |> Seq.map(fun notes -> {| Ch = notes.[0].Lyric; Notes = notes |})
                    |> ImmutableArray.CreateRange
                KeyValuePair(utt, chars))
            |> ImmutableDictionary.CreateRange

    let mutable cursorActiveNotes = ImmutableHashSet.Empty
    do x.OnCursorPositionChanged.Add <| fun (_, playbackPos) ->
        let newCursorActiveNotes =
            x.Composition.Utts
            |> Seq.collect(fun utt -> utt.Notes)
            |> Seq.filter(fun note -> note.On <= playbackPos && note.Off > playbackPos)
            |> ImmutableHashSet.CreateRange
        if not(cursorActiveNotes.SetEquals newCursorActiveNotes) then
            cursorActiveNotes <- newCursorActiveNotes
            x.InvalidateVisual()

    static let majorTickPen = Pen(SolidColorBrush(argb 0x40000000), 0.5) |>! freeze
    static let octavePen = Pen(SolidColorBrush(argb 0x40000000), 0.5) |>! freeze
    static let blackKeyPen = Pen(SolidColorBrush(argb 0x40000000), 0.5) |>! freeze

    static let baseColor = rgb 0xFF8000

    static let noteRowBgBrushCursorActive = SolidColorBrush(lerpColor baseColor (rgb 0xFFFFFF) 0.4 |> withAlphaF 0.15) |>! freeze

    static let noteWaveformBgBrush = SolidColorBrush(lerpColor baseColor (rgb 0xFFFFFF) 0.75) |>! freeze
    static let noteBgBrush = SolidColorBrush(lerpColor baseColor (rgb 0xFFFFFF) 0.4) |>! freeze
    static let noteBgPen = Pen(noteBgBrush, 1.0) |>! freeze
    static let noteBgPenCursorActive = Pen(noteBgBrush, 3.0) |>! freeze
    static let charConnectPen = Pen(noteBgBrush, 1.0, DashStyle = DashStyle([| 2.0; 4.0 |], 0.0)) |>! freeze
    static let f0Pen = Pen(SolidColorBrush(rgb 0xFF5555), 1.0) |>! freeze

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let timeSig = x.TimeSignature
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

        let majorHop = RulerGrid.FindTickHop timeSig quarterWidth RulerGrid.MinMajorTickHop
        let minorHop = RulerGrid.FindTickHop timeSig quarterWidth RulerGrid.MinMinorTickHop

        let botPitch = pixelToPitch keyHeight actualHeight vOffset actualHeight |> int |> max minKey
        let topPitch = pixelToPitch keyHeight actualHeight vOffset 0.0 |> ceil |> int |> min maxKey

        // background
        dc.PushClip(RectangleGeometry(Rect(Size(actualWidth, actualHeight))))
        dc.DrawRectangle(Brushes.Transparent, null, Rect(Size(actualWidth, actualHeight)))

        // time grids
        for currPulse in minPulse / minorHop * minorHop .. minorHop .. maxPulse do
            let x = pulseToPixel quarterWidth hOffset (float currPulse)
            let isMajorTick = currPulse % majorHop = 0L
            if isMajorTick then
                dc.DrawLine(majorTickPen, Point(x, 0.0), Point(x, actualHeight))

        // pitch grids
        for pitch in botPitch .. topPitch do
            let y = pitchToPixel keyHeight actualHeight vOffset (float pitch)
            match pitch % 12 with
            | 0 | 5 ->
                let y = y + half keyHeight - half octavePen.Thickness
                dc.DrawLine(octavePen, Point(0.0, y), Point(actualWidth, y))
            | _ -> ()

            if pitch |> Midi.isBlackKey then
                for currPulse in minPulse / minorHop * minorHop .. minorHop .. maxPulse do
                    let isMajorTick = currPulse % majorHop = 0L
                    if not isMajorTick then
                        let x = pulseToPixel quarterWidth hOffset (float currPulse)
                        dc.DrawLine(blackKeyPen, Point(x, y - half keyHeight), Point(x, y + half keyHeight))

        for note in cursorActiveNotes do
            if note.Pitch >= botPitch && note.Pitch <= topPitch then
                let y = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch + 0.5)
                dc.DrawRectangle(noteRowBgBrushCursorActive, null, Rect(0.0, y, actualWidth, keyHeight))

        // utt states
        for utt in comp.Utts do
            let uttSynthResult = comp.GetUttSynthResult utt
            if utt.On >= minPulse && utt.On <= maxPulse then
                let x0 = pulseToPixel quarterWidth hOffset (float utt.On)
                let yMid = pitchToPixel keyHeight actualHeight vOffset (float utt.Notes.[0].Pitch)
                let ft =
                    let text = String.concat Environment.NewLine [|
                        $"({TextResources.getRomSchemeChar utt.RomScheme})"
                        $"({TextResources.getIsSynthingDescription uttSynthResult.IsSynthing})"
                        $"({TextResources.getHasAudioDescription uttSynthResult.HasAudio})" |]
                    x |> makeFormattedText text
                ft.TextAlignment <- TextAlignment.Right
                ft.SetFontSize(0.75 * TextBlock.GetFontSize x)
                dc.DrawText(ft, Point(x0 - 5.0, yMid - ft.Height))

        // notes
        let bpm0 = comp.Bpm0
        for utt in comp.Utts do
            let uttSynthResult = comp.GetUttSynthResult utt
            let chars = uttGetChars.[utt]

            // char connection
            for ch0, ch1 in Seq.pairwise chars do
                let n0 = ch0.Notes.[^0]
                let n1 = ch1.Notes.[0]
                if (max n0.Off n1.Off >= minPulse && min n0.On n1.On <= maxPulse &&
                    max n0.Pitch n1.Pitch >= botPitch && min n0.Pitch n1.Pitch <= topPitch) then
                    let n0x1 = pulseToPixel quarterWidth hOffset (float n0.Off)
                    let n0yMid = pitchToPixel keyHeight actualHeight vOffset (float n0.Pitch)
                    let n1x0 = pulseToPixel quarterWidth hOffset (float n1.On)
                    let n1yMid = pitchToPixel keyHeight actualHeight vOffset (float n1.Pitch)
                    dc.DrawLine(charConnectPen, Point(n0x1, n0yMid), Point(n1x0, n1yMid))

            chars |> Seq.iteri(fun charIndex ch ->
                let charCursorActive = ch.Notes.[0].On <= playbackPos && ch.Notes.[^0].Off > playbackPos
                let noteBgPen = if charCursorActive then noteBgPenCursorActive else noteBgPen

                // note connection inside char
                for n0, n1 in Seq.pairwise ch.Notes do
                    if (max n0.Off n1.Off >= minPulse && min n0.On n1.On <= maxPulse &&
                        max n0.Pitch n1.Pitch >= botPitch && min n0.Pitch n1.Pitch <= topPitch) then
                        let n0x1 = pulseToPixel quarterWidth hOffset (float n0.Off)
                        let n0yMid = pitchToPixel keyHeight actualHeight vOffset (float n0.Pitch)
                        let n1x0 = pulseToPixel quarterWidth hOffset (float n1.On)
                        let n1yMid = pitchToPixel keyHeight actualHeight vOffset (float n1.Pitch)
                        dc.DrawLine(noteBgPen, Point(n0x1, n0yMid), Point(n1x0, n1yMid))

                // notes
                ch.Notes |> Array.iteri(fun noteIndex note ->
                    if (note.Off >= minPulse && note.On <= maxPulse &&
                        note.Pitch >= botPitch && note.Pitch <= topPitch) then
                        let x0 = pulseToPixel quarterWidth hOffset (float note.On)  |> max 0.0
                        let x1 = pulseToPixel quarterWidth hOffset (float note.Off) |> min actualWidth
                        let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)
                        if uttSynthResult.HasAudio then
                            let waveformUpperContourPoints = List()
                            let waveformLowerContourPoints = List()
                            let samples = uttSynthResult.AudioSamples
                            let x0 =
                                if charIndex > 0 || noteIndex > 0 then x0 else
                                    uttSynthResult.SampleOffset
                                    |> Audio.sampleToPulse bpm0 |> pulseToPixel quarterWidth hOffset |> max 0.0
                            let x1 =
                                if charIndex < chars.Length - 1 || noteIndex < ch.Notes.Length - 1 then x1 else
                                    uttSynthResult.SampleOffset + samples.Length
                                    |> Audio.sampleToPulse bpm0 |> pulseToPixel quarterWidth hOffset |> min actualWidth
                            for xi0 in x0 .. 1.0 .. x1 do
                                let globalSi0 = xi0       |> pixelToPulse quarterWidth hOffset |> Audio.pulseToSample bpm0
                                let globalSi1 = xi0 + 1.0 |> pixelToPulse quarterWidth hOffset |> Audio.pulseToSample bpm0
                                let si0 = globalSi0 - uttSynthResult.SampleOffset |> max 0
                                let si1 = globalSi1 - uttSynthResult.SampleOffset |> min samples.Length
                                let mutable sMax, sMin = samples.[si0], samples.[si0]
                                for si in si0 + 1 .. si1 - 1 do
                                    sMax <- max sMax samples.[si]
                                    sMin <- min sMin samples.[si]
                                waveformUpperContourPoints.Add(Point(xi0, yMid - float sMax * 25.0))
                                waveformLowerContourPoints.Add(Point(xi0, yMid - float sMin * 25.0))
                            waveformLowerContourPoints.Reverse()
                            let waveformGeometry = pointsToGeometry true (Seq.append waveformUpperContourPoints waveformLowerContourPoints)
                            dc.DrawGeometry(noteWaveformBgBrush, noteBgPen, waveformGeometry)
                        else
                            dc.DrawLine(noteBgPen, Point(x0, yMid), Point(x1, yMid)))

                let note = ch.Notes.[0]
                if (note.Off >= minPulse && note.On <= maxPulse &&
                    note.Pitch >= botPitch && note.Pitch <= topPitch) then
                    let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                    let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)
                    let fillBrush = if charIndex = 0 then noteBgPen.Brush else Brushes.White :> _
                    dc.DrawEllipse(fillBrush, noteBgPen, Point(x0, yMid), 5.0, 5.0)
                    //dc.DrawRoundedRectangle(fillBrush, noteBgPen, Rect(x0 - 2.0, yMid - 5.0, 4.0, 10.0), 2.0, 2.0)

                    // text
                    let textOpacity = if charCursorActive then 1.0 else 0.5
                    let ft = x |> makeFormattedText note.Rom
                    ft.SetFontSize(1.0 * TextBlock.GetFontSize x)
                    dc.DrawText(ft, Point(x0, yMid - ft.Height)))

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
        //                ft.SetForegroundBrush phBorderPen.Brush
        //                ft.SetFontSize(0.75 * TextBlock.GetFontSize x)
        //                dc.DrawLine(phBorderPen, Point(x0, y), Point(x1, y))
        //                let fillBrush = if i = 0 then phBorderPen.Brush else Brushes.White :> _
        //                dc.DrawEllipse(fillBrush, phBorderPen, Point(x0, y), 2.0, 2.0)
        //                dc.DrawText(ft, Point(x0, y)))

        // utt f0 samples
        for utt in comp.Utts do
            let f0Geometry = drawGeometry <| fun sgc ->
                let uttSynthResult = comp.GetUttSynthResult utt
                let f0Samples = uttSynthResult.F0Samples
                let uttTimeOffset = uttSynthResult.SampleOffset |> Audio.sampleToTime
                let startSampleIndex =
                    TimeTable.timeToFrame(Midi.toTimeSpan bpm0 (pixelToPulse quarterWidth hOffset 0.0) - uttTimeOffset) - 1.0
                    |> int |> max 0
                let endSampleIndex =
                    TimeTable.timeToFrame(Midi.toTimeSpan bpm0 (pixelToPulse quarterWidth hOffset actualWidth) - uttTimeOffset) + 1.0
                    |> ceil |> int |> min f0Samples.Length
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

    static let playbackCursorPen = Pen(SolidColorBrush(rgb 0xFF0000), 0.5) |>! freeze

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterWidth = x.QuarterWidth
        let hOffset = x.HOffsetAnimated
        let vOffset = x.VOffsetAnimated
        let playbackPos = x.CursorPosition
        let comp = x.Composition

        // playback cursor
        let xPos = pulseToPixel quarterWidth hOffset (float playbackPos)
        if xPos >= 0.0 && xPos <= actualWidth then
            dc.DrawLine(playbackCursorPen, Point(xPos, 0.0), Point(xPos, actualHeight))


