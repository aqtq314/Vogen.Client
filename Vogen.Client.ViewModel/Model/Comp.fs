﻿namespace Vogen.Client.Model

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
open System.Runtime.InteropServices
open System.Text
open System.Text.Encodings


type Note(pitch, lyric, rom, moreRoms, on, dur) =
    do  if dur <= 0L then
            raise(ArgumentException("Note dur <= 0"))

    member x.Pitch : int = pitch
    member x.Lyric : string = lyric
    member x.Rom : string = rom
    member x.MoreRoms : ImmutableArray<string> = moreRoms
    member x.On : int64 = on
    member x.Dur : int64 = dur

    member x.Off = x.On + x.Dur
    member x.IsHyphen = x.Lyric = "-"

    new(pitch, lyric, rom, on, dur) = Note(pitch, lyric, rom, ImmutableArray.Empty, on, dur)

    member x.SetText(lyric, rom, moreRoms) = Note(pitch, lyric, rom, moreRoms, on, dur)
    member x.SetOn on = Note(pitch, lyric, rom, moreRoms, on, dur)
    member x.SetDur dur = Note(pitch, lyric, rom, moreRoms, on, dur)
    member x.SetOff off = Note(pitch, lyric, rom, moreRoms, on, off - on)
    member x.MoveDelta(deltaPitch, deltaOn, deltaDur) = Note(pitch + deltaPitch, lyric, rom, moreRoms, on + deltaOn, dur + deltaDur)

    static member CompareByPosition(n1 : Note)(n2 : Note) =
        let onDiff = compare n1.On n2.On
        if onDiff <> 0 then onDiff
        else compare n1.Dur n2.Dur

type Utterance(singerId, romScheme, notes) =
    let notes = (notes : ImmutableArray<Note>).Sort Note.CompareByPosition
    do  if notes.Length = 0 then
            raise(ArgumentException("An utterance must have notes.Length > 0"))

    member x.SingerId : string = singerId
    member x.RomScheme : string = romScheme
    member x.Notes : ImmutableArray<Note> = notes
    member x.On = notes.[0].On

    member x.Copy() = Utterance(singerId, romScheme, notes)
    member x.SetSingerId singerId = Utterance(singerId, romScheme, notes)
    member x.SetRomScheme romScheme = Utterance(singerId, romScheme, notes)
    member x.SetNotes notes = Utterance(singerId, romScheme, notes)
    member x.UpdateNotes updateNotes = Utterance(singerId, romScheme, updateNotes notes)

    static member CompareByPosition(utt1 : Utterance)(utt2 : Utterance) =
        match compare utt1.On utt2.On with
        | 0 -> -(compare utt1.Notes.[0].Pitch utt2.Notes.[0].Pitch)
        | onDiff -> onDiff

type PhonemeInterval(ph, on, off) =
    member x.Ph : string = ph
    member x.On : int = on      // unit in vocoder frames
    member x.Off : int = off    // unit in vocoder frames

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

    static member Create sampleOffset = UttSynthResult(sampleOffset, false, Array.empty, Array.empty, false, Array.empty, Array.empty)

    static member Create(bpm0, utt : Utterance) =
        let sampleOffset =
            float utt.On
            |> Midi.toTimeSpan bpm0
            |> (+) -headSil
            |> Audio.timeToSample
        UttSynthResult.Create sampleOffset

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

type Composition(timeSig0, bpm0, utts) =
    let utts = (utts : ImmutableArray<_>).Sort Utterance.CompareByPosition

    member x.TimeSig0 : TimeSignature = timeSig0
    member x.Bpm0 : float = bpm0
    member x.Utts : ImmutableArray<Utterance> = utts
    //member x.GetIsNoteSelected note = (selectedNotes : ImmutableHashSet<Note>).Contains note
    //member x.GetUttSynthResult utt = (uttSynthResults : ImmutableDictionary<_, _>).[utt]

    //new(timeSig0, bpm0, utts) =
    //    let uttSynthResults = (utts : ImmutableArray<_>).ToImmutableDictionary(id, fun utt -> UttSynthResult.Create(bpm0, utt))
    //    Composition(timeSig0, bpm0, utts, ImmutableHashSet.Empty, uttSynthResults)

    member x.AllNotes = utts |> Seq.collect(fun utt -> utt.Notes)

    new(bpm0, utts) = Composition(timeSignature 4 4, bpm0, utts)
    static member val Empty = Composition(timeSignature 4 4, 120.0, ImmutableArray.Empty)

    member x.SetTimeSig timeSig0 = Composition(timeSig0, bpm0, utts)
    member x.SetBpm bpm0 = Composition(timeSig0, bpm0, utts)
    member x.SetUtts utts = Composition(timeSig0, bpm0, utts)

    member x.UpdateUtts updateUtts = Composition(timeSig0, bpm0, updateUtts utts)

    // TODO prevent adding identical notes more than once
    //member x.SetUtts(utts : ImmutableArray<Utterance>, [<Optional; DefaultParameterValue(false)>] enforceStateConsistencies) =
    //    let selectedNotes =
    //        if not enforceStateConsistencies then selectedNotes else
    //            selectedNotes.Intersect(utts |> Seq.collect(fun utt -> utt.Notes))
    //    let uttSynthResults =
    //        utts.ToImmutableDictionary(id, fun utt ->
    //            uttSynthResults.TryGetValue utt
    //            |> Option.ofByRef
    //            |> Option.defaultWith(fun () -> UttSynthResult.Create(bpm0, utt)))
    //    Composition(timeSig0, bpm0, utts, selectedNotes, uttSynthResults)

    //member x.UpdateSelectedNotes updateSelectedNotes =
    //    let selectedNotes = updateSelectedNotes selectedNotes
    //    Composition(timeSig0, bpm0, utts, selectedNotes, uttSynthResults)

    //member x.UpdateUttSynthResult updateUttSynthResult utt =
    //    match uttSynthResults.TryGetValue utt |> Option.ofByRef with
    //    | None -> x
    //    | Some uttSynthResult ->
    //        let uttSynthResults = uttSynthResults.SetItem(utt, updateUttSynthResult uttSynthResult)
    //        Composition(timeSig0, bpm0, utts, selectedNotes, uttSynthResults)

type UttSynthCache(bpm0, uttSynthResultDict) =
    member x.Bpm0 : float = bpm0
    member x.UttSynthResultDict : ImmutableDictionary<Utterance, UttSynthResult> = uttSynthResultDict

    member x.GetOrDefault utt =
        match uttSynthResultDict.TryGetValue utt with
        | true, uttSynthResult -> uttSynthResult
        | false, _ -> UttSynthResult.Create(bpm0, utt)

    static member val Empty = UttSynthCache(Composition.Empty.Bpm0, ImmutableDictionary.Empty)
    static member Create(bpm0 : float) = UttSynthCache(bpm0, ImmutableDictionary.Empty)

    member x.UpdateUttSynthResult updateUttSynthResult utt =
        let uttSynthResultDict = uttSynthResultDict.SetItem(utt, updateUttSynthResult(x.GetOrDefault utt))
        UttSynthCache(bpm0, uttSynthResultDict)

    member x.Clear() =
        UttSynthCache(bpm0, ImmutableDictionary.Empty)

    member x.SlimWith(comp : Composition) =
        let uttSynthResultDict =
            ImmutableDictionary.CreateRange(comp.Utts |> Seq.choose(fun utt ->
                match uttSynthResultDict.TryGetValue utt with
                | false, _ -> None
                | true, uttSynthResult -> Some(KeyValuePair(utt, uttSynthResult))))
        UttSynthCache(bpm0, uttSynthResultDict)

type CompSelection(activeUtt, selectedNotes) =
    member x.ActiveUtt : Utterance option = activeUtt
    member x.SelectedNotes : ImmutableHashSet<Note> = selectedNotes

    member x.GetIsNoteSelected note = selectedNotes.Contains note

    static member val Empty = CompSelection(None, ImmutableHashSet.Empty)

    member x.SetActiveUtt activeUtt = CompSelection(activeUtt, selectedNotes)
    member x.SetSelectedNotes selectedNotes = CompSelection(activeUtt, selectedNotes)

    member x.UpdateActiveUtt updateActiveUtt = CompSelection(updateActiveUtt activeUtt, selectedNotes)
    member x.UpdateSelectedNotes updateSelectedNotes = CompSelection(activeUtt, updateSelectedNotes selectedNotes)

    member x.EnsureIntersectionWith(comp : Composition) =
        let activeUtt = activeUtt |> Option.filter comp.Utts.Contains
        let selectedNotes = selectedNotes.Intersect comp.AllNotes
        CompSelection(activeUtt, selectedNotes)


