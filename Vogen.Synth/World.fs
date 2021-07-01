module Vogen.Synth.World

open Microsoft.FSharp.NativeInterop
open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices


#nowarn "9"

[<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
extern void private DecodeAperiodicity([<In>] nativeint [] coded_aperiodicity,
    int f0_length, int fs, int fft_size, [<In; Out>] nativeint [] aperiodicity)

[<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
extern void private DecodeSpectralEnvelope([<In>] nativeint [] coded_spectral_envelope,
    int f0_length, int fs, int fft_size, int number_of_dimensions, [<In; Out>] nativeint [] spectrogram)

[<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
extern void private Synthesis([<In>] float [] f0, int f0_length,
    [<In>] nativeint [] spectrogram, [<In>] nativeint [] aperiodicity,
    int fft_size, float frame_period, int fs, int y_length, [<Out>] float [] y)

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

let array2dIntptrConv = Array2DIntPtrConversionBuilder()

let synthesize(f0 : float [])(mgc : float [,])(bap : float [,]) = array2dIntptrConv {
    let! mgcFramePtrs = mgc
    let! spFramePtrs = Array2D.zeroCreate(mgc.GetLength 0)(worldFftSize / 2 + 1)
    DecodeSpectralEnvelope(mgcFramePtrs, mgc.GetLength 0, fs, worldFftSize, mgc.GetLength 1, spFramePtrs)

    let! bapFramePtrs = bap
    let! apFramePtrs = Array2D.zeroCreate(bap.GetLength 0)(worldFftSize / 2 + 1)
    DecodeAperiodicity(bapFramePtrs, bap.GetLength 0, fs, worldFftSize, apFramePtrs)

    let yLength = int(f0.LongLength * int64 hopSize.TotalMilliseconds * int64 fs / 1000L)
    let y = Array.zeroCreate yLength
    Synthesis(f0, f0.Length, spFramePtrs, apFramePtrs, worldFftSize, hopSize.TotalMilliseconds, fs, yLength, y)
    return y }

let synthesize32(f0 : float32 [])(mgc : float32 [,])(bap : float32 [,]) =
    synthesize(Array.map float f0)(Array2D.map float mgc)(Array2D.map float bap)
    |> Array.map float32


