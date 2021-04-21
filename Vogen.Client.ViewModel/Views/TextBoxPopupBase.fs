namespace Vogen.Client.Views

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
open System.Windows.Media.Animation
open Vogen.Client.Model


type TextBoxPopupBase() =
    inherit Popup()

    let mutable textChangedHandler = fun _ -> Ok()
    let mutable revertChangesHandler = ignore

    abstract ContentTextBox : TextBox
    default x.ContentTextBox = Unchecked.defaultof<_>

    member x.BindBehaviors() =
        x.ContentTextBox.TextChanged.Add <| fun e ->
            match textChangedHandler x.ContentTextBox.Text with
            | Ok() ->
                // TODO: Ok prompt
                ()
            | Error() ->
                // TODO: Error prompt
                ()

        x.ContentTextBox.KeyDown.Add(fun e ->
            match e.Key with
            | Key.Enter ->
                x.IsOpen <- false
                x.Focus() |> ignore
                e.Handled <- true

            | Key.Escape ->
                revertChangesHandler()
                x.IsOpen <- false
                x.Focus() |> ignore
                e.Handled <- true

            | _ -> ())

        x.Closed.Add(fun e ->
            textChangedHandler <- fun _ -> Ok()
            revertChangesHandler <- ignore)

    member x.Open initText newRevertChangesHandler newTextChangedHandler =
        if Mouse.Captured <> null then 
            Mouse.Captured.ReleaseMouseCapture()
        x.IsOpen <- true

        x.ContentTextBox.Text <- initText
        x.ContentTextBox.SelectAll()
        x.ContentTextBox.Focus() |> ignore

        textChangedHandler <- newTextChangedHandler
        revertChangesHandler <- newRevertChangesHandler


