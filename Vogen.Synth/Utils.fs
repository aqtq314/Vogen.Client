namespace Vogen.Synth

open Microsoft.FSharp.NativeInterop
open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open System
open System.Collections.Generic
open System.IO
open System.Reflection

#nowarn "9"


[<AutoOpen>]
module Params =
    let [<Literal>] fs = 44100
    let [<Literal>] channels = 1
    let [<Literal>] worldFftSize = 2048

    let hopSize = TimeSpan.FromMilliseconds 10.0
    let headSil = TimeSpan.FromSeconds 0.5
    let tailSil = TimeSpan.FromSeconds 0.5

    let appDir =
        let entryAssembly = Assembly.GetEntryAssembly()
        Path.GetDirectoryName entryAssembly.Location

module InferenceSession =
    let ofStream(stream : Stream) =
        let modelBytes =
            if stream.CanSeek then
                use reader = new BinaryReader(stream)
                reader.ReadBytes(int stream.Length)
            else
                use memoryStream = new MemoryStream()
                stream.CopyTo memoryStream
                memoryStream.ToArray()

        new InferenceSession(modelBytes)

    let ofEmbedded uri =
        use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream uri
        ofStream stream

module Array =
    let ofDenseTensor(tensor : DenseTensor<_>) =
        let outArr = Array.zeroCreate(int tensor.Length)
        tensor.Buffer.Span.CopyTo(outArr.AsSpan())
        outArr

module Array2D =
    let ofDenseTensor(tensor : DenseTensor<'a>) =
        let width = tensor.Dimensions.[tensor.Dimensions.Length - 1]
        let length = int tensor.Length / width

        let outArr : 'a [,] = Array2D.zeroCreate length width
        use x = fixed &outArr.[0, 0]
        tensor.Buffer.Span.CopyTo(Span<'a>(NativePtr.toVoidPtr x, outArr.Length))
        outArr

type Array2DIntPtrConversionBuilder() =
    member x.Zero() = ()
    member x.Return m = m
    member x.Bind(arr : float [,], cont) =
        use arrPtr = fixed &arr.[0, 0]
        let arrRowPtrs = Array.zeroCreate(arr.GetLength 0)
        for i in 0 .. arr.GetLength 0 - 1 do
            arrRowPtrs.[i] <- NativePtr.toNativeInt(NativePtr.add arrPtr (i * arr.GetLength 1))
        let contResult = cont arrRowPtrs
        GC.KeepAlive arrRowPtrs     // prevent tail call
        contResult

[<AutoOpen>]
module Array2DIntPtrConversionBuilderUtil =
    let array2dIntptrConv = Array2DIntPtrConversionBuilder()


