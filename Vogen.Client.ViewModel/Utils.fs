namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.IO.Compression
open System.Linq
open System.Reflection
open System.Text
open Vogen.Synth


[<AutoOpen>]
module Utils =
    let appDir =
        let entryAssembly = Assembly.GetEntryAssembly()
        Path.GetDirectoryName entryAssembly.Location

    type ImmutableDictionary<'k, 'v> with
        member x.GetOrDefault defaultValue key =
            match x.TryGetValue key with
            | true, value -> value
            | false, _ -> defaultValue

    type Stream with
        member x.CacheAsMemoryStream() =
            let cacheStream = new MemoryStream()
            x.CopyTo cacheStream
            cacheStream.Position <- 0L
            cacheStream


