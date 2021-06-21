namespace Vogen.Synth

open Microsoft.ML.OnnxRuntime
open System
open System.IO
open System.Reflection


[<AutoOpen>]
module Params =
    let hopSize = TimeSpan.FromMilliseconds 10.0
    let headSil = TimeSpan.FromSeconds 0.5
    let tailSil = TimeSpan.FromSeconds 0.5

module InferenceSession =
    let ofEmbedded uri =
        use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream uri
        use reader = new BinaryReader(stream)
        let modelBytes = reader.ReadBytes(int stream.Length)
        new InferenceSession(modelBytes)


