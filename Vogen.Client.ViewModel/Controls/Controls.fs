namespace Vogen.Client.Controls

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open FSharp.NativeInterop
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Runtime.InteropServices
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Animation
open System.Windows.Media.Imaging
open Vogen.Client.Model
open Vogen.Client.ViewModel
open Vogen.Synth
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
    static member CoerceCursorPosition x v = max 0L v
    static member val CursorPositionProperty =
        Dp.reg<int64, NoteChartEditBase> "CursorPosition"
            (Dp.Meta(0L,
                (fun (x : NoteChartEditBase)(oldValue, newValue) -> x.OnCursorPositionChanged(oldValue, newValue)),
                NoteChartEditBase.CoerceCursorPosition))
    member val private CursorPositionChangedEvent : Event<_> = Event<_>()
    [<CLIEvent>] member x.CursorPositionChanged = x.CursorPositionChangedEvent.Publish
    abstract OnCursorPositionChanged : oldValue : int64 * newValue : int64 -> unit
    default x.OnCursorPositionChanged(oldValue, newValue) =
        x.CursorPositionChangedEvent.Trigger((oldValue, NoteChartEditBase.CoerceCursorPosition x newValue))

    member x.IsPlaying
        with get() = x.GetValue NoteChartEditBase.IsPlayingProperty :?> bool
        and set(v : bool) = x.SetValue(NoteChartEditBase.IsPlayingProperty, box v)
    static member val IsPlayingProperty =
        Dp.reg<bool, NoteChartEditBase> "IsPlaying"
            (Dp.Meta(false, Dp.MetaFlags.AffectsRender,
                (fun (x : NoteChartEditBase)(oldValue, newValue) -> x.OnIsPlayingChanged(oldValue, newValue))))
    abstract OnIsPlayingChanged : oldValue : bool * newValue : bool -> unit
    default x.OnIsPlayingChanged(oldValue, newValue) = ()

type SideKeyboard() =
    inherit NoteChartEditBase()

    let whiteKeyFill = Brushes.White
    let whiteKeyPen : Pen = Pen(Brushes.Black, 0.6) |>! freeze
    let blackKeyFill = Brushes.Black
    let blackKeyPen : Pen = null

    let defaultKeyHeight = 12.0
    let keyOffsetLookup = [| -8; 0; -4; 0; 0; -9; 0; -6; 0; -3; 0; 0 |] |> Array.map float
    let keyHeightLookup = [| 20; 12; 20; 12; 20; 21; 12; 21; 12; 21; 12; 21 |] |> Array.map float

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

    let majorTickHeight = 6.0
    let minorTickHeight = 4.0

    let tickPen = Pen(SolidColorBrush(rgb 0), 1.0) |>! freeze

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

    member x.TimeSignature
        with get() = x.GetValue RulerGrid.TimeSignatureProperty :?> TimeSignature
        and set(v : TimeSignature) = x.SetValue(RulerGrid.TimeSignatureProperty, box v)
    static member val TimeSignatureProperty =
        Dp.reg<TimeSignature, RulerGrid> "TimeSignature"
            (Dp.Meta(timeSignature 4 4, Dp.MetaFlags.AffectsRender))

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

module BitmapPalettes =
    open System.IO
    open System.Reflection

    let ofEmbedded uri =
        use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream uri
        use reader = new BinaryReader(stream)
        let colors = Array.init 256 (fun _ -> reader.ReadInt32() |> ColorConv.argb)
        BitmapPalette(colors)

    let afmhot = ofEmbedded @"Vogen.Client.ViewModel.cmaps.afmhot.cmap"

type BgAudioDisplay() =
    inherit NoteChartEditBase()

    static let fftSize = 512

    let bgBrush = Brushes.Transparent
    let waveBrush = SolidColorBrush(aRgb 0x60 -1) |>! freeze
    let mutable spImage : WriteableBitmap = null

    override x.CanScrollV = false

    member x.Bpm0
        with get() = x.GetValue BgAudioDisplay.Bpm0Property :?> float
        and set(v : float) = x.SetValue(BgAudioDisplay.Bpm0Property, box v)
    static member val Bpm0Property =
        Dp.reg<float, BgAudioDisplay> "Bpm0"
            (Dp.Meta(120.0, Dp.MetaFlags.AffectsRender, (fun (x : BgAudioDisplay) -> x.OnBpm0Changed)))
    member x.OnBpm0Changed(prevValue, value) = ()

    member x.AudioTrack
        with get() = x.GetValue BgAudioDisplay.AudioTrackProperty :?> AudioTrack
        and set(v : AudioTrack) = x.SetValue(BgAudioDisplay.AudioTrackProperty, box v)
    static member val AudioTrackProperty =
        Dp.reg<AudioTrack, BgAudioDisplay> "AudioTrack"
            (Dp.Meta(AudioTrack.Empty, Dp.MetaFlags.AffectsRender, (fun (x : BgAudioDisplay) -> x.OnAudioTrackChanged)))
    member x.OnAudioTrackChanged(prevValue, value) = ()

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterWidth = x.QuarterWidth
        let hOffset = x.HOffsetAnimated
        let bpm0 = x.Bpm0
        let audioTrack = x.AudioTrack

        dc.PushClip(RectangleGeometry(Rect(Size(actualWidth, actualHeight))))
        dc.DrawRectangle(bgBrush, null, Rect(Size(actualWidth, actualHeight)))

        if audioTrack.HasAudio then
            let samples = audioTrack.AudioSamples
            let sampleOffset = audioTrack.SampleOffset
            let fftSampleIndices = [|
                for x in 0.0 .. floor actualWidth - 1.0 ->
                    (x |> pixelToPulse quarterWidth hOffset |> Audio.pulseToSample bpm0) - sampleOffset |]
            try let cis = Rfft.run fftSize samples fftSampleIndices
                if spImage = null || spImage.PixelHeight <> cis.GetLength 0 || spImage.PixelWidth <> cis.GetLength 1 then
                    let dpi = VisualTreeHelper.GetDpi x
                    spImage <- WriteableBitmap(
                        cis.GetLength 1, cis.GetLength 0, dpi.PixelsPerInchX, 96.0, PixelFormats.Indexed8, BitmapPalettes.afmhot)
                spImage.WritePixels(
                    Int32Rect(0, 0, spImage.PixelWidth, spImage.PixelHeight),
                    cis, cis.GetLength 1, 0)
                dc.DrawImage(spImage, Rect(Size(floor actualWidth, actualHeight)))
            with ex -> ()

        let minWaveformSamplesPerFrame = 100
        let frameRadius = half(max 1.0 (Audio.sampleToPulse bpm0 minWaveformSamplesPerFrame |> pulseToPixel quarterWidth 0.0))
        let inline pixelToSample x = x |> pixelToPulse quarterWidth hOffset |> Audio.pulseToSample bpm0
        let inline sampleToPixel si = si |> Audio.sampleToPulse bpm0 |> pulseToPixel quarterWidth hOffset
        let drawWaveformGeometry(samples : _ []) sampleOffset x0 x1 yMid yOffset yScale (sgc : StreamGeometryContext) =
            if x0 < x1 then
                let inline getFrameDrawPoints x =
                    let si0 = pixelToSample(x - frameRadius) - sampleOffset |> clamp 0 (samples.Length - 1)
                    let si1 = pixelToSample(x + frameRadius) - sampleOffset |> min samples.Length
                    let mutable sMin : float32 = samples.[si0]
                    let mutable sMax : float32 = samples.[si0]
                    for si in si0 + 1 .. si1 - 1 do
                        sMin <- min sMin samples.[si]
                        sMax <- max sMax samples.[si]
                    Point(x, yMid - float sMin * yScale - float(sign sMin) * yOffset + 0.5),
                    Point(x, yMid - float sMax * yScale - float(sign sMax) * yOffset - 0.5)

                let waveformLowerContourPoints = List()
                let waveformUpperContourPoints = List()
                for x in x0 .. frameRadius * 2.0 .. x1 do
                    let pMin, pMax = getFrameDrawPoints x
                    waveformLowerContourPoints.Add pMin
                    waveformUpperContourPoints.Add pMax

                if waveformLowerContourPoints.Count > 0 then
                    let pMin, pMax = getFrameDrawPoints x1
                    sgc.BeginFigure(pMin, true, true)

                    waveformLowerContourPoints.Reverse()
                    sgc.PolyLineTo(waveformLowerContourPoints, true, false)

                    waveformUpperContourPoints.Add pMax
                    sgc.PolyLineTo(waveformUpperContourPoints, true, false)

        if audioTrack.HasAudio then
            let samples = audioTrack.AudioSamples
            let sampleOffset = audioTrack.SampleOffset

            let waveformGeometry = drawGeometry FillRule.Nonzero <| fun sgc ->
                let x0 = 0.0
                let x1 = actualWidth
                let yMid = half actualHeight

                sgc |> drawWaveformGeometry samples sampleOffset x0 x1 yMid 0.0 (0.75 * actualHeight)

            dc.DrawGeometry(waveBrush, null, waveformGeometry)

            let timeStr = pixelToSample 0.0 - sampleOffset |> Audio.sampleToTime |> sprintf "%A"
            let ft = x |> makeFormattedText timeStr
            dc.DrawText(ft, Point())

        else
            dc.DrawRectangle(waveBrush, null, Rect(0.0, half actualHeight - 0.5, actualWidth, 1.0))

type ChartEditor() =
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

    let inactiveBackgroundBrush = SolidColorBrush(aRgb 0x10 0) |>! freeze
    let activeBackgroundBrush = SolidColorBrush(rgb -1) |>! freeze

    let minGridScreenHop = 5.0
    let majorGridPen = Pen(SolidColorBrush(aRgb 0x80 0), 0.5) |>! freeze
    let minorGridPen = Pen(SolidColorBrush(aRgb 0x40 0), 0.5) |>! freeze

    let phPen = Pen(SolidColorBrush(argb 0xC08040FF), 0.5)

    let waveBrush = SolidColorBrush(aRgb 0x10 0) |>! freeze

    let noteBaseColor = rgb 0xFF8040
    let hyphBaseColor = rgb 0xFF6080
    let activeUttStyle = {|
        NoteBrush = SolidColorBrush(lerpColor noteBaseColor (rgb -1) 0.6) |>! freeze
        HyphBrush = SolidColorBrush(lerpColor hyphBaseColor (rgb -1) 0.6) |>! freeze
        NoteBrushInvalid = SolidColorBrush(rgb -1) |>! freeze
        NotePen = Pen(SolidColorBrush(lerpColor noteBaseColor (rgb -1) 0.2), 1.0) |>! freeze
        HyphPen = Pen(SolidColorBrush(lerpColor hyphBaseColor (rgb -1) 0.2), 1.0) |>! freeze
        RestPen = Pen(SolidColorBrush(lerpColor noteBaseColor (rgb -1) 0.2), 1.0, DashStyle = DashStyle([| 2.0; 4.0 |], 0.0)) |>! freeze
        TextBrush = Brushes.Black |}

    let inactiveNoteBaseColor = lerpColor noteBaseColor (rgb 0xA0A0A0) 0.9
    let inactiveHyphBaseColor = lerpColor hyphBaseColor (rgb 0x808080) 0.9
    let inactiveUttStyle = {|
        NoteBrush = SolidColorBrush(lerpColor inactiveNoteBaseColor (rgb -1) 0.8) |>! freeze
        HyphBrush = SolidColorBrush(lerpColor inactiveHyphBaseColor (rgb -1) 0.6) |>! freeze
        NoteBrushInvalid = SolidColorBrush(rgb -1) |>! freeze
        NotePen = Pen(SolidColorBrush(lerpColor inactiveNoteBaseColor (rgb -1) 0.2), 1.0) |>! freeze
        HyphPen = Pen(SolidColorBrush(lerpColor inactiveHyphBaseColor (rgb -1) 0.6), 1.0) |>! freeze
        RestPen = Pen(SolidColorBrush(lerpColor inactiveNoteBaseColor (rgb -1) 0.2), 1.0, DashStyle = DashStyle([| 2.0; 4.0 |], 0.0)) |>! freeze
        TextBrush = SolidColorBrush(aRgb 0xA0 0) |>! freeze |}

    let selNoteBrush = SolidColorBrush(aRgb 0x20 0x000080) |>! freeze
    let selNotePen = Pen(SolidColorBrush(aRgb 0x80 0x000080), 2.0) |>! freeze

    let cursorActivePitchBrush = SolidColorBrush(aRgb 0x10 0xFF0000) |>! freeze
    let cursorActiveNotePen = Pen(SolidColorBrush(aRgb 0x80 0xFF0000), 2.0) |>! freeze

    let f0Pen = Pen(SolidColorBrush(aRgb 0x40 0x800000), 1.0) |>! freeze

    member val NoteSynthingOverlayBrush : Brush = null with get, set

    member val private UttToCharsDict = ImmutableDictionary.Empty with get, set
    member private x.UpdateUttToCharsDict chart =
        x.UttToCharsDict <-
            (chart : ChartState).Comp.Utts
            |> Seq.map(fun utt ->
                let chars =
                    utt.Notes
                    |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen)
                    |> Seq.map(fun notes -> {| Ch = notes.[0].Lyric; Notes = notes |})
                    |> ImmutableArray.CreateRange
                KeyValuePair(utt, chars))
            |> ImmutableDictionary.CreateRange

    member val private CursorActiveNotes = ImmutableHashSet.Empty with get, set
    member private x.UpdateCursorActiveNotes playbackPos isPlaying chart =
        let newActiveNotes =
            if not isPlaying then ImmutableHashSet.Empty else
                (chart : ChartState).Comp.AllNotes
                |> Seq.filter(fun note -> note.On <= playbackPos && note.Off > playbackPos)
                |> ImmutableHashSet.CreateRange
        if not(x.CursorActiveNotes.SetEquals newActiveNotes) then
            x.CursorActiveNotes <- newActiveNotes
            x.InvalidateVisual()

    override x.OnCursorPositionChanged(oldValue, (newValue as playbackPos)) =
        x.UpdateCursorActiveNotes playbackPos x.IsPlaying x.ChartState
        base.OnCursorPositionChanged(oldValue, newValue)

    override x.OnIsPlayingChanged(oldValue, (newValue as isPlaying)) =
        x.UpdateCursorActiveNotes x.CursorPosition isPlaying x.ChartState
        base.OnIsPlayingChanged(oldValue, newValue)

    member x.ChartState
        with get() = x.GetValue ChartEditor.ChartStateProperty :?> ChartState
        and set(v : ChartState) = x.SetValue(ChartEditor.ChartStateProperty, box v)
    static member val ChartStateProperty =
        Dp.reg<ChartState, ChartEditor> "ChartState"
            (Dp.Meta(ChartState.Empty, Dp.MetaFlags.AffectsRender,
                (fun (x : ChartEditor)(oldValue, newValue) -> x.OnChartStateChanged(oldValue, newValue))))
    member private x.OnChartStateChanged(oldValue, (newValue as chart)) =
        x.UpdateUttToCharsDict chart
        x.UpdateCursorActiveNotes x.CursorPosition x.IsPlaying chart

    member x.UttSynthCache
        with get() = x.GetValue ChartEditor.UttSynthCacheProperty :?> UttSynthCache
        and set(v : UttSynthCache) = x.SetValue(ChartEditor.UttSynthCacheProperty, box v)
    static member val UttSynthCacheProperty =
        Dp.reg<UttSynthCache, ChartEditor> "UttSynthCache"
            (Dp.Meta(UttSynthCache.Empty, Dp.MetaFlags.AffectsRender))

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quantization = x.Quantization
        let quarterWidth = x.QuarterWidth
        let keyHeight = x.KeyHeight
        let minKey = x.MinKey
        let maxKey = x.MaxKey
        let hOffset = x.HOffsetAnimated
        let vOffset = x.VOffsetAnimated
        let playbackPos = x.CursorPosition
        let isPlaying = x.IsPlaying
        let chart = x.ChartState
        let comp = chart.Comp
        let uttSynthCache = x.UttSynthCache

        let minPulse = int64(pixelToPulse quarterWidth hOffset 0.0)
        let maxPulse = int64(pixelToPulse quarterWidth hOffset actualWidth |> ceil)

        let botPitch = pixelToPitch keyHeight actualHeight vOffset actualHeight |> int |> max minKey
        let topPitch = pixelToPitch keyHeight actualHeight vOffset 0.0 |> ceil |> int |> min maxKey

        dc.PushClip(RectangleGeometry(Rect(Size(actualWidth, actualHeight))))

        // validate active utt
        match chart.ActiveUtt with
        | Some utt when not(comp.Utts.Contains utt) -> raise(ArgumentException("Active utt not part of composition."))
        | _ -> ()

        // background
        if isPlaying then
            dc.DrawRectangle(activeBackgroundBrush, null, Rect(Size(actualWidth, actualHeight)))

        else
            // active utt background
            match chart.ActiveUtt with
            | None ->
                dc.DrawRectangle(activeBackgroundBrush, null, Rect(Size(actualWidth, actualHeight)))

            | Some utt ->
                let xMin = pulseToPixel quarterWidth hOffset (float utt.Notes.[0].On) |> clamp 0.0 actualWidth
                let xMax = pulseToPixel quarterWidth hOffset (float utt.Notes.[^0].Off) |> clamp 0.0 actualWidth

                let uttMaxPitch = utt.Notes |> Seq.map(fun note -> note.Pitch) |> Seq.max
                let uttMinPitch = utt.Notes |> Seq.map(fun note -> note.Pitch) |> Seq.min
                let yTop = pitchToPixel keyHeight actualHeight vOffset (float uttMaxPitch)
                let yBot = pitchToPixel keyHeight actualHeight vOffset (float uttMinPitch)
                dc.DrawRectangle(inactiveBackgroundBrush, null, Rect(Size(actualWidth, actualHeight)))
                dc.DrawRectangle(activeBackgroundBrush, null, Rect(xMin, yTop - half keyHeight, xMax - xMin, yBot - yTop + keyHeight))

        // time grids
        let timeSig = comp.TimeSig0
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

        // playback active note pitch background
        let cursorActivePitches = HashSet()
        for note in x.CursorActiveNotes do
            if note.Pitch >= botPitch && note.Pitch <= topPitch then
                cursorActivePitches.Add note.Pitch |> ignore

        for pitch in cursorActivePitches do
            let yMid = pitchToPixel keyHeight actualHeight vOffset (float pitch)
            dc.DrawRectangle(cursorActivePitchBrush, null, Rect(0.0, yMid - half keyHeight, actualWidth, keyHeight))

        // utt start decor
        let uttsReordered =
            match chart.ActiveUtt with
            | None -> comp.Utts :> seq<_>
            | Some utt -> seq {
                yield! comp.Utts |> Seq.filter((<>) utt)
                yield utt }

        for utt in uttsReordered do
            let uttStyle =
                if isPlaying then activeUttStyle
                elif chart.ActiveUtt = Some utt then activeUttStyle
                else inactiveUttStyle

            if utt.On >= minPulse && utt.On <= maxPulse then
                let x0 = pulseToPixel quarterWidth hOffset (float utt.On)
                let yMid = pitchToPixel keyHeight actualHeight vOffset (float utt.Notes.[0].Pitch)
                dc.DrawLine(uttStyle.NotePen, Point(x0 - 0.0, yMid - half keyHeight - 8.0), Point(x0 - 0.0, yMid + half keyHeight + 8.0))
                dc.DrawLine(uttStyle.NotePen, Point(x0 - 3.0, yMid - half keyHeight - 8.0), Point(x0 - 3.0, yMid + half keyHeight + 8.0))
                dc.DrawLine(uttStyle.NotePen, Point(x0 - 6.0, yMid - half keyHeight - 8.0), Point(x0 - 6.0, yMid + half keyHeight + 8.0))

                let ft =
                    let text = String.concat Environment.NewLine [|
                        $"{TextResources.getSingerName utt.SingerId}"
                        $"({TextResources.getRomSchemeChar utt.RomScheme})" |]
                    x |> makeFormattedText text
                ft.TextAlignment <- TextAlignment.Right
                ft.SetFontSize(0.65 * TextBlock.GetFontSize x)
                ft.SetForegroundBrush uttStyle.NotePen.Brush
                //ft.SetFontWeight FontWeights.Bold
                dc.DrawText(ft, Point(x0 - 8.0, yMid - half ft.Height))

        // waveform
        let bpm0 = comp.Bpm0

        let minWaveformSamplesPerFrame = 100
        let frameRadius = half(max 1.0 (Audio.sampleToPulse bpm0 minWaveformSamplesPerFrame |> pulseToPixel quarterWidth 0.0))
        let inline pixelToSample x = x |> pixelToPulse quarterWidth hOffset |> Audio.pulseToSample bpm0
        let inline sampleToPixel si = si |> Audio.sampleToPulse bpm0 |> pulseToPixel quarterWidth hOffset
        let drawWaveformGeometry(uttSynthResult : UttSynthResult) x0 x1 yMid yOffset yScale (sgc : StreamGeometryContext) =
            if x0 < x1 then
                let samples = uttSynthResult.AudioSamples
                let sampleOffset = uttSynthResult.SampleOffset
                let inline getFrameDrawPoints x =
                    let si0 = pixelToSample(x - frameRadius) - sampleOffset |> clamp 0 (samples.Length - 1)
                    let si1 = pixelToSample(x + frameRadius) - sampleOffset |> min samples.Length
                    let mutable sMin = samples.[si0]
                    let mutable sMax = samples.[si0]
                    for si in si0 + 1 .. si1 - 1 do
                        sMin <- min sMin samples.[si]
                        sMax <- max sMax samples.[si]
                    Point(x, yMid - float sMin * yScale - float(sign sMin) * yOffset + 0.5),
                    Point(x, yMid - float sMax * yScale - float(sign sMax) * yOffset - 0.5)

                let waveformLowerContourPoints = List()
                let waveformUpperContourPoints = List()
                for x in x0 .. frameRadius * 2.0 .. x1 do
                    let pMin, pMax = getFrameDrawPoints x
                    waveformLowerContourPoints.Add pMin
                    waveformUpperContourPoints.Add pMax

                if waveformLowerContourPoints.Count > 0 then
                    let pMin, pMax = getFrameDrawPoints x1
                    sgc.BeginFigure(pMin, true, true)

                    waveformLowerContourPoints.Reverse()
                    sgc.PolyLineTo(waveformLowerContourPoints, true, false)

                    waveformUpperContourPoints.Add pMax
                    sgc.PolyLineTo(waveformUpperContourPoints, true, false)

        let waveformGeometry = drawGeometry FillRule.Nonzero <| fun sgc ->
            for utt in uttsReordered do
                let uttSynthResult = uttSynthCache.GetOrDefault utt
                if uttSynthResult.HasAudio then
                    let samples = uttSynthResult.AudioSamples
                    let sampleOffset = uttSynthResult.SampleOffset

                    for noteIndex in 0 .. utt.Notes.Length - 1 do
                        let note = utt.Notes.[noteIndex]
                        if note.Off >= minPulse && note.On <= maxPulse && note.Pitch |> betweenInc botPitch topPitch then
                            let x0 = pulseToPixel quarterWidth hOffset (float note.On) |> clamp 0.0 actualWidth
                            let x1 = pulseToPixel quarterWidth hOffset (float note.Off) |> clamp 0.0 actualWidth
                            let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)

                            let n1x0 =
                                if noteIndex = utt.Notes.Length - 1 then x1 else
                                    pulseToPixel quarterWidth hOffset (float utt.Notes.[noteIndex + 1].On) |> clamp 0.0 actualWidth

                            if noteIndex = 0 then
                                let x00 = sampleToPixel sampleOffset |> clamp 0.0 actualWidth
                                sgc |> drawWaveformGeometry uttSynthResult x00 x0 yMid 0.0 50.0

                            sgc |> drawWaveformGeometry uttSynthResult x0 (min x1 n1x0) yMid (half keyHeight) 50.0

                            if x1 < n1x0 then
                                sgc |> drawWaveformGeometry uttSynthResult x1 n1x0 yMid 0.0 50.0

                            if noteIndex = utt.Notes.Length - 1 then
                                let uttX1 = sampleToPixel(sampleOffset + samples.Length) |> clamp 0.0 actualWidth
                                sgc |> drawWaveformGeometry uttSynthResult x1 uttX1 yMid 0.0 50.0

        dc.DrawGeometry(waveBrush, null, waveformGeometry)

        // notes
        for utt in uttsReordered do
            let uttStyle =
                if isPlaying then activeUttStyle
                elif chart.ActiveUtt = Some utt then activeUttStyle
                else inactiveUttStyle
            let uttSynthResult = uttSynthCache.GetOrDefault utt

            for note, nextNote in Seq.pairwise(utt.Notes) do
                if nextNote.On >= minPulse && note.Off <= maxPulse && note.Pitch |> betweenInc botPitch topPitch then
                    let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                    let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                    let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)

                    // note connection
                    let n1x0 = pulseToPixel quarterWidth hOffset (float nextNote.On)
                    let n1yMid = pitchToPixel keyHeight actualHeight vOffset (float nextNote.Pitch)
                    let currNotePen = if nextNote.IsHyphen then uttStyle.HyphPen else uttStyle.NotePen
                    let yMidMin = min yMid n1yMid
                    let yMidMax = max yMid n1yMid
                    dc.DrawLine(currNotePen, Point(n1x0, yMidMin), Point(n1x0, yMidMax))

                    // rest
                    if x1 > n1x0 then
                        let noteExcessRect = Rect(n1x0, yMid - half keyHeight, x1 - n1x0, keyHeight)
                        dc.DrawRoundedRectangle(uttStyle.NoteBrushInvalid, currNotePen, noteExcessRect, 3.0, 3.0)
                    elif x1 < n1x0 then
                        let noteConnectPen = if nextNote.IsHyphen then uttStyle.HyphPen else uttStyle.RestPen
                        dc.DrawLine(noteConnectPen, Point(x1, yMid), Point(n1x0, yMid))

            for noteIndex in 0 .. utt.Notes.Length - 1 do
                let note = utt.Notes.[noteIndex]
                if note.Off >= minPulse && note.On <= maxPulse && note.Pitch |> betweenInc botPitch topPitch then
                    let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                    let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                    let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)

                    let hasNextNote = noteIndex < utt.Notes.Length - 1
                    let n1x0 =
                        if not hasNextNote then x1 else
                            let nextNote = utt.Notes.[noteIndex + 1]
                            pulseToPixel quarterWidth hOffset (float nextNote.On)

                    // note
                    let currNoteBrush = if note.IsHyphen then uttStyle.HyphBrush else uttStyle.NoteBrush
                    let currNotePen = if note.IsHyphen then uttStyle.HyphPen else uttStyle.NotePen

                    let noteRectHeight = keyHeight 
                    let noteRect      = Rect(x0, yMid - half noteRectHeight, x1 - x0, noteRectHeight)
                    let noteValidRect = Rect(x0, yMid - half noteRectHeight, min x1 n1x0 - x0, noteRectHeight)
                    dc.DrawRoundedRectangle(currNoteBrush, currNotePen, noteValidRect, 3.0, 3.0)

                    // synth in progress overlay
                    if uttSynthResult.IsSynthing then
                        dc.DrawRoundedRectangle(x.NoteSynthingOverlayBrush, null, noteValidRect, 3.0, 3.0)

                    // selection
                    if chart.GetIsNoteSelected note then
                        dc.DrawRoundedRectangle(selNoteBrush, selNotePen, Rect.Inflate(noteRect, 0.0, 1.5), 3.0, 3.0)

                    // playback active notes
                    if x.CursorActiveNotes.Contains note then
                        dc.DrawRoundedRectangle(null, cursorActiveNotePen, Rect.Inflate(noteRect, 0.0, 1.5), 3.0, 3.0)

                    // text
                    if not note.IsHyphen then
                        let ft = x |> makeFormattedText($"{note.Lyric}{note.Rom}")
                        ft.SetForegroundBrush uttStyle.TextBrush
                        dc.DrawText(ft, Point(x0, yMid - half ft.Height))

        // utt ph bounds
        //for utt in uttsReordered do
        //    let uttSynthResult = uttSynthCache.GetOrDefault utt
        //    let uttTimeOffset = uttSynthResult.SampleOffset |> Audio.sampleToTime
        //    for charGrid in uttSynthResult.CharGrids do
        //        let pitch = charGrid.Pitch
        //        let y = pitchToPixel keyHeight actualHeight vOffset (float pitch)
        //        let yBot = y + half keyHeight
        //        charGrid.Phs |> Array.iteri(fun i ph ->
        //            let x0 = pulseToPixel quarterWidth hOffset (Midi.ofTimeSpan bpm0 (uttTimeOffset + TimeTable.frameToTime(float ph.On)))
        //            let x1 = pulseToPixel quarterWidth hOffset (Midi.ofTimeSpan bpm0 (uttTimeOffset + TimeTable.frameToTime(float ph.Off)))
        //            if x1 >= 0.0 && x0 <= actualWidth then
        //                let ft = x |> makeFormattedText ph.Ph
        //                ft.SetForegroundBrush phPen.Brush
        //                ft.SetFontSize(0.5 * TextBlock.GetFontSize x)
        //                dc.DrawLine(phPen, Point(x0, yBot), Point(x1, yBot))
        //                let fillBrush = if i = 0 then phPen.Brush else Brushes.White :> _
        //                dc.DrawEllipse(fillBrush, phPen, Point(x0, yBot), 2.0, 2.0)
        //                dc.DrawText(ft, Point(x0, yBot)))

        // utt f0 samples
        let f0Geometry = drawGeometry FillRule.EvenOdd <| fun sgc ->
            for utt in uttsReordered do
                let uttSynthResult = uttSynthCache.GetOrDefault utt
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

type NoteDragType =
    | NoteDragMove
    | NoteDragResizeLeft
    | NoteDragResizeRight

[<NoComparison; ReferenceEquality>]
type ChartEditorHint =
    | HoverNote of utt : Utterance * note : Note * noteDragType : NoteDragType
    | GhostCursor of cursorPos : int64
    | GhostNote of note : Note

type ChartEditorAdornerLayer() =
    inherit NoteChartEditBase()

    static do
        let baseMeta = NoteChartEditBase.CursorPositionProperty.DefaultMetadata
        NoteChartEditBase.CursorPositionProperty.OverrideMetadata(
            typeof<ChartEditorAdornerLayer>, FrameworkPropertyMetadata(
                0L, Dp.MetaFlags.AffectsRender, baseMeta.PropertyChangedCallback, baseMeta.CoerceValueCallback))

    let playbackCursorPen = Pen(SolidColorBrush(rgb 0xFF0000), 0.75) |>! freeze
    let selBoxBrush = SolidColorBrush(aRgb 0x20 0x000080) |>! freeze
    let selBoxPen = Pen(SolidColorBrush(aRgb 0x80 0x000080), 0.75) |>! freeze
    let hoverNoteBrush = SolidColorBrush(aRgb 0x80 -1) |>! freeze
    let hoverCursorPen = Pen(SolidColorBrush(aFRgb 0.5 0), 0.75) |>! freeze

    let ghostNoteBrush = SolidColorBrush(aRgb 0x20 0xFF8040) |>! freeze
    let ghostNotePen = Pen(SolidColorBrush(aRgb 0xC0 0xFF8040), 2.0) |>! freeze
    let ghostHyphBrush = SolidColorBrush(aRgb 0x20 0xFF6080) |>! freeze
    let ghostHyphPen = Pen(SolidColorBrush(aRgb 0xC0 0xFF6080), 2.0) |>! freeze

    static let getCursorHeadGeometry xPos =
        pointsToGeometry true [|
            Point(xPos, 0.0)
            Point(xPos - 5.0, -5.0)
            Point(xPos - 5.0, -10.0)
            Point(xPos + 5.0, -10.0)
            Point(xPos + 5.0, -5.0) |]

    member x.EditorHint
        with get() = x.GetValue ChartEditorAdornerLayer.EditorHintProperty :?> ChartEditorHint option
        and set(v : ChartEditorHint option) = x.SetValue(ChartEditorAdornerLayer.EditorHintProperty, box v)
    static member val EditorHintProperty =
        Dp.reg<ChartEditorHint option, ChartEditorAdornerLayer> "EditorHint"
            (Dp.Meta(None, Dp.MetaFlags.AffectsRender, (fun (x : ChartEditorAdornerLayer) -> x.OnEditorHintChanged)))
    member x.OnEditorHintChanged(prevEditorHint, editorHint) =
        match editorHint with
        | Some hint ->
            match hint with
            | HoverNote(utt, note, NoteDragMove) ->
                x.Cursor <- Cursors.Hand
            | HoverNote(utt, note, NoteDragResizeLeft)
            | HoverNote(utt, note, NoteDragResizeRight) ->
                x.Cursor <- Cursors.SizeWE
            | GhostNote note ->
                x.Cursor <- Cursors.Pen
            | _ ->
                x.Cursor <- Cursors.Arrow
        | None ->
            x.Cursor <- Cursors.Arrow

    member x.SelectionBoxOp
        with get() = x.GetValue ChartEditorAdornerLayer.SelectionBoxOpProperty :?> (int64 * int64 * int * int) option
        and set(v : (int64 * int64 * int * int) option) = x.SetValue(ChartEditorAdornerLayer.SelectionBoxOpProperty, box v)
    static member val SelectionBoxOpProperty =
        Dp.reg<(int64 * int64 * int * int) option, ChartEditorAdornerLayer> "SelectionBoxOp"
            (Dp.Meta(None, Dp.MetaFlags.AffectsRender, (fun (x : ChartEditorAdornerLayer) -> x.OnSelectionBoxOpChanged)))
    member x.OnSelectionBoxOpChanged(prevSelBoxOp, selBoxOp) = ()

    override x.OnRender dc =
        let actualWidth = x.ActualWidth
        let actualHeight = x.ActualHeight
        let quarterWidth = x.QuarterWidth
        let keyHeight = x.KeyHeight
        let hOffset = x.HOffsetAnimated
        let vOffset = x.VOffsetAnimated
        let playbackPos = x.CursorPosition
        let editorHint = x.EditorHint
        let selBoxOp = x.SelectionBoxOp

        // playback cursor
        let xPos = pulseToPixel quarterWidth hOffset (float playbackPos)
        if xPos >= 0.0 && xPos <= actualWidth then
            dc.DrawLine(playbackCursorPen, Point(xPos, 0.0), Point(xPos, actualHeight))
            dc.DrawGeometry(Brushes.White, playbackCursorPen, getCursorHeadGeometry xPos)

        // selection box
        match selBoxOp with
        | None -> ()
        | Some(selMinPulse, selMaxPulse, selMinPitch, selMaxPitch) ->
            let selBoxPenThicknessRadius = half selBoxPen.Thickness
            let x0 = pulseToPixel quarterWidth hOffset (float selMinPulse)       |> clamp(0.0 - selBoxPenThicknessRadius)(actualWidth - selBoxPenThicknessRadius)
            let x1 = pulseToPixel quarterWidth hOffset (float(selMaxPulse + 1L)) |> clamp(0.0 + selBoxPenThicknessRadius)(actualWidth + selBoxPenThicknessRadius)
            let y0 = pitchToPixel keyHeight actualHeight vOffset (float selMaxPitch) - half keyHeight |> max(0.0 - selBoxPenThicknessRadius)
            let y1 = pitchToPixel keyHeight actualHeight vOffset (float selMinPitch) + half keyHeight |> min(actualHeight + selBoxPenThicknessRadius)
            dc.DrawRectangle(selBoxBrush, selBoxPen, Rect(x0, y0, x1 - x0, y1 - y0))

        // editor hint
        match editorHint with
        | None -> ()
        | Some hint ->
            match hint with
            // mouse over note
            | HoverNote(utt, note, noteDragType) ->
                let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)

                let noteRect = Rect(x0, yMid - half keyHeight, x1 - x0, keyHeight)
                dc.DrawRectangle(hoverNoteBrush, null, noteRect)

            // mouse over cursor
            | GhostCursor cursorPos ->
                let xPos = pulseToPixel quarterWidth hOffset (float cursorPos)
                if xPos >= 0.0 && xPos <= actualWidth then
                    dc.DrawLine(hoverCursorPen, Point(xPos, 0.0), Point(xPos, actualHeight))
                    dc.DrawGeometry(Brushes.White, hoverCursorPen, getCursorHeadGeometry xPos)

            | GhostNote note ->
                let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch)
                let noteRect = Rect(x0, yMid - half keyHeight, x1 - x0, keyHeight)
                if not note.IsHyphen then
                    dc.DrawRectangle(ghostNoteBrush, null, Rect(0.0, noteRect.Y, actualWidth, noteRect.Height))
                    dc.DrawRectangle(null, ghostNotePen, Rect.Inflate(noteRect, 0.0, 1.5))
                else
                    dc.DrawRectangle(ghostHyphBrush, null, Rect(0.0, noteRect.Y, actualWidth, noteRect.Height))
                    dc.DrawRectangle(null, ghostHyphPen, Rect.Inflate(noteRect, 0.0, 1.5))


