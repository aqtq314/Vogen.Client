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
open Vogen.Client.JsonModels


type Note = {
    [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
    [<JsonProperty("lyric", Required=Required.Always)>] Lyric : string
    [<JsonProperty("rom", Required=Required.Always)>]   Rom : string
    [<JsonProperty("on", Required=Required.Always)>]    On : int64
    [<JsonProperty("dur", Required=Required.Always)>]   Dur : int64 }

type Utterance = {
    [<JsonProperty("notes", Required=Required.Always)>] Notes : ImmutableList<Note> }

type Composition = {
    [<JsonProperty("utts", Required=Required.Always)>]  Utts : ImmutableList<Utterance> }

type Note with
    [<JsonIgnore>] member x.Off = x.On + x.Dur

type Composition with
    static member Empty = { Utts = ImmutableList.Empty }

type NoteDisplay() =
    inherit FrameworkElement()

    static let noteBgPen = Pen(SolidColorBrush((0xFFFFBB77u).AsColor()), 3.0) |>! freeze

    static member val CompositionProperty =
        Dp.reg<Composition, NoteDisplay> "Composition"
            (Dp.Meta(Composition.Empty, Dp.MetaFlags.AffectsRender))

    member x.Composition
        with get() = x.GetValue NoteDisplay.CompositionProperty :?> Composition
        and set(v : Composition) = x.SetValue(NoteDisplay.CompositionProperty, box v)

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

        let minPulse = pixelToPulse quarterWidth hOffset 0.0 |> int64
        let maxPulse = pixelToPulse quarterWidth hOffset actualWidth |> ceil |> int64
        let botPitch = pixelToPitch keyHeight actualHeight vOffset actualHeight |> int
        let topPitch = pixelToPitch keyHeight actualHeight vOffset 0.0 |> ceil |> int

        let comp = x.Composition
        for utt in comp.Utts do
            for note in utt.Notes do
                if note.Off >= minPulse && note.On <= maxPulse && note.Pitch >= botPitch && note.Pitch <= topPitch then
                    let x0 = pulseToPixel quarterWidth hOffset (float note.On)
                    let x1 = pulseToPixel quarterWidth hOffset (float note.Off)
                    let yMid = pitchToPixel keyHeight actualHeight vOffset (float note.Pitch + 0.5)
                    dc.DrawLine(noteBgPen, new Point(x0, yMid), new Point(x1, yMid))
                    dc.DrawEllipse(noteBgPen.Brush, null, new Point(x0, yMid), 5.0, 5.0)
                    if note.Lyric <> "-" then
                        let ft = x |> makeFormattedText note.Lyric
                        dc.DrawText(ft, new Point(x0, yMid - ft.Height))



