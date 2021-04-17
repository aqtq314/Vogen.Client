namespace Vogen.Client.Romanization

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Text


[<NoComparison; ReferenceEquality>]
type JsonRomDict = {
    [<JsonProperty("dict", Required=Required.Always)>] Dict : ImmutableDictionary<string, string []> }

type Romanizer(romScheme, fallbackRom, jsonDictFileName) =
    let jsonDictFilePath = Path.Combine(Directory.GetCurrentDirectory(), "dict-rom", jsonDictFileName)
    let trie = lazy(
        let jsonDictStr = File.ReadAllText(jsonDictFilePath, Encoding.UTF8)
        let jsonDictObj = JsonConvert.DeserializeObject<JsonRomDict> jsonDictStr
        RomTrie.ofFlatJsonDict jsonDictObj.Dict)

    member x.RomScheme = romScheme
    member x.FallbackRom = fallbackRom
    member x.Convert chs romHints =
        RomTrie.matchRoms fallbackRom chs romHints trie.Value

module Romanizer =
    let man = Romanizer("man", "wu", "rime-luna-pinyin.json")
    let yue = Romanizer("yue", "wu", "rime-cantonese.json")
    let wuu_sh = Romanizer("wuu-sh", "o", "ngli-wugniu-zaonhe.json")
    let wuu_sz = Romanizer("wuu-sz", "o", "ngli-wugniu-soutseu.json")

    let all = [|
        man; yue; wuu_sh; wuu_sz |]

    let allDict =
        all.ToImmutableDictionary((fun romanizer -> romanizer.RomScheme), id)

    let get romScheme =
        allDict.[romScheme]


