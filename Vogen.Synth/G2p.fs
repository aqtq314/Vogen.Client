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

let encLetterIndices(romsNonNull : string []) =
    let lis = Array.create(romsNonNull.Length * xLength) ""
    romsNonNull |> Array.iteri(fun i rom ->
        rom |> String.iteri(fun j letter ->
            if j < xLength then
                lis.[i * xLength + j] <- letter.ToString()))
    lis.ToTensor().Reshape(ReadOnlySpan([| romsNonNull.Length; xLength |]))

let enc romsNonNull = [|
    NamedOnnxValue.CreateFromTensor("letters", encLetterIndices romsNonNull) |]

let models = dict [|
    "man", lazy new InferenceSession(@"models\g2p\man.v20210617-144543.g2p.onnx")
    "yue", lazy new InferenceSession(@"models\g2p\yue.v20210617-144354.g2p.onnx")
    "yue-wz", lazy new InferenceSession(@"models\g2p\yue-wz.v20210617-131737.g2p.onnx") |]

let dec(phis : Tensor<string>) = [|
    for i in 0 .. int phis.Length / yLength - 1 ->
        [| for j in 0 .. yLength - 1 -> phis.GetValue(i * yLength + j) |]
        |> Array.filter(fun ph -> not(String.IsNullOrEmpty ph)) |]

let run romScheme (roms : string []) =
    let romsNonNullIndices = [|
        for i in 0 .. roms.Length - 1 do
            if roms.[i] <> null then
                yield i |]
    let romsNonNull =
        romsNonNullIndices |> Array.map(fun i -> roms.[i])

    let xs = enc romsNonNull
    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let phis = ys.[0].Value :?> Tensor<string>
    let phsNonNull = dec phis

    let phs = Array.zeroCreate roms.Length
    romsNonNullIndices |> Seq.iteri(fun nonNullIndex outIndex ->
        phs.[outIndex] <- phsNonNull.[nonNullIndex])
    phs


