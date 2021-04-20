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
    | "yue-wz" -> "梧"
    | _ -> raise(KeyNotFoundException($"Unknown romScheme {romScheme}"))

let getIsSynthingDescription isSynthing =
    match isSynthing with
    | false -> "算法待机"
    | true -> "算法请求中"

let getHasAudioDescription hasAudio =
    match hasAudio with
    | false -> "未合成"
    | true -> "合成完毕"

let getQuantizationDescription quantization =
    match quantization with
    | 1920L -> "1/1 音符"
    | 960L  -> "1/2 音符"
    | 480L  -> "1/4 音符"
    | 240L  -> "1/8 音符"
    | 120L  -> "1/16 音符"
    | 60L   -> "1/32 音符"
    | 30L   -> "1/64 音符"
    | 15L   -> "1/128 音符"
    | 1L    -> "自由（1/1920）"
    | 320L  -> "1/4 三连音（1/6）"
    | 160L  -> "1/8 三连音（1/12）"
    | 80L   -> "1/16 三连音（1/24）"
    | 40L   -> "1/32 三连音（1/48）"
    | 20L   -> "1/64 三连音（1/96）"
    | _ -> raise(KeyNotFoundException($"Unknown quantization duration {quantization}"))

let getContextMenuSetRom lyric rom =
    $"设置发音为 {rom}"


