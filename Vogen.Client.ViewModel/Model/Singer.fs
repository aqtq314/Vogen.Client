namespace Vogen.Client.Model

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Text
open System.Text.RegularExpressions


module Singer =
    let all = [|
        "gloria"
        "wonder"
        "mei"
        "chao"
        "rgb"
        "aquachord"
        "kiritan"
        "kurumi" |]

    let defaultId = Seq.head all

