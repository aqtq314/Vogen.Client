module Doaz.Reactive.Command

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Diagnostics
open System.Windows
open System.Windows.Input


let relay canExecute execute =
    let canExecuteChangedEvent = Event<_, _>()

    let rec command =
        { new ICommand with
            [<CLIEvent>] member x.CanExecuteChanged = canExecuteChangedEvent.Publish
            member x.CanExecute parameter = !!canExecute
            member x.Execute parameter = execute() }

    canExecute |> Rpo.leaf(fun canExecute ->
        canExecuteChangedEvent.Trigger(command, EventArgs()))

    command


