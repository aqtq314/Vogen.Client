namespace Vogen.Client.Model

open Doaz.Reactive
open NAudio
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
open System.Text.RegularExpressions
open Vogen.Client.ViewModel
open Vogen.Synth
open Vogen.Synth.VogPackage


module FilePackage =
    type FNote with
        static member toNote x =
            let { Pitch = pitch; Lyric = lyric; Rom = rom; On = on; Dur = dur } = x
            Note(pitch, lyric, rom, on, dur)

        static member ofNote(note : Note) =
            { Pitch = note.Pitch; Lyric = note.Lyric; Rom = note.Rom; On = note.On; Dur = note.Dur }

    type FUtt with
        static member toUtt bpm0 x =
            let { Name = name; SingerId = singerId; RomScheme = romScheme; Notes = fNotes } = x
            let singerId = if String.IsNullOrEmpty singerId then "gloria" else singerId
            let notes = ImmutableArray.CreateRange(Seq.map FNote.toNote fNotes)
            Utterance(singerId, romScheme, bpm0, notes), name

        static member ofUtt uttName (utt : Utterance) =
            let fNotes = Array.ofSeq(Seq.map FNote.ofNote utt.Notes)
            { Name = uttName; SingerId = utt.SingerId; RomScheme = utt.RomScheme; Notes = fNotes }

    type FClip with
        static member toUtts bpm0 refNoteOn x =
            let { FClip.Utts = fUtts } = x
            let uttAndNames = fUtts |> Array.map(fun fUtt ->
                let utt, name = FUtt.toUtt bpm0 fUtt
                let utt = utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> note.SetOn(note.On + refNoteOn)))
                utt, name)
            let activeUttOp = uttAndNames |> Array.tryPick(fun (utt, name) -> if name = "active" then Some utt else None)
            let otherUtts = uttAndNames |> Seq.filter(fun (utt, name) -> name <> "active") |> Seq.map(fun (utt, name) -> utt)
            activeUttOp, ImmutableArray.CreateRange otherUtts

        static member ofUtts refNoteOn activeUttOp otherUtts =
            let uttAndNames =
                Seq.append
                    (activeUttOp |> Option.map(fun utt -> utt, "active") |> Option.toArray)
                    (otherUtts |> Seq.mapi(fun i utt -> utt, $"utt-{i}"))
            let fUtts =
                uttAndNames
                |> Seq.map(fun (utt : Utterance, name) ->
                    let utt = utt.SetNotes(ImmutableArray.CreateRange(utt.Notes, fun note -> note.SetOn(note.On - refNoteOn)))
                    FUtt.ofUtt name utt)
                |> Array.ofSeq
            { Utts = fUtts }

    type FPh with
        static member toPh x =
            let { Ph = ph; On = on; Off = off } = x
            PhonemeInterval(ph, on, off)

        static member ofPh(ph : PhonemeInterval) =
            { Ph = ph.Ph; On = ph.On; Off = ph.Off }

    type FChGrid with
        static member toCharGrid x =
            let { Pitch = pitch; Phs = fPhs } = x
            let phs = fPhs |> Array.map FPh.toPh
            CharGrid(pitch, phs)

        static member ofCharGrid(charGrid : CharGrid) =
            let fPhs = charGrid.Phs |> Array.map FPh.ofPh
            { Pitch = charGrid.Pitch; Phs = fPhs }

    type FComp with
        static member toComp x =
            let { TimeSig0 = timeSig0Str; Bpm0 = bpm0; AccomOffset = accomOffset; Utts = fUtts } = x
            let timeSig0 =
                match timeSig0Str with
                | null | "" -> timeSignature 4 4
                | _ -> TimeSignature.Parse timeSig0Str
            let uttsByNameDict = dict(Seq.map(FUtt.toUtt bpm0)fUtts)
            let utts = ImmutableArray.CreateRange uttsByNameDict.Keys
            let getUttName utt = uttsByNameDict.[utt]
            Composition(timeSig0, bpm0, utts).SetBgAudioOffset accomOffset, getUttName

        static member ofComp getUttName (comp : Composition) =
            let timeSig0Str = comp.TimeSig0.ToString()
            let accomOffset = comp.BgAudio.SampleOffset
            let uttNames = Seq.map getUttName comp.Utts
            let fUtts = Array.ofSeq(Seq.map2 FUtt.ofUtt uttNames comp.Utts)
            { TimeSig0 = timeSig0Str; Bpm0 = comp.Bpm0; AccomOffset = accomOffset; Utts = fUtts }

    let ofVogPackage(vogPackage : VogPackage) =
        let fComp = vogPackage.Chart
        let comp, getUttName = FComp.toComp fComp

        let comp =
            let accomAudioOp = vogPackage.AccomAudio |> Option.bind AudioSamples.tryValidate
            match accomAudioOp with
            | None -> comp
            | Some audio -> comp.SetBgAudio(AudioTrack(comp.BgAudio.SampleOffset, vogPackage.AccomPath, audio))

        let uttSynthCache =
            (UttSynthCache.Create comp.Bpm0, comp.Utts)
            ||> Seq.fold(fun uttSynthCache utt ->
                let uttName = getUttName utt
                let uttSynthCache =
                    match vogPackage.UttCharGrids.TryGetValue uttName |> Option.ofByRef with
                    | None -> uttSynthCache
                    | Some fChGrids ->
                        let charGrids = Array.map FChGrid.toCharGrid fChGrids
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult -> uttSynthResult.SetCharGrids charGrids)

                let uttSynthCache =
                    match vogPackage.UttF0s.TryGetValue uttName |> Option.ofByRef with
                    | None -> uttSynthCache
                    | Some f0Samples ->
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult -> uttSynthResult.SetF0Samples f0Samples)

                let uttSynthCache =
                    let audioOp =
                        vogPackage.UttAudios.TryGetValue uttName |> Option.ofByRef
                        |> Option.bind AudioSamples.tryValidate
                    match audioOp with
                    | None -> uttSynthCache
                    | Some audio ->
                        utt |> uttSynthCache.UpdateUttSynthResult(fun uttSynthResult -> uttSynthResult.SetAudio audio)

                uttSynthCache)

        comp, uttSynthCache

    let readFromStream stream filePath =
        let vogPackage = VogPackage.read stream filePath
        ofVogPackage vogPackage

    let readFromFile filePath =
        use stream = File.OpenRead filePath
        readFromStream stream filePath

    let toVogPackage comp uttSynthCache =
        let getUttName =
            let uttsByNameDict = dict((comp : Composition).Utts |> Seq.mapi(fun i utt -> utt, $"utt-{i}"))
            fun utt -> uttsByNameDict.[utt]

        let fComp = FComp.ofComp getUttName comp
        let accomPath = comp.BgAudio.AudioFilePath
        let accomAudio = if comp.BgAudio.HasAudio then Some comp.BgAudio.Audio.AsLazy else None

        let uttCharGrids = Dictionary()
        let uttF0s = Dictionary()
        let uttAudios = Dictionary()
        for utt in comp.Utts do
            let uttSynthResult = (uttSynthCache : UttSynthCache).GetOrDefault utt
            let uttName = getUttName utt
            if uttSynthResult.HasCharGrids then
                let charGrids = uttSynthResult.CharGrids
                let fChGrids = Array.map FChGrid.ofCharGrid charGrids
                uttCharGrids.Add(uttName, fChGrids)
            if uttSynthResult.HasF0Samples then
                uttF0s.Add(uttName, uttSynthResult.F0Samples)
            match uttSynthResult.Audio with
            | Some audio when audio.FileBytes.Length > 0 -> uttAudios.Add(uttName, audio.AsLazy)
            | _ -> ()

        {   Chart = fComp
            AccomPath = accomPath
            AccomAudio = accomAudio
            UttCharGrids = uttCharGrids
            UttF0s = uttF0s
            UttAudios = uttAudios }

    let saveToStream stream filePath comp uttSynthCache =
        let vogPackage = toVogPackage comp uttSynthCache
        VogPackage.save stream filePath vogPackage

    let saveToFile filePath comp uttSynthCache =
        use stream = File.Open(filePath, FileMode.Create)
        saveToStream stream filePath comp uttSynthCache

    let toClipboardText refNoteOn activeUttOp otherUtts =
        let fClip = FClip.ofUtts refNoteOn activeUttOp otherUtts
        JsonConvert.SerializeObjectFormatted fClip

    let ofClipboardText bpm0 refNoteOn jStr =
        let fClip = JsonConvert.DeserializeObject<_> jStr
        FClip.toUtts bpm0 refNoteOn fClip


