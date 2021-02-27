namespace Doaz.Reactive

open System
open System.Collections.Generic


[<AbstractClass>]
type ArraySeg<'a> () =
    abstract Length : int
    abstract Item : int -> 'a with get

    member x.AsSeq = Seq.init x.Length (fun i -> x.[i])

    interface IEnumerable<'a> with
        member x.GetEnumerator () =
            x.AsSeq.GetEnumerator ()

    interface System.Collections.IEnumerable with
        member x.GetEnumerator () =
            (x.AsSeq :> System.Collections.IEnumerable).GetEnumerator ()


type private SharedArraySeg<'a> (arr, start, length) =
    inherit ArraySeg<'a> ()

    member x.Array : 'a [] = arr
    member x.Start : int = start
    override x.Length : int = length
    override x.Item
        with get i =
            if i >= length || i < 0 then raise (ArgumentOutOfRangeException "i")
            else arr.[i + start]


module ArraySeg =
    let inline private ofArray values =
        SharedArraySeg (values, 0, values.Length) :> ArraySeg<_>

    let create length value =
        { new ArraySeg<_> () with
            member x.Length = length
            member x.Item
                with get i =
                    if i >= length || i < 0 then raise (ArgumentOutOfRangeException "i")
                    else value }

    let initLazy length initializer =
        { new ArraySeg<_> () with
            member x.Length = length
            member x.Item
                with get i =
                    if i >= length || i < 0 then raise (ArgumentOutOfRangeException "i")
                    else initializer i }

    let empty<'a> =
        { new ArraySeg<'a> () with
            member x.Length = 0
            member x.Item with get i = raise (ArgumentOutOfRangeException "i") }

    let singleton value =
        create 1 value

    let ofArrayUnchecked values =
        ofArray values

    let init length initializer =
        ofArray (Array.init length initializer)

    let ofSeq (values : seq<'a>) =
        ofArray (Array.ofSeq values)

    let ofList (values : 'a list) =
        ofArray (Array.ofList values)

    let ofIList (values : IList<'a>) =
        let valuesArray = Array.zeroCreate values.Count
        values.CopyTo (valuesArray, 0)
        ofArray valuesArray

    let ofIReadOnlyList (values : IReadOnlyList<'a>) =
        ofArray (Array.init values.Count (fun i -> values.[i]))

    let mapOfArray mapper (values : _ []) =
        init values.Length (fun i -> mapper values.[i])

    let cache (aseg : ArraySeg<_>) =
        init aseg.Length (fun i -> aseg.[i])

    let sub start length (aseg : ArraySeg<_>) =
        if start + length > aseg.Length then
            raise (ArgumentOutOfRangeException "start, length")
        match aseg with
        | :? SharedArraySeg<_> as aseg ->
            SharedArraySeg (aseg.Array, aseg.Start + start, length) :> ArraySeg<_>
        | _ ->
            { new ArraySeg<_> () with
                member x.Length = length
                member x.Item with get i = aseg.[i + start] }

    let take length (aseg : ArraySeg<_>) =
        sub 0 length aseg

    let skip length (aseg : ArraySeg<_>) =
        sub length (aseg.Length - length) aseg

    let toSeq (aseg : ArraySeg<_>) =
        aseg :> seq<_>

    let toArray (aseg : ArraySeg<_>) =
        Array.init aseg.Length (fun i -> aseg.[i])

    let toResizeArray (aseg : ArraySeg<_>) =
        let ra = ResizeArray (aseg.Length)
        ra.AddRange (toSeq aseg)
        ra

    let toList (aseg : ArraySeg<_>) =
        List.init aseg.Length (fun i -> aseg.[i])

    let inline length (aseg : ArraySeg<_>) =
        aseg.Length

    let inline isEmpty (aseg : ArraySeg<_>) =
        aseg.Length <= 0

    let iter iterator (aseg : ArraySeg<_>) =
        for i in 0 .. aseg.Length - 1 do
            iterator aseg.[i]

    let iteri iterator (aseg : ArraySeg<_>) =
        for i in 0 .. aseg.Length - 1 do
            iterator i aseg.[i]

    let map mapper (aseg : ArraySeg<_>) =
        init aseg.Length (fun i -> mapper aseg.[i])

    let mapi mapper (aseg : ArraySeg<_>) =
        init aseg.Length (fun i -> mapper i aseg.[i])

    let mapLazy mapper (aseg : ArraySeg<_>) =
        { new ArraySeg<_> () with
            member x.Length = aseg.Length
            member x.Item with get i = mapper aseg.[i] }

    let zipMap mapper (t : ArraySeg<_>) (u : ArraySeg<_>) =
        init t.Length (fun i -> mapper t.[i] u.[i])

    let zipMapLazy mapper (t : ArraySeg<_>) (u : ArraySeg<_>) =
        { new ArraySeg<_> () with
            member x.Length = t.Length
            member x.Item with get i = mapper t.[i] u.[i] }

    let zip3Map mapper (t : ArraySeg<_>) (u : ArraySeg<_>) (v : ArraySeg<_>) =
        init t.Length (fun i -> mapper t.[i] u.[i] v.[i])

    let zip3MapLazy mapper (t : ArraySeg<_>) (u : ArraySeg<_>) (v : ArraySeg<_>) =
        { new ArraySeg<_> () with
            member x.Length = t.Length
            member x.Item with get i = mapper t.[i] u.[i] v.[i] }

    let mapFold mapping state values =
        let mutable acc = state
        let n = length values
        let mutable res = Array.zeroCreate n
        for i = 0 to n - 1 do
            let h', s' = mapping acc values.[i]
            res.[i] <- h';
            acc <- s'
        ofArray res, acc

    let append (t : ArraySeg<_>) (u : ArraySeg<_>) =
        { new ArraySeg<_> () with
            member x.Length = t.Length + u.Length
            member x.Item
                with get i =
                    if i < t.Length then
                        t.[i]
                    else
                        u.[i - t.Length] }

    let append3 (t : ArraySeg<_>) (u : ArraySeg<_>) (v : ArraySeg<_>) =
        { new ArraySeg<_> () with
            member x.Length = t.Length + u.Length + v.Length
            member x.Item
                with get i =
                    if i < t.Length then
                        t.[i]
                    else if i < t.Length + u.Length then
                        u.[i - t.Length]
                    else
                        v.[i - t.Length - u.Length] }

    let concat asegs =
        Seq.concat asegs
        |> ofSeq

    let collect collector (aseg : ArraySeg<_>) =
        toSeq aseg
        |> Seq.collect collector
        |> ofSeq

    let find predicate (aseg : ArraySeg<_>) =
        Seq.find predicate (toSeq aseg)

    let findIndex predicate (aseg : ArraySeg<_>) =
        Seq.findIndex predicate (toSeq aseg)

    let remove index (aseg : ArraySeg<_>) =
        { new ArraySeg<_> () with
            member x.Length = aseg.Length - 1
            member x.Item with get i = if i < index then aseg.[i] else aseg.[i + 1] }

    let rotate amount (aseg : ArraySeg<_>) =
        let length = aseg.Length
        { new ArraySeg<_> () with
            member x.Length = length
            member x.Item with get i = aseg.[(i + amount) % length] }

    let rotateBack amount (aseg : ArraySeg<_>) =
        let length = aseg.Length
        { new ArraySeg<_> () with
            member x.Length = length
            member x.Item with get i = aseg.[(i + length - amount) % length] }

    let fold folder state (aseg : ArraySeg<_>) =
        let mutable state = state
        for i in 0 .. aseg.Length - 1 do
            state <- folder state aseg.[i]
        state

    let foldBack folder (aseg : ArraySeg<_>) state =
        let mutable state = state
        for i = aseg.Length - 1 downto 0 do
            state <- folder aseg.[i] state
        state

    let filter predicate (aseg : ArraySeg<_>) =
        ofSeq (Seq.filter predicate (toSeq aseg))

    let contains item (aseg : ArraySeg<_>) =
        Seq.exists (fun value -> obj.Equals (value, item)) (toSeq aseg)

    let exists predicate (aseg : ArraySeg<_>) =
        Seq.exists predicate (toSeq aseg)

    let forall predicate (aseg : ArraySeg<_>) =
        Seq.forall predicate (toSeq aseg)

    let partition predicate (aseg : ArraySeg<_>) =
        let a, b = Array.partition predicate (toArray aseg)
        ofArray a, ofArray b

    let maxBy proj (aseg : ArraySeg<_>) =
        if aseg.Length = 0 then raise (ArgumentException "aseg.Length = 0")
        let mutable accv = aseg.[0]
        let mutable acc = proj accv
        for i = 1 to aseg.Length - 1 do
            let currv = aseg.[i]
            let curr = proj currv
            if curr > acc then
                acc <- curr
                accv <- currv
        accv

    let inline sumBy proj (aseg : ArraySeg<_>) =
        let mutable acc = LanguagePrimitives.GenericZero
        for i = 0 to aseg.Length - 1 do
            acc <- acc + proj aseg.[i]
        acc

    let toIReadOnlyList (aseg : ArraySeg<_>) =
        { new IReadOnlyList<'a> with
                member __.Item
                    with get index = aseg.[index]
            interface IReadOnlyCollection<'a> with
                member __.Count = aseg.Length
            interface IEnumerable<'a> with
                member __.GetEnumerator () =
                    (toSeq aseg).GetEnumerator ()
            interface System.Collections.IEnumerable with
                member __.GetEnumerator () =
                    (toSeq aseg).GetEnumerator () :> _ }

    let toIList (aseg : ArraySeg<_>) =
        { new IList<'a> with
                member x.IndexOf item = findIndex ((=) item) aseg
                member x.Insert (index, item) = raise (NotSupportedException ())
                member x.Item
                    with get index = aseg.[index]
                    and set index v = raise (NotSupportedException ())
                member x.RemoveAt index = raise (NotSupportedException ())

            interface ICollection<'a> with
                member x.Add item = raise (NotSupportedException ())
                member x.Clear () = raise (NotSupportedException ())
                member x.Contains item = contains item aseg
                member x.CopyTo (array, arrayIndex) = (toArray aseg).CopyTo (array, arrayIndex)
                member x.Count = aseg.Length
                member x.IsReadOnly = true
                member x.Remove item = raise (NotSupportedException ())

            interface IEnumerable<'a> with
                member x.GetEnumerator () = (toSeq aseg).GetEnumerator ()

            interface System.Collections.IEnumerable with
                member x.GetEnumerator () = (toSeq aseg).GetEnumerator () :> _ }

    let toBoxedIList (aseg : ArraySeg<_>) =
        { new System.Collections.IList with
                member x.Add value = raise (NotSupportedException ())
                member x.Clear () = raise (NotSupportedException ())
                member x.Contains value = contains value (unbox aseg)
                member x.IndexOf value = findIndex ((=) value) (unbox aseg)
                member x.Insert (index, value) = raise (NotSupportedException ())
                member x.IsFixedSize = true
                member x.IsReadOnly = true
                member x.Item
                    with get index = box aseg.[index]
                    and set index v = raise (NotSupportedException ())
                member x.Remove value = raise (NotSupportedException ())
                member x.RemoveAt index = raise (NotSupportedException ())

            interface System.Collections.ICollection with
                member x.CopyTo (array, index) = (toArray aseg :> System.Collections.ICollection).CopyTo (array, index)
                member x.Count = aseg.Length
                member x.IsSynchronized = false
                member x.SyncRoot = box aseg

            interface System.Collections.IEnumerable with
                member x.GetEnumerator () = (toSeq aseg).GetEnumerator () :> _ }


