namespace Doaz.Reactive

open Checked
open Doaz.Reactive
open System
open System.Collections.Generic

#nowarn "40"
#nowarn "21"


type ISubscriberBehavior<'e> =
    abstract OnFirstLock : unit -> unit
    abstract OnEachLock : unit -> unit
    abstract OnEachUnlock : e : 'e -> unit
    abstract OnFinalUnlock : unit -> unit

type IPropertySubscriberBehavior = ISubscriberBehavior<PropertyChangedArgs>
type IListSubscriberBehavior<'a> = ISubscriberBehavior<ListChangedArgs<'a>>

module Subscriber =
    let create(stateLock : IStateLock)(behavior : ISubscriberBehavior<_>) =
        { new ISubscriber<_> with
            member x.Lock() =
                stateLock.Lock()
                if stateLock.LockCount = 1u then
                    behavior.OnFirstLock()
                behavior.OnEachLock()
            member x.Unlock e =
                behavior.OnEachUnlock e
                if stateLock.LockCount = 1u then
                    behavior.OnFinalUnlock()
                stateLock.Unlock() }

module PropertySubscriber =
    let createBehavior(valueChanged : _ ref) onFinalUnblock =
        { new IPropertySubscriberBehavior with
            member x.OnFirstLock() = ()
            member x.OnEachLock() = ()
            member x.OnEachUnlock e = valueChanged := valueChanged.Value || e.HasValueChanged
            member x.OnFinalUnlock() = onFinalUnblock() }

    let leaf valueChanged onFinalUnblock : IPropertySubscriber =
        Subscriber.create(LeafStateLock())(createBehavior valueChanged onFinalUnblock)

    let bindToProperty valueChanged onFinalUnblock (bindTarget : MutableReactiveProperty<_>) : IPropertySubscriber =
        Subscriber.create bindTarget.MutableStateLock (createBehavior valueChanged onFinalUnblock)

    let bindToList valueChanged onFinalUnblock (bindTarget : MutableReactiveList<_>) : IPropertySubscriber =
        Subscriber.create bindTarget.MutableStateLock (createBehavior valueChanged onFinalUnblock)

module ListSubscriber =
    let createBehavior(ops : List<_>) onFinalUnlock =
        { new IListSubscriberBehavior<_> with
            member x.OnFirstLock() = ()
            member x.OnEachLock() = ()
            member x.OnEachUnlock e = ops.AddRange e.Operations
            member x.OnFinalUnlock() = onFinalUnlock() }

    let leaf ops onFinalUnlock : IListSubscriber<'a> =
        Subscriber.create(LeafStateLock())(createBehavior ops onFinalUnlock)

    let bindToProperty ops onFinalUnlock (bindTarget : MutableReactiveProperty<_>) : IListSubscriber<'a> =
        Subscriber.create bindTarget.MutableStateLock (createBehavior ops onFinalUnlock)

    let bindToList ops onFinalUnlock (bindTarget : MutableReactiveList<_>) : IListSubscriber<'a> =
        Subscriber.create bindTarget.MutableStateLock (createBehavior ops onFinalUnlock)


module ReactivePropertyFunctorParams =
    let inline private (~+) (rp : ReactiveProperty<_>) = rp :> IPropertyPublisher

    type Rp1 =
        static member inline Apply ((p1), f) = f !!p1
        static member inline ToArray ((p1)) = [| +p1 |]
    type Rp2 =
        static member inline Apply ((p1, p2), f) = f !!p1 !!p2
        static member inline ToArray ((p1, p2)) = [| +p1; +p2 |]
    type Rp3 =
        static member inline Apply ((p1, p2, p3), f) = f !!p1 !!p2 !!p3
        static member inline ToArray ((p1, p2, p3)) = [| +p1; +p2; +p3 |]
    type Rp4 =
        static member inline Apply ((p1, p2, p3, p4), f) = f !!p1 !!p2 !!p3 !!p4
        static member inline ToArray ((p1, p2, p3, p4)) = [| +p1; +p2; +p3; +p4 |]
    type Rp5 =
        static member inline Apply ((p1, p2, p3, p4, p5), f) = f !!p1 !!p2 !!p3 !!p4 !!p5
        static member inline ToArray ((p1, p2, p3, p4, p5)) = [| +p1; +p2; +p3; +p4; +p5 |]
    type Rp6 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6
        static member inline ToArray ((p1, p2, p3, p4, p5, p6)) = [| +p1; +p2; +p3; +p4; +p5; +p6 |]
    type Rp7 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6, p7), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6 !!p7
        static member inline ToArray ((p1, p2, p3, p4, p5, p6, p7)) = [| +p1; +p2; +p3; +p4; +p5; +p6; +p7 |]
    type Rp8 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6, p7, p8), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6 !!p7 !!p8
        static member inline ToArray ((p1, p2, p3, p4, p5, p6, p7, p8)) = [| +p1; +p2; +p3; +p4; +p5; +p6; +p7; +p8 |]
    type Rp9 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6, p7, p8, p9), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6 !!p7 !!p8 !!p9
        static member inline ToArray ((p1, p2, p3, p4, p5, p6, p7, p8, p9)) = [| +p1; +p2; +p3; +p4; +p5; +p6; +p7; +p8; +p9 |]
    type Rp10 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6 !!p7 !!p8 !!p9 !!p10
        static member inline ToArray ((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10)) = [| +p1; +p2; +p3; +p4; +p5; +p6; +p7; +p8; +p9; +p10 |]
    type Rp11 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6 !!p7 !!p8 !!p9 !!p10 !!p11
        static member inline ToArray ((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11)) = [| +p1; +p2; +p3; +p4; +p5; +p6; +p7; +p8; +p9; +p10; +p11 |]
    type Rp12 =
        static member inline Apply ((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12), f) = f !!p1 !!p2 !!p3 !!p4 !!p5 !!p6 !!p7 !!p8 !!p9 !!p10 !!p11 !!p12
        static member inline ToArray ((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12)) = [| +p1; +p2; +p3; +p4; +p5; +p6; +p7; +p8; +p9; +p10; +p11; +p12 |]
    type RpArray =
        static member inline Apply (ps : IPropertyPublisher [], f) = f ()
        static member inline ToArray (ps : IPropertyPublisher []) = ps

    let inline tupApply (_ : TypeArgs<'tup>) ps mapper =
        (^tup : (static member Apply : 't -> 'f -> 'a) (ps, mapper))

    let inline tupToArray (_ : TypeArgs<'tup>) ps =
        (^tup : (static member ToArray : 't -> IPropertyPublisher []) ps)


module Rpo =
    open ReactivePropertyFunctorParams

    [<NoDynamicInvocation>]
    let inline mapFunctor(_ : TypeArgs<'tup>) mapper (ps : 't) =
        let getValue() = tupApply typeArgs<'tup> ps mapper
        let parents = tupToArray typeArgs<'tup> ps
        let rp = MutableReactiveProperty<_>(getValue())
        let valueChanged = ref false
        let onFinalUnblock() =
            if valueChanged.Value then
                rp.SetValue(getValue())
                valueChanged := false
        let subscriber = rp |> PropertySubscriber.bindToProperty valueChanged onFinalUnblock
        for p : IPropertyPublisher in parents do
            p.Subscribe subscriber
        Rp.createBase rp

    let map mapper p = mapFunctor typeArgs<Rp1> mapper p
    let map2 mapper ps = mapFunctor typeArgs<Rp2> mapper ps
    let map3 mapper ps = mapFunctor typeArgs<Rp3> mapper ps
    let map4 mapper ps = mapFunctor typeArgs<Rp4> mapper ps
    let map5 mapper ps = mapFunctor typeArgs<Rp5> mapper ps
    let map6 mapper ps = mapFunctor typeArgs<Rp6> mapper ps
    let map7 mapper ps = mapFunctor typeArgs<Rp7> mapper ps
    let map8 mapper ps = mapFunctor typeArgs<Rp8> mapper ps
    let map9 mapper ps = mapFunctor typeArgs<Rp9> mapper ps
    let map10 mapper ps = mapFunctor typeArgs<Rp10> mapper ps
    let map11 mapper ps = mapFunctor typeArgs<Rp11> mapper ps
    let map12 mapper ps = mapFunctor typeArgs<Rp12> mapper ps
    let mapArray getValue ps = mapFunctor typeArgs<RpArray> getValue ps

    // This implementation does not prevent bindTarget to have a separate valueChanged and onFinalUnblock
    // that potentially have different values than these ones
    let dataBindTo bindTarget mapper bindSource =
        let getValue() = mapper !!bindSource
        let valueChanged = ref false
        let onFinalUnblock() =
            if valueChanged.Value then
                bindTarget |> Rp.set(getValue())
                valueChanged := false
        bindSource.Subscribe(bindTarget |> PropertySubscriber.bindToProperty valueChanged onFinalUnblock)
        bindTarget.SetValueIfNotCyclic getValue

    let doubleDataBindTo bindTarget mapper backMapper bindSource =
        let getValue() = mapper !!bindSource
        let bindSourceValueChanged = ref false
        let onBindSourceFinalUnblock() =
            if bindSourceValueChanged.Value then
                bindTarget |> Rp.set(getValue())
                bindSourceValueChanged := false
        bindSource.Subscribe(bindTarget |> PropertySubscriber.bindToProperty bindSourceValueChanged onBindSourceFinalUnblock)
        bindTarget.SetValueIfNotCyclic getValue

        let bindTargetValueChanged = ref false
        let onBindTargetFinalUnblock() =
            if bindTargetValueChanged.Value then
                bindSource |> Rp.set(backMapper !!bindTarget)
                bindTargetValueChanged := false
        bindTarget.Subscribe(bindSource |> PropertySubscriber.bindToProperty bindTargetValueChanged onBindTargetFinalUnblock)

    let dataBindBackRef getBackRef self (rp : ReactiveProperty<_>) =
        let mutable prevChild = !!rp
        let behavior =
            { new IPropertySubscriberBehavior with
                member x.OnFirstLock() =
                    !!rp |> Option.iter(fun child ->
                        (getBackRef child : MutableReactiveProperty<_>).Lock())
                member x.OnEachLock() = ()
                // alternative is to record hasValueChanged and lock children if e.HasValueChanged
                // but only update backRef on final unlock
                member x.OnEachUnlock e =
                    if e.HasValueChanged then
                        prevChild |> Option.iter(fun child ->
                            let backRef = getBackRef child
                            match !!backRef with
                            | Some existingBackRef when Unchecked.equals existingBackRef self ->
                                backRef.SetValue None
                                backRef.Unlock()
                            | _ -> raise(InvalidOperationException("Item not referenced by current instance.")))
                        !!rp |> Option.iter(fun child ->
                            let backRef = getBackRef child
                            match !!backRef with
                            | None ->
                                backRef.Lock()
                                backRef.SetValue(Some self)
                            | Some _ -> raise(InvalidOperationException("Item already referenced by another instance.")))
                        prevChild <- !!rp
                member x.OnFinalUnlock() =
                    !!rp |> Option.iter(fun child ->
                        (getBackRef child).Unlock()) }
        let subscriber = Subscriber.create(LeafStateLock()) behavior    // need to add check for cycles

        rp.Subscribe subscriber
        !!rp |> Option.iter(fun child ->
            (getBackRef child).SetValue(Some self))

    let flatten getInnerParent outerParent =
        let mutable innerParent = getInnerParent !!outerParent :> ReactiveProperty<_>
        let rp = MutableReactiveProperty<_>(!!innerParent)
        let innerValueChanged, outerValueChanged = ref false, ref false
        let rec onFinalUnblock() =
            if outerValueChanged.Value then
                innerParent.Unsubscribe innerSubscriber
                innerParent <- getInnerParent !!outerParent
                innerParent.Subscribe innerSubscriber
                innerValueChanged := true
                outerValueChanged := false
            if innerValueChanged.Value then
                rp |> Rp.set !!innerParent
                innerValueChanged := false
        and innerSubscriber = rp |> PropertySubscriber.bindToProperty innerValueChanged onFinalUnblock
        innerParent.Subscribe innerSubscriber
        let outerSubscriber = rp |> PropertySubscriber.bindToProperty outerValueChanged onFinalUnblock
        outerParent.Subscribe outerSubscriber
        Rp.createBase rp

    let flattenFill getInnerParent defaultValue outerParent =
        outerParent |> flatten (fun outerParent ->
            match outerParent with
            | Some outerParent -> getInnerParent outerParent :> ReactiveProperty<_>
            | None -> Rp.createImmutable defaultValue)

    let aggregate propertySelector getValue (parentList : ReactiveList<_>) =
        let readOnlyParentList = parentList |> ReadOnlyList.map(fun item -> !!(propertySelector item))
        let rp = MutableReactiveProperty<_>(getValue readOnlyParentList)
        let valueChanged, parentOps = ref false, List()
        let rec onFinalUnblock() =
            for op in parentOps do
                match op with
                | AddItems(items, index) ->
                    for item in items do
                        (propertySelector item).Subscribe itemSubscriber
                | MoveItems(items, indexDiff) -> ()
                | RemoveItems(items, index) ->
                    for item in items do
                        (propertySelector item).Unsubscribe itemSubscriber
                | ReplaceItems(itemDiffs, index) ->
                    for (Diff(oldItem, newItem)) in itemDiffs do
                        (propertySelector oldItem).Unsubscribe itemSubscriber
                        (propertySelector newItem).Subscribe itemSubscriber
            if valueChanged.Value || parentOps.Count > 0 then
                rp.SetValue(getValue readOnlyParentList)
                parentOps.Clear()
                valueChanged := false
        and itemSubscriber = rp |> PropertySubscriber.bindToProperty valueChanged onFinalUnblock
        for item in parentList do
            (propertySelector item).Subscribe itemSubscriber
        parentList.Subscribe(rp |> ListSubscriber.bindToProperty parentOps onFinalUnblock)
        Rp.createBase rp

    let aggregateCount mapper (parentList : ReactiveList<_>) =
        let rp = MutableReactiveProperty<_>(mapper parentList.Count)
        let ops = List()
        let onFinalUnblock() =
            if ops.Count > 0 then
                rp.SetValue(mapper parentList.Count)
                ops.Clear()
        parentList.Subscribe(rp |> ListSubscriber.bindToProperty ops onFinalUnblock)
        Rp.createBase rp


module Rlo =
    let ofOption rp =
        let rl = MutableReactiveList<_>(Option.toList !!rp)
        let valueChanged = ref false
        let onFinalUnblock() =
            if valueChanged.Value then
                let oldValue = match rl.Count with | 0 -> None | _ -> Some rl.[0]
                let newValue = !!rp
                match oldValue, newValue with
                | None, None -> ()
                | None, Some newValue -> rl.Add newValue
                | Some oldValue, None -> rl.RemoveAt 0
                | Some oldValue, Some newValue ->
                    if not(Unchecked.equals oldValue newValue) then
                        rl.Set 0 newValue
                valueChanged := false
        rp.Subscribe(rl |> PropertySubscriber.bindToList valueChanged onFinalUnblock)
        Rl.createBase rl

    let append(list1 : ReactiveList<_>)(list2 : ReactiveList<_>) =
        let rl = MutableReactiveList<_>(Seq.append list1 list2)
        let ops1, ops2 = List(), List()  // order of appying ops doesn't matter here
        let onFinalUnblock() =
            for op in ops1 do
                match op with
                | AddItems(items, index) -> rl.InsertRange index items
                | MoveItems(items, indexDiff) -> rl.MoveRange indexDiff.OldValue indexDiff.NewValue items.Length
                | RemoveItems(items, index) -> rl.RemoveRange index items.Length
                | ReplaceItems(itemDiffs, index) -> rl.ReplaceRange index (itemDiffs |> Seq.map Diff.newValue)
            ops1.Clear()
            for op in ops2 do   // directly using list1.Count because all of ops1 are applied before ops2
                match op with
                | AddItems(items, index) -> rl.InsertRange(index + list1.Count) items
                | MoveItems(items, indexDiff) -> rl.MoveRange(indexDiff.OldValue + list1.Count)(indexDiff.NewValue + list1.Count) items.Length
                | RemoveItems(items, index) -> rl.RemoveRange(index + list1.Count) items.Length
                | ReplaceItems(itemDiffs, index) -> rl.ReplaceRange(index + list1.Count)(itemDiffs |> Seq.map Diff.newValue)
            ops2.Clear()
        list1.Subscribe(rl |> ListSubscriber.bindToList ops1 onFinalUnblock)
        list2.Subscribe(rl |> ListSubscriber.bindToList ops2 onFinalUnblock)
        Rl.createBase rl

    let map mapper (parentList : ReactiveList<_>) =
        let rl = MutableReactiveList<_>(Seq.map mapper parentList)
        let ops = List()
        let onFinalUnblock() =
            for op in ops do
                match op with
                | AddItems(items, index) -> rl.InsertRange index (Seq.map mapper items)
                | MoveItems(items, indexDiff) -> rl.MoveRange indexDiff.OldValue indexDiff.NewValue items.Length
                | RemoveItems(items, index) -> rl.RemoveRange index items.Length
                | ReplaceItems(itemDiffs, index) -> rl.ReplaceRange index (itemDiffs |> Seq.map(Diff.newValue >> mapper))
            ops.Clear()
        parentList.Subscribe(rl |> ListSubscriber.bindToList ops onFinalUnblock)
        Rl.createBase rl

    let dataBindTo bindTarget applyOps (bindSource : ReactiveList<_>) =
        let ops = List()
        let onFinalUnblock() =
            applyOps(ArraySeg.ofIList ops)
            ops.Clear()
        bindSource.Subscribe(bindTarget |> ListSubscriber.bindToList ops onFinalUnblock)
        if bindTarget.Count > 0 then
            bindTarget.Clear()
        applyOps(ArraySeg.singleton(AddItems(ArraySeg.ofIReadOnlyList bindSource, 0)))

    let mapDataBindTo bindTarget mapper (bindSource : ReactiveList<_>) =
        bindSource |> dataBindTo bindTarget (fun ops ->
            for op in ops do
                match op with
                | AddItems(items, index) -> bindTarget.InsertRange index (Seq.map mapper items)
                | MoveItems(items, indexDiff) -> bindTarget.MoveRange indexDiff.OldValue indexDiff.NewValue items.Length
                | RemoveItems(items, index) -> bindTarget.RemoveRange index items.Length
                | ReplaceItems(itemDiffs, index) -> bindTarget.ReplaceRange index (itemDiffs |> Seq.map(Diff.newValue >> mapper)))

    let doubleDataBindTo bindTarget applyOps backApplyOps (bindSource : MutableReactiveList<_>) =
        let ops, backOps = List(), List()
        let onFinalUnblock() =
            applyOps(ArraySeg.ofIList ops)
            ops.Clear()
        let onBackFinalUnblock() =
            backApplyOps(ArraySeg.ofIList backOps)
            backOps.Clear()
        bindSource.Subscribe(bindTarget |> ListSubscriber.bindToList ops onFinalUnblock)
        if bindTarget.Count > 0 then
            bindTarget.Clear()
        applyOps(ArraySeg.singleton(AddItems(ArraySeg.ofIReadOnlyList bindSource, 0)))
        bindTarget.Subscribe(bindSource |> ListSubscriber.bindToList backOps onBackFinalUnblock)

    let mapDoubleDataBindTo bindTarget mapper backMapper (bindSource : MutableReactiveList<_>) =
        bindSource |> doubleDataBindTo bindTarget (fun ops ->
            for op in ops do
                match op with
                | AddItems(items, index) -> bindTarget.InsertRange index (Seq.map mapper items)
                | MoveItems(items, indexDiff) -> bindTarget.MoveRange indexDiff.OldValue indexDiff.NewValue items.Length
                | RemoveItems(items, index) -> bindTarget.RemoveRange index items.Length
                | ReplaceItems(itemDiffs, index) -> bindTarget.ReplaceRange index (itemDiffs |> Seq.map(Diff.newValue >> mapper))) (fun ops ->
            for op in ops do
                match op with
                | AddItems(items, index) -> bindSource.InsertRange index (Seq.map backMapper items)
                | MoveItems(items, indexDiff) -> bindSource.MoveRange indexDiff.OldValue indexDiff.NewValue items.Length
                | RemoveItems(items, index) -> bindSource.RemoveRange index items.Length
                | ReplaceItems(itemDiffs, index) -> bindSource.ReplaceRange index (itemDiffs |> Seq.map(Diff.newValue >> backMapper)))

    let dataBindBackRef getParentRef parent (list : ReactiveList<_>) =
        let lockedParentRefs = List()

        let onAddChild child =
            let parentRef : #MutableReactiveProperty<_> = getParentRef child
            match !!parentRef with
            | Some _ -> raise(InvalidOperationException("Item already has a parent."))
            | None ->
                parentRef.Lock()
                lockedParentRefs.Add parentRef
                parentRef |> Rp.set(Some parent)

        let onRemoveChild child =
            let parentRef = getParentRef child
            match !!parentRef with
            | Some existingParent when Unchecked.equals existingParent parent ->
                parentRef.Lock()
                lockedParentRefs.Add parentRef
                parentRef |> Rp.set None
            | _ -> raise(InvalidOperationException("Item does not belong to this parent."))

        let behavior =
            { new IListSubscriberBehavior<_> with
                member x.OnFirstLock() = ()
                member x.OnEachLock() = ()
                member x.OnEachUnlock(e : ListChangedArgs<_>) =
                    for op in e.Operations do
                        match op with
                        | AddItems(items, index) ->
                            ArraySeg.iter onAddChild items
                        | MoveItems(items, indexDiff) -> ()
                        | RemoveItems(items, index) ->
                            ArraySeg.iter onRemoveChild items
                        | ReplaceItems(itemDiffs, index) ->
                            ArraySeg.iter onRemoveChild (ArraySeg.mapLazy Diff.oldValue itemDiffs)
                            ArraySeg.iter onAddChild (ArraySeg.mapLazy Diff.newValue itemDiffs)
                member x.OnFinalUnlock() =
                    for parentRef in lockedParentRefs do
                        parentRef.Unlock()
                    lockedParentRefs.Clear() }
        let subscriber = Subscriber.create(LeafStateLock()) behavior    // need to add check for cycles
        list.Subscribe subscriber

        list |> Seq.iter onAddChild
        behavior.OnFinalUnlock()

    let dataBindIndex getIndexProp (list : ReactiveList<_>) =
        let behavior =
            { new IListSubscriberBehavior<_> with
                member x.OnFirstLock() =
                    for child in list do
                        (getIndexProp child : MutableReactiveProperty<_>).Lock()
                member x.OnEachLock() = ()
                member x.OnEachUnlock e =
                    for op in e.Operations do
                        match op with
                        | AddItems(items, index) ->
                            for child in items do
                                (getIndexProp child).Lock()
                            for i in index .. list.Count - 1 do
                                (getIndexProp list.[i]).SetValue i
                        | MoveItems(items, indexDiff) ->
                            let startIndex = min indexDiff.OldValue indexDiff.NewValue
                            let endIndex = max indexDiff.OldValue indexDiff.NewValue + items.Length
                            for i in startIndex .. endIndex - 1 do
                                (getIndexProp list.[i]).SetValue i
                        | RemoveItems(items, index) ->
                            for child in items do
                                (getIndexProp child).SetValue -1
                                (getIndexProp child).Unlock()
                        | ReplaceItems(itemDiffs, index) ->
                            for i in 0 .. itemDiffs.Length - 1 do
                                let (Diff(oldChild, newChild)) = itemDiffs.[i]
                                (getIndexProp oldChild).SetValue -1
                                (getIndexProp oldChild).Unlock()
                                (getIndexProp newChild).Lock()
                                (getIndexProp newChild).SetValue(i + index)
                member x.OnFinalUnlock() =
                    for child in list do
                        (getIndexProp child).Unlock() }
        let subscriber = Subscriber.create(LeafStateLock()) behavior    // need to add check for cycles
        list.Subscribe subscriber

        behavior.OnFirstLock()
        for i in 0 .. list.Count - 1 do
            (getIndexProp list.[i]).SetValue i
        behavior.OnFinalUnlock()

    let flattenProperty getInnerList (outerProp : ReactiveProperty<_>) =
        let mutable innerList = getInnerList !!outerProp :> ReactiveList<_>
        let rl = MutableReactiveList<_>(innerList)
        let outerChanged, innerOps = ref false, List()
        let rec onFinalUnblock() =
            if outerChanged.Value then
                innerList.Unsubscribe innerSubscriber
                innerList <- getInnerList !!outerProp
                innerList.Subscribe innerSubscriber
                rl.Clear()
                rl.AddRange innerList
                innerOps.Clear()
                outerChanged := false
            else
                for op in innerOps do
                    match op with
                    | AddItems(items, index) -> rl.InsertRange index items
                    | MoveItems(items, indexDiff) -> rl.MoveRange indexDiff.OldValue indexDiff.NewValue items.Length
                    | RemoveItems(items, index) -> rl.RemoveRange index items.Length
                    | ReplaceItems(itemDiffs, index) -> rl.ReplaceRange index (itemDiffs |> Seq.map Diff.newValue)
                innerOps.Clear()
        and innerSubscriber = rl |> ListSubscriber.bindToList innerOps onFinalUnblock
        innerList.Subscribe innerSubscriber
        let outerSubscriber = rl |> PropertySubscriber.bindToList outerChanged onFinalUnblock
        outerProp.Subscribe outerSubscriber
        Rl.createBase rl

    let collect(collector : 'a -> #ReactiveList<_>)(parentList : ReactiveList<'a>) : ReactiveList<_> =
        let rl = MutableReactiveList<_>(parentList |> Seq.collect collector)
        let childrenRangeList = RangeList(parentList |> Seq.map(fun item -> (collector item).Count))
        let parentOps = List()
        let childSubscriptions = List()
        let rec createSubscription item =
            let ops = List()
            let subscriber = rl |> ListSubscriber.bindToList ops onFinalUnblock
            (collector item).Subscribe subscriber
            ops, subscriber
        and destroySubscription(ops, subscriber) item =
            (ops : List<_>).Clear()
            (collector item).Unsubscribe subscriber
        and onFinalUnblock() =
            for parentOp in parentOps do
                match parentOp with
                | AddItems(items, index) ->
                    rl.InsertRange(childrenRangeList.GetStart index)(items |> Seq.collect collector)
                    childrenRangeList.InsertRange index (items |> Seq.map(fun item -> (collector item).Count))
                    let subscriptions = items |> ArraySeg.map createSubscription
                    childSubscriptions.InsertRange(index, subscriptions)
                | MoveItems(items, Diff(oldIndex, newIndex)) ->
                    let rlOldRange = childrenRangeList.GetRange oldIndex items.Length
                    let rlNewIndex =
                        if oldIndex >= newIndex then childrenRangeList.GetStart newIndex
                        else childrenRangeList.GetStart(newIndex + items.Length) - rlOldRange.Length
                    rl.MoveRange rlOldRange.Start rlNewIndex rlOldRange.Length
                    childrenRangeList.MoveRange oldIndex newIndex items.Length
                    childSubscriptions.MoveRange oldIndex newIndex items.Length
                | RemoveItems(items, index) ->
                    let rlRange = childrenRangeList.GetRange index items.Length
                    rl.RemoveRange rlRange.Start rlRange.Length
                    childrenRangeList.RemoveRange index items.Length
                    items |> ArraySeg.iteri(fun i item -> destroySubscription childSubscriptions.[i + index] item)
                    childSubscriptions.RemoveRange(index, items.Length)
                | ReplaceItems(itemDiffs, index) ->
                    let oldItems = itemDiffs |> Seq.map Diff.oldValue
                    let newItems = itemDiffs |> Seq.map Diff.newValue
                    let rlRange = childrenRangeList.GetRange index itemDiffs.Length
                    rl.RemoveRange rlRange.Start rlRange.Length
                    rl.InsertRange rlRange.Start (newItems |> Seq.collect collector)
                    childrenRangeList.ReplaceRange index (newItems |> Seq.map(fun item -> (collector item).Count))
                    oldItems |> Seq.iteri(fun i item -> destroySubscription childSubscriptions.[i + index] item)
                    let newSubscriptions = newItems |> Seq.map createSubscription |> Array.ofSeq
                    childSubscriptions.ReplaceRange index newSubscriptions
            parentOps.Clear()
            let mutable rli = 0
            childSubscriptions |> Seq.iteri(fun childIndex (childOps, subscriber) ->
                for childOp in childOps do
                    match childOp with
                    | AddItems(items, index) ->
                        childrenRangeList.[childIndex] <- childrenRangeList.[childIndex] + items.Length
                        rl.InsertRange(index + rli) items
                    | MoveItems(items, indexDiff) ->
                        rl.MoveRange(indexDiff.OldValue + rli)(indexDiff.NewValue + rli) items.Length
                    | RemoveItems(items, index) ->
                        childrenRangeList.[childIndex] <- childrenRangeList.[childIndex] - items.Length
                        rl.RemoveRange(index + rli) items.Length
                    | ReplaceItems(itemDiffs, index) ->
                        rl.ReplaceRange(index + rli)(itemDiffs |> Seq.map Diff.newValue)
                childOps.Clear()
                rli <- rli + childrenRangeList.[childIndex])
        childSubscriptions.AddRange(parentList |> Seq.map createSubscription)
        parentList.Subscribe(rl |> ListSubscriber.bindToList parentOps onFinalUnblock)
        Rl.createBase rl

    let chooseImmutable chooser parentList =
        parentList |> collect(chooser >> Option.toList >> ArraySeg.ofSeq >> Rl.createImmutable)


