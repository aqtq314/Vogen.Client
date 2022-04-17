namespace rec Doaz.Reactive

open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions


[<Struct>]
[<StructuralEquality>]
[<StructuralComparison>]
type MidiClock(tick : int64) =
    member x.Tick = tick

    override x.ToString() = $"{nameof MidiClock}({tick})"

    static member TryParse inStr =
        if isNotNull inStr then
            let m = Regex.Match(inStr, $@"^\s*{nameof MidiClock}\((?<num>.*)\)\s*$")
            if m.Success then
                let success, num = Int64.TryParse m.Groups.["num"].Value
                if success then
                    Some(MidiClock(num))
                else None
            else None
        else None

    static member Parse inStr =
        match MidiClock.TryParse inStr with
        | Some p -> p
        | None -> raise(FormatException($"Cannot parse {nameof MidiClock} \"{inStr}\"."))

    static member op_Explicit(p : MidiClock) : int64 = p.Tick
    static member op_Explicit(p : MidiClock) : int = int p.Tick
    static member op_Explicit(midiTicks : int64) : MidiClock = MidiClock(midiTicks)
    static member op_Explicit(midiTicks : int) : MidiClock = MidiClock(int64 midiTicks)
    static member op_Explicit(q : MidiClockF) : MidiClock = MidiClock(int64 q.Tick)

    static member (+)(p : MidiClock, q : MidiClock) = MidiClock(p.Tick + q.Tick)
    static member (-)(p : MidiClock, q : MidiClock) = MidiClock(p.Tick - q.Tick)
    static member (~-)(p : MidiClock) = MidiClock(-p.Tick)

    static member (*)(p : MidiClock, k) = MidiClock(p.Tick * k)
    static member (*)(p : MidiClock, k) = MidiClock(p.Tick * int64(k : int))
    static member (*)(k, p : MidiClock) = MidiClock(p.Tick * k)
    static member (*)(k, p : MidiClock) = MidiClock(p.Tick * int64(k : int))
    static member (/)(p : MidiClock, k) = MidiClock(p.Tick / k)
    static member (/)(p : MidiClock, k) = MidiClock(p.Tick / int64(k : int))
    static member (%)(p : MidiClock, k) = MidiClock(p.Tick % k)
    static member (%)(p : MidiClock, k) = MidiClock(p.Tick % int64(k : int))

    static member op_Equality(p : MidiClock, q : MidiClock) = p.Tick = q.Tick
    static member op_LessThan(p : MidiClock, q : MidiClock) = p.Tick < q.Tick
    static member op_GreaterThan(p : MidiClock, q : MidiClock) = p.Tick > q.Tick
    static member op_LessThanOrEqual(p : MidiClock, q : MidiClock) = p.Tick <= q.Tick
    static member op_GreaterThanOrEqual(p : MidiClock, q : MidiClock) = p.Tick >= q.Tick

    static member Zero = MidiClock()

    static member inline tickOf(p : MidiClock) = p.Tick

    static member ToTimeSpan bpm (p : MidiClock) =
        float p.Tick / float Midi.ppqn / bpm |> TimeSpan.FromMinutes

[<Struct>]
[<StructuralEquality>]
[<StructuralComparison>]
type MidiClockF(tick : float) =
    member x.Tick = tick

    override x.ToString() = $"{nameof MidiClockF}({tick})"

    static member TryParse inStr =
        if isNotNull inStr then
            let m = Regex.Match(inStr, $@"^\s*{nameof MidiClockF}\((?<num>.*)\)\s*$")
            if m.Success then
                let success, num = Double.TryParse m.Groups.["num"].Value
                if success then
                    Some(MidiClockF(num))
                else None
            else None
        else None

    static member Parse inStr =
        match MidiClockF.TryParse inStr with
        | Some p -> p
        | None -> raise(FormatException($"Cannot parse {nameof MidiClockF} \"{inStr}\"."))

    static member op_Explicit(p : MidiClockF) : float = p.Tick
    static member op_Explicit(p : MidiClockF) : float32 = float32 p.Tick
    static member op_Explicit(midiTicks : float) : MidiClockF = MidiClockF(midiTicks)
    static member op_Explicit(midiTicks : float32) : MidiClockF = MidiClockF(float midiTicks)
    static member op_Explicit(q : MidiClock) : MidiClockF = MidiClockF(float q.Tick)

    static member (+)(p : MidiClockF, q : MidiClockF) = MidiClockF(p.Tick + q.Tick)
    static member (-)(p : MidiClockF, q : MidiClockF) = MidiClockF(p.Tick - q.Tick)
    static member (~-)(p : MidiClockF) = MidiClockF(-p.Tick)

    static member (*)(p : MidiClockF, k) = MidiClockF(p.Tick * k)
    static member (*)(p : MidiClockF, k) = MidiClockF(p.Tick * float(k : float32))
    static member (*)(k, p : MidiClockF) = MidiClockF(p.Tick * k)
    static member (*)(k, p : MidiClockF) = MidiClockF(p.Tick * float(k : float32))
    static member (/)(p : MidiClockF, k) = MidiClockF(p.Tick / k)
    static member (/)(p : MidiClockF, k) = MidiClockF(p.Tick / float(k : float32))
    static member (%)(p : MidiClockF, k) = MidiClockF(p.Tick % k)
    static member (%)(p : MidiClockF, k) = MidiClockF(p.Tick % float(k : float32))

    static member op_Equality(p : MidiClockF, q : MidiClockF) = p.Tick = q.Tick
    static member op_LessThan(p : MidiClockF, q : MidiClockF) = p.Tick < q.Tick
    static member op_GreaterThan(p : MidiClockF, q : MidiClockF) = p.Tick > q.Tick
    static member op_LessThanOrEqual(p : MidiClockF, q : MidiClockF) = p.Tick <= q.Tick
    static member op_GreaterThanOrEqual(p : MidiClockF, q : MidiClockF) = p.Tick >= q.Tick

    static member Zero = MidiClockF()

    static member Round(p : MidiClockF) = MidiClockF(round p.Tick)
    static member Floor(p : MidiClockF) = MidiClockF(floor p.Tick)
    static member Ceil(p : MidiClockF) = MidiClockF(ceil p.Tick)

    static member inline tickOf(p : MidiClockF) = p.Tick

    static member ToTimeSpan bpm (p : MidiClockF) =
        p.Tick / float Midi.ppqn / bpm |> TimeSpan.FromMinutes

    static member OfTimeSpan bpm (timeSpan : TimeSpan) =
        MidiClockF(timeSpan.TotalMinutes * bpm * float Midi.ppqn)

module Midi =
    let [<Literal>] ppqn = 480L
    let [<Literal>] middleC = 60

    let isBlackKey pitch =
        match pitch % 12 with
        | 1 | 3 | 6 | 8 | 10 -> true
        | _ -> false

    let toFreq pitch = Math.Pow(2.0, (pitch - 69.0) / 12.0) * 440.0
    let ofFreq f0 = 12.0 * log(f0 / 440.0) / log 2.0 + 69.0


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

    member x.TicksPerBeat = Midi.ppqn <<< 2 >>> denominatorExp
    member x.TicksPerMeasure = int64 numerator * x.TicksPerBeat

    override x.Equals y =
        match y with
        | :? TimeSignature as y -> x.Numerator = y.Numerator && x.Denominator = y.Denominator
        | _ -> base.Equals y

    override x.GetHashCode() =
        numerator * 8 + denominatorExp

    override x.ToString() = sprintf "%d/%d" numerator (1 <<< denominatorExp)

    static member TryParse timeSigStr =
        if isNull timeSigStr then None
        else
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

    static member FormatMeasures(p : MidiClock)(timeSig : TimeSignature) =
        let measure = p.Tick / timeSig.TicksPerMeasure
        sprintf "%d" (measure + 1L)

    static member FormatMeasureBeats(p : MidiClock)(timeSig : TimeSignature) =
        let measure, ticksInMeasure = p.Tick /% timeSig.TicksPerMeasure
        let beats = ticksInMeasure / timeSig.TicksPerBeat
        sprintf "%d:%d" (measure + 1L) (beats + 1L)

    static member FormatFull(p : MidiClock)(timeSig : TimeSignature) =
        let measure, ticksInMeasure = p.Tick /% timeSig.TicksPerMeasure
        let beats, ticksInBeat = ticksInMeasure /% timeSig.TicksPerBeat
        sprintf "%d:%d.%d" (measure + 1L) (beats + 1L) ticksInBeat

[<AutoOpen>]
module TimeSignatureUtils =
    let inline timeSignature num denom = TimeSignature(int num, int denom)


