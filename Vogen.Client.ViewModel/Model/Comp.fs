namespace Vogen.Client.Model

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


type Note(pitch, lyric, rom, on, dur) =
    member x.Pitch : int = pitch
    member x.Lyric : string = lyric
    member x.Rom : string = rom
    member x.On : int64 = on
    member x.Dur : int64 = dur

    member x.Off = on + dur
    member x.IsHyphen = x.Lyric = "-"

    member x.SetOn on = Note(pitch, lyric, rom, on, dur)
    member x.SetDur dur = Note(pitch, lyric, rom, on, dur)
    member x.SetOff off = Note(pitch, lyric, rom, on, off - on)

    static member CompareByPosition(n1 : Note)(n2 : Note) =
        let onDiff = compare n1.On n2.On
        if onDiff <> 0 then onDiff
        else compare n1.Dur n2.Dur

type Utterance(romScheme, notes) =
    let notes = (notes : ImmutableList<Note>).Sort(Note.CompareByPosition)
    let on = notes.[0].On

    member x.RomScheme : string = romScheme
    member x.Notes : ImmutableList<Note> = notes
    member x.On = on

    member x.SetNotes notes = Utterance(romScheme, notes)

    static member CompareByPosition(utt1 : Utterance)(utt2 : Utterance) =
        match compare utt1.On utt2.On with
        | 0 -> -(compare utt1.Notes.[0].Pitch utt2.Notes.[0].Pitch)
        | onDiff -> onDiff

type PhonemeInterval(ph, on, off) =      // on/off in vocoder frames
    member x.Ph : string = ph
    member x.On : int = on
    member x.Off : int = off

type CharGrid(pitch, phs) =
    member x.Pitch : int = pitch
    member x.Phs : PhonemeInterval [] = phs

type UttSynthResult(sampleOffset, isSynthing, charGrids, f0Samples, hasAudio, audioFileBytes, audioSamples) =
    member x.SampleOffset : int = sampleOffset
    member x.CharGrids : CharGrid [] = charGrids
    member x.F0Samples : float32 [] = f0Samples
    member x.AudioFileBytes : byte [] = audioFileBytes
    member x.AudioSamples : float32 [] = audioSamples
    member x.HasAudio : bool = hasAudio
    member x.IsSynthing : bool = isSynthing

    member x.HasCharGrids = x.CharGrids.Length > 0
    member x.HasF0Samples = x.F0Samples.Length > 0

    new(sampleOffset) = UttSynthResult(sampleOffset, false, Array.empty, Array.empty, false, Array.empty, Array.empty)

    static member Create bpm0 (utt : Utterance) =
        let sampleOffset =
            float utt.Notes.[0].On
            |> Midi.toTimeSpan bpm0
            |> (+) -headSil
            |> Audio.timeToSample
        UttSynthResult(sampleOffset)

    member x.Clear() =
        UttSynthResult(sampleOffset, false, Array.empty, Array.empty, false, Array.empty, Array.empty)

    member x.SetIsSynthing isSynthing =
        UttSynthResult(sampleOffset, isSynthing, charGrids, f0Samples, hasAudio, audioFileBytes, audioSamples)

    member x.SetCharGrids charGrids =
        UttSynthResult(sampleOffset, isSynthing, charGrids, f0Samples, hasAudio, audioFileBytes, audioSamples)

    member x.SetF0Samples f0Samples =
        UttSynthResult(sampleOffset, isSynthing, charGrids, f0Samples, hasAudio, audioFileBytes, audioSamples)

    member x.SetNoAudio() =
        UttSynthResult(sampleOffset, isSynthing, charGrids, f0Samples, false, Array.empty, Array.empty)

    member x.SetAudio(audioFileBytes, audioSamples) =
        UttSynthResult(sampleOffset, isSynthing, charGrids, f0Samples, true, audioFileBytes, audioSamples)

type Composition private(bpm0, utts, uttSynthResults) =
    let utts = (utts : ImmutableList<Utterance>).Sort(Utterance.CompareByPosition)

    member x.Bpm0 : float = bpm0
    member x.Utts : ImmutableList<Utterance> = utts

    member x.GetUttSynthResult utt = (uttSynthResults : ImmutableDictionary<_, _>).[utt]

    new(bpm0, utts) =
        let uttSynthResults = (utts : ImmutableList<_>).ToImmutableDictionary(id, UttSynthResult.Create bpm0)
        Composition(bpm0, utts, uttSynthResults)

    new() = Composition(120.0, ImmutableList.Empty)
    static member Empty = Composition()

    member x.SetUtts utts =
        let uttSynthResults = (utts : ImmutableList<_>).ToImmutableDictionary(id, fun utt ->
            uttSynthResults.TryGetValue utt
            |> Option.ofByRef
            |> Option.defaultWith(fun () -> UttSynthResult.Create bpm0 utt))
        Composition(bpm0, utts, uttSynthResults)

    member x.SetUttSynthResult updateUttSynthResult utt =
        match uttSynthResults.TryGetValue utt |> Option.ofByRef with
        | None -> x
        | Some uttSynthResult ->
            let uttSynthResults = uttSynthResults.SetItem(utt, updateUttSynthResult uttSynthResult)
            Composition(bpm0, utts, uttSynthResults)


