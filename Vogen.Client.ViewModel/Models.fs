namespace Vogen.Client.Model

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable


type Note = {
    [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
    [<JsonProperty("lyric", Required=Required.Always)>] Lyric : string
    [<JsonProperty("rom", Required=Required.Always)>]   Rom : string
    [<JsonProperty("on", Required=Required.Always)>]    On : int64
    [<JsonProperty("dur", Required=Required.Always)>]   Dur : int64 }

type Utterance = {
    [<JsonProperty("notes", Required=Required.Always)>] Notes : ImmutableList<Note> }

type Composition = {
    [<JsonProperty("utts", Required=Required.Always)>]  Utts : ImmutableList<Utterance> }

type Note with
    [<JsonIgnore>] member x.Off = x.On + x.Dur

type Composition with
    static member Empty = { Utts = ImmutableList.Empty }

