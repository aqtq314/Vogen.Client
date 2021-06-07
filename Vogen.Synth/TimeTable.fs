namespace Vogen.Synth

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable


module TimeTable =
    let timeToFrame(timeSpan : TimeSpan) = timeSpan / hopSize
    let frameToTime(frames : float) = frames * hopSize

    [<NoComparison; ReferenceEquality>]
    type TPhoneme = {
        [<JsonProperty("ph", Required=Required.AllowNull)>] Ph : string
        [<JsonProperty("on", Required=Required.Always)>]    On : int
        [<JsonProperty("off", Required=Required.Always)>]   Off : int }

    [<NoComparison; ReferenceEquality>]
    type TNote = {
        [<JsonProperty("pitch", Required=Required.Always)>] Pitch : int
        [<JsonProperty("on", Required=Required.Always)>]    On : int
        [<JsonProperty("off", Required=Required.Always)>]   Off : int }

    [<NoComparison; ReferenceEquality>]
    type TChar = {
        [<JsonProperty("ch", Required=Required.AllowNull)>]                   Ch : string
        [<JsonProperty("rom", Required=Required.AllowNull)>]                  Rom : string
        [<JsonProperty("notes", NullValueHandling=NullValueHandling.Ignore)>] Notes : ImmutableList<TNote>
        [<JsonProperty("ipa", NullValueHandling=NullValueHandling.Ignore)>]   Ipa : ImmutableList<TPhoneme> }

    [<NoComparison; ReferenceEquality>]
    type TUtt = {
        [<JsonProperty("uttStartSec", Required=Required.Always)>] UttStartSec : float
        [<JsonProperty("uttDur", Required=Required.Always)>]      UttDur : int
        [<JsonProperty("romScheme", Required=Required.Always)>]   RomScheme : string
        [<JsonProperty("chars", Required=Required.Always)>]       Chars : ImmutableList<TChar> }


