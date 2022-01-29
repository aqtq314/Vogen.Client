namespace Vogen.Synth.Romanization

open FSharp.Data
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Text
open System.Text.RegularExpressions
open Vogen.Synth

#nowarn "40"


type IRomanizer =
    abstract Convert : chs : string [] -> romHints : string [] -> string [] []

module Romanizer =
    let createEmpty fallbackRom =
        { new IRomanizer with
            member x.Convert chs romHints = Array.create chs.Length [| fallbackRom |] }

    let createRomanizer fallbackRom romScheme csvDictFileName =
        let csvDictFilePath = Path.Combine(appDir, "RomDicts", csvDictFileName)
        let trie =
            use csv = CsvFile.Load(csvDictFilePath, hasHeaders = true)
            csv.Rows
            |> Seq.groupBy(fun row -> row.["ch"])
            |> Seq.map(fun (ch, rows) -> KeyValuePair(ch, [| for row in rows -> row.[romScheme : string] |]))
            |> RomTrie.ofFlatCsvEntries

        { new IRomanizer with
            member x.Convert chs romHints =
                RomTrie.matchRoms fallbackRom chs romHints trie }

    let rec all = dict [|
        "man",      lazy createRomanizer "du" "man"     "man.csv"
        //"man-pop",  lazy createRomanizer "du" "man-pop" "man.csv"
        "yue",      lazy createRomanizer "du" "yue"     "yue.csv"
        "yue-wz",   lazy createRomanizer "du" "yue-wz"  "yue.csv"
        //"yue-nn",   lazy createRomanizer "du" "yue-nn"  "yue.csv"
        "wuu-sh",   lazy createRomanizer "to" "wuu-sh"  "wuu.csv"
        "wuu-sz",   lazy createRomanizer "to" "wuu-sz"  "wuu.csv" |]

    let get romScheme =
        all.[romScheme].Value

    let allIds = all.Keys

    let defaultId = Seq.head all.Keys


