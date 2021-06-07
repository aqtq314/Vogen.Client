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

let private phToIndexLookup =
    ImmutableDictionary.CreateRange(allPhs |> Seq.mapi(fun i ph -> KeyValuePair(ph, i)) |> Seq.skip 1)
let phToIndex ph =
    match ph with
    | null -> 0
    | _ -> phToIndexLookup.[ph]

let allLetters = "\000qwertyuiopasdfghjklzxcvbnm1234567890"

let letterToIndex =
    ImmutableDictionary.CreateRange(allLetters |> Seq.mapi(fun i letter -> KeyValuePair(letter, i)))

let xLength = 8
let yLength = 4

let encLetterIndices(romsNonNull : string []) =
    let lis = Array.zeroCreate(romsNonNull.Length * xLength)
    romsNonNull |> Array.iteri(fun i rom ->
        rom |> String.iteri(fun j letter ->
            if j < xLength then
                lis.[i * xLength + j] <- letterToIndex.[letter]))
    lis.ToTensor().Reshape(ReadOnlySpan([| romsNonNull.Length; xLength |]))

let enc romsNonNull = [|
    NamedOnnxValue.CreateFromTensor("lis", encLetterIndices romsNonNull) |]

let models = dict [|
    "man", lazy new InferenceSession(@"models\g2p\man.v20210604-221933.g2p.onnx")
    "yue", lazy new InferenceSession(@"models\g2p\yue.v20210605-000325.g2p.onnx")
    "yue-wz", lazy new InferenceSession(@"models\g2p\yue-wz.v20210605-000658.g2p.onnx") |]

let dec(phis : Tensor<int>) = [|
    for i in 0 .. int phis.Length / yLength - 1 ->
        [| for j in 0 .. yLength - 1 -> allPhs.[phis.GetValue(i * yLength + j)] |]
        |> Array.filter(fun ph -> ph <> null) |]

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
    let phis = ys.[0].Value :?> Tensor<int>
    let phsNonNull = dec phis

    let phs = Array.zeroCreate roms.Length
    romsNonNullIndices |> Seq.iteri(fun nonNullIndex outIndex ->
        phs.[outIndex] <- phsNonNull.[nonNullIndex])
    phs


