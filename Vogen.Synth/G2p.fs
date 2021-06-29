module Vogen.Synth.G2p

open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq
open System.Reflection
open System.Text


let xLength = 8
let yLength = 4

let models = dict [|
    "man", lazy InferenceSession.ofEmbedded @"Vogen.Synth.models.g2p.man.onnx"
    "yue", lazy InferenceSession.ofEmbedded @"Vogen.Synth.models.g2p.yue.onnx"
    "yue-wz", lazy InferenceSession.ofEmbedded @"Vogen.Synth.models.g2p.yue-wz.onnx" |]

let run romScheme (roms : string []) =
    let romsNonNullIndices = [|
        for i in 0 .. roms.Length - 1 do
            if roms.[i] <> null then
                yield i |]
    let romsNonNull =
        romsNonNullIndices |> Array.map(fun i -> roms.[i])

    let romLetters =
        let letters = Array.create(romsNonNull.Length * xLength) ""
        romsNonNull |> Array.iteri(fun i rom ->
            rom |> String.iteri(fun j letter ->
                if j < xLength then
                    letters.[i * xLength + j] <- letter.ToString()))
        letters.ToTensor().Reshape(ReadOnlySpan([| romsNonNull.Length; xLength |]))

    let xs = [|
        NamedOnnxValue.CreateFromTensor("letters", romLetters) |]

    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let phis = ys.[0].Value :?> DenseTensor<string>
    let phsNonNull = [|
        for i in 0 .. int phis.Length / yLength - 1 ->
            [| for j in 0 .. yLength - 1 -> phis.GetValue(i * yLength + j) |]
            |> Array.filter(fun ph -> not(String.IsNullOrEmpty ph)) |]

    let phs = Array.zeroCreate roms.Length
    romsNonNullIndices |> Seq.iteri(fun nonNullIndex outIndex ->
        phs.[outIndex] <- phsNonNull.[nonNullIndex])
    phs


