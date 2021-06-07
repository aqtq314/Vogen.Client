module Vogen.Synth.Program

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO


[<EntryPoint>]
let main argv =
    printfn "Hello world from F#"

    //printfn "%A" (G2p.run "man" [| null; "chan"; "sheng"; null; "pei"; "ban"; "zheh"; null |])

    printfn "%A" <| Prosody.run "man" 100 [|
        { Ch = null; Rom = null; Notes = null; Ipa = null }
        { Ch = "来"; Rom = "lai"; Notes = ImmutableList.CreateRange [| { Pitch = 69; On = 30; Off = 60 } |]; Ipa = null }
        { Ch = null; Rom = null; Notes = null; Ipa = null }
    |]

    0


