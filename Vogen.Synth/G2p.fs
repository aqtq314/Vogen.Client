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


let allPhs =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream @"Vogen.Synth.orderedAlphabet.json"
    use reader = new StreamReader(stream, Encoding.UTF8)
    JsonConvert.DeserializeObject<string []>(reader.ReadToEnd())

let allLetters = "\000qwertyuiopasdfghjklzxcvbnm1234567890"

let letterToIndex =
    ImmutableDictionary.CreateRange(allLetters |> Seq.mapi(fun i letter -> KeyValuePair(letter, i)))

let xLength = 8
let yLength = 4

let encLetterIndices(roms : string []) =
    let lis = Array.zeroCreate(roms.Length * xLength)
    roms |> Array.iteri(fun i rom ->
        rom |> String.iteri(fun j letter ->
            lis.[i * xLength + j] <- letterToIndex.[letter]))
    lis.ToTensor().Reshape(ReadOnlySpan([| roms.Length; xLength |]))

let enc roms = [|
    NamedOnnxValue.CreateFromTensor("lis", encLetterIndices roms) |]

let models = dict [|
    "man", lazy new InferenceSession(@"models\g2p\man.v20210604-221933.onnx")
    "yue", lazy new InferenceSession(@"models\g2p\yue.v20210605-000325.onnx")
    "yue-wz", lazy new InferenceSession(@"models\g2p\yue-wz.v20210605-000658.onnx") |]

let dec(phis : Tensor<int>) = [|
    for i in 0 .. int phis.Length / yLength - 1 ->
        [| for j in 0 .. yLength - 1 -> allPhs.[phis.GetValue(i * yLength + j)] |]
        |> Array.filter(fun ph -> ph <> null) |]

let run romScheme roms =
    let xs = enc roms
    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let phis = ys.[0].Value :?> Tensor<int>
    dec phis


