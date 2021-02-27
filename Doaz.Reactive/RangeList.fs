namespace Doaz.Reactive

open System
open System.Collections.Generic
open System.Text


[<Struct>]
type Range =
    val Start : int
    val Length : int

    new(start, length) = { Start = start; Length = length }
    member x.End = x.Start + x.Length
    override x.ToString() = sprintf "[%d, %d)" x.Start x.End

    static member ofStartEnd vStart vEnd = Range(vStart, vEnd - vStart)
    static member startOf(range : Range) = range.Start
    static member length(range : Range) = range.Length
    static member endOf(range : Range) = range.End

module Avl =
    type tree =     // leaf has height 1, count 1
        internal
        | Leaf of length : int
        | Node of left : tree * height : int * length : int * count : int * right : tree

        member x.Height = match x with | Leaf length -> 1            | Node(left, height, length, count, right) -> height
        member x.Length = match x with | Leaf length -> length       | Node(left, height, length, count, right) -> length
        member x.Count  = match x with | Leaf length -> 1            | Node(left, height, length, count, right) -> count
        member x.Data   = match x with | Leaf length -> 1, length, 1 | Node(left, height, length, count, right) -> height, length, count

        member x.GetChildren() =
            match x with
            | Leaf length -> None
            | Node(left, height, length, count, right) -> Some(left, right)

        override x.ToString() =
            let balanced, height = treeDebug.checkBalanceAndHeight x
            let correct, length, count = treeDebug.checkLengthAndCount x
            treeDebug.prettyPrint x + Environment.NewLine +
            sprintf "Balanced: %b, Real Height: %d" balanced height + Environment.NewLine +
            sprintf "Data Correct: %b, Real Length: %d, Real Count: %d" correct length count

    and treeDebug =
        static member checkBalanceAndHeight tree =
            match tree with
            | Leaf length -> true, 1
            | Node(left, height, length, count, right) ->
                let lbalanced, lheight = treeDebug.checkBalanceAndHeight left
                let rbalanced, rheight = treeDebug.checkBalanceAndHeight right
                let balanced = lbalanced && rbalanced && abs(lheight - rheight) <= 1
                let height = max lheight rheight + 1
                balanced, height

        static member checkLengthAndCount tree =
            match tree with
            | Leaf length -> true, length, 1
            | Node(left, height, length, count, right) ->
                let lcorrect, llength, lcount = treeDebug.checkLengthAndCount left
                let rcorrect, rlength, rcount = treeDebug.checkLengthAndCount right
                let correct = lcorrect && rcorrect && length = llength + rlength && count = lcount + rcount
                let length = llength + rlength
                let count = lcount + rcount
                correct, length, count

        static member prettyPrint(tree : tree) =
            let strbs = Array.init(tree.Height + 1)(fun _ -> StringBuilder())
            let fillCount layerIndex refLayerIndex =
                max 0 (strbs.[refLayerIndex].Length - strbs.[layerIndex].Length)
            let rec fillStrb layerIndex tree =
                match tree with
                | Leaf length ->
                    strbs.[layerIndex].Append(length).Append(' ') |> ignore
                    for i in layerIndex + 1 .. strbs.Length - 1 do
                        strbs.[i].Append(' ', fillCount i (i - 1)) |> ignore
                | Node(left, height, length, count, right) ->
                    strbs.[layerIndex].Append(length) |> ignore
                    fillStrb(layerIndex + 1) left
                    strbs.[layerIndex].Append('─', fillCount layerIndex (layerIndex + 1)).Append('┐') |> ignore
                    fillStrb(layerIndex + 1) right
                    strbs.[layerIndex].Append(' ', fillCount layerIndex (layerIndex + 1)) |> ignore
            fillStrb 0 tree
            Array.map(sprintf "%s%O" Environment.NewLine) strbs
            |> String.concat ""

    let leaf value = Leaf value

    let length(tree : tree) = tree.Length

    let count(tree : tree) = tree.Count

    let rec toSeq tree = seq {
        match tree with
        | Leaf length ->
            yield length
        | Node(left, height, length, count, right) ->
            yield! toSeq left
            yield! toSeq right }

    let toArray tree = Array.ofSeq(toSeq tree)

    let append(left : tree)(right : tree) =
        let lheight, llength, lcount = left.Data
        let rheight, rlength, rcount = right.Data
        Node(left, max lheight rheight + 1, llength + rlength, lcount + rcount, right)

    let rec private concat' minh (trees : seq<tree>) =
        // 1 -> 1
        // 0 0 -> 1
        // 0 1 0 -> 1 1
        // 0 1 1 -> 2 1
        let newTrees = List()
        (None, trees) ||> Seq.fold(fun t1t2 t3 ->
            match t1t2 with
            | None when t3.Height = minh ->
                Some(t3, None)
            | None ->
                newTrees.Add t3; None
            | Some(t1, None) when t3.Height = minh ->
                newTrees.Add(append t1 t3); None
            | Some(t1, None) ->
                Some(t1, Some t3)
            | Some(t1, Some t2) when t3.Height = minh ->
                let t2l, t2r = Option.get(t2.GetChildren())
                newTrees.Add(append t1 t2l); newTrees.Add(append t2r t3); None
            | Some(t1, Some t2) ->
                newTrees.Add(append t1 t2); newTrees.Add t3; None)
        |>  if newTrees.Count = 0 then
                function
                | None -> raise(ArgumentException("trees.Length = 0"))
                | Some(t1, None) -> t1
                | Some(t1, Some t2) -> append t1 t2
            else fun state ->
                match state with
                | None -> ()
                | Some(t1, None) -> newTrees.[newTrees.Count - 1] <- append(newTrees.[newTrees.Count - 1]) t1
                | Some(t1, Some t2) -> newTrees.Add(append t1 t2)
                concat'(minh + 1) newTrees

    let rec concat(trees : seq<tree>) =
        let trees = Seq.cache trees
        let minheight = trees |> Seq.map(fun tree -> tree.Height) |> Seq.min
        concat' minheight trees

    let ofSeq values =
        concat(Seq.map Leaf values)

    let rec private debranch maxHeight tree = seq {
        match tree with
        | Node(left, height, length, count, right) when height > maxHeight ->
            yield! debranch maxHeight left
            yield! debranch maxHeight right
        | _ ->
            yield tree }

    let private ensureBalance tree =
        match tree with
        | Node(left, height, length, count, right) when abs(left.Height - right.Height) > 1 ->
            let childTreeHeight = min left.Height right.Height
            debranch childTreeHeight tree
            |> concat'(childTreeHeight - 1)
        | _ -> tree

    let rec insert index newTree tree =
        if index = 0 then
            append newTree tree
        elif index = tree.Count then
            append tree newTree
        else
            match tree with
            | Leaf length -> raise(ArgumentOutOfRangeException("index"))
            | Node(left, height, length, count, right) ->
                if index < left.Count then
                    append(insert index newTree left) right
                else
                    append left (insert(index - left.Count) newTree right)
        |> ensureBalance

    let rec sub startIndex itemCount tree = seq {
        match tree with
        | Leaf length ->
            match startIndex, itemCount with
            | 0, 0 | 1, 0 -> ()
            | 0, 1 -> yield length
            | _ ->
                eprintfn "%A" (startIndex, itemCount)
                raise(ArgumentOutOfRangeException("startIndex or itemCount"))
        | Node(left, height, length, count, right) ->
            if startIndex < left.Count then
                let newItemCount = min itemCount (left.Count - startIndex)
                yield! sub startIndex newItemCount left
            if startIndex + itemCount > left.Count then
                let newStartIndex = max 0 (startIndex - left.Count)
                let newItemCount = startIndex + itemCount - left.Count - newStartIndex
                yield! sub newStartIndex newItemCount right }

    let rec remove startIndex itemCount tree =
        match tree with
        | Leaf length ->
            match startIndex, itemCount with
            | 0, 0 | 1, 0 -> Some tree
            | 0, 1 -> None
            | _ ->
                eprintfn "%A" (startIndex, itemCount)
                raise(ArgumentOutOfRangeException("startIndex or itemCount"))
        | Node(left, height, length, count, right) ->
            if startIndex = 0 && itemCount = count then
                None
            else
                let newLeft =
                    if startIndex < left.Count then
                        let newItemCount = min itemCount (left.Count - startIndex)
                        remove startIndex newItemCount left
                    else
                        Some left
                let newRight =
                    if startIndex + itemCount > left.Count then
                        let newStartIndex = max 0 (startIndex - left.Count)
                        let newItemCount = startIndex + itemCount - left.Count - newStartIndex
                        remove newStartIndex newItemCount right
                    else
                        Some right
                match newLeft, newRight with
                | None, None -> raise(InvalidOperationException("Should not reach this branch"))
                | side, None | None, side -> side
                | Some newLeft, Some newRight -> Some(append newLeft newRight |> ensureBalance)

    let rec getRange startIndex itemCount tree =
        match tree with
        | Leaf length ->
            match startIndex, itemCount with
            | 0, 0 -> Range(0, 0)
            | 1, 0 -> Range(length, 0)
            | 0, 1 -> Range(0, length)
            | _ ->
                eprintfn "%A" (startIndex, itemCount)
                raise(ArgumentOutOfRangeException("startIndex or itemCount"))
        | Node(left, height, length, count, right) ->
            if startIndex = 0 && itemCount = count then
                Range(0, length)
            else
                if startIndex + itemCount <= left.Count then
                    getRange startIndex itemCount left
                elif startIndex >= left.Count then
                    let range = getRange(startIndex - left.Count) itemCount right
                    Range(range.Start + left.Length, range.Length)
                else
                    let lrange = getRange startIndex (left.Count - startIndex) left
                    let rrange = getRange 0 (startIndex + itemCount - left.Count) right
                    Range(lrange.Start, lrange.Length + rrange.Length)

    let rec set startIndex items tree =
        match tree with
        | Leaf length ->
            match startIndex, ArraySeg.length items with
            | 0, 0 | 1, 0 -> tree
            | 0, 1 -> Leaf items.[0]
            | _ ->
                eprintfn "%A" (startIndex, ArraySeg.length items)
                raise(ArgumentOutOfRangeException("startIndex or items.Length"))
        | Node(left, height, length, count, right) ->
            let newLeft, newRight =
                if startIndex + items.Length <= left.Count then
                    set startIndex items left, right
                elif startIndex >= left.Count then
                    left, set(startIndex - left.Count) items right
                else
                    set startIndex (items |> ArraySeg.sub 0 (left.Count - startIndex)) left,
                    set 0 (items |> ArraySeg.sub(left.Count - startIndex)(startIndex + items.Length - left.Count)) right
            Node(newLeft, height, newLeft.Length + newRight.Length, count, newRight)

module AvlOp =
    type treeDebug =
        static member checkBalanceAndHeight treeOp =
            match treeOp with
            | None -> true, 0
            | Some tree -> Avl.treeDebug.checkBalanceAndHeight tree

        static member checkLengthAndCount treeOp =
            match treeOp with
            | None -> true, 0, 0
            | Some tree -> Avl.treeDebug.checkLengthAndCount tree

        static member prettyPrint treeOp =
            match treeOp with
            | None -> ""
            | Some tree -> Avl.treeDebug.prettyPrint tree

    let leaf value = Some(Avl.leaf value)

    let length treeOp = match treeOp with | None -> 0 | Some tree -> Avl.length tree

    let count treeOp = match treeOp with | None -> 0 | Some tree -> Avl.count tree

    let toSeq treeOp =
        match treeOp with
        | Some tree -> Avl.toSeq tree
        | None -> Seq.empty

    let toArray tree = Array.ofSeq(toSeq tree)

    let append left right =
        match left, right with
        | None, None -> None
        | Some left, None -> Some left
        | None, Some right -> Some right
        | Some left, Some right -> Some(Avl.append left right)

    let concat trees =
        if Seq.isEmpty trees then None else Some(Avl.concat trees)

    let concatOp treeOps =
        concat(Seq.choose id treeOps)

    let ofSeq values =
        ArraySeg.ofSeq(Seq.map Avl.Leaf values)
        |> concat

    let insert index newTreeOp treeOp =
        match treeOp, newTreeOp with
        | None, None -> None
        | Some tree, None -> Some tree
        | None, Some newTree -> Some newTree
        | Some tree, Some newTree -> Some(Avl.insert index newTree tree)

    let sub startIndex itemCount treeOp =
        match startIndex, itemCount, treeOp with
        | 0, 0, None -> Seq.empty
        | _, _, None -> raise(ArgumentOutOfRangeException("startIndex or itemCount"))
        | _, _, Some tree -> Avl.sub startIndex itemCount tree

    let remove startIndex itemCount treeOp =
        match startIndex, itemCount, treeOp with
        | 0, 0, None -> None
        | _, _, None -> raise(ArgumentOutOfRangeException("startIndex or itemCount"))
        | _, _, Some tree -> Avl.remove startIndex itemCount tree

    let getRange startIndex itemCount treeOp =
        match startIndex, itemCount, treeOp with
        | 0, 0, None -> Range(0, 0)
        | _, _, None -> raise(ArgumentOutOfRangeException("startIndex or itemCount"))
        | _, _, Some tree -> Avl.getRange startIndex itemCount tree

    let set startIndex items treeOp =
        match startIndex, ArraySeg.length items, treeOp with
        | 0, 0, None -> None
        | _, _, None -> raise(ArgumentOutOfRangeException("startIndex or items.Length"))
        | _, _, Some tree -> Some(Avl.set startIndex items tree)

type RangeList(ranges) =
    let mutable tree = AvlOp.ofSeq ranges

    new() = RangeList(Seq.empty)

    member x.TotalLength = AvlOp.length tree
    member x.Count = AvlOp.count tree
    member x.Item
        with get index = tree |> AvlOp.getRange index 1 |> Range.length
        and set index value = tree <- tree |> AvlOp.set index (ArraySeg.singleton value)

    member x.Get index =
        tree |> AvlOp.getRange index 1

    member x.GetRange index count =
        tree |> AvlOp.getRange index count

    member x.GetStart index =
        tree |> AvlOp.getRange index 0 |> Range.startOf

    member x.Add item =
        tree <- tree |> AvlOp.insert(AvlOp.count tree)(AvlOp.leaf item)

    member x.AddRange items =
        tree <- tree |> AvlOp.insert(AvlOp.count tree)(AvlOp.ofSeq items)

    member x.Clear() =
        tree <- None

    member x.RemoveAt index =
        tree <- tree |> AvlOp.remove index 1

    member x.RemoveRange index count =
        tree <- tree |> AvlOp.remove index count

    member x.Insert index item =
        tree <- tree |> AvlOp.insert index (AvlOp.leaf item)

    member x.InsertRange index items =
        tree <- tree |> AvlOp.insert index (AvlOp.ofSeq items)

    member x.Move oldIndex newIndex =
        let item = tree |> AvlOp.sub oldIndex 1
        tree <- tree |> AvlOp.remove oldIndex 1 |> AvlOp.insert newIndex (AvlOp.ofSeq item)

    member x.MoveRange oldIndex newIndex count =
        let items = tree |> AvlOp.sub oldIndex count
        tree <- tree |> AvlOp.remove oldIndex count |> AvlOp.insert newIndex (AvlOp.ofSeq items)

    member x.Replace index item =
        tree <- tree |> AvlOp.set index (ArraySeg.singleton item)

    member x.ReplaceRange index items =
        tree <- tree |> AvlOp.set index (ArraySeg.ofSeq items)

    interface IEnumerable<int> with
        member x.GetEnumerator() = (AvlOp.toSeq tree).GetEnumerator()

    interface System.Collections.IEnumerable with
        member x.GetEnumerator() = (AvlOp.toSeq tree).GetEnumerator() :> _




