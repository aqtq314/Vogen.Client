namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Runtime.InteropServices
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.Romanization
open Vogen.Client.ViewModel

#nowarn "40"


type ChartMouseEvent =
    | ChartMouseDown of e : MouseButtonEventArgs
    | ChartMouseMove of e : MouseEventArgs
    | ChartMouseRelease of e : MouseEventArgs
    | ChartMouseEnter of e : MouseEventArgs
    | ChartMouseLeave of e : MouseEventArgs

    static member BindEvents push (x : NoteChartEditBase) =
        x.MouseDown.Add(fun e ->
            push(ChartMouseDown e)
            e.Handled <- true)

        x.MouseMove.Add(fun e ->
            push(ChartMouseMove e)
            e.Handled <- true)

        x.LostMouseCapture.Add(fun e ->
            push(ChartMouseRelease e)
            e.Handled <- true)

        x.MouseEnter.Add(fun e ->
            push(ChartMouseEnter e)
            e.Handled <- true)

        x.MouseLeave.Add(fun e ->
            push(ChartMouseLeave e)
            e.Handled <- true)

type NoteChartEditPanelBase() =
    inherit UserControl()

    member x.Quantization
        with get() = x.GetValue NoteChartEditPanelBase.QuantizationProperty :?> int64
        and set(v : int64) = x.SetValue(NoteChartEditPanelBase.QuantizationProperty, box v)
    static member val QuantizationProperty =
        Dp.reg<int64, NoteChartEditPanelBase> "Quantization"
            (Dp.Meta(Midi.ppqn / 2L, Dp.MetaFlags.AffectsRender))

    member x.Snap
        with get() = x.GetValue NoteChartEditPanelBase.SnapProperty :?> bool
        and set(v : bool) = x.SetValue(NoteChartEditPanelBase.SnapProperty, box v)
    static member val SnapProperty =
        Dp.reg<bool, NoteChartEditPanelBase> "Snap"
            (Dp.Meta(true, Dp.MetaFlags.AffectsRender))

    member x.ProgramModel = x.DataContext :?> ProgramModel

    abstract ChartEditor : ChartEditor
    default x.ChartEditor = Unchecked.defaultof<_>
    abstract ChartEditorAdornerLayer : ChartEditorAdornerLayer
    default x.ChartEditorAdornerLayer = Unchecked.defaultof<_>
    abstract RulerGrid : RulerGrid
    default x.RulerGrid = Unchecked.defaultof<_>
    abstract SideKeyboard : SideKeyboard
    default x.SideKeyboard = Unchecked.defaultof<_>
    abstract BgAudioDisplay : BgAudioDisplay
    default x.BgAudioDisplay = Unchecked.defaultof<_>
    abstract HScrollZoom : ChartScrollZoomKitBase
    default x.HScrollZoom = Unchecked.defaultof<_>
    abstract VScrollZoom : ChartScrollZoomKitBase
    default x.VScrollZoom = Unchecked.defaultof<_>
    abstract LyricPopup : TextBoxPopupBase
    default x.LyricPopup = Unchecked.defaultof<_>
    abstract ChartEditorContextMenu : ContextMenu
    default x.ChartEditorContextMenu = Unchecked.defaultof<_>

    member x.PixelToPulse xPos =
        let quarterWidth = x.ChartEditor.QuarterWidth
        let hOffset = x.ChartEditor.HOffsetAnimated
        pixelToPulse quarterWidth hOffset xPos

    member x.PixelToPitch yPos =
        let actualHeight = x.ChartEditor.ActualHeight
        let keyHeight = x.ChartEditor.KeyHeight
        let vOffset = x.ChartEditor.VOffsetAnimated
        pixelToPitch keyHeight actualHeight vOffset yPos

    member x.PulseToPixel pulses =
        let quarterWidth = x.ChartEditor.QuarterWidth
        let hOffset = x.ChartEditor.HOffsetAnimated
        pulseToPixel quarterWidth hOffset pulses

    member x.PitchToPixel pitch =
        let actualHeight = x.ChartEditor.ActualHeight
        let keyHeight = x.ChartEditor.KeyHeight
        let vOffset = x.ChartEditor.VOffsetAnimated
        pitchToPixel keyHeight actualHeight vOffset pitch

    member x.Quantize timeSig pulses =
        let quantization = x.Quantization
        let snap = x.Snap
        quantize snap quantization timeSig pulses

    member x.QuantizeCeil timeSig pulses =
        let quantization = x.Quantization
        let snap = x.Snap
        quantizeCeil snap quantization timeSig pulses

    member x.EditSelectedNoteLyrics() =
        let comp = !!x.ProgramModel.ActiveComp
        let selection = !!x.ProgramModel.ActiveSelection
        match selection.ActiveUtt with
        | None -> ()
        | Some utt ->
            let uttSelectedNotes = utt.Notes |> Seq.filter selection.GetIsNoteSelected |> ImmutableArray.CreateRange
            let uttSelectedLyricNotes = uttSelectedNotes.RemoveAll(fun note -> note.IsHyphen)
            if uttSelectedLyricNotes.Length > 0 then
                let keyHeight = x.ChartEditor.KeyHeight
                let minPulse = uttSelectedLyricNotes.[0].On
                let maxPulse = uttSelectedLyricNotes.[^0].Off
                let minPitch = uttSelectedLyricNotes |> Seq.map(fun note -> note.Pitch) |> Seq.min
                let maxPitch = uttSelectedLyricNotes |> Seq.map(fun note -> note.Pitch) |> Seq.max

                let xMin = x.PulseToPixel(float minPulse)
                let xMax = x.PulseToPixel(float maxPulse)
                let yMin = x.PitchToPixel(float maxPitch) - half keyHeight
                let yMax = x.PitchToPixel(float minPitch) + half keyHeight
                x.LyricPopup.PlacementRectangle <- Rect(xMin, yMin, xMax - xMin, yMax - yMin)
                x.LyricPopup.MinWidth <- xMax - xMin

                let initLyricText =
                    uttSelectedLyricNotes
                    |> Seq.map(fun note ->
                        let lyricOrSpace = if String.IsNullOrEmpty note.Lyric then " " else note.Lyric
                        lyricOrSpace + note.Rom)
                    |> String.concat ""
                    |> fun s -> s.TrimStart()

                let selection = selection.SetSelectedNotes(ImmutableHashSet.CreateRange uttSelectedLyricNotes)
                x.ProgramModel.ActiveSelection |> Rp.set selection

                let undoWriter =
                    x.ProgramModel.UndoRedoStack.BeginPushUndo(
                        EditNoteLyric, (comp, selection))

                let candidateLyricNotes =
                    ImmutableArray.CreateRange(seq {
                        yield! uttSelectedLyricNotes
                        yield! utt.Notes |> Seq.skipWhile((<>) uttSelectedLyricNotes.[^0]) |> Seq.skip 1
                            |> Seq.filter(fun note -> not note.IsHyphen) })

                Task.Run(fun () -> Romanizer.get utt.RomScheme) |> ignore

                let revertChanges() =
                    x.ProgramModel.SetComp(comp, selection)
                    undoWriter.UnpushUndo()

                x.LyricPopup.Open initLyricText revertChanges <| fun lyricText ->
                    let lyricText = lyricText.Trim()

                    let pChar = @"[\u3400-\u4DBF\u4E00-\u9FFF]"
                    let pAlphaNum = @"[A-Za-z0-9]"
                    let pattern = Regex($@"\G\s*(?<ch>{pChar})(?<rom>{pAlphaNum}*)|\G\s*(?<ch>)(?<rom>{pAlphaNum}+)")
                    let matches = pattern.Matches lyricText

                    let matchedCharCount =
                        if matches.Count = 0 then 0 else
                            let lastCapture = matches.[matches.Count - 1]
                            lastCapture.Index + lastCapture.Length
                    if matchedCharCount <> lyricText.Length then
                        // TODO: Error prompt
                        Error()

                    else
                        let noteLyrics = matches |> Seq.map(fun m -> m.Groups.["ch"].Value) |> Array.ofSeq
                        let noteRoms = matches |> Seq.map(fun m -> m.Groups.["rom"].Value) |> Array.ofSeq
                        let noteRoms =
                            (Romanizer.get utt.RomScheme).Convert noteLyrics noteRoms
                            |> Array.map(fun roms -> roms.[0], ImmutableArray.CreateRange roms.[1..])

                        // DiffDict: no existance -> no modification
                        let noteDiffDict =
                            Seq.zip3 candidateLyricNotes noteLyrics noteRoms
                            |> Seq.filter(fun (note, newLyric, (newRom, newMoreRoms)) ->
                                note.Lyric <> newLyric.ToString() || note.Rom <> newRom)
                            |> Seq.map(fun (note, newLyric, (newRom, newMoreRoms)) ->
                                KeyValuePair(note, note.SetText(newLyric.ToString(), newRom, newMoreRoms)))
                            |> ImmutableDictionary.CreateRange

                        if noteDiffDict.Count = 0 then
                            x.ProgramModel.SetComp(
                                comp,
                                selection.SetSelectedNotes(
                                    ImmutableHashSet.CreateRange(
                                        candidateLyricNotes
                                        |> Seq.take(min matches.Count candidateLyricNotes.Length))))
                            undoWriter.UnpushUndo()

                        else
                            let newUtt =
                                utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note ->
                                    noteDiffDict.GetOrDefault note note))
                            let newSelectedNotes =
                                ImmutableHashSet.CreateRange(
                                    candidateLyricNotes
                                    |> Seq.take(min matches.Count candidateLyricNotes.Length)
                                    |> Seq.map(fun note -> noteDiffDict.GetOrDefault note note))

                            x.ProgramModel.SetComp(
                                comp.SetUtts(comp.Utts.Replace(utt, newUtt)),
                                CompSelection(Some newUtt, newSelectedNotes))

                            undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                            x.ProgramModel.CompIsSaved |> Rp.set false

                        Ok()

    member x.CutSelectedNotes() =
        x.CopySelectedNotes()
        x.DeleteSelectedNotes CutNote

    member x.CopySelectedNotes() =
        let comp = !!x.ProgramModel.ActiveComp
        let selection = !!x.ProgramModel.ActiveSelection
        let selection = selection.EnsureIntersectionWith comp
        if selection.SelectedNotes.Count > 0 then
            let minNoteOn = selection.SelectedNotes |> Seq.map(fun note -> note.On) |> Seq.min

            let getClipUtt(utt : Utterance) =
                let selectedNotes = utt.Notes |> Seq.filter selection.GetIsNoteSelected |> ImmutableArray.CreateRange
                if selectedNotes.Length = 0 then None
                else Some(utt.SetNotes selectedNotes)

            let activeClipUttOp = selection.ActiveUtt |> Option.bind getClipUtt

            let otherClipUtts =
                match selection.ActiveUtt with
                | None -> comp.Utts
                | Some activeUtt -> comp.Utts.Remove activeUtt
                |> Seq.choose getClipUtt

            FilePackage.toClipboardText minNoteOn activeClipUttOp otherClipUtts
            |> Clipboard.SetText

    member x.Paste() =
        try let hScrollValue = x.HScrollZoom.ScrollValue
            let comp = !!x.ProgramModel.ActiveComp
            let selection = !!x.ProgramModel.ActiveSelection
            let selection = selection.SetSelectedNotes ImmutableHashSet.Empty

            let clipboardText = Clipboard.GetText()
            let minNoteOn = x.QuantizeCeil comp.TimeSig0 (int64 hScrollValue)
            let activeClipUttOp, otherClipUtts = FilePackage.ofClipboardText minNoteOn clipboardText
            let newSelectedNotes =
                ImmutableHashSet.CreateRange(
                    Seq.append(Option.toArray activeClipUttOp) otherClipUtts
                    |> Seq.collect(fun utt -> utt.Notes))

            let newComp, newSelection =
                match activeClipUttOp, selection.ActiveUtt with
                | Some activeClipUtt, Some activeUtt ->
                    let newActiveUtt = activeUtt.UpdateNotes(fun notes -> notes.AddRange activeClipUtt.Notes)
                    comp.UpdateUtts(fun utts -> utts.Replace(activeUtt, newActiveUtt).AddRange otherClipUtts),
                    CompSelection(Some newActiveUtt, newSelectedNotes)
                | Some activeClipUtt, None ->
                    comp.UpdateUtts(fun utts -> utts.AddRange(Seq.prependItem activeClipUtt otherClipUtts)),
                    CompSelection(Some activeClipUtt, newSelectedNotes)
                | None, _ ->
                    comp.UpdateUtts(fun utts -> utts.AddRange otherClipUtts),
                    CompSelection(None, newSelectedNotes)

            x.ProgramModel.SetComp(newComp, newSelection)

            x.ProgramModel.UndoRedoStack.PushUndo(
                PasteNote, (comp, selection), (!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
            x.ProgramModel.CompIsSaved |> Rp.set false

        with | ex ->
            System.Media.SystemSounds.Exclamation.Play()

    member x.DeleteSelectedNotes undoDesc =
        let comp = !!x.ProgramModel.ActiveComp
        let selection = !!x.ProgramModel.ActiveSelection
        let mouseDownSelection = selection.SelectedNotes.Intersect comp.AllNotes
        if not mouseDownSelection.IsEmpty then
            // DelDict: no existance -> deletion
            let uttDelDict =
                comp.Utts
                |> Seq.choose(fun utt ->
                    let newNotes = utt.Notes.RemoveAll(Predicate(selection.GetIsNoteSelected))
                    if newNotes.Length = 0 then None
                    elif newNotes.Length = utt.Notes.Length then Some(KeyValuePair(utt, utt))
                    else Some(KeyValuePair(utt, utt.SetNotes newNotes)))
                |> ImmutableDictionary.CreateRange
            let newActiveUtt = selection.ActiveUtt |> Option.bind(uttDelDict.TryGetValue >> Option.ofByRef)
            x.ProgramModel.SetComp(
                comp.SetUtts(ImmutableArray.CreateRange uttDelDict.Values),
                CompSelection(newActiveUtt, ImmutableHashSet.Empty))

            x.ProgramModel.UndoRedoStack.PushUndo(
                undoDesc, (comp, selection), (!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
            x.ProgramModel.CompIsSaved |> Rp.set false

    member x.DeleteSelectedNotes() =
        x.DeleteSelectedNotes DeleteNote

    member x.SelectAll() =
        let comp = !!x.ProgramModel.ActiveComp
        x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
            selection.SetSelectedNotes(ImmutableHashSet.CreateRange comp.AllNotes))

    member x.BlurUtt() =
        let selection = !!x.ProgramModel.ActiveSelection
        match selection.ActiveUtt with
        | None -> ()
        | Some _ -> x.ProgramModel.ActiveSelection |> Rp.set(selection.SetActiveUtt None)

    member x.BindBehaviors() =
        let rec mouseMidDownDragging(prevMousePos : Point, idle)(edit : NoteChartEditBase) = behavior {
            match! () with
            | ChartMouseMove e ->
                let hOffset = edit.HOffsetAnimated
                let vOffset = edit.VOffsetAnimated
                let quarterWidth = edit.QuarterWidth
                let keyHeight = edit.KeyHeight

                let mousePos = e.GetPosition edit
                if edit.CanScrollH then
                    let xDelta = pixelToPulse quarterWidth 0.0 (mousePos.X - prevMousePos.X)
                    x.HScrollZoom.EnableAnimation <- false
                    x.HScrollZoom.ScrollValue <- hOffset - xDelta
                    x.HScrollZoom.EnableAnimation <- true
                if edit.CanScrollV then
                    let yDelta = pixelToPitch keyHeight 0.0 0.0 (mousePos.Y - prevMousePos.Y)
                    x.VScrollZoom.EnableAnimation <- false
                    x.VScrollZoom.ScrollValue <- vOffset - yDelta
                    x.VScrollZoom.EnableAnimation <- true

                return! edit |> mouseMidDownDragging(mousePos, idle)

            | ChartMouseRelease e -> return! idle()

            | _ -> return! edit |> mouseMidDownDragging(prevMousePos, idle) }

        let enumerateUttsByDepth(activeUtt, utts : ImmutableArray<Utterance>) =
            match activeUtt with
            | Some activeUtt -> seq {
                yield activeUtt
                for uttIndex in utts.Length - 1 .. -1 .. 0 do
                    if utts.[uttIndex] <> activeUtt then
                        yield utts.[uttIndex] }
            | None -> seq {
                for uttIndex in utts.Length - 1 .. -1 .. 0 do
                    yield utts.[uttIndex] }

        let findMouseOverNote(mousePos : Point) activeUtt utts (edit : ChartEditor) =
            let mousePulse = x.PixelToPulse mousePos.X |> int64
            let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

            let uttsReordered = enumerateUttsByDepth(activeUtt, utts)
            Seq.tryHead <| seq {
                for utt in uttsReordered do
                    for noteIndex in utt.Notes.Length - 1 .. -1 .. 0 do
                        let note = utt.Notes.[noteIndex]
                        if mousePulse |> between note.On note.Off && mousePitch = note.Pitch then
                            yield utt, note }
            |> Option.map(fun (utt, note) ->
                let x0 = x.PulseToPixel (float note.On)
                let x1 = x.PulseToPixel (float note.Off)
                let noteDragType =
                    if   mousePos.X <= min(x0 + 10.0)(lerp x0 x1 0.2) then NoteDragResizeLeft
                    elif mousePos.X >= max(x1 - 10.0)(lerp x0 x1 0.8) then NoteDragResizeRight
                    else NoteDragMove
                utt, note, noteDragType)

        let mouseToCursorPos(mousePos : Point) =
            int64(x.PixelToPulse mousePos.X) |> NoteChartEditBase.CoerceCursorPosition x.RulerGrid

        let hintSetNone() =
            x.ChartEditorAdornerLayer.EditorHint <- None

        let hintSetGhostCursor mousePos =
            let comp = !!x.ProgramModel.ActiveComp
            let cursorPos = mouseToCursorPos mousePos |> x.Quantize comp.TimeSig0
            x.ChartEditorAdornerLayer.EditorHint <- Some(GhostCursor cursorPos)

        let hintSetMouseOverNote mousePos =
            let mouseOverNoteOp =
                let comp = !!x.ProgramModel.ActiveComp
                let selection = !!x.ProgramModel.ActiveSelection
                findMouseOverNote mousePos selection.ActiveUtt comp.Utts x.ChartEditor

            x.ChartEditorAdornerLayer.EditorHint <- Option.map HoverNote mouseOverNoteOp

        let hintSetGhostNote mousePos =
            let edit = x.ChartEditor
            let minKey = edit.MinKey
            let maxKey = edit.MaxKey
            let comp = !!x.ProgramModel.ActiveComp
            let selection = !!x.ProgramModel.ActiveSelection
            let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt ImmutableArray.Empty edit
            let note =
                match mouseDownNoteOp with
                | None ->
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int
                    let noteOn = mousePulse |> x.Quantize comp.TimeSig0 |> max 0L
                    let noteOff = noteOn + 1L |> x.QuantizeCeil comp.TimeSig0
                    let notePitch = mousePitch |> clamp minKey maxKey
                    Note(notePitch, "", "du", noteOn, noteOff - noteOn)

                | Some(mouseDownUtt, mouseDownNote, noteDragType) ->
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let noteOn =
                        mousePulse
                        |> min(mouseDownNote.Off - 1L)
                        |> x.Quantize comp.TimeSig0
                        |> max mouseDownNote.On
                    Note(mouseDownNote.Pitch, "-", "-", noteOn, mouseDownNote.Off - noteOn)

            x.ChartEditorAdornerLayer.EditorHint <- Some(GhostNote note)

        x.ChartEditor |> ChartMouseEvent.BindEvents(
            let edit = x.ChartEditor

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    let keyboardModifiers = Keyboard.Modifiers

                    match e.ChangedButton with
                    | MouseButton.Left when keyboardModifiers.IsAlt ->
                        hintSetNone()
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection

                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt ImmutableArray.Empty edit
                        match mouseDownNoteOp with
                        | None ->
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    WriteNote, (comp, selection.SetSelectedNotes ImmutableHashSet.Empty))

                            let minKey = edit.MinKey
                            let maxKey = edit.MaxKey
                            let mousePulse = x.PixelToPulse mousePos.X |> int64
                            let mousePitch = x.PixelToPitch mousePos.Y |> round |> int
                            let maxNoteOn = mousePulse |> x.Quantize comp.TimeSig0 |> max 0L
                            let minNoteOff = maxNoteOn + 1L |> x.QuantizeCeil comp.TimeSig0

                            let buildNewNote mousePulse mousePitch =
                                let noteOn = min maxNoteOn (mousePulse |> x.Quantize comp.TimeSig0) |> max 0L
                                let noteOff = max minNoteOff (mousePulse |> x.QuantizeCeil comp.TimeSig0)
                                let notePitch = mousePitch |> clamp minKey maxKey
                                Note(notePitch, "", "du", noteOn, noteOff - noteOn)

                            let buildNewComp =
                                match selection.ActiveUtt with
                                | None ->
                                    let singerId = !!x.ProgramModel.UttPanelSingerId
                                    let romScheme = !!x.ProgramModel.UttPanelRomScheme
                                    fun note ->
                                        let utt = Utterance(singerId, romScheme, ImmutableArray.Create(note : Note))
                                        utt, comp.UpdateUtts(fun utts -> utts.Add utt)
                                | Some activeUtt ->
                                    fun note ->
                                        let utt = activeUtt.UpdateNotes(fun notes -> notes.Add note)
                                        utt, comp.UpdateUtts(fun utts -> utts.Replace(activeUtt, utt))

                            let note = buildNewNote mousePulse mousePitch
                            let utt, comp = buildNewComp note
                            x.ProgramModel.SetComp(comp, CompSelection(Some utt, ImmutableHashSet.Create note))

                            MidiPlayback.playPitch note.Pitch
                            undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                            x.ProgramModel.CompIsSaved |> Rp.set false

                            let writeNoteArgs = buildNewNote, buildNewComp, note, undoWriter
                            return! writingNote writeNoteArgs

                        | Some(mouseDownUtt, mouseDownNote, noteDragType) ->
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    WriteHyphenNote, (comp, selection.SetSelectedNotes(ImmutableHashSet.Create mouseDownNote)))

                            let minKey = edit.MinKey
                            let maxKey = edit.MaxKey
                            let mousePulse = x.PixelToPulse mousePos.X |> int64

                            let buildNewNote mousePulse mousePitch =
                                let noteOn =
                                    mousePulse
                                    |> min(mouseDownNote.Off - 1L)
                                    |> x.Quantize comp.TimeSig0
                                    |> max mouseDownNote.On
                                let notePitch = mousePitch |> clamp minKey maxKey
                                Note(notePitch, "-", "-", noteOn, mouseDownNote.Off - noteOn)

                            let buildNewComp(note : Note) =
                                let utt =
                                    if note.On = mouseDownNote.On then
                                        mouseDownUtt.UpdateNotes(fun notes -> notes.Replace(mouseDownNote, note))
                                    else
                                        mouseDownUtt.UpdateNotes(fun notes ->
                                            notes.Remove(mouseDownNote).AddRange([| mouseDownNote.SetOff note.On; note |]))
                                utt, comp.UpdateUtts(fun utts -> utts.Replace(mouseDownUtt, utt))

                            let note = buildNewNote mousePulse mouseDownNote.Pitch
                            let utt, comp = buildNewComp note
                            x.ProgramModel.SetComp(comp, CompSelection(Some utt, ImmutableHashSet.Create note))

                            MidiPlayback.playPitch note.Pitch
                            undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                            x.ProgramModel.CompIsSaved |> Rp.set false

                            let writeNoteArgs = buildNewNote, buildNewComp, note, undoWriter
                            return! writingNote writeNoteArgs

                    | MouseButton.Left ->
                        hintSetNone()
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt comp.Utts edit
                        match mouseDownNoteOp with
                        | None ->
                            if e.ClickCount = 2 then
                                x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                    selection.SetActiveUtt None)

                            match keyboardModifiers with
                            | ModifierKeys.Control -> ()
                            | _ ->
                                x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                    selection.SetSelectedNotes ImmutableHashSet.Empty)

                            let mouseDownSelection = !!x.ProgramModel.ActiveSelection
                            return! draggingSelBox mouseDownSelection mousePos

                        | Some(utt, note, noteDragType) ->
                            let mousePulse = x.PixelToPulse mousePos.X |> int64
                            let mouseDownPulse =
                                match noteDragType with
                                | NoteDragResizeLeft
                                | NoteDragMove ->
                                    let noteGridDeviation = note.On - (note.On |> x.Quantize comp.TimeSig0)
                                    (mousePulse - noteGridDeviation |> x.Quantize comp.TimeSig0) + noteGridDeviation
                                | NoteDragResizeRight ->
                                    let noteGridDeviation = note.Off - (note.Off |> x.QuantizeCeil comp.TimeSig0)
                                    (mousePulse - noteGridDeviation |> x.QuantizeCeil comp.TimeSig0) + noteGridDeviation

                            let pendingDeselectNotes, selection =
                                if e.ClickCount >= 2 then
                                    let targetNotes =
                                        if e.ClickCount >= 3 then utt.Notes :> seq<_> else
                                            utt.Notes
                                            |> Seq.partitionBeforeWhen(fun note -> not note.IsHyphen)
                                            |> Seq.find(fun notes -> notes |> Array.contains note) :> seq<_>
                                    let pendingDeselectNotes = if selection.GetIsNoteSelected note then Seq.empty else targetNotes
                                    pendingDeselectNotes, selection.UpdateSelectedNotes(fun selectedNotes -> selectedNotes.Union targetNotes)

                                else
                                    match selection.GetIsNoteSelected note, keyboardModifiers with
                                    | true, ModifierKeys.Control ->
                                        seq { note }, selection
                                    | true, _ ->
                                        Seq.empty, selection
                                    | false, ModifierKeys.Control ->
                                        Seq.empty, selection.UpdateSelectedNotes(fun selectedNotes -> selectedNotes.Add note)
                                    | false, _ ->
                                        Seq.empty, selection.SetSelectedNotes(ImmutableHashSet.Create note)

                            let selection = selection.SetActiveUtt(Some utt).EnsureIntersectionWith comp
                            x.ProgramModel.ActiveSelection |> Rp.set selection

                            MidiPlayback.playPitch note.Pitch
                            let undoWriter =
                                x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                    MouseDragNote noteDragType, (comp, selection))

                            let dragNoteArgs = note, comp, selection, mouseDownPulse, note, noteDragType, undoWriter
                            return! mouseDownNotePendingDeselect pendingDeselectNotes dragNoteArgs

                    | MouseButton.Middle ->
                        hintSetNone()
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt comp.Utts edit
                        match mouseDownNoteOp with
                        | None when e.ClickCount = 2 ->
                            x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                selection.SetActiveUtt None)

                        | Some(utt, note, noteDragType) ->
                            x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                selection.SetActiveUtt(Some utt))

                        | _ -> ()

                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)

                    | MouseButton.Right ->
                        // Context menu preparations
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let mousePos = e.GetPosition edit
                        let mouseDownNoteOp = findMouseOverNote mousePos selection.ActiveUtt comp.Utts edit
                        match mouseDownNoteOp with
                        | None ->
                            match keyboardModifiers with
                            | ModifierKeys.Control -> ()
                            | _ ->
                                x.ProgramModel.ActiveSelection |> Rp.modify(fun selection ->
                                    selection.SetSelectedNotes ImmutableHashSet.Empty)

                        | Some(utt, note, noteDragType) ->
                            let selection =
                                match selection.GetIsNoteSelected note, keyboardModifiers with
                                | true, _ ->
                                    selection
                                | false, ModifierKeys.Control ->
                                    selection.UpdateSelectedNotes(fun selectedNotes -> selectedNotes.Add note)
                                | false, _ ->
                                    selection.SetSelectedNotes(ImmutableHashSet.Create note)

                            let selection = selection.SetActiveUtt(Some utt).EnsureIntersectionWith comp
                            x.ProgramModel.ActiveSelection |> Rp.set selection

                            if not note.IsHyphen then
                                let romMenuItems = List()
                                do for i in 0 .. note.MoreRoms.Length - 1 do
                                    let rom = note.MoreRoms.[i]
                                    let romMenuItem = MenuItem(Header = TextResources.getContextMenuSetRom note.Lyric rom)
                                    romMenuItem.Click.Add(fun e ->
                                        let comp = !!x.ProgramModel.ActiveComp

                                        let newMoreRoms = ImmutableArray.CreateRange(Seq.prependItem note.Rom (note.MoreRoms.Remove rom))
                                        let newNote = note.SetText(note.Lyric, rom, newMoreRoms)
                                        let newUtt = utt.SetNotes(utt.Notes.Replace(note, newNote))

                                        x.ProgramModel.SetComp(
                                            comp.SetUtts(comp.Utts.Replace(utt, newUtt)),
                                            CompSelection(Some newUtt, ImmutableHashSet.Create newNote))

                                        x.ProgramModel.UndoRedoStack.PushUndo(
                                            SetNoteRomContextMenu,
                                            (comp, CompSelection(Some utt, ImmutableHashSet.Create note)),
                                            (!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                                        x.ProgramModel.CompIsSaved |> Rp.set false)

                                    x.ChartEditorContextMenu.Items.Insert(i, romMenuItem)
                                    romMenuItems.Add romMenuItem

                                let rec eventUnsubscriber =
                                    [| contextMenuClosedSubscriber |] 
                                    |> Disposable.join id

                                and contextMenuClosedSubscriber = x.ChartEditorContextMenu.Closed.Subscribe(fun e ->
                                    for romMenuItem in romMenuItems do
                                        x.ChartEditorContextMenu.Items.Remove romMenuItem
                                    eventUnsubscriber |> Disposable.dispose)

                                ()

                        return! idle()

                    | _ -> return! idle()

                | ChartMouseMove e ->
                    let keyboardModifiers = Keyboard.Modifiers

                    if keyboardModifiers.IsAlt then
                        hintSetGhostNote(e.GetPosition edit)
                    else
                        hintSetMouseOverNote(e.GetPosition edit)

                    return! idle()

                | _ -> return! idle() }

            and mouseDownNotePendingDeselect pendingDeselectNotes dragNoteArgs = behavior {
                let mouseDownNote, mouseDownComp, mouseDownSelection, mouseDownPulse, prevNote, noteDragType, undoWriter = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    return! (draggingNote dragNoteArgs : _ BehaviorAction).Run(ChartMouseMove e)

                | ChartMouseRelease e ->
                    x.ProgramModel.ActiveSelection |> Rp.set(
                        mouseDownSelection.UpdateSelectedNotes(fun selectedNotes ->
                            selectedNotes.Except pendingDeselectNotes))
                    return! (draggingNote dragNoteArgs).Run(ChartMouseRelease e)

                | _ -> return! mouseDownNotePendingDeselect pendingDeselectNotes dragNoteArgs }

            and writingNote(buildNewNote, buildNewComp, prevNote, undoWriter as writeNoteArgs) = behavior {
                match! () with
                | ChartMouseMove e ->
                    let mousePos = e.GetPosition edit
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

                    let note = buildNewNote mousePulse mousePitch

                    if (prevNote.On, prevNote.Off, prevNote.Pitch) <> (note.On, note.Off, note.Pitch) then
                        let utt, comp = buildNewComp note

                        x.ProgramModel.SetComp(comp, CompSelection(Some utt, ImmutableHashSet.Create note))

                        MidiPlayback.switchPitch prevNote.Pitch note.Pitch
                        undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                        x.ProgramModel.CompIsSaved |> Rp.set false

                        return! writingNote(buildNewNote, buildNewComp, note, undoWriter)

                    else
                        return! writingNote writeNoteArgs

                | ChartMouseRelease e ->
                    MidiPlayback.stopPitch prevNote.Pitch
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! writingNote writeNoteArgs }

            and draggingNote dragNoteArgs = behavior {
                let mouseDownNote, comp, mouseDownSelection, mouseDownPulse, prevNote, noteDragType, undoWriter = dragNoteArgs
                match! () with
                | ChartMouseMove e ->
                    let minKey = edit.MinKey
                    let maxKey = edit.MaxKey
                    let mousePos = e.GetPosition edit
                    let mousePulse = x.PixelToPulse mousePos.X |> int64
                    let mousePitch = x.PixelToPitch mousePos.Y |> round |> int

                    let newNoteOn =
                        match noteDragType with
                        | NoteDragResizeLeft
                        | NoteDragMove ->
                            mouseDownNote.On + mousePulse - mouseDownPulse |> x.Quantize comp.TimeSig0
                        | NoteDragResizeRight ->
                            mouseDownNote.Off + mousePulse - mouseDownPulse |> x.QuantizeCeil comp.TimeSig0

                    let deltaPulse, deltaDur =
                        let selMinPulse = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.On) |> Seq.min
                        let selMinDur   = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.Dur) |> Seq.min
                        match noteDragType with
                        | NoteDragResizeLeft ->
                            let minOn = mouseDownNote.On - selMinPulse
                            let maxOn = mouseDownNote.On + selMinDur - 1L |> x.Quantize comp.TimeSig0
                            let deltaPulse = (newNoteOn |> clamp minOn maxOn) - mouseDownNote.On
                            deltaPulse, -deltaPulse
                        | NoteDragMove ->
                            let minOn = mouseDownNote.On - selMinPulse
                            (newNoteOn |> max minOn) - mouseDownNote.On, 0L
                        | NoteDragResizeRight ->
                            let minOff = mouseDownNote.Off - selMinDur + 1L |> x.QuantizeCeil comp.TimeSig0
                            0L, (newNoteOn |> max minOff) - mouseDownNote.Off

                    let deltaPitch =
                        match noteDragType with
                        | NoteDragResizeLeft
                        | NoteDragResizeRight -> 0
                        | NoteDragMove ->
                            let mouseDownSelMinPitch = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.Pitch) |> Seq.min
                            let mouseDownSelMaxPitch = mouseDownSelection.SelectedNotes |> Seq.map(fun note -> note.Pitch) |> Seq.max
                            mousePitch - mouseDownNote.Pitch |> clamp(minKey - mouseDownSelMinPitch)(maxKey - mouseDownSelMaxPitch)

                    let note = mouseDownNote.MoveDelta(deltaPitch, deltaPulse, deltaDur)

                    if (prevNote.On, prevNote.Off, prevNote.Pitch) <> (note.On, note.Off, note.Pitch) then
                        if deltaPulse = 0L && deltaDur = 0L && deltaPitch = 0 then
                            x.ProgramModel.SetComp(comp, mouseDownSelection)
                            MidiPlayback.switchPitch prevNote.Pitch note.Pitch
                            undoWriter.UnpushUndo()

                        else
                            // DiffDict: no existance -> no modification
                            let noteDiffDict = mouseDownSelection.SelectedNotes.ToImmutableDictionary(id, fun (note : Note) ->
                                note.MoveDelta(deltaPitch, deltaPulse, deltaDur))

                            let uttDiffDict = comp.Utts.ToImmutableDictionary(id, fun (utt : Utterance) ->
                                if utt.Notes |> Seq.forall(fun note -> not(noteDiffDict.ContainsKey note)) then utt else
                                    utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> noteDiffDict.GetOrDefault note note)))

                            let newComp = comp.SetUtts(ImmutableArray.CreateRange(comp.Utts, fun utt -> uttDiffDict.GetOrDefault utt utt))
                            let activeUtt = mouseDownSelection.ActiveUtt |> Option.map(fun utt -> uttDiffDict.GetOrDefault utt utt)
                            let selectedNotes = ImmutableHashSet.CreateRange noteDiffDict.Values
                            x.ProgramModel.SetComp(newComp, CompSelection(activeUtt, selectedNotes))

                            MidiPlayback.switchPitch prevNote.Pitch note.Pitch
                            undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                            x.ProgramModel.CompIsSaved |> Rp.set false

                        return! draggingNote(mouseDownNote, comp, mouseDownSelection, mouseDownPulse, note, noteDragType, undoWriter)

                    else
                        return! draggingNote dragNoteArgs

                | ChartMouseRelease e ->
                    MidiPlayback.stopPitch prevNote.Pitch
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! draggingNote dragNoteArgs }

            and draggingSelBox mouseDownSelection mouseDownPos = behavior {
                match! () with
                | ChartMouseMove e ->
                    let comp = !!x.ProgramModel.ActiveComp
                    let mousePos = e.GetPosition edit

                    let selMinPulse = x.PixelToPulse (min mousePos.X mouseDownPos.X) |> int64
                    let selMaxPulse = x.PixelToPulse (max mousePos.X mouseDownPos.X) |> int64
                    let selMinPitch = x.PixelToPitch (max mousePos.Y mouseDownPos.Y) |> round |> int
                    let selMaxPitch = x.PixelToPitch (min mousePos.Y mouseDownPos.Y) |> round |> int
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- Some(selMinPulse, selMaxPulse, selMinPitch, selMaxPitch)

                    let selection =
                        mouseDownSelection.SetSelectedNotes(
                            comp.AllNotes
                            |> Seq.filter(fun note ->
                                let noteHasIntersection =
                                    note.On <= selMaxPulse && note.Off >= selMinPulse && note.Pitch |> betweenInc selMinPitch selMaxPitch
                                noteHasIntersection <> mouseDownSelection.GetIsNoteSelected note)
                            |> ImmutableHashSet.CreateRange)
                    x.ProgramModel.ActiveSelection |> Rp.set selection

                    return! draggingSelBox mouseDownSelection mouseDownPos

                | ChartMouseRelease e ->
                    x.ChartEditorAdornerLayer.SelectionBoxOp <- None
                    hintSetMouseOverNote(e.GetPosition edit)
                    return! idle()

                | _ -> return! draggingSelBox mouseDownSelection mouseDownPos }

            Behavior.agent(idle()))

        x.RulerGrid |> ChartMouseEvent.BindEvents(
            let edit = x.RulerGrid

            let playNoteMidi(note : Note) = MidiPlayback.playPitch note.Pitch
            let stopNoteMidi(note : Note) = MidiPlayback.stopPitch note.Pitch

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        hintSetNone()
                        let comp = !!x.ProgramModel.ActiveComp
                        let playbackPos = mouseToCursorPos(e.GetPosition edit)

                        match Keyboard.Modifiers with
                        | ModifierKeys.Control when not !!x.ProgramModel.IsPlaying ->
                            let scrubNotes =
                                comp.AllNotes
                                |> Seq.filter(fun note -> note.On <= playbackPos && note.Off > playbackPos)
                                |> ImmutableHashSet.CreateRange

                            scrubNotes |> Seq.iter playNoteMidi
                            x.ProgramModel.ManualSetCursorPos playbackPos
                            return! mouseLeftDown true (Some scrubNotes)

                        | ModifierKeys.Control ->
                            x.ProgramModel.ManualSetCursorPos playbackPos
                            return! mouseLeftDown true None

                        | _ ->
                            let playbackPos = playbackPos |> x.Quantize comp.TimeSig0

                            x.ProgramModel.ManualSetCursorPos playbackPos
                            return! mouseLeftDown false None

                    | MouseButton.Middle ->
                        hintSetNone()
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)

                    | _ -> return! idle()

                | ChartMouseMove e ->
                    hintSetGhostCursor(e.GetPosition edit)
                    return! idle()

                | ChartMouseLeave e ->
                    hintSetNone()
                    return! idle()

                | _ -> return! idle() }

            and mouseLeftDown forceNoQuantize scrubNotesOp = behavior {
                match! () with
                | ChartMouseMove e ->
                    let comp = !!x.ProgramModel.ActiveComp
                    let playbackPos = mouseToCursorPos(e.GetPosition edit)
                    match scrubNotesOp with
                    | Some prevScrubPitches ->
                        let scrubNotes =
                            comp.AllNotes
                            |> Seq.filter(fun note -> note.On <= playbackPos && note.Off > playbackPos)
                            |> ImmutableHashSet.CreateRange

                        prevScrubPitches.Except scrubNotes |> Seq.iter stopNoteMidi
                        scrubNotes.Except prevScrubPitches |> Seq.iter playNoteMidi
                        x.ProgramModel.ManualSetCursorPos playbackPos
                        return! mouseLeftDown forceNoQuantize (Some scrubNotes)

                    | None ->
                        let playbackPos =
                            if forceNoQuantize then playbackPos else
                                playbackPos |> x.Quantize comp.TimeSig0

                        x.ProgramModel.ManualSetCursorPos playbackPos
                        return! mouseLeftDown forceNoQuantize scrubNotesOp

                | ChartMouseRelease e ->
                    match scrubNotesOp with
                    | Some scrubNotes ->
                        scrubNotes |> Seq.iter stopNoteMidi
                    | None -> ()

                    hintSetGhostCursor(e.GetPosition edit)
                    return! idle()

                | _ -> return! mouseLeftDown forceNoQuantize scrubNotesOp }

            Behavior.agent(idle()))

        x.SideKeyboard |> ChartMouseEvent.BindEvents(
            let edit = x.SideKeyboard

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Middle ->
                        return! edit |> mouseMidDownDragging(e.GetPosition edit, idle)
                    | _ -> return! idle()
                | _ -> return! idle() }

            Behavior.agent(idle()))

        x.BgAudioDisplay |> ChartMouseEvent.BindEvents(
            let edit = x.BgAudioDisplay

            let rec idle() = behavior {
                match! () with
                | ChartMouseDown e ->
                    match e.ChangedButton with
                    | MouseButton.Left ->
                        let comp = !!x.ProgramModel.ActiveComp
                        let selection = !!x.ProgramModel.ActiveSelection
                        let mouseDownPos = e.GetPosition edit

                        let undoWriter =
                            x.ProgramModel.UndoRedoStack.BeginPushUndo(
                                MoveBgAudio, (comp, selection))

                        return! mouseDragging mouseDownPos comp selection undoWriter

                    | _ -> return! idle()

                | _ -> return! idle() }

            and mouseDragging mouseDownPos comp selection undoWriter = behavior {
                match! () with
                | ChartMouseMove e ->
                    let mousePos = e.GetPosition edit
                    let mouseDownSamplePos = mouseDownPos.X |> x.PixelToPulse |> Audio.pulseToSample comp.Bpm0
                    let mouseSamplePos     = mousePos.X     |> x.PixelToPulse |> Audio.pulseToSample comp.Bpm0

                    let newComp = comp.UpdateBgAudioOffset(fun sampleOffset ->
                        sampleOffset - mouseDownSamplePos + mouseSamplePos)
                    x.ProgramModel.SetComp(newComp, !!x.ProgramModel.ActiveSelection)

                    undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                    x.ProgramModel.CompIsSaved |> Rp.set false

                    return! mouseDragging mouseDownPos comp selection undoWriter

                | ChartMouseRelease e ->
                    return! idle()

                | _ ->
                    return! mouseDragging mouseDownPos comp selection undoWriter }

            Behavior.agent(idle()))

        // mouse wheel events
        let onMouseWheel(edit : NoteChartEditBase)(e : MouseWheelEventArgs) =
            if edit.CanScrollH then
                let zoomDelta = float(sign e.Delta) * 0.2       // TODO Use Slider.SmallChange
                let log2Zoom = x.HScrollZoom.Log2ZoomValue
                let log2ZoomMin = x.HScrollZoom.Log2ZoomMinimum
                let log2ZoomMax = x.HScrollZoom.Log2ZoomMaximum
                let newLog2Zoom = log2Zoom + zoomDelta |> clamp log2ZoomMin log2ZoomMax
                let mousePos = e.GetPosition edit
                let xPos = mousePos.X
                let hScrollValue = x.HScrollZoom.ScrollValue
                let quarterWidth = 2.0 ** log2Zoom
                let newQuarterWidth = 2.0 ** newLog2Zoom
                let currPulse = pixelToPulse quarterWidth hScrollValue xPos
                let nextPulse = pixelToPulse newQuarterWidth hScrollValue xPos
                let offsetDelta = nextPulse - currPulse

                x.HScrollZoom.Log2ZoomValue <- newLog2Zoom
                x.HScrollZoom.ScrollValue <- hScrollValue - offsetDelta

            elif edit.CanScrollV then
                let zoomDelta = float(sign e.Delta) * 0.1       // TODO Use Slider.SmallChange
                let log2Zoom = x.VScrollZoom.Log2ZoomValue
                let log2ZoomMin = x.VScrollZoom.Log2ZoomMinimum
                let log2ZoomMax = x.VScrollZoom.Log2ZoomMaximum
                let newLog2Zoom = log2Zoom + zoomDelta |> clamp log2ZoomMin log2ZoomMax
                let mousePos = e.GetPosition edit
                let yPos = mousePos.Y
                let vScrollValue = x.VScrollZoom.ScrollValue
                let keyHeight = 2.0 ** log2Zoom
                let newKeyHeight = 2.0 ** newLog2Zoom
                let actualHeight = x.ChartEditor.ActualHeight
                let currPitch = pixelToPitch keyHeight actualHeight vScrollValue yPos
                let nextPitch = pixelToPitch newKeyHeight actualHeight vScrollValue yPos
                let offsetDelta = nextPitch - currPitch

                x.VScrollZoom.Log2ZoomValue <- newLog2Zoom
                x.VScrollZoom.ScrollValue <- vScrollValue - offsetDelta

        x.ChartEditor.MouseWheel.Add(onMouseWheel x.ChartEditor)
        x.RulerGrid.MouseWheel.Add(onMouseWheel x.RulerGrid)
        x.SideKeyboard.MouseWheel.Add(onMouseWheel x.SideKeyboard)
        x.BgAudioDisplay.MouseWheel.Add(onMouseWheel x.SideKeyboard)

        // playback cursor
        x.ChartEditor.CursorPositionChanged.Add <| fun (prevPlayPos, playPos) ->
            let edit = x.ChartEditor
            if edit.IsPlaying then
                let quarterWidth = edit.QuarterWidth
                let hOffset = x.HScrollZoom.ScrollValue
                let actualWidth = edit.ActualWidth
                let hRightOffset = pixelToPulse quarterWidth hOffset actualWidth
                if float prevPlayPos < hRightOffset && float playPos >= hRightOffset then
                    x.HScrollZoom.ScrollValue <- hOffset + (hRightOffset - hOffset) * 0.9

        // key events
        x.KeyDown.Add <| fun e ->
            x.ChartEditorAdornerLayer.InvalidateVisual()


