namespace Vogen.Client.Romanization

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Text
open System.Text.RegularExpressions

#nowarn "40"


[<NoComparison; ReferenceEquality>]
type JsonRomDict = {
    [<JsonProperty("dict", Required=Required.Always)>] Dict : ImmutableDictionary<string, string []> }

type IRomanizer =
    abstract Convert : chs : string [] -> romHints : string [] -> string [] []

module Romanizer =
    let createEmpty fallbackRom =
        { new IRomanizer with
            member x.Convert chs romHints = Array.create chs.Length [| fallbackRom |] }

    let createRomanizer fallbackRom jsonDictFileName =
        let jsonDictFilePath = Path.Combine(Directory.GetCurrentDirectory(), "dict-rom", jsonDictFileName)
        let trie =
            let jsonDictStr = File.ReadAllText(jsonDictFilePath, Encoding.UTF8)
            let jsonDictObj = JsonConvert.DeserializeObject<JsonRomDict> jsonDictStr
            RomTrie.ofFlatJsonDict jsonDictObj.Dict

        { new IRomanizer with
            member x.Convert chs romHints =
                RomTrie.matchRoms fallbackRom chs romHints trie }

    let deriveFromExisting(baseRomanizer : IRomanizer) substitutionRules =
        let updateRom ch rom =
            (rom, substitutionRules) ||> Seq.fold(fun rom (pattern, getRepl) ->
                Regex.Replace(rom, pattern, (getRepl ch rom : string)))
        { new IRomanizer with
            member x.Convert chs romHints =
                let romArrs = baseRomanizer.Convert chs romHints
                Array.zip chs romArrs
                |> Array.map(fun (ch, roms) -> Array.map(updateRom ch) roms) }

    let rec all = dict [|
        "man", lazy createRomanizer "du" "rime-luna-pinyin.json"
        "yue", lazy createRomanizer "du" "rime-cantonese.json"
        "yue-wz", lazy deriveFromExisting all.["yue"].Value [|
            "ui", fun _ _ -> "oi"
            "ei", fun _ _ -> "i"
            "ou", fun ch _ ->
                let manRoms = all.["man"].Value.Convert [| ch |] [| "" |]
                if Regex.IsMatch(manRoms.[0].[0], @"ao$") then "au" else "u"
            "eoi", fun ch _ ->
                let manRoms = all.["man"].Value.Convert [| ch |] [| "" |]
                if Regex.IsMatch(manRoms.[0].[0], @"v$|^[jqxy]u$") then "yu" else "oi"
            "eot", fun _ _ -> "at"
            "eon", fun _ _ -> "an" |]
        "wuu-sh", lazy createRomanizer "to" "ngli-wugniu-zaonhe.json"
        "wuu-sz", lazy createRomanizer "to" "ngli-wugniu-soutseu.json" |]

    let get romScheme =
        all.[romScheme].Value


