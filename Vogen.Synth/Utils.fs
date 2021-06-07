namespace Vogen.Synth

open System


[<AutoOpen>]
module Params =
    let hopSize = TimeSpan.FromMilliseconds 10.0
    let headSil = TimeSpan.FromSeconds 0.5
    let tailSil = TimeSpan.FromSeconds 0.5


