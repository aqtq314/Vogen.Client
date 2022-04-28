namespace Vogen.Synth

open Doaz.Reactive
open Microsoft.FSharp.NativeInterop
open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices


#nowarn "9"

module private WorldNativeInterop =
    let [<Literal>] DefaultFramePeriod = 5.0
    let [<Literal>] DefaultF0Floor = 71.0
    let [<Literal>] DefaultF0Ceil = 800.0

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type DioOption =
        val mutable F0Floor : float
        val mutable F0Ceil : float
        val mutable ChannelsInOctave : float
        val mutable FramePeriod : float     // msec
        val mutable Speed : int             // (1, 2, ..., 12)
        val mutable AllowedRange : float    // Threshold used for fixing the F0 contour.

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type HarvestOption =
        val mutable F0Floor : float
        val mutable F0Ceil : float
        val mutable FramePeriod : float

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type CheapTrickOption =
        val mutable Q1 : float
        val mutable F0Floor : float
        val mutable FftSize : int

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type D4COption =
        val mutable Threshold : float

    // Dio
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void Dio([<In>] float [] x, int x_length, int fs, [<In>] DioOption& option,
        [<Out>] float [] temporal_positions, [<Out>] float [] f0)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void InitializeDioOption([<Out>] DioOption& option)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetSamplesForDIO(int fs, int xLength, float framePeriod)

    // StoneMask
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void StoneMask([<In>] float [] x, int x_length, int fs,
        [<In>] float [] temporal_positions, [<In>] float [] f0, int f0_length, [<Out>] float [] refined_f0)

    // Harvest
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void Harvest([<In>] float [] x, int x_length, int fs, [<In>] HarvestOption& option,
        [<Out>] float [] temporal_positions, [<Out>] float [] f0)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void InitializeHarvestOption([<Out>] HarvestOption& option)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetSamplesForHarvest(int fs, int xLength, float framePeriod)

    // CheapTrick
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void CheapTrick([<In>] float [] x, int x_length, int fs,
        [<In>] float [] temporal_positions, [<In>] float [] f0, int f0_length,
        [<In>] CheapTrickOption& option, [<In; Out>] nativeint [] spectrogram)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void InitializeCheapTrickOption(int fs, [<Out>] CheapTrickOption& option)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetFFTSizeForCheapTrick(int fs, [<In>] CheapTrickOption& option)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern float GetF0FloorForCheapTrick(int fs, int fftSize)

    // D4C
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void D4C([<In>] float [] x, int x_length, int fs,
        [<In>] float [] temporal_positions, [<In>] float [] f0, int f0_length,
        int fft_size, [<In>] D4COption& option, [<In; Out>] nativeint [] aperiodicity)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void InitializeD4COption([<Out>] D4COption& option)

    // Codecs
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern int GetNumberOfAperiodicities(int fs)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void CodeAperiodicity([<In>] nativeint [] aperiodicity, int f0_length,
        int fs, int fft_size, [<In; Out>] nativeint [] coded_aperiodicity)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void DecodeAperiodicity([<In>] nativeint [] coded_aperiodicity,
        int f0_length, int fs, int fft_size, [<In; Out>] nativeint [] aperiodicity)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void CodeSpectralEnvelope([<In>] nativeint [] spectrogram, int f0_length,
        int fs, int fft_size, int number_of_dimensions, [<In; Out>] nativeint [] coded_spectral_envelope)

    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void DecodeSpectralEnvelope([<In>] nativeint [] coded_spectral_envelope,
        int f0_length, int fs, int fft_size, int number_of_dimensions, [<In; Out>] nativeint [] spectrogram)

    // Synthesis
    [<DllImport(@"World.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern void Synthesis([<In>] float [] f0, int f0_length,
        [<In>] nativeint [] spectrogram, [<In>] nativeint [] aperiodicity,
        int fft_size, float frame_period, int fs, int y_length, [<Out>] float [] y)


open WorldNativeInterop
type World =
    static member Dio(
        x, fs,
        [<Optional; DefaultParameterValue(DefaultF0Floor)>]     f0Floor,
        [<Optional; DefaultParameterValue(DefaultF0Ceil)>]      f0Ceil,
        [<Optional; DefaultParameterValue(2.0)>]                channelsInOctave,
        [<Optional; DefaultParameterValue(DefaultFramePeriod)>] framePeriod,
        [<Optional; DefaultParameterValue(1)>]                  speed,
        [<Optional; DefaultParameterValue(0.1)>]                allowedRange) =
        let mutable option = DioOption()
        InitializeDioOption &option
        option.F0Floor          <- f0Floor
        option.F0Ceil           <- f0Ceil
        option.ChannelsInOctave <- channelsInOctave
        option.FramePeriod      <- framePeriod
        option.Speed            <- speed
        option.AllowedRange     <- allowedRange

        let f0Length = GetSamplesForDIO(fs, (x : _ []).Length, option.FramePeriod)
        let f0 = Array.zeroCreate f0Length
        let tpos = Array.zeroCreate f0Length
        Dio(x, x.Length, fs, &option, tpos, f0)
        f0, tpos

    static member GetSamplesForDIO(fs, xLength,
        [<Optional; DefaultParameterValue(DefaultFramePeriod)>] framePeriod) =
        let framePeriod = framePeriod
        GetSamplesForDIO(fs, xLength, framePeriod)

    static member Stonemask(x, fs, tpos, f0) =
        let outF0 = Array.zeroCreate (f0 : _ []).Length
        StoneMask(x, x.Length, fs, tpos, f0, f0.Length, outF0)
        outF0

    static member Harvest(x, fs,
        [<Optional; DefaultParameterValue(DefaultF0Floor)>]     f0Floor,
        [<Optional; DefaultParameterValue(DefaultF0Ceil)>]      f0Ceil,
        [<Optional; DefaultParameterValue(DefaultFramePeriod)>] framePeriod) =
        let mutable option = HarvestOption()
        InitializeHarvestOption &option
        option.F0Floor     <- f0Floor
        option.F0Ceil      <- f0Ceil
        option.FramePeriod <- framePeriod

        let f0Length = GetSamplesForHarvest(fs, (x : _ []).Length, option.FramePeriod)
        let f0 = Array.zeroCreate f0Length
        let tpos = Array.zeroCreate f0Length
        Harvest(x, x.Length, fs, &option, tpos, f0)
        f0, tpos

    static member GetSamplesForHarvest(fs, xLength,
        [<Optional; DefaultParameterValue(DefaultFramePeriod)>] framePeriod) =
        let framePeriod = framePeriod
        GetSamplesForHarvest(fs, xLength, framePeriod)

    static member CheapTrick(x, fs, tpos, f0,
        [<Optional; DefaultParameterValue(-0.15)>]          q1,
        [<Optional; DefaultParameterValue(DefaultF0Floor)>] f0Floor,
        [<Optional; DefaultParameterValue(0)>]              fftSize) = array2dIntptrConv {
        let mutable option = CheapTrickOption()
        InitializeCheapTrickOption(fs, &option)
        option.Q1      <- q1
        option.F0Floor <- f0Floor
        option.FftSize <- if fftSize > 0 then fftSize else GetFFTSizeForCheapTrick(fs, &option)

        let sp = Array2D.zeroCreate((f0 : _ []).Length)(option.FftSize / 2 + 1)
        let! spFramePtrs = sp
        CheapTrick(x, x.Length, fs, tpos, f0, f0.Length, &option, spFramePtrs)
        return sp }

    static member GetFftSize(fs,
        [<Optional; DefaultParameterValue(DefaultF0Floor)>] f0Floor) =
        let mutable option = CheapTrickOption()
        InitializeCheapTrickOption(fs, &option)
        option.F0Floor <- f0Floor
        GetFFTSizeForCheapTrick(fs, &option)

    static member GetF0FloorForCheapTrick(fs, fftSize) =
        GetF0FloorForCheapTrick(fs, fftSize)

    static member D4C(x, fs, tpos, f0,
        [<Optional; DefaultParameterValue(0.85)>] threshold,
        [<Optional; DefaultParameterValue(0)>]    fftSize) = array2dIntptrConv {
        let mutable option = D4COption()
        InitializeD4COption &option
        option.Threshold <- threshold
        let fftSize = if fftSize > 0 then fftSize else World.GetFftSize fs

        let ap = Array2D.zeroCreate((f0 : _ []).Length)(fftSize / 2 + 1)
        let! apFramePtrs = ap
        D4C(x, x.Length, fs, tpos, f0, f0.Length, fftSize, &option, apFramePtrs)
        return ap }

    static member GetNumberOfAperiodicities fs =
        GetNumberOfAperiodicities fs

    static member CodeAperiodicity(ap, fs) = array2dIntptrConv {
        let fftSize = ((ap : _ [,]).GetLength 1 - 1) * 2
        let numCodedAp = GetNumberOfAperiodicities fs

        let bap = Array2D.zeroCreate(ap.GetLength 0)(numCodedAp)
        let! apFramePtrs = ap
        let! bapFramePtrs = bap
        CodeAperiodicity(apFramePtrs, ap.GetLength 0, fs, fftSize, bapFramePtrs)
        return bap }

    static member DecodeAperiodicity(bap, fs, fftSize) = array2dIntptrConv {
        let ap = Array2D.zeroCreate((bap : _ [,]).GetLength 0)(fftSize / 2 + 1)
        let! bapFramePtrs = bap
        let! apFramePtrs = ap
        DecodeAperiodicity(bapFramePtrs, bap.GetLength 0, fs, fftSize, apFramePtrs)
        return ap }

    static member CodeSpectralEnvelope(sp, fs, numDimensions) = array2dIntptrConv {
        let fftSize = ((sp : _ [,]).GetLength 1 - 1) * 2

        let mgc = Array2D.zeroCreate(sp.GetLength 0) numDimensions
        let! spFramePtrs = sp
        let! mgcFramePtrs = mgc
        CodeSpectralEnvelope(spFramePtrs, sp.GetLength 0, fs, fftSize, numDimensions, mgcFramePtrs)
        return mgc }

    static member DecodeSpectralEnvelope(mgc, fs, fftSize) = array2dIntptrConv {
        let sp = Array2D.zeroCreate((mgc : _ [,]).GetLength 0)(fftSize / 2 + 1)
        let! mgcFramePtrs = mgc
        let! spFramePtrs = sp
        DecodeSpectralEnvelope(mgcFramePtrs, mgc.GetLength 0, fs, fftSize, mgc.GetLength 1, spFramePtrs)
        return sp }

    static member Synthesis(f0, sp, ap, fs,
        [<Optional; DefaultParameterValue(DefaultFramePeriod)>] framePeriod) = array2dIntptrConv {
        let fftSize = ((sp : _ [,]).GetLength 1 - 1) * 2
        let framePeriod = framePeriod

        let yLength = int(float (f0 : _ []).LongLength * framePeriod * float fs / 1000.0)
        let y = Array.zeroCreate(yLength)
        let! spFramePtrs = sp
        let! apFramePtrs = ap
        Synthesis(f0, f0.Length, spFramePtrs, apFramePtrs, fftSize, framePeriod, fs, yLength, y)
        return y }


module World =
    let synthesize f0 mgc bap =
        let sp = World.DecodeSpectralEnvelope(mgc, fs, worldFftSize)
        let ap = World.DecodeAperiodicity(bap, fs, worldFftSize)
        World.Synthesis(f0, sp, ap, fs, hopSize.TotalMilliseconds)

    let synthesize32(f0 : float32 [])(mgc : float32 [,])(bap : float32 [,]) =
        synthesize(Array.map float f0)(Array2D.map float mgc)(Array2D.map float bap)
        |> Array.map float32


