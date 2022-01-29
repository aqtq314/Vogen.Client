module Vogen.Synth.Prosody

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
    "man", lazy InferenceSession.ofEmbedded @"Vogen.Synth.Models.po.man.onnx"
    "yue", lazy InferenceSession.ofEmbedded @"Vogen.Synth.Models.po.yue.onnx"
    "yue-wz", lazy models.["yue"].Value |]

let run romScheme uttDur (chars : seq<TChar>) =
    let chars = Array.ofSeq chars
    let roms = chars |> Array.map(fun ch -> ch.Rom)
    let chPhs = G2p.run romScheme roms

    // encode
    let chPhs = chPhs |> Array.map(fun phs -> if phs <> null then phs else [| null |])
    let phs = chPhs |> Array.collect(Array.map(fun ph ->
        if ph = null then ""
        elif ph.Contains ':' then ph
        else $"{romScheme}:{ph}"))
    let chPhCounts = chPhs |> Array.map(fun phs -> phs.Length)
    let chPhCounts64 = chPhCounts |> Array.map int64
    let noteBounds = [|
        yield 0
        yield! Seq.pairwise chars
            |> Seq.map(fun (ch0, ch1) ->
                if ch1.Notes <> null then
                    ch1.Notes.[0].On
                else
                    ch0.Notes.[^0].Off)
        yield uttDur |]
    let noteBoundsSec = noteBounds |> Array.map(fun t -> float32(frameToTime(float t).TotalSeconds))
    let noteDursSec = [| for nt0, nt1 in Array.pairwise noteBoundsSec -> nt1 - nt0 |]

    let xs = [|
        NamedOnnxValue.CreateFromTensor("phs", phs.ToTensor())
        NamedOnnxValue.CreateFromTensor("chPhCounts", chPhCounts64.ToTensor())
        NamedOnnxValue.CreateFromTensor("noteDursSec", noteDursSec.ToTensor()) |]

    // run model
    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let phBoundsSec = ys.[0].Value :?> DenseTensor<float32> |> Array.ofDenseTensor

    // fix phs with duration <= 0
    let minPhDurSec = float32 (frameToTime 1.01).TotalSeconds
    for nt0, nt1 in Array.pairwise noteBoundsSec |> Array.rev do
        let phStartIndex = phBoundsSec |> Array.findIndex(fun phTime -> phTime >= nt0)
        let phEndIndex = phBoundsSec |> Array.findIndexBack(fun phTime -> phTime < nt1)
        if phStartIndex <= phEndIndex then
            let notePhCount = phEndIndex - phStartIndex + 2
            let minNoteDurSec = float32 notePhCount * minPhDurSec
            let nt0 =
                if phStartIndex = 0 || phBoundsSec.[phStartIndex] - nt0 < nt0 - phBoundsSec.[phStartIndex - 1] then
                    nt0 - minPhDurSec  // if first ph has more duration in note from previous char than in current char then
                else nt0               // modify nt0 by -1 frames so that the first ph can have min duration 0 in current char
            let nt0 = min nt0 (nt1 - minNoteDurSec)

            let notePhBoundsSec = [| yield nt0; yield! phBoundsSec.[phStartIndex .. phEndIndex]; yield nt1 |]
            let notePhDursSec = Array.pairwise notePhBoundsSec |> Array.map(fun (phOn, phOff) -> phOff - phOn)
            if notePhDursSec |> Array.exists(fun phDur -> phDur < minPhDurSec) then
                let notePhExDursSec = notePhDursSec |> Array.map(fun phDur -> phDur - minPhDurSec |> max 0.0f)
                let noteExDurSec = notePhExDursSec |> Array.sum
                let outNotePhDursSec = notePhExDursSec |> Array.map(fun phDur ->
                    phDur / noteExDurSec * (nt1 - nt0 - minNoteDurSec) + minPhDurSec)
                let outNotePhBoundsSec = outNotePhDursSec |> Array.scan(+) nt0
                for phIndex in phStartIndex .. phEndIndex do
                    phBoundsSec.[phIndex] <- outNotePhBoundsSec.[phIndex - phStartIndex + 1]
                GC.KeepAlive(obj())

    // decode
    let phBounds = Array.init(int phBoundsSec.Length)(fun i ->
        float phBoundsSec.[i]
        |> TimeSpan.FromSeconds
        |> timeToFrame
        |> round |> int)
    let chPhIndexBounds = chPhCounts |> Array.scan(+) 0
    let chPhBounds = chPhIndexBounds |> Array.pairwise |> Array.map(fun (startIndex, endIndex) ->
        phBounds.[startIndex .. endIndex])

    let outChars = (chars, chPhs, chPhBounds) |||> Array.map3(fun ch phs phBounds ->
        let ipa = (phs, Array.pairwise phBounds) ||> Array.map2(fun ph (phOn, phOff) ->
            { Ph = ph; On = phOn; Off = phOff })
        { ch with Ipa = ImmutableList.CreateRange ipa })

    ImmutableList.CreateRange outChars


