namespace Doaz.Reactive

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Windows
open System.Windows.Input


[<RequireQualifiedAccess>]
type TypeArgs<'a> =
    | TypeArgs

[<AutoOpen>]
module Util =
    let (|>!) a f = f a; a

    let inline (|||>) (a, b, c) f = f a b c

    let rec fix f x = f (fix f) x
    let rec fix2 f x y = f (fix f) x y

    let inline zeroIfInf v = if Double.IsInfinity v then 0.0 else v

    let inline isNotNull value = not (isNull value)

    let inline typeArgs<'a> = TypeArgs<'a>.TypeArgs

    let inline nullCheck fallbackValue value =
        match value with
        | null -> fallbackValue
        | _ -> value

    let memoize f =
        let cache = Dictionary<_, _> ()
        fun t ->
            match cache.TryGetValue t with
            | false, _ ->
                let v = f t
                cache.Add (t, v)
                v
            | true, v ->
                v

    type ObservableCollection<'a> with
        member x.AddRange items =
            Seq.iter x.Add items

    type Array =
        static member ofParams ([<ParamArray>] items : 'a []) = items

    type List<'a> with
        member inline x.ForceRemove item =
            let removed = x.Remove item
            if not removed then
                raise (KeyNotFoundException ())

        member x.SubArray index count =
            Array.init count (fun i -> x.[i + index])

        member x.Move oldIndex newIndex =
            let item = x.[oldIndex]
            x.RemoveAt oldIndex
            x.Insert(newIndex, item)

        member x.MoveRange oldIndex newIndex count =
            let items = x.SubArray oldIndex count
            x.RemoveRange(oldIndex, count)
            x.InsertRange(newIndex, items)

        member x.ReplaceRange index newItems =
            for item in newItems do
                x.[index] <- item

    type Dictionary<'k, 'v> with
        member inline x.ForceRemove item =
            let removed = x.Remove item
            if not removed then
                raise (KeyNotFoundException ())


[<CustomEquality>]
[<NoComparison>]
type UnitNeq =
    | UnitNeq
    override x.Equals y = false
    override x.GetHashCode () = 0


module Option =
    let ofByRef (isSuccess, value) =
        if isSuccess then Some value else None

    let ofBool value =
        if value then Some () else None


type Counter () =
    let mutable value = 0UL

    member x.Peek () = value

    member x.Register () =
        let currentValue = value
        value <- Checked.(+) value 1UL
        currentValue


type LateInit<'a> () =
    let mutable value : Option<'a> = None

    member x.TryGetValue () = value

    member x.Value =
        match value with
        | Some value -> value
        | None -> raise (InvalidOperationException ("The LateInit value has not been set."))

    member x.Set newValue =
        match value with
        | Some value -> raise (InvalidOperationException ("The LateInit value has already been set."))
        | None -> value <- Some newValue

type LateInit =
    static member FromValue value =
        let li = LateInit ()
        li.Set value
        li

    static member inline set newValue (value : LateInit<_>) =
        value.Set newValue


module Seq =
    let private headTailEnumerator (source : seq<_>) =
        let ie = source.GetEnumerator ()
        if ie.MoveNext () then
            Some (ie.Current, ie)
        else
            None

    let pairwiseCirc source =
        seq {
            match headTailEnumerator source with
            | None -> ()
            | Some (head, tail) ->
                let mutable prev = head
                while tail.MoveNext () do
                    let curr = tail.Current
                    yield prev, curr
                    prev <- curr
                yield prev, head }

    let headTailLazy source =
        if Seq.isEmpty source then
            None
        else
            Some (Seq.head source, Seq.skip 1 source)

    let headTail source =
        headTailLazy (Seq.cache source)

    let zip4 s1 s2 s3 s4 =
        Seq.zip (Seq.zip3 s1 s2 s3) s4
        |> Seq.map (fun ((a1, a2, a3), a4) -> a1, a2, a3, a4)

    let partitionBy (proj : 'a -> 'key) source =
        seq {
            let enumerator = (source : seq<_>).GetEnumerator ()
            if enumerator.MoveNext () then
                let acc = List ()
                acc.Add enumerator.Current
                let mutable currKey = proj enumerator.Current
                while enumerator.MoveNext () do
                    let itemKey = proj enumerator.Current
                    if itemKey = currKey then
                        acc.Add enumerator.Current
                    else
                        yield currKey, acc.ToArray ()
                        acc.Clear ()
                        acc.Add enumerator.Current
                        currKey <- itemKey
                yield currKey, acc.ToArray () }

    let partitionBeforeWhen pred source =
        seq {
            let enumerator = (source : seq<_>).GetEnumerator ()
            if enumerator.MoveNext () then
                let acc = List ()
                acc.Add enumerator.Current
                while enumerator.MoveNext () do
                    if pred enumerator.Current then
                        yield acc.ToArray ()
                        acc.Clear ()
                    acc.Add enumerator.Current
                yield acc.ToArray () }

    let partitionAfterWhen pred source =
        seq {
            let enumerator = (source : seq<_>).GetEnumerator ()
            let acc = List ()
            while enumerator.MoveNext () do
                acc.Add enumerator.Current
                if pred enumerator.Current then
                    yield acc.ToArray ()
                    acc.Clear ()
            if acc.Count > 0 then
                yield acc.ToArray () }

    let partitionBetween pred source =
        seq {
            let enumerator = (source : seq<_>).GetEnumerator ()
            if enumerator.MoveNext () then
                let acc = List ()
                acc.Add enumerator.Current
                while enumerator.MoveNext () do
                    if pred acc.[acc.Count - 1] enumerator.Current then
                        yield acc.ToArray ()
                        acc.Clear ()
                    acc.Add enumerator.Current
                yield acc.ToArray () }

    let join separator source =
        seq {
            let enumerator = (source : seq<_>).GetEnumerator ()
            if enumerator.MoveNext () then
                yield! enumerator.Current
            while enumerator.MoveNext () do
                yield! separator
                yield! enumerator.Current }

    let prepend source1 source2 =
        seq {
            yield! source2
            yield! source1 }

    let prependItem item source =
        seq {
            yield item
            yield! source }

    let appendItem item source =
        seq {
            yield! source
            yield item }

    let shuffle (rand : Random) values =
        let values = Array.ofSeq values
        for i in values.Length - 1 .. -1 .. 1 do
            let j = rand.Next (i + 1)
            let temp = values.[i]
            values.[i] <- values.[j]
            values.[j] <- temp
        values :> seq<_>

    let diff seq1 seq2 =
        let l1 = List (seq1 : seq<_>)
        let l2 = List ()
        seq2
        |> Seq.iter (fun s2item ->
            if l1.Remove s2item then () else l2.Add s2item)
        l1 :> seq<_>, l2 :> seq<_>

    let applyFuncs funcs arg =
        Seq.map (fun f -> f arg) funcs


module Map =
    let choose chooser table =
        Map.toSeq table
        |> Seq.choose (fun (k, v) -> chooser k v |> Option.map (fun v -> k, v))
        |> Map.ofSeq


module Dict =
    let create (keyValuePairs : seq<KeyValuePair<_, _>>) =
        System.Linq.Enumerable.ToDictionary (
            keyValuePairs,
            (fun (KeyValue (key, value)) -> key),
            (fun (KeyValue (key, value)) -> value))

    let ofSeq (keyValuePairs : seq<_ * _>) =
        System.Linq.Enumerable.ToDictionary (
            keyValuePairs,
            (fun (key, value) -> key),
            (fun (key, value) -> value))

    let find key (table : Dictionary<_, _>) =
        table.[key]

    let tryFind key (table : Dictionary<_, _>) =
        table.TryGetValue key |> Option.ofByRef


type 'a list1 =
    | List1 of v : 'a * vs : 'a list

module List1 =
    let rev l =
        let (List1 (v, vs)) = l
        let rec revAcc v vs acc =
            match vs with
            | [] -> List1 (v, acc)
            | v2 :: vs -> revAcc v2 vs (v :: acc)
        revAcc v vs []

    let toSeq l =
        let (List1 (v, vs)) = l
        seq {
            yield v
            yield! vs }


module Disposable =
    let empty =
        { new IDisposable with
            member __.Dispose () = () }

    let inline create dispose =
        { new IDisposable with
            member __.Dispose () = dispose () }

    let createOnce dispose =
        let dispose = Lazy<_>.Create dispose
        { new IDisposable with
            member __.Dispose () = (dispose : Lazy<_>).Force () }

    let inline dispose (d : IDisposable) =
        d.Dispose ()

    let join mapper collection =
        let children = Array.ofSeq (Seq.map mapper collection)
        { new IDisposable with
            member __.Dispose () =
                Array.iter dispose children }


module Lazy =
    let inline force (v : Lazy<_>) =
        v.Force ()

    let inline isValueCreated (v : Lazy<_>) =
        v.IsValueCreated


[<Struct>]
type Diff<'a> (oldValue : 'a, newValue : 'a) =
    member x.OldValue = oldValue
    member x.NewValue = newValue

[<AutoOpen>]
module DiffExt =
    let (|Diff|) (diff : Diff<_>) = diff.OldValue, diff.NewValue

module Diff =
    let inline oldValue (diff : Diff<_>) = diff.OldValue
    let inline newValue (diff : Diff<_>) = diff.NewValue


module Event =
    let mapDelegate sender mapper sourceEvent =
        let ev = Event<_, _> ()

        sourceEvent |> Event.add (fun e -> ev.Trigger (sender, mapper e))

        ev.Publish

    let empty =
        {   new IEvent<'h, 't>
            interface IObservable<'t> with
                member __.Subscribe _ = Disposable.empty
            interface IDelegateEvent<'h> with
                member __.AddHandler _ = ()
                member __.RemoveHandler _ = () }


module ObservableCollection =
    let subscribe action (collection : ObservableCollection<'a>) =
        collection.CollectionChanged.Add (fun _ -> action ())

    let subscribeAll addHandlers removeHandlers (collection : ObservableCollection<'a>) =
        collection.CollectionChanged.Add (fun e ->
            if e.OldItems <> null then
                e.OldItems |> Seq.cast<'a> |> Seq.iter removeHandlers
            if e.NewItems <> null then
                e.NewItems |> Seq.cast<'a> |> Seq.iter addHandlers)


module ReadOnlyList =
    let empty<'a> =
        { new IReadOnlyList<'a> with
            member __.Item
                with get index = raise (ArgumentOutOfRangeException ())
        interface IReadOnlyCollection<'a> with
            member __.Count = 0
        interface IEnumerable<'a> with
            member __.GetEnumerator () =
                (Seq.empty<'a>).GetEnumerator ()
        interface System.Collections.IEnumerable with
            member __.GetEnumerator () =
                (Seq.empty<'a>).GetEnumerator () :> _ }

    let ofIList (collection : IList<_>) =
        { new IReadOnlyList<'a> with
            member __.Item
                with get index = collection.[index]
        interface IReadOnlyCollection<'a> with
            member __.Count = collection.Count
        interface IEnumerable<'a> with
            member __.GetEnumerator () =
                collection.GetEnumerator ()
        interface System.Collections.IEnumerable with
            member __.GetEnumerator () =
                collection.GetEnumerator () :> _ }

    let map mapper (collection : IReadOnlyList<_>) =
        { new IReadOnlyList<'a> with
            member __.Item
                with get index = mapper collection.[index]
        interface IReadOnlyCollection<'a> with
            member __.Count = collection.Count
        interface IEnumerable<'a> with
            member __.GetEnumerator () =
                (Seq.map mapper collection).GetEnumerator ()
        interface System.Collections.IEnumerable with
            member __.GetEnumerator () =
                (Seq.map mapper collection).GetEnumerator () :> _ }


module ReadOnlyCollection =
    let map mapper (collection : IReadOnlyCollection<_>) =
        { new IReadOnlyCollection<'a> with
            member __.Count = collection.Count
        interface IEnumerable<'a> with
            member __.GetEnumerator () =
                (Seq.map mapper collection).GetEnumerator ()
        interface System.Collections.IEnumerable with
            member __.GetEnumerator () =
                (Seq.map mapper collection).GetEnumerator () :> _ }

    let ofCollection (collection : #ICollection<_>) =
        { new IReadOnlyCollection<'a> with
            member __.Count = collection.Count
        interface IEnumerable<'a> with
            member __.GetEnumerator () =
                collection.GetEnumerator ()
        interface System.Collections.IEnumerable with
            member __.GetEnumerator () =
                collection.GetEnumerator () :> _ }


[<AutoOpen>]
module ReverseIndexExtensions =
    type List<'a> with
        member x.GetReverseIndex(_, i) = x.Count - 1 - i

    type IList<'a> with
        member x.GetReverseIndex(_, i) = x.Count - 1 - i

    type IReadOnlyList<'a> with
        member x.GetReverseIndex(_, i) = x.Count - 1 - i

    type ImmutableArray<'a> with
        member x.GetReverseIndex(_, i) = x.Length - 1 - i

    type ImmutableList<'a> with
        member x.GetReverseIndex(_, i) = x.Count - 1 - i


