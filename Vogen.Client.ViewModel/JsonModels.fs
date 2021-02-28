module Vogen.Client.JsonModels

open Newtonsoft.Json
open System
open System.Collections.Generic


type [<ReferenceEquality; NoComparison>] Phoneme = {
    [<JsonProperty("ph", Required=Required.AllowNull)>] mutable Ph : string
    [<JsonProperty("on", Required=Required.Always)>]    mutable On : int
    [<JsonProperty("off", Required=Required.Always)>]   mutable Off : int }

type [<ReferenceEquality; NoComparison>] Note = {
    [<JsonProperty("pitch", Required=Required.Always)>] mutable Pitch : int
    [<JsonProperty("on", Required=Required.Always)>]    mutable On : int
    [<JsonProperty("off", Required=Required.Always)>]   mutable Off : int }

type [<ReferenceEquality; NoComparison>] Char = {
    [<JsonProperty("ch", Required=Required.AllowNull)>]  mutable Ch : string
    [<JsonProperty("rom", Required=Required.AllowNull)>] mutable Rom : string
    [<JsonProperty("ipa")>]   mutable Ipa : List<Phoneme>
    [<JsonProperty("notes")>] mutable Notes : List<Note> }

type [<ReferenceEquality; NoComparison>] Utt = {
    [<JsonProperty("chars", Required=Required.Always)>] mutable Chars : List<Char> }

type [<ReferenceEquality; NoComparison>] Comp = {
    [<JsonProperty("utts", Required=Required.Always)>] mutable Utts : List<Utt> }


