module Vogen.Synth.G2p

open Doaz.Reactive
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
    "man", lazy InferenceSession.ofEmbedded @"Vogen.Synth.Models.g2p.man.onnx"
    "yue", lazy InferenceSession.ofEmbedded @"Vogen.Synth.Models.g2p.yue.onnx"
    "yue-wz", lazy InferenceSession.ofEmbedded @"Vogen.Synth.Models.g2p.yue-wz.onnx" |]

let runForScheme romScheme (roms : string []) =
    let romLetters =
        let letters = Array.create(roms.Length * xLength) ""
        roms |> Array.iteri(fun i rom ->
            rom |> String.iteri(fun j letter ->
                if j < xLength then
                    letters.[i * xLength + j] <- letter.ToString()))
        letters.ToTensor().Reshape(ReadOnlySpan([| roms.Length; xLength |]))

    let xs = [|
        NamedOnnxValue.CreateFromTensor("letters", romLetters) |]

    let model = models.[romScheme].Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let phis = ys.[0].Value :?> DenseTensor<string>
    let phs = [|
        for i in 0 .. int phis.Length / yLength - 1 ->
            [| for j in 0 .. yLength - 1 -> phis.GetValue(i * yLength + j) |]
            |> Array.filter(fun ph -> not(String.IsNullOrEmpty ph)) |]

    phs

let run romScheme (roms : string []) =
    let schemeToRomIndices =
        roms
        |> Seq.mapi(fun romIndex rom ->
            let currRomScheme =
                if isNotNull rom && rom.Contains ':' then rom.[.. rom.IndexOf ':' - 1]
                else romScheme
            currRomScheme, romIndex)
        |> Seq.filter(fun (currRomScheme, romIndex) -> isNotNull roms.[romIndex])
        |> Seq.groupBy(fun (currRomScheme, romIndex) -> currRomScheme)
        |> Dict.ofSeq
        |> Dict.mapValue(fun entries ->
            entries |> Seq.map(fun (currRomScheme, romIndex) -> romIndex) |> Array.ofSeq)

    let romsNoPrefix = roms |> Array.map(fun rom ->
        if isNotNull rom && rom.Contains ':' then rom.[rom.IndexOf ':' + 1 ..]
        else rom)

    let phs = Array.zeroCreate roms.Length
    for KeyValue(currRomScheme, romIndices) in schemeToRomIndices do
        let schemeRoms = romIndices |> Array.map(fun romIndex -> romsNoPrefix.[romIndex])
        let schemePhs = runForScheme currRomScheme schemeRoms
        for romIndex, schemePh in Array.zip romIndices schemePhs do
            phs.[romIndex] <-
                if currRomScheme = romScheme then schemePh else
                    schemePh |> Array.map(fun ph ->
                        if String.IsNullOrEmpty ph then ph else $"{currRomScheme}:{ph}")

    phs


