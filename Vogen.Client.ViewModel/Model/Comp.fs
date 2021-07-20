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
open System.Runtime.InteropServices
open System.Text
open System.Text.Encodings
open Vogen.Client.ViewModel
open Vogen.Synth


type Note(pitch, lyric, rom, on, dur) =
    do  if dur <= 0L then
            raise(ArgumentException("Note dur <= 0"))

    member x.Pitch : int = pitch
    member x.Lyric : string = lyric
    member x.Rom : string = rom
    member x.On : int64 = on
    member x.Dur : int64 = dur

    member x.Off = x.On + x.Dur
    member x.IsHyphen = x.Lyric = "-"   // TODO: make value

    member x.SetText(lyric, rom) = Note(pitch, lyric, rom, on, dur)
    member x.SetOn on = Note(pitch, lyric, rom, on, dur)
    member x.SetDur dur = Note(pitch, lyric, rom, on, dur)
    member x.SetOff off = Note(pitch, lyric, rom, on, off - on)
    member x.MoveDelta(deltaPitch, deltaOn, deltaDur) = Note(pitch + deltaPitch, lyric, rom, on + deltaOn, dur + deltaDur)

    static member CompareByPosition(n1 : Note)(n2 : Note) =
        let onDiff = compare n1.On n2.On
        if onDiff <> 0 then onDiff
        else compare n1.Dur n2.Dur

type Utterance(singerId, romScheme, bpm0, notes) =
    let notes = (notes : ImmutableArray<Note>).Sort Note.CompareByPosition
    do  if notes.Length = 0 then
            raise(ArgumentException("An utterance must have notes.Length > 0"))

    member x.SingerId : string = singerId
    member x.RomScheme : string = romScheme
    member x.Bpm0 : float = bpm0
    member x.Notes : ImmutableArray<Note> = notes
    member x.On = notes.[0].On

    member x.SetSingerId singerId = Utterance(singerId, romScheme, bpm0, notes)
    member x.SetRomScheme romScheme = Utterance(singerId, romScheme, bpm0, notes)
    member x.SetBpm0 bpm0 = Utterance(singerId, romScheme, bpm0, notes)
    member x.SetNotes notes = Utterance(singerId, romScheme, bpm0, notes)
    member x.UpdateNotes updateNotes = Utterance(singerId, romScheme, bpm0, updateNotes notes)

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

// TODO: use weak reference to avoid memory leak when saved in undo/redo
type AudioTrack private(sampleOffset, hasAudio, audioFilePath, audioSamples) =
    member x.SampleOffset : int = sampleOffset
    member x.HasAudio : bool = hasAudio
    member x.AudioFilePath : string = audioFilePath
    member x.AudioSamples : float32 [] = audioSamples

    new(sampleOffset) = AudioTrack(sampleOffset, false, "", Array.empty)
    new(sampleOffset, audioFilePath, audioSamples : _ []) =
        AudioTrack(sampleOffset, true, audioFilePath, audioSamples)
    static member val Empty = AudioTrack(0, false, "", Array.empty)

    member x.SetSampleOffset sampleOffset = AudioTrack(sampleOffset, hasAudio, audioFilePath, audioSamples)
    //member x.SetNoAudio() = AudioTrack(sampleOffset, false, Array.empty, Array.empty)
    //member x.SetAudio(audioFileBytes, audioSamples : _ []) =
    //    AudioTrack(sampleOffset, true, audioFileBytes, audioSamples)

    member x.UpdateSampleOffset updateSampleOffset = AudioTrack(updateSampleOffset sampleOffset, hasAudio, audioFilePath, audioSamples)

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

    static member GetSampleOffset(utt : Utterance) =
        float utt.On
        |> Midi.toTimeSpan utt.Bpm0
        |> (+) -headSil
        |> Audio.timeToSample

    static member Create sampleOffset = UttSynthResult(sampleOffset, false, Array.empty, Array.empty, false, Array.empty, Array.empty)

    static member Create utt =
        let sampleOffset = UttSynthResult.GetSampleOffset utt
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

type Composition(timeSig0, bpm0, bgAudio, utts) =
    let utts = (utts : ImmutableArray<_>).Sort Utterance.CompareByPosition

    member x.TimeSig0 : TimeSignature = timeSig0
    member x.Bpm0 : float = bpm0
    member x.BgAudio : AudioTrack = bgAudio
    member x.Utts : ImmutableArray<Utterance> = utts

    member x.AllNotes = utts |> Seq.collect(fun utt -> utt.Notes)

    new(timeSig0, bpm0, utts) = Composition(timeSig0, bpm0, AudioTrack.Empty, utts)
    static member val Empty = Composition(timeSignature 4 4, 120.0, AudioTrack.Empty, ImmutableArray.Empty)

    member x.SetTimeSig timeSig0 = Composition(timeSig0, bpm0, bgAudio, utts)
    member x.SetBpm bpm0 = Composition(timeSig0, bpm0, bgAudio, utts)
    member x.SetBgAudio bgAudio = Composition(timeSig0, bpm0, bgAudio, utts)
    member x.SetBgAudioOffset bgAudioOffset = Composition(timeSig0, bpm0, bgAudio.SetSampleOffset bgAudioOffset, utts)
    member x.SetUtts utts = Composition(timeSig0, bpm0, bgAudio, utts)

    member x.UpdateBgAudioOffset updateBgAudioOffset = Composition(timeSig0, bpm0, bgAudio.UpdateSampleOffset updateBgAudioOffset, utts)
    member x.UpdateUtts updateUtts = Composition(timeSig0, bpm0, bgAudio, updateUtts utts)

type UttSynthCache(uttSynthResultDict) =
    member x.UttSynthResultDict : ImmutableDictionary<Utterance, UttSynthResult> = uttSynthResultDict

    member x.IsSynthing = uttSynthResultDict.Values |> Seq.exists(fun synthResult -> synthResult.IsSynthing)

    member x.GetOrDefault utt =
        match uttSynthResultDict.TryGetValue utt with
        | true, uttSynthResult -> uttSynthResult
        | false, _ -> UttSynthResult.Create utt

    static member val Empty = UttSynthCache(ImmutableDictionary.Empty)
    static member Create(bpm0 : float) = UttSynthCache(ImmutableDictionary.Empty)

    member x.SetUttSynthResult uttSynthResult utt =
        let uttSynthResultDict = uttSynthResultDict.SetItem(utt, uttSynthResult)
        UttSynthCache(uttSynthResultDict)

    member x.UpdateUttSynthResult updateUttSynthResult utt =
        let uttSynthResultDict = uttSynthResultDict.SetItem(utt, updateUttSynthResult(x.GetOrDefault utt))
        UttSynthCache(uttSynthResultDict)

    member x.Clear() =
        UttSynthCache(ImmutableDictionary.Empty)

    member x.SlimWith(comp : Composition) =
        let uttSynthResultDict =
            ImmutableDictionary.CreateRange(comp.Utts |> Seq.choose(fun utt ->
                match uttSynthResultDict.TryGetValue utt with
                | false, _ -> None
                | true, uttSynthResult -> Some(KeyValuePair(utt, uttSynthResult))))
        UttSynthCache(uttSynthResultDict)

type ChartState(comp : Composition, activeUtt, selectedNotes : ImmutableHashSet<_>) =
    do  match activeUtt with
        | Some activeUtt when not(comp.Utts.Contains activeUtt) ->
            raise(ArgumentException($"ActiveUtt not part of composition."))
        | _ -> ()

    do  if not (selectedNotes.Except comp.AllNotes).IsEmpty then
            raise(ArgumentException($"One or more notes in selectedNotes are not part of composition."))

    let uttsWithSelection =
        ImmutableHashSet.CreateRange(
            comp.Utts |> Seq.filter(fun utt -> utt.Notes |> Seq.exists selectedNotes.Contains))

    member x.Comp = comp
    member x.ActiveUtt = activeUtt
    member x.SelectedNotes = selectedNotes
    member x.UttsWithSelection = uttsWithSelection

    member x.GetIsNoteSelected note = selectedNotes.Contains note

    new(comp) = ChartState(comp, None, ImmutableHashSet.Empty)
    static member val Empty = ChartState(Composition.Empty, None, ImmutableHashSet.Empty)

    member x.SetComp comp = ChartState(comp, activeUtt, selectedNotes)
    member x.SetActiveUtt(comp, activeUtt) = ChartState(comp, activeUtt, selectedNotes)
    member x.SetSelectedNotes(comp, selectedNotes) = ChartState(comp, activeUtt, selectedNotes)

    member x.UpdateComp updateComp = ChartState(updateComp comp, activeUtt, selectedNotes)
    member x.UpdateActiveUtt(comp, updateActiveUtt) = ChartState(comp, updateActiveUtt activeUtt, selectedNotes)
    member x.UpdateSelectedNotes(comp, updateSelectedNotes) = ChartState(comp, activeUtt, updateSelectedNotes selectedNotes)


