module Vogen.Synth.Rfft

open Doaz.Reactive
open Doaz.Reactive.Math
open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.IO.Compression
open System.Linq
open System.Text


let modelLazy = lazy InferenceSession.ofEmbedded @"Vogen.Synth.Models.rfft.onnx"

let run fftSize (audioSamples : _ [])(indices : _ []) =
    let audioSlices = DenseTensor<float32>(ReadOnlySpan([| indices.Length; fftSize |]))
    let audioSamplesMemory = audioSamples.AsMemory()
    for i in 0 .. indices.Length - 1 do
        let sliceStart = indices.[i] |> clamp 0 audioSamples.Length
        let sliceEnd = indices.[i] + fftSize |> clamp 0 audioSamples.Length
        let slice = audioSamplesMemory.Slice(sliceStart, sliceEnd - sliceStart)
        let outSliceStart = -indices.[i] |> clamp 0 fftSize
        slice.CopyTo(audioSlices.Buffer.Slice(i * fftSize + outSliceStart, sliceEnd - sliceStart))

    let xs = [|
        NamedOnnxValue.CreateFromTensor("xs", audioSlices) |]

    let model = modelLazy.Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let cis = ys.[0].Value :?> DenseTensor<byte> |> Array2D.ofDenseTensor

    cis


