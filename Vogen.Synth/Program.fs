module Vogen.Synth.Program

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


let allOrderedPhs =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream @"Vogen.Synth.orderedAlphabet.json"
    use reader = new StreamReader(stream, Encoding.UTF8)
    JsonConvert.DeserializeObject<string []>(reader.ReadToEnd())

let letterToIndex =
    ImmutableDictionary.CreateRange(
        Seq.append [| '\000' |] "qwertyuiopasdfghjklzxcvbnm1234567890"
        |> Seq.mapi(fun i letter -> KeyValuePair(letter, i)))

[<EntryPoint>]
let main argv =
    printfn "Hello world from F#"

    printfn "All Ordered Phonemes: %A" allOrderedPhs

    let xLength = 8
    let buildLetterIndicesArray(roms : string []) =
        let lis = Array.zeroCreate(roms.Length * xLength)
        roms |> Array.iteri(fun i rom ->
            rom |> String.iteri(fun j letter ->
                lis.[i * xLength + j] <- letterToIndex.[letter]))
        lis.ToTensor().Reshape(ReadOnlySpan([| roms.Length; xLength |]))

    //DenseTensor
    let xs = [|
        NamedOnnxValue.CreateFromTensor("lis", buildLetterIndicesArray([| "bu"; "yuan"; "ran" |]))
    |]

    use sess = new InferenceSession(@"models\g2p\man.v20210604-221933.onnx")
    let ys = (sess.Run xs).ToArray()
    let phis = ys.[0].Value :?> Tensor<int>

    let yLength = 4
    let phs = [|
        for i in 0 .. int phis.Length / yLength - 1 -> [|
            for j in 0 .. yLength - 1 ->
                allOrderedPhs.[phis.GetValue(i * yLength + j)] |] |]

    printfn "Output: %A" phs

    0


