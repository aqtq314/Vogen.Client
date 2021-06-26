module Vogen.Synth.F0

open Doaz.Reactive
open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq
open System.Text
open Vogen.Synth.TimeTable

#nowarn "40"


let rec models = dict [|
    "man", lazy InferenceSession.ofEmbedded @"Vogen.Synth.models.f0.man.v20210620-044257.onnx"
    "yue", lazy InferenceSession.ofEmbedded @"Vogen.Synth.models.f0.yue.v20210620-044257.onnx"
    "yue-wz", lazy models.["yue"].Value |]

let run romScheme (chars : IReadOnlyList<TimeTable.TChar>) =
    let phs = chars |> Seq.collect(fun (ch : TimeTable.TChar) -> ch.Ipa) |> Array.ofSeq

    let uttDur = chars.[^0].Ipa.[^0].Off
    let noteOps = chars |> Seq.collect(fun (ch : TimeTable.TChar) ->
        if ch.Notes <> null then Seq.map Some ch.Notes else seq { None }) |> Array.ofSeq
    let noteBounds = [|
        yield 0
        yield! Seq.pairwise noteOps |> Seq.map(
            function
            | _, Some note -> note.On
            | Some prevNote, _ -> prevNote.Off
            | _, _ -> raise(ArgumentException(sprintf "Consecutive sil chars in %A" chars)))
        yield uttDur |]

    let phSyms = phs |> Array.map(fun ph ->
        if ph.Ph = null then ""
        elif ph.Ph.Contains ':' then ph.Ph
        else $"{romScheme}:{ph.Ph}")
    let notePitches = noteOps |> Array.map(function | None -> 0f | Some note -> float32 note.Pitch)
    let noteDurs = Array.pairwise noteBounds |> Array.map(fun (t0, t1) -> int64(t1 - t0))
    let noteToCharIndex = noteOps |> Array.mapi(fun i noteOp -> int64 i)
    let phDurs = phs |> Array.map(fun ph -> int64(ph.Off - ph.On))

    let xs = [|
        NamedOnnxValue.CreateFromTensor("phs", phSyms.ToTensor())
        NamedOnnxValue.CreateFromTensor("notePitches", notePitches.ToTensor())
        NamedOnnxValue.CreateFromTensor("noteDurs", noteDurs.ToTensor())
        NamedOnnxValue.CreateFromTensor("noteToCharIndex", noteToCharIndex.ToTensor())
        NamedOnnxValue.CreateFromTensor("phDurs", phDurs.ToTensor()) |]

    // run model
    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let f0 = ys.[0].Value :?> DenseTensor<float32> |> Array.ofDenseTensor

    f0


