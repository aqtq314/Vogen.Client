namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open FSharp.Linq
open Microsoft.Win32
open System
open System.Collections.Generic
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.ViewModel


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

    override x.OnClosing e =
        match x.CheckChanges() with
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

    member x.CheckChanges() = result {
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
        do! x.CheckChanges()
        x.ProgramModel.New() }

    member x.Open() = result {
        do! x.CheckChanges()
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
        do! x.CheckChanges()
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
            let defaultFileName = Path.ChangeExtension(defaultFileName, ".m4a")
            SaveFileDialog(
                FileName = defaultFileName,
                DefaultExt = ".m4a",
                Filter = "MP4 Audio|*.m4a")
        let dialogResult = saveFileDialog.ShowDialog x
        if dialogResult ?= true then
            let filePath = saveFileDialog.FileName
            x.ProgramModel.Export filePath }


