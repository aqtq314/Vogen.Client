module Vogen.Synth.Program

open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open NAudio.Wave
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq


[<EntryPoint>]
let main argv =
    printfn "%A" Acoustics.voiceLibs

    //printfn "%A" (G2p.run "yue-wz" [| null; "woi"; "loi"; null; "soeng"; "zoek"; "seot"; null |])

    let romScheme = "man"
    let tChars = Prosody.run romScheme 288 [|
        { Ch = null; Rom = null; Ipa = null; Notes = null }
        { Ch = "梨"; Rom = "li"; Ipa = null; Notes = ImmutableList.CreateRange [|
            { Pitch = 65; On = 50; Off = 97 }
        |] }
        { Ch = "花"; Rom = "huar"; Ipa = null; Notes = ImmutableList.CreateRange [|
            { Pitch = 65; On = 97; Off = 120 }
            { Pitch = 67; On = 120; Off = 144 }
        |] }
        { Ch = "香"; Rom = "xiang"; Ipa = null; Notes = ImmutableList.CreateRange [|
            { Pitch = 65; On = 144; Off = 238 }
        |] }
        { Ch = null; Rom = null; Ipa = null; Notes = null }
    |]

    //printfn "%A" tChars

    let f0 = F0.run romScheme tChars

    //printfn "%A" f0

    let mgc, bap = Acoustics.run romScheme (Seq.head Acoustics.voiceLibs.Keys) f0 tChars

    //printfn "%A" mgc
    //printfn "%A" bap

    let y = World.synthesize32 f0 mgc bap
    do  use waveWriter = new WaveFileWriter(Environment.ExpandEnvironmentVariables @"%USERPROFILE%\Desktop\test.wav", WaveFormat(44100, 1))
        waveWriter.WriteSamples(y, 0, y.Length)

    //let cis = Rfft.run 1024 y [| 14336 .. 512 .. 16384 |]
    //printfn "%A" cis

    printfn "Done"
    let _ = Console.ReadKey true
    0


