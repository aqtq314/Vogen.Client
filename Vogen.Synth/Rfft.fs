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

let run fftSize (x : _ [])(xIndices : _ [])(outFreqs : _ []) =
    let xFramesTensor = DenseTensor<float32>(ReadOnlySpan([| xIndices.Length; fftSize |]))
    let xMemory = x.AsMemory()
    let xLength = x.Length
    let xUpper = xLength - 1
    if xLength >= 2 then
        for i in 0 .. xIndices.Length - 1 do
            let xIndexMid = xIndices.[i]
            let mutable xIndexStart = xIndexMid - fftSize / 2
            let mutable xIndexEnd = xIndexStart + fftSize
            let mutable outIndexStart = i * fftSize
            let mutable outIndexEnd = outIndexStart + fftSize
            if xIndexStart < xLength && xIndexEnd >= 0 then
                if xIndexStart < 0 then
                    for t in xIndexStart .. 0 - 1 do    // reflect
                        xFramesTensor.[outIndexStart + t - xIndexStart] <- x.[abs((t - xUpper) % (xUpper * 2) + xUpper)]
                    outIndexStart <- outIndexStart - xIndexStart
                    xIndexStart <- 0
                if xIndexEnd >= xLength then
                    for t in xLength .. xIndexEnd - 1 do    // reflect
                        xFramesTensor.[outIndexEnd + t - xIndexEnd] <- x.[abs((t + xUpper) % (xUpper * 2) - xUpper)]
                    outIndexEnd <- outIndexEnd + xLength - xIndexEnd
                    xIndexEnd <- xLength
                xMemory.Slice(xIndexStart, xIndexEnd - xIndexStart).CopyTo(
                    xFramesTensor.Buffer.Slice(outIndexStart, outIndexEnd - outIndexStart))

    let outFreqsTensor = DenseTensor<float32>(ReadOnlySpan([| outFreqs.Length |]))
    outFreqs.CopyTo outFreqsTensor.Buffer

    let xs = [|
        NamedOnnxValue.CreateFromTensor("xFrames", xFramesTensor)
        NamedOnnxValue.CreateFromTensor("outFreqs", outFreqsTensor) |]

    let model = modelLazy.Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let cis = ys.[0].Value :?> DenseTensor<byte> |> Array2D.ofDenseTensor

    cis


