namespace Doaz.Reactive.Controls

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input

#nowarn "40"
#nowarn "21"


type CheckableButton () =
    inherit Button ()

    static member val IsCheckedProperty =
        Dp.reg<bool, CheckableButton> "IsChecked"
            (Dp.Meta (false))

    member x.IsChecked
        with get () = x.GetValue CheckableButton.IsCheckedProperty :?> bool
        and set (v : bool) = x.SetValue (CheckableButton.IsCheckedProperty, box v)


