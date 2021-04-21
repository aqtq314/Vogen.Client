namespace rec Doaz.Reactive

open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions


module Midi =
    let [<Literal>] ppqn = 480L
    let [<Literal>] middleC = 60

    let isBlackKey pitch =
        match pitch % 12 with
        | 1 | 3 | 6 | 8 | 10 -> true
        | _ -> false

    let toFreq pitch = Math.Pow(2.0, (pitch - 69.0) / 12.0) * 440.0
    let ofFreq f0 = 12.0 * log(f0 / 440.0) / log 2.0 + 69.0

    let formatMeasures(timeSig : TimeSignature)(pulses : int64) =
        let measure = pulses / timeSig.PulsesPerMeasure
        sprintf "%d" (measure + 1L)

    let formatMeasureBeats(timeSig : TimeSignature)(pulses : int64) =
        let measure, pulsesInMeasure = pulses /% timeSig.PulsesPerMeasure
        let beats = pulsesInMeasure / timeSig.PulsesPerBeat
        sprintf "%d:%d" (measure + 1L) (beats + 1L)

    let formatFull(timeSig : TimeSignature)(pulses : int64) =
        let measure, pulsesInMeasure = pulses /% timeSig.PulsesPerMeasure
        let beats, pulsesInBeat = pulsesInMeasure /% timeSig.PulsesPerBeat
        sprintf "%d:%d.%d" (measure + 1L) (beats + 1L) pulsesInBeat

    let toTimeSpan bpm pulses =
        pulses / float Midi.ppqn / bpm |> TimeSpan.FromMinutes

    let ofTimeSpan bpm (timeSpan : TimeSpan) =
        timeSpan.TotalMinutes * bpm * float Midi.ppqn


type TimeSignature(numerator, denominator) =
    inherit obj()

    static let isValidNumerator numerator = numerator |> betweenInc 1 256
    static let allowedDenominators = [| 1; 2; 4; 8; 16; 32; 64; 128 |]

    do  if not(isValidNumerator numerator) then
            raise(ArgumentException(sprintf "TimeSignature numerator %d is not valid." numerator))

    let denominatorExp = Array.IndexOf(allowedDenominators, denominator)
    do  if denominatorExp < 0 then
            raise(ArgumentException(sprintf "TimeSignature denominator %d is not part of %A." denominator allowedDenominators))

    static member AllowedDenominators = allowedDenominators

    member x.Numerator = numerator
    member x.Denominator = 1 <<< denominatorExp
    member x.DenominatorExponent = denominatorExp

    member x.AsFloat = float numerator / float(1 <<< denominatorExp)

    member x.PulsesPerBeat = Midi.ppqn <<< 2 >>> denominatorExp
    member x.PulsesPerMeasure = int64 numerator * x.PulsesPerBeat

    override x.Equals y =
        match y with
        | :? TimeSignature as y -> x.Numerator = y.Numerator && x.Denominator = y.Denominator
        | _ -> base.Equals y

    override x.GetHashCode() =
        numerator * 8 + denominatorExp

    override x.ToString() = sprintf "%d/%d" numerator (1 <<< denominatorExp)

    static member TryParse timeSigStr =
        let m = Regex.Match(timeSigStr, @"^\s*(?<num>[0-9]{1,3})\s*/\s*(?<denom>[0-9]{1,3})\s*$")
        if m.Success then
            let numerator = Int32.Parse m.Groups.["num"].Value
            let denominator = Int32.Parse m.Groups.["denom"].Value
            if isValidNumerator numerator && Array.IndexOf(allowedDenominators, denominator) >= 0 then
                Some(TimeSignature(numerator, denominator))
            else
                None
        else
            None

    static member Parse timeSigStr =
        match TimeSignature.TryParse timeSigStr with
        | Some timeSig -> timeSig
        | None -> raise(FormatException($"Cannot parse time signature \"{timeSigStr}\"."))

[<AutoOpen>]
module TimeSignatureUtils =
    let inline timeSignature num denom = TimeSignature(int num, int denom)


