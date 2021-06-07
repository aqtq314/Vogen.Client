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
    "man", lazy new InferenceSession(@"models\po\man.v20210606-033513.po.onnx")
    "yue", lazy new InferenceSession(@"models\po\yue.v20210606-040514.po.onnx")
    "yue-wz", lazy models.["yue"].Value |]

let run romScheme uttDur (chars : seq<TChar>) =
    let chars = Array.ofSeq chars
    let roms = chars |> Array.map(fun ch -> ch.Rom)
    let chPhs = G2p.run romScheme roms

    // encode
    let chPhs = chPhs |> Array.map(fun phs -> if phs <> null then phs else [| null |])
    let phEncs = chPhs |> Array.collect(fun phs -> Array.map G2p.phToIndex phs)
    let chPhCounts = chPhs |> Array.map(fun phs -> phs.Length)
    let noteBounds = [|
        yield 0
        yield! Seq.pairwise chars
            |> Seq.map(fun (ch0, ch1) ->
                if ch1.Notes <> null then
                    ch1.Notes.[0].On
                else
                    ch0.Notes.[^0].Off)
        yield uttDur |]
    let noteBoundsSec = noteBounds |> Array.map(fun t ->
        float32(frameToTime(float t).TotalSeconds))

    let xs = [|
        NamedOnnxValue.CreateFromTensor("phs", phEncs.ToTensor())
        NamedOnnxValue.CreateFromTensor("chPhCounts", chPhCounts.ToTensor())
        NamedOnnxValue.CreateFromTensor("noteBoundsSec", noteBoundsSec.ToTensor()) |]

    // run model
    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let phBoundsSec = ys.[0].Value :?> Tensor<float32>

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


