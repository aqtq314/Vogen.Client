namespace Doaz.Reactive

open Checked
open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.Specialized
open System.ComponentModel

#nowarn "40"
#nowarn "21"


type ILockable =
    abstract Lock : unit -> unit
    abstract Unlock : unit -> unit

type ISubscriber<'e> =
    abstract Lock : unit -> unit
    abstract Unlock : args : 'e -> unit

type IPublisher<'e> =
    abstract Subscribe : child : ISubscriber<'e> -> unit
    abstract Unsubscribe : child : ISubscriber<'e> -> unit


[<Struct>]
type PropertyChangedArgs (hasValueChanged : bool) =
    member x.HasValueChanged = hasValueChanged

type IPropertySubscriber = ISubscriber<PropertyChangedArgs>
type IPropertyPublisher = IPublisher<PropertyChangedArgs>


[<NoComparison; StructuralEquality>]
type ListOperation<'a> =
    | AddItems of items : ArraySeg<'a> * index : int
    | MoveItems of items : ArraySeg<'a> * indexDiff : Diff<int>
    | RemoveItems of items : ArraySeg<'a> * index : int
    | ReplaceItems of itemDiffs : ArraySeg<Diff<'a>> * index : int
    member x.AsCollectionChangedEventArgs =
        //match x with
        //| AddItems(items, index) ->
        //    NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, ArraySeg.toBoxedIList items, index)
        //| MoveItems(items, indexDiff) ->
        //    NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, ArraySeg.toBoxedIList items, indexDiff.NewValue, indexDiff.OldValue)
        //| RemoveItems(items, index) ->
        //    NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, ArraySeg.toBoxedIList items, index)
        //| ReplaceItems(itemDiffs, index) ->
        //    let newItems = itemDiffs |> ArraySeg.mapLazy Diff.newValue
        //    let oldItems = itemDiffs |> ArraySeg.mapLazy Diff.oldValue
        //    NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, ArraySeg.toBoxedIList newItems, ArraySeg.toBoxedIList oldItems, index)
        match x with
        | AddItems(items, index) -> seq {
            for i in 0 .. items.Length - 1 ->
                NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, box items.[i], index + i) }
        | MoveItems(items, indexDiff) -> seq {
            let (Diff(oldIndex, newIndex)) = indexDiff
            yield! RemoveItems(items, oldIndex).AsCollectionChangedEventArgs
            yield! AddItems(items, newIndex).AsCollectionChangedEventArgs }
        | RemoveItems(items, index) -> seq {
            for i in 0 .. items.Length - 1 ->
                NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, box items.[i], index) }
        | ReplaceItems(itemDiffs, index) -> seq {
            for i in 0 .. itemDiffs.Length - 1 ->
                let (Diff(oldItem, newItem)) = itemDiffs.[i]
                NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, box newItem, box oldItem, index + i) }

[<Struct; NoComparison>]
type ListChangedArgs<'a> (ops : ArraySeg<ListOperation<'a>>) =
    member x.Operations = ops

type IListSubscriber<'a> = ISubscriber<ListChangedArgs<'a>>
type IListPublisher<'a> = IPublisher<ListChangedArgs<'a>>


type IReorderableCollection =
    abstract Move : oldIndex : int -> newIndex : int -> unit
    abstract MoveRange : oldIndex : int -> newIndex : int -> count : int -> unit


[<AbstractClass>]
type ReactiveProperty<'a> () =
    [<CLIEvent>] abstract PropertyChanged : IEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>
    abstract Subscribe : child : IPropertySubscriber -> unit
    abstract Unsubscribe : child : IPropertySubscriber -> unit
    abstract Value : 'a
    override x.ToString () = sprintf "rp %O" x.Value
    interface IPropertyPublisher with
        member x.Subscribe child = x.Subscribe child
        member x.Unsubscribe child = x.Unsubscribe child
    interface INotifyPropertyChanged with
        [<CLIEvent>] member x.PropertyChanged = x.PropertyChanged

[<AbstractClass>]
type ReactiveList<'a> () =
    [<CLIEvent>] abstract CollectionChanged : IEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>
    [<CLIEvent>] abstract PropertyChanged : IEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>
    abstract Subscribe : child : IListSubscriber<'a> -> unit
    abstract Unsubscribe : child : IListSubscriber<'a> -> unit
    abstract Item : index : int -> 'a with get
    abstract Count : int
    abstract GetEnumerator : unit -> IEnumerator<'a>
    member x.CopyTo (array : Array, arrayIndex) =
        for i in 0 .. x.Count - 1 do
            array.SetValue(x.[i], i + arrayIndex)
    interface IListPublisher<'a> with
        member x.Subscribe child = x.Subscribe child
        member x.Unsubscribe child = x.Unsubscribe child
    interface IList<'a> with
        member x.IndexOf item = x |> Seq.tryFindIndex(Unchecked.equals item) |> Option.defaultValue -1
        member x.Insert(index, item) = raise(NotSupportedException())
        member x.Item
            with get index = x.[index]
            and set index v = raise(NotSupportedException())
        member x.RemoveAt index = raise(NotSupportedException())
    interface System.Collections.IList with
        member x.Add value = raise(NotSupportedException())
        member x.Clear() = raise(NotSupportedException())
        member x.Contains value = Seq.cast x |> Seq.exists(Unchecked.equals value)
        member x.IndexOf value = Seq.cast x |> Seq.tryFindIndex(Unchecked.equals value) |> Option.defaultValue -1
        member x.Insert(index, value) = raise(NotSupportedException())
        member x.IsFixedSize = false
        member x.IsReadOnly = true
        member x.Item
            with get index = box x.[index]
            and set index v = raise(NotSupportedException())
        member x.Remove value = raise(NotSupportedException())
        member x.RemoveAt index = raise(NotSupportedException())
    interface ICollection<'a> with
        member x.Add item = raise(NotSupportedException())
        member x.Clear() = raise(NotSupportedException())
        member x.Contains item = x |> Seq.exists(Unchecked.equals item)
        member x.CopyTo(array, arrayIndex) = x.CopyTo(array, arrayIndex)
        member x.Count = x.Count
        member x.IsReadOnly = true
        member x.Remove item = raise(NotSupportedException())
    interface System.Collections.ICollection with
        member x.CopyTo(array, index) = x.CopyTo(array, index)
        member x.Count = x.Count
        member x.IsSynchronized = false
        member x.SyncRoot = box x
    interface IReadOnlyList<'a> with
        member x.Item with get index = x.[index]
    interface IReadOnlyCollection<'a> with
        member x.Count = x.Count
    interface IEnumerable<'a> with
        member x.GetEnumerator () = x.GetEnumerator ()
    interface System.Collections.IEnumerable with
        member x.GetEnumerator () = x.GetEnumerator () :> _
    interface INotifyCollectionChanged with
        [<CLIEvent>] member x.CollectionChanged = x.CollectionChanged
    interface INotifyPropertyChanged with
        [<CLIEvent>] member x.PropertyChanged = x.PropertyChanged


type IStateLock =
    abstract Lock : unit -> unit
    abstract Unlock : unit -> unit
    abstract LockCount : uint32

type LeafStateLock() =
    let mutable lockCount = 0u

    member x.Lock () = lockCount <- Checked.(+) lockCount 1u
    member x.Unlock () = lockCount <- Checked.(-) lockCount 1u
    member x.LockCount = lockCount

    interface IStateLock with
        member x.Lock () = x.Lock ()
        member x.Unlock () = x.Unlock ()
        member x.LockCount = x.LockCount

type internal StateLockTransitionState =
    | TSIdle
    | TSLocking
    | TSUnlocking

type MutableStateLock<'e> (defaultUnlockArgs : 'e, initState, dumpState : unit -> 'e) =
    let subscribers = List<ISubscriber<'e>> ()
    let mutable transitionState = TSIdle
    let mutable lockCount = 0u
    let mutable reentryCount = 0u
    let toSubscribe = Queue<Choice<ISubscriber<'e>, ISubscriber<'e>>> ()

    new (defaultUnlockArgs, dumpState) = MutableStateLock<'e> (defaultUnlockArgs, ignore, dumpState)

    member x.Subscribe child =
        match transitionState with
        | TSIdle ->
            if lockCount > 0u then
                (child : ISubscriber<_>).Lock ()
                subscribers.Add child
            else
                subscribers.Add child

        | TSLocking | TSUnlocking ->
            toSubscribe.Enqueue (Choice1Of2 child)

    member x.Unsubscribe child =
        match transitionState with
        | TSIdle ->
            subscribers.ForceRemove child
            if lockCount > 0u then
                (child : ISubscriber<_>).Unlock defaultUnlockArgs

        | TSLocking | TSUnlocking ->
            toSubscribe.Enqueue (Choice2Of2 child)

    member x.Lock () =
        match transitionState with
        | TSIdle ->
            if lockCount > 0u then
                lockCount <- Checked.(+) lockCount 1u

            elif reentryCount > 0u then
                raise (InvalidOperationException "Reentry count has not been cleared yet.")

            else
                lockCount <- Checked.(+) lockCount 1u
                transitionState <- TSLocking

                for child in subscribers do
                    child.Lock ()

                while toSubscribe.Count > 0 do
                    match toSubscribe.Dequeue () with
                    | Choice1Of2 child ->
                        child.Lock ()
                        subscribers.Add child

                    | Choice2Of2 child ->
                        subscribers.ForceRemove child
                        child.Unlock defaultUnlockArgs

                initState ()

                transitionState <- TSIdle

        | TSLocking ->
            reentryCount <- Checked.(+) reentryCount 1u

        | TSUnlocking ->
            raise (InvalidOperationException "Cannot lock while unlocking.")

    member x.Unlock () =
        match transitionState with
        | TSIdle ->
            if lockCount > 0u then
                if lockCount - 1u = 0u then
                    transitionState <- TSUnlocking

                    let state = dumpState ()
                    for child in subscribers do
                        child.Unlock state

                    while toSubscribe.Count > 0 do
                        match toSubscribe.Dequeue () with
                        | Choice1Of2 child ->
                            subscribers.Add child

                        | Choice2Of2 child ->
                            subscribers.ForceRemove child

                    transitionState <- TSIdle

                lockCount <- Checked.(-) lockCount 1u

            else
                reentryCount <- Checked.(-) reentryCount 1u

        | TSLocking ->
            raise (InvalidOperationException "Cannot unlock while locking.")

        | TSUnlocking ->
            if lockCount - 1u <> 0u then
                raise (InvalidOperationException (sprintf "Invalid state: lockCount cannot be %d" lockCount))

            reentryCount <- Checked.(-) reentryCount 1u

    member x.LockCount = lockCount

    member x.CanTriggerUnlockNext () =
        match transitionState with
        | TSIdle -> lockCount - 1u = 0u
        | _ -> false

    member internal x.Modify modify =
        if lockCount > 0u then
            modify ()
        else
            x.Lock ()
            modify ()
            x.Unlock ()

    member internal x.ModifyIfNotCyclic modify =
        if reentryCount = 0u then
            x.Lock ()
            if reentryCount = 0u then
                modify ()
            x.Unlock ()

    interface IStateLock with
        member x.Lock () = x.Lock ()
        member x.Unlock () = x.Unlock ()
        member x.LockCount = x.LockCount


type MutableReactiveProperty<'a> (value : 'a) as x =
    inherit ReactiveProperty<'a> ()

    let propertyChangedEvent = Event<PropertyChangedEventHandler, _>()
    let mutable value = value

    let mutable hasValueChanged = false
    let node = MutableStateLock (PropertyChangedArgs (false), fun () ->
        propertyChangedEvent.Trigger(box x, PropertyChangedEventArgs("Value"))
        let e = PropertyChangedArgs (hasValueChanged)
        hasValueChanged <- false
        e)

    [<CLIEvent>] override x.PropertyChanged = propertyChangedEvent.Publish
    override x.Subscribe child = node.Subscribe child
    override x.Unsubscribe child = node.Unsubscribe child
    override x.Value = value

    member x.Lock () = node.Lock ()
    member x.Unlock () = node.Unlock ()
    member internal x.CanTriggerUnlockNext () = node.CanTriggerUnlockNext ()
    member internal x.MutableStateLock = node

    member x.MarkAsChanged () =
        if not hasValueChanged then
            node.Modify <| fun () ->
                hasValueChanged <- true

    member x.SetValue newValue =
        if not (obj.Equals (value, newValue)) then
            node.Modify <| fun () ->
                value <- newValue
                hasValueChanged <- true

    member internal x.SetValueIfNotCyclic getNewValue =
        let newValue = getNewValue ()
        if not (obj.Equals (value, newValue)) then
            node.ModifyIfNotCyclic <| fun () ->
                value <- newValue
                hasValueChanged <- true

    interface ILockable with
        member x.Lock () = x.Lock ()
        member x.Unlock () = x.Unlock ()


type MutableReactiveList<'a> (values : seq<'a>) as x =
    inherit ReactiveList<'a> ()

    let collectionChangedEvent = Event<NotifyCollectionChangedEventHandler, _>()
    let propertyChangedEvent = Event<PropertyChangedEventHandler, _>()
    let values = List<'a> (values)

    let mutable listOperations = List<ListOperation<_>> ()
    let node = MutableStateLock (ListChangedArgs (ArraySeg.empty), fun () ->
        propertyChangedEvent.Trigger(box x, PropertyChangedEventArgs("Count"))
        propertyChangedEvent.Trigger(box x, PropertyChangedEventArgs("Item[]"))
        for op in listOperations do
            for args in op.AsCollectionChangedEventArgs do
                collectionChangedEvent.Trigger(box x, args)
        let e = ListChangedArgs (ArraySeg.ofIList listOperations)
        listOperations.Clear ()
        e)

    [<CLIEvent>] override x.CollectionChanged = collectionChangedEvent.Publish
    [<CLIEvent>] override x.PropertyChanged = propertyChangedEvent.Publish
    override x.Subscribe child = node.Subscribe child
    override x.Unsubscribe child = node.Unsubscribe child
    override x.Item with get i = values.[i]
    override x.Count = values.Count
    override x.GetEnumerator () = values.GetEnumerator () :> _

    member x.Lock () = node.Lock ()
    member x.Unlock () = node.Unlock ()
    member internal x.CanTriggerUnlockNext () = node.CanTriggerUnlockNext ()
    member internal x.MutableStateLock = node

    member private x.GetSubList (start : int) count =
        let array = Array.zeroCreate count
        values.CopyTo (start, array, 0, count)
        ArraySeg.ofArrayUnchecked array

    member private x.InsertItem index item =
        node.Modify <| fun () ->
            values.Insert (index, item)
            listOperations.Add (AddItems (ArraySeg.singleton item, index))

    member private x.InsertItems index items =
        let items = ArraySeg.ofSeq items
        if items.Length > 0 then
            node.Modify <| fun () ->
                values.InsertRange (index, items)
                listOperations.Add (AddItems (items, index))

    member private x.MoveItem oldIndex newIndex =
        if oldIndex <> newIndex then
            let item = values.[oldIndex]
            node.Modify <| fun () ->
                values.RemoveAt oldIndex
                values.Insert (newIndex, item)
                listOperations.Add (MoveItems (ArraySeg.singleton item, Diff (oldIndex, newIndex)))

    member private x.MoveItems oldIndex newIndex count =
        if oldIndex <> newIndex then
            let items = x.GetSubList oldIndex count
            if items.Length > 0 then
                node.Modify <| fun () ->
                    values.RemoveRange (oldIndex, count)
                    values.InsertRange (newIndex, items)
                    listOperations.Add (MoveItems (items, Diff (oldIndex, newIndex)))

    member private x.RemoveItem index =
        let item = values.[index]
        node.Modify <| fun () ->
            values.RemoveAt index
            listOperations.Add (RemoveItems (ArraySeg.singleton item, index))

    member private x.RemoveItems index count =
        let items = x.GetSubList index count
        if items.Length > 0 then
            node.Modify <| fun () ->
                values.RemoveRange (index, count)
                listOperations.Add (RemoveItems (items, index))

    member private x.ReplaceItem index item =
        let oldItem = values.[index]
        node.Modify <| fun () ->
            values.[index] <- item
            listOperations.Add (ReplaceItems (ArraySeg.singleton (Diff (oldItem, item)), index))

    member private x.ReplaceItems index items =
        let items = ArraySeg.ofSeq items
        if items.Length > 0 then
            node.Modify <| fun () ->
                let itemDiffs =
                    items
                    |> ArraySeg.mapi (fun i newItem ->
                        let oldItem = values.[i + index]
                        values.[i + index] <- newItem
                        Diff (oldItem, newItem))
                listOperations.Add (ReplaceItems (itemDiffs, index))

    member x.Add item = x.InsertItem values.Count item
    member x.AddRange items = x.InsertItems values.Count items
    member x.Clear () = x.RemoveItems 0 values.Count
    member x.RemoveAt index = x.RemoveItem index
    member x.RemoveRange index count = x.RemoveItems index count
    member x.Insert index item = x.InsertItem index item
    member x.InsertRange index items = x.InsertItems index items
    member x.Move oldIndex newIndex = x.MoveItem oldIndex newIndex
    member x.MoveRange oldIndex newIndex count = x.MoveItems oldIndex newIndex count
    member x.Replace index item = x.ReplaceItem index item
    member x.ReplaceRange index items = x.ReplaceItems index items

    member x.Set index value = x.ReplaceItem index value

    member x.RemoveIfExists item =
        let i = values.IndexOf item
        if i < 0 then false
        else
            x.RemoveItem i
            true

    member x.Remove item =
        let removed = x.RemoveIfExists item
        if not removed then
            raise (KeyNotFoundException ())

    member x.RemoveAll predicate =
        x
        |> Seq.mapi (fun i item -> item, i)
        |> Seq.filter (fun (item, i) -> predicate item)
        |> Seq.map (fun (item, i) -> i)
        |> Seq.distinct
        |> Seq.sortDescending
        |> Seq.iter (fun i -> x.RemoveItem i)

    interface ILockable with
        member x.Lock () = x.Lock ()
        member x.Unlock () = x.Unlock ()

    interface IReorderableCollection with
        member x.Move oldIndex newIndex = x.Move oldIndex newIndex
        member x.MoveRange oldIndex newIndex count = x.MoveRange oldIndex newIndex count

    interface IList<'a> with
        member x.IndexOf item = values.IndexOf item
        member x.Insert (index, item) = x.Insert index item
        member x.Item
            with get index = x.[index]
            and set index v = x.Set index v
        member x.RemoveAt index = x.RemoveAt index

    interface System.Collections.IList with
        member x.Add value =
            x.Add (unbox value)
            x.Count - 1
        member x.Clear () = x.Clear ()
        member x.Contains value = (values :> System.Collections.IList).Contains value
        member x.IndexOf value = (values :> System.Collections.IList).IndexOf value
        member x.Insert (index, value) = x.Insert index (unbox value)
        member x.IsFixedSize = false
        member x.IsReadOnly = false
        member x.Item
            with get index = box x.[index]
            and set index v = x.Set index (unbox v)
        member x.Remove value =
            let i = (values :> System.Collections.IList).IndexOf value
            if i >= 0 then x.RemoveItem i
        member x.RemoveAt index = x.RemoveAt index

    interface ICollection<'a> with
        member x.Add item = x.Add item
        member x.Clear () = x.Clear ()
        member x.Contains item = values.Contains item
        member x.CopyTo (array, arrayIndex) = values.CopyTo (array, arrayIndex)
        member x.Count = x.Count
        member x.IsReadOnly = false
        member x.Remove item = x.RemoveIfExists item

    interface System.Collections.ICollection with
        member x.CopyTo (array, index) = (values :> System.Collections.ICollection).CopyTo (array, index)
        member x.Count = x.Count
        member x.IsSynchronized = false
        member x.SyncRoot = (values :> System.Collections.ICollection).SyncRoot

    interface IEnumerable<'a> with
        member x.GetEnumerator () = x.GetEnumerator ()

    interface System.Collections.IEnumerable with
        member x.GetEnumerator () = x.GetEnumerator () :> _


type ReactivePropertyBulkSetter () =
    let lockables = List ()

    member x.LockList (rl : MutableReactiveList<'a>) =
        rl.Lock ()
        lockables.Add (rl :> ILockable)

    member x.LockItems ([<ParamArray>] items : ILockable []) =
        for item in items do item.Lock ()
        lockables.AddRange items

    member x.LockSetProp (value : 'a) (rp : MutableReactiveProperty<'a>) =
        rp.Lock ()
        rp.SetValue value
        lockables.Add (rp :> ILockable)

    member x.LockSetPropIfDiff (value : 'a) (rp : MutableReactiveProperty<'a>) =
        if value <> rp.Value then
            x.LockSetProp value rp

    member x.Dispose () =
        for lockable in lockables do
            lockable.Unlock ()
        lockables.Clear ()

    interface IDisposable with
        member x.Dispose () = x.Dispose ()


module Rp =
    let inline get(rp : ReactiveProperty<_>) = rp.Value
    let inline set v1 (p1 : MutableReactiveProperty<_>) = p1.SetValue v1
    let inline modify modifier (rp : MutableReactiveProperty<_>) = rp.SetValue(modifier rp.Value)

    let bulkSetter () = new ReactivePropertyBulkSetter()

    let create value = MutableReactiveProperty(value)

    type private ReadOnlyReactiveProperty<'a>(rp : ReactiveProperty<'a>) as x =
        inherit ReactiveProperty<'a>()
        let propertyChangedEvent = Event<PropertyChangedEventHandler, _>()
        do  rp.PropertyChanged.Add(fun e -> propertyChangedEvent.Trigger(box x, e))
        [<CLIEvent>] override x.PropertyChanged = propertyChangedEvent.Publish
        override x.Subscribe child = rp.Subscribe child
        override x.Unsubscribe child = rp.Unsubscribe child
        override x.Value = rp.Value

    let createBase(rp : #ReactiveProperty<_>) =
        ReadOnlyReactiveProperty<_>(rp) :> ReactiveProperty<_>

    let createImmutable value =
        { new ReactiveProperty<_>() with
            [<CLIEvent>] member x.PropertyChanged = Event.empty
            member x.Subscribe child = ()
            member x.Unsubscribe child = ()
            member x.Value = value }

module Rl =
    let create values = MutableReactiveList(values)

    type ReadOnlyReactiveList<'a>(rl : ReactiveList<'a>) as x =
        inherit ReactiveList<'a>()
        let collectionChangedEvent = Event<NotifyCollectionChangedEventHandler, _>()
        let propertyChangedEvent = Event<PropertyChangedEventHandler, _>()
        do  rl.CollectionChanged.Add(fun e -> collectionChangedEvent.Trigger(box x, e))
            rl.PropertyChanged.Add(fun e -> propertyChangedEvent.Trigger(box x, e))
        [<CLIEvent>] override x.CollectionChanged = collectionChangedEvent.Publish
        [<CLIEvent>] override x.PropertyChanged = propertyChangedEvent.Publish
        override x.Subscribe child = rl.Subscribe child
        override x.Unsubscribe child = rl.Unsubscribe child
        override x.Item with get index = rl.[index]
        override x.Count = rl.Count
        override x.GetEnumerator() = rl.GetEnumerator()

    let inline createBase (rl : #ReactiveList<_>) =
        ReadOnlyReactiveList<_>(rl) :> ReactiveList<_>

    let empty<'a> =
        { new ReactiveList<'a>() with
            [<CLIEvent>] member x.CollectionChanged = Event.empty
            [<CLIEvent>] member x.PropertyChanged = Event.empty
            member x.Subscribe child = ()
            member x.Unsubscribe child = ()
            member x.Item with get index = raise (ArgumentOutOfRangeException())
            member x.Count = 0
            member x.GetEnumerator() = Seq.empty<'a>.GetEnumerator() }

    let createImmutable (values : ArraySeg<_>) =
        { new ReactiveList<_>() with
            [<CLIEvent>] member x.CollectionChanged = Event.empty
            [<CLIEvent>] member x.PropertyChanged = Event.empty
            member x.Subscribe child = ()
            member x.Unsubscribe child = ()
            member x.Item with get index = values.[index]
            member x.Count = values.Length
            member x.GetEnumerator() = (ArraySeg.toSeq values).GetEnumerator() }


[<AutoOpen>]
module RpUtils =
    let inline (!!) (rp : #ReactiveProperty<_>) = rp.Value
    let inline rp value = Rp.create value
    let inline rl values = Rl.create values


