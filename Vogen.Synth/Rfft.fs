module Vogen.Synth.Rfft

open Doaz.Reactive
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


let modelLazy = lazy InferenceSession.ofEmbedded @"Vogen.Synth.models.rfft.onnx"

let run fftSize (audioSamples : _ [])(indices : _ []) =
    let audioSlices = DenseTensor<float32>(ReadOnlySpan([| indices.Length; fftSize |]))
    let audioSamplesMemory = audioSamples.AsMemory()
    for i in 0 .. indices.Length - 1 do
        let audioStartIndex = indices.[i] |> min audioSamples.Length
        let audioSliceLength = fftSize |> min(audioSamples.Length - audioStartIndex)
        audioSamplesMemory.Slice(audioStartIndex, audioSliceLength).CopyTo(audioSlices.Buffer.Slice(i * fftSize, audioSliceLength))

    let xs = [|
        NamedOnnxValue.CreateFromTensor("xs", audioSlices) |]

    let model = modelLazy.Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let cis = ys.[0].Value :?> DenseTensor<byte> |> Array2D.ofDenseTensor

    cis


