namespace Vogen.Synth.Romanization

open Doaz.Reactive
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Text


type RomTrieNode =
    | RomTrieNode of romsArr : string [] * subTrie : ImmutableDictionary<string, RomTrieNode>

module RomTrie =
    let rec ofFlatCsvEntries(csvEntries : IEnumerable<KeyValuePair<string, string []>>) =
        csvEntries
        |> Seq.groupBy(fun (KeyValue(text, romsArr)) -> text.[0])
        |> Seq.map(fun (ch, chEntries) ->
            let localDict = ImmutableDictionary.CreateRange chEntries
            let romsArr = localDict.GetValueOrDefault(ch.ToString(), Array.empty)
            let subTrie =
                localDict.Remove(ch.ToString())
                |> Seq.map(fun (KeyValue(chs, romsArr)) -> KeyValuePair(chs.[1..], romsArr))
                |> ImmutableDictionary.CreateRange
                |> ofFlatCsvEntries
            KeyValuePair(ch.ToString(), RomTrieNode(romsArr, subTrie)))
        |> ImmutableDictionary.CreateRange

    let matchRoms fallback (chs : _ [])(romHints : _ []) trie =
        if chs.Length <> romHints.Length then
            raise(ArgumentException($"chs.Length ({chs.Length}) <> romHints.Length ({romHints.Length})"))

        let rec matchLongestList(trie : ImmutableDictionary<_, _>) offsetIndex advanceIndex =
            if offsetIndex + advanceIndex >= chs.Length then None    // Empty list
            else
                let ch = chs.[offsetIndex + advanceIndex]
                let romHintOp =
                    let romHintRaw = romHints.[offsetIndex + advanceIndex]
                    if String.IsNullOrEmpty romHintRaw then None else Some romHintRaw
                match trie.TryGetValue ch with
                | false, _ ->   // Trie node not found
                    if advanceIndex = 0 then romHintOp else None
                | true, RomTrieNode(romsArr, subTrie) ->
                    let subNodeResult : string option = matchLongestList subTrie offsetIndex (advanceIndex + 1)
                    match subNodeResult, romHintOp with
                    | Some roms, Some romHint when roms.Split().[advanceIndex] = romHint -> Some roms
                    | Some roms, None -> Some roms
                    | _, Some romHint when advanceIndex = 0 -> Some romHint
                    | _, Some romHint -> None
                    | _, _ ->
                        match romsArr with
                        | [| |] -> None
                        | _ -> Some romsArr.[0]

        let rec matchAndAdvance offsetIndex = seq {
            if offsetIndex < chs.Length then
                match matchLongestList trie offsetIndex 0 with
                | Some roms ->
                    let romArr = roms.Split()
                    yield! romArr
                    yield! matchAndAdvance(offsetIndex + romArr.Length)
                | None ->
                    yield fallback
                    yield! matchAndAdvance(offsetIndex + 1) }

        matchAndAdvance 0
        |> Seq.zip chs
        |> Seq.map(fun (ch, rom) ->
            let moreRoms =
                match trie.TryGetValue ch with
                | true, RomTrieNode(romsArr, subTrie) -> romsArr
                | false, _ -> [| |]
            Seq.prependItem rom moreRoms
            |> Seq.distinct
            |> Array.ofSeq)
        |> Array.ofSeq


