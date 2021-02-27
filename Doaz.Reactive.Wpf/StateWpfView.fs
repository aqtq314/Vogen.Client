namespace Doaz.Reactive

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Diagnostics

#nowarn "40"
#nowarn "21"


type MutableReactivePropertyWpfView<'a>(rp : MutableReactiveProperty<'a>) as x =
    let propertyChangedEvent = Event<PropertyChangedEventHandler, _>()
    do  let valueChanged = ref false
        let onFinalUnblock() =
            if valueChanged.Value then
                propertyChangedEvent.Trigger(box x, PropertyChangedEventArgs("Value"))
                valueChanged := false
        rp.Subscribe(PropertySubscriber.leaf valueChanged onFinalUnblock)

    [<CLIEvent>]
    member x.PropertyChanged = propertyChangedEvent.Publish

    member x.Unwrapped = rp
    member x.Value
        with get() = rp.Value
        and set newValue = rp.SetValue newValue

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = x.PropertyChanged


