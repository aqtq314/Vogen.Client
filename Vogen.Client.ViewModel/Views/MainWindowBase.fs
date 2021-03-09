namespace Vogen.Client.Views

open Doaz.Reactive
open Doaz.Reactive.Controls
open Doaz.Reactive.Math
open FSharp.Linq
open Microsoft.Win32
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.ViewModel


type MainWindowBase() =
    inherit Window()

    static member AppName = "Vogen Client"

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

    member x.ShowError(ex : #exn) =
        MessageBox.Show(
            x, $"Error saving file: {ex.Message}\r\n\r\n{ex.StackTrace}", MainWindowBase.AppName,
            MessageBoxButton.OK, MessageBoxImage.Error)
        |> ignore

    member x.SaveAs() =
        try let saveFileDialog =
                SaveFileDialog(
                    FileName = (!!x.ProgramModel.CompFilePathOp |> Option.defaultValue !!x.ProgramModel.CompFileName),
                    DefaultExt = ".vog",
                    Filter = "Vogen Package|*.vog")
            let dialogResult = saveFileDialog.ShowDialog x
            if dialogResult ?= true then
                let filePath = saveFileDialog.FileName
                x.ProgramModel.Save filePath
                Ok()
            else
                Error()
        with ex ->
            x.ShowError ex
            Error()

    member x.Save() =
        match !!x.ProgramModel.CompFilePathOp with
        | None -> x.SaveAs()
        | Some filePath ->
            try x.ProgramModel.Save filePath
                Ok()
            with ex ->
                x.ShowError ex
                x.SaveAs()

    member x.CheckChanges() =
        if not !!x.ProgramModel.CompIsSaved then
            let messageBoxResult =
                MessageBox.Show(
                    x, $"Save changes to {!!x.ProgramModel.CompFileName}?", MainWindowBase.AppName,
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes)
            match messageBoxResult with
            | MessageBoxResult.Yes ->
                x.Save()
            | MessageBoxResult.No ->
                Ok()
            | _ ->
                Error()
        else
            Ok()

    member x.New() =
        match x.CheckChanges() with
        | Ok() ->
            x.ProgramModel.New()
            Ok()
        | _ ->
            Error()

    member x.Open() =
        match x.CheckChanges() with
        | Ok() ->
            let openFileDialog =
                OpenFileDialog(
                    DefaultExt = ".vog",
                    Filter = "Vogen Package|*.vog")
            let dialogResult = openFileDialog.ShowDialog x
            if dialogResult ?= true then
                let filePath = openFileDialog.FileName
                x.ProgramModel.Open filePath
                Ok()
            else
                Error()
        | _ ->
            Error()

