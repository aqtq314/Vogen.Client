namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open FSharp.Linq
open Microsoft.Win32
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.ViewModel

#nowarn "40"


type MainWindowBase() =
    inherit Window()

    static member AppName = "未来虚拟唱歌人训练营"

    static member WindowTitleConverter = ValueConverter.CreateMulti(fun vs ->
        match vs with
        | [| fileName; isSaved |] ->
            let fileName = fileName.ToString()
            let isSaved = Convert.ToBoolean isSaved
            let notSavedStar = if isSaved then "" else "*"
            $"{notSavedStar}{fileName} - {MainWindowBase.AppName}"
        | _ ->
            raise(ArgumentException()))

    member x.ProgramModel = x.DataContext :?> ProgramModel

    abstract TempoPopup : Popup
    default x.TempoPopup = Unchecked.defaultof<_>
    abstract TempoTextBox : TextBox
    default x.TempoTextBox = Unchecked.defaultof<_>

    override x.OnClosing e =
        match x.AskSaveChanges() with
        | Ok() -> ()
        | Error() -> e.Cancel <- true
        base.OnClosing e

    member x.ShowError(ex : #exn) =
        MessageBox.Show(
            x, $"Error saving file: {ex.Message}\r\n\r\n{ex.StackTrace}", MainWindowBase.AppName,
            MessageBoxButton.OK, MessageBoxImage.Error)
        |> ignore

    member x.SaveAs() = result {
        try let saveFileDialog =
                SaveFileDialog(
                    FileName = (!!x.ProgramModel.CompFilePathOp |> Option.defaultValue !!x.ProgramModel.CompFileName),
                    DefaultExt = ".vog",
                    Filter = "Vogen Package|*.vog")
            let dialogResult = saveFileDialog.ShowDialog x
            if dialogResult ?= true then
                let filePath = saveFileDialog.FileName
                x.ProgramModel.Save filePath
            else
                return! Error()
        with ex ->
            x.ShowError ex
            return! Error() }

    member x.Save() = result {
        match !!x.ProgramModel.CompFilePathOp with
        | None ->
            return! x.SaveAs()
        | Some filePath ->
            try x.ProgramModel.Save filePath
            with ex ->
                x.ShowError ex
                return! x.SaveAs() }

    member x.AskSaveChanges() = result {
        if not !!x.ProgramModel.CompIsSaved then
            let messageBoxResult =
                MessageBox.Show(
                    x, $"Save changes to {!!x.ProgramModel.CompFileName}?", MainWindowBase.AppName,
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes)
            match messageBoxResult with
            | MessageBoxResult.Yes -> return! x.Save()
            | MessageBoxResult.No -> ()
            | _ -> return! Error() }

    member x.New() = result {
        do! x.AskSaveChanges()
        x.ProgramModel.New() }

    member x.Open() = result {
        do! x.AskSaveChanges()
        try let openFileDialog =
                OpenFileDialog(
                    DefaultExt = ".vog",
                    Filter = "Vogen Package|*.vog")
            let dialogResult = openFileDialog.ShowDialog x
            if dialogResult ?= true then
                let filePath = openFileDialog.FileName
                x.ProgramModel.Open filePath
            else
                return! Error()
        with ex ->
            x.ShowError ex
            return! Error() }

    member x.Import() = result {
        do! x.AskSaveChanges()
        try let openFileDialog =
                OpenFileDialog(
                    Filter = "Supported file formats|*.vog;*.vpr")
            let dialogResult = openFileDialog.ShowDialog x
            if dialogResult ?= true then
                let filePath = openFileDialog.FileName
                x.ProgramModel.Import filePath
            else
                return! Error()
        with ex ->
            x.ShowError ex
            return! Error() }

    member x.Export() : Result<unit, unit> = result {
        let saveFileDialog =
            let defaultFileName = !!x.ProgramModel.CompFilePathOp |> Option.defaultValue !!x.ProgramModel.CompFileName
            let defaultFileName = Path.GetFileNameWithoutExtension defaultFileName + ".m4a"
            SaveFileDialog(
                FileName = defaultFileName,
                DefaultExt = ".m4a",
                Filter = "MP4 Audio|*.m4a")
        let dialogResult = saveFileDialog.ShowDialog x
        if dialogResult ?= true then
            let filePath = saveFileDialog.FileName
            x.ProgramModel.Export filePath }

    member x.EditTempo() =
        let comp = !!x.ProgramModel.ActiveComp
        let selection = !!x.ProgramModel.ActiveSelection
        let tempo = comp.Bpm0

        //x.TempoPopup.PlacementRectangle <- Rect(xMin, yMin, xMax - xMin, yMax - yMin)
        //x.TempoPopup.MinWidth <- xMax - xMin
        if Mouse.Captured <> null then 
            Mouse.Captured.ReleaseMouseCapture()
        x.TempoPopup.IsOpen <- true

        let initText = tempo.ToString()
        x.TempoTextBox.Text <- initText
        x.TempoTextBox.SelectAll()
        x.TempoTextBox.Focus() |> ignore

        //let selection = selection.SetSelectedNotes(ImmutableHashSet.CreateRange uttSelectedLyricNotes)
        //x.ProgramModel.ActiveSelection |> Rp.set selection

        let undoWriter =
            x.ProgramModel.UndoRedoStack.BeginPushUndo(
                EditCompPanelValue, (comp, selection))

        //let candidateLyricNotes =
        //    ImmutableArray.CreateRange(seq {
        //        yield! uttSelectedLyricNotes
        //        yield! utt.Notes |> Seq.skipWhile((<>) uttSelectedLyricNotes.[^0]) |> Seq.skip 1
        //            |> Seq.filter(fun note -> not note.IsHyphen) })

        //Task.Run(fun () -> Romanizer.get utt.RomScheme) |> ignore

        let rec eventUnsubscriber =
            [| textChangeSubscriber; keyDownSubscriber; popupClosedSubscriber |] 
            |> Disposable.join id

        and textChangeSubscriber = x.TempoTextBox.TextChanged.Subscribe(fun e ->
            let tempoText = x.TempoTextBox.Text.Trim()
            let newTempoOp = tempoText |> Double.TryParse |> Option.ofByRef

            match newTempoOp with
            | Some newTempo when newTempo |> betweenInc 40.0 300.0 ->
                if tempo = newTempo then
                    x.ProgramModel.SetComp(comp, selection)
                    undoWriter.UnpushUndo()

                else
                    let uttDiffDict = comp.Utts.ToImmutableDictionary(id, fun (utt : Utterance) -> utt.Copy())
                    let newComp = comp.SetBpm(newTempo).SetUtts(ImmutableArray.CreateRange(comp.Utts, fun utt -> uttDiffDict.[utt]))
                    let newSelection = selection.SetActiveUtt(selection.ActiveUtt |> Option.map(fun utt -> uttDiffDict.[utt]))

                    x.ProgramModel.SetComp(newComp, newSelection)
                    undoWriter.PutRedo((!!x.ProgramModel.ActiveComp, !!x.ProgramModel.ActiveSelection))
                    x.ProgramModel.CompIsSaved |> Rp.set false
            | _ ->
                // TODO: Error prompt
                ())

        and keyDownSubscriber = x.TempoTextBox.KeyDown.Subscribe(fun e ->
            match e.Key with
            | Key.Enter ->
                x.TempoPopup.IsOpen <- false
                x.Focus() |> ignore
                e.Handled <- true

            | Key.Escape ->
                x.ProgramModel.SetComp(comp, selection)
                undoWriter.UnpushUndo()
                x.TempoPopup.IsOpen <- false
                x.Focus() |> ignore
                e.Handled <- true

            | _ -> ())

        and popupClosedSubscriber = x.TempoPopup.Closed.Subscribe(fun e ->
            eventUnsubscriber |> Disposable.dispose)

        ()


