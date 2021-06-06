module Vogen.Synth.Program

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


[<EntryPoint>]
let main argv =
    printfn "Hello world from F#"

    printfn "%A" (G2p.run "man" [| "chan"; "sheng"; "pei"; "ban"; "zheh" |])

    0


