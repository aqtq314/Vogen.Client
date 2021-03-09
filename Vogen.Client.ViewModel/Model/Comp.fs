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


type RomScheme(code, charName) =
    member x.Code : string = code
    member x.CharName : string = charName

module RomSchemes =
    let all = ImmutableArray.CreateRange([|
        RomScheme("man", "普")
        RomScheme("yue", "粤") |])

    let codeLookup = all.ToImmutableDictionary(fun romScheme -> romScheme.Code)

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

type Composition(bpm0, utts, audioSegments) =
    let audioSampleOffsetsLazy =
        lazy
        utts
        |> Seq.map(fun (utt : Utterance) ->
            let sampleOffset =
                utt.Notes.[0].On
                |> Midi.toTimeSpan bpm0
                |> (+) -headSil
                |> Audio.timeToSample
            KeyValuePair(utt.Name, sampleOffset))
        |> ImmutableDictionary.CreateRange

    member x.Bpm0 : float = bpm0
    member x.Utts : ImmutableList<Utterance> = utts
    member x.AudioSegments : ImmutableDictionary<string, float32 []> = audioSegments

    new(bpm0, utts) = Composition(bpm0, utts, ImmutableDictionary.Empty)
    new() = Composition(120.0, ImmutableList.Empty)

    static member Empty = Composition()

    member x.AudioSampleOffsets = audioSampleOffsetsLazy.Value

    member x.SetAudioSegments audioSegments = Composition(bpm0, utts, audioSegments)




