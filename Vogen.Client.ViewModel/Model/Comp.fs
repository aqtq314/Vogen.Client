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


type SynthState =
    | NoSynth           // No audio synthesized or synth result outdated
    | Synthing          // Audio synth in progress, previous synth result may or may not exist
    | Synthed           // Synth result available and up to date

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

type Utterance(name, romScheme, notes) =
    let notes = (notes : ImmutableList<Note>).Sort(Note.CompareByPosition)
    let on = notes.[0].On

    member x.Name : string = name
    member x.RomScheme : string = romScheme
    member x.Notes : ImmutableList<Note> = notes
    member x.On = on

    member x.SetNotes notes = Utterance(name, romScheme, notes)

    static member CompareByPosition(utt1 : Utterance)(utt2 : Utterance) =
        compare utt1.On utt2.On

type UttAudio private(sampleOffset, samples, synthState) =
    member x.SampleOffset : int = sampleOffset
    member x.Samples : float32 [] = samples
    member x.SynthState : SynthState = synthState

    member x.IsSynthed =
        match synthState with
        | NoSynth | Synthing -> false
        | Synthed -> true

    new(sampleOffset) = UttAudio(sampleOffset, Array.empty, NoSynth)

    static member Create bpm0 (utt : Utterance) =
        let sampleOffset =
            utt.Notes.[0].On
            |> Midi.toTimeSpan bpm0
            |> (+) -headSil
            |> Audio.timeToSample
        UttAudio(sampleOffset)

    member x.SetNoSynth() = UttAudio(sampleOffset, Array.empty, NoSynth)
    member x.SetSynthing() = UttAudio(sampleOffset, Array.empty, Synthing)
    member x.SetSynthed audioSamples = UttAudio(sampleOffset, audioSamples, Synthed)

type Composition private(bpm0, utts, uttAudios) =
    let utts = (utts : ImmutableList<Utterance>).Sort(Utterance.CompareByPosition)

    member x.Bpm0 : float = bpm0
    member x.Utts : ImmutableList<Utterance> = utts
    member x.UttAudios = uttAudios

    new(bpm0, utts) =
        let uttAudios = (utts : ImmutableList<_>).ToImmutableDictionary(id, UttAudio.Create bpm0)
        Composition(bpm0, utts, uttAudios)

    new() = Composition(120.0, ImmutableList.Empty)
    static member Empty = Composition()

    member x.SetUtts utts =
        let uttAudios = (utts : ImmutableList<_>).ToImmutableDictionary(id, fun utt ->
            x.UttAudios.TryGetValue utt
            |> Option.ofByRef
            |> Option.defaultWith(fun () -> UttAudio.Create bpm0 utt))
        Composition(bpm0, utts, uttAudios)

    member private x.SetUttAudio(utt, updateUttAudio) =
        match x.UttAudios.TryGetValue utt |> Option.ofByRef with
        | None -> x
        | Some uttAudio ->
            let uttAudios = x.UttAudios.SetItem(utt, updateUttAudio uttAudio)
            Composition(bpm0, utts, uttAudios)

    member x.SetUttAudioNoSynth utt =
        x.SetUttAudio(utt, fun uttAudio -> uttAudio.SetNoSynth())

    member x.SetUttAudioSynthing utt =
        x.SetUttAudio(utt, fun uttAudio -> uttAudio.SetSynthing())

    member x.SetUttAudioSynthed utt audioSamples =
        x.SetUttAudio(utt, fun uttAudio -> uttAudio.SetSynthed audioSamples)


