module Vogen.Synth.Program

open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq


[<EntryPoint>]
let main argv =
    printfn "Hello world from F#"

    //printfn "%A" (G2p.run "yue-wz" [| null; "woi"; "loi"; null; "soeng"; "zoek"; "seot"; null |])

    let tChars = Prosody.run "man" 100 [|
        { Ch = null; Rom = null; Notes = null; Ipa = null }
        { Ch = "来"; Rom = "lai"; Notes = ImmutableList.CreateRange [| { Pitch = 69; On = 30; Off = 60 } |]; Ipa = null }
        { Ch = null; Rom = null; Notes = null; Ipa = null }
    |]

    printfn "%A" tChars

    printfn "%A" <| F0.run "man" tChars

    0


