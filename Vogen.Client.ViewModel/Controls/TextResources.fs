module Vogen.Client.Controls.TextResources

open Doaz.Reactive
open NAudio.Wave
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.IO.Compression
open System.Linq
open System.Text
open System.Text.Encodings
open Vogen.Client.Model


let getRomSchemeChar romScheme =
    match romScheme with
    | "man" -> "普"
    | "yue" -> "粤"
    | _ -> raise(KeyNotFoundException($"Unknown romScheme {romScheme}"))

let getSynthStateDescription synthState =
    match synthState with
    | NoSynth -> "未合成"
    | Synthing -> "合成中"
    | Synthed -> "合成完毕"


