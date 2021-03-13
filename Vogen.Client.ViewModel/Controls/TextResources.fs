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

let getIsSynthingDescription isSynthing =
    match isSynthing with
    | false -> "算法待机"
    | true -> "算法请求中"

let getHasAudioDescription hasAudio =
    match hasAudio with
    | false -> "未合成"
    | true -> "合成完毕"


