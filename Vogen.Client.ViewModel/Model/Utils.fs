namespace Vogen.Client.Model

open Doaz.Reactive
open NAudio.Wave
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Collections.ObjectModel
open System.IO
open System.IO.Compression
open System.Linq
open System.Text


[<AutoOpen>]
module Utils =
    let hopSize = TimeSpan.FromMilliseconds 10.0
    let headSil = TimeSpan.FromSeconds 0.5
    let tailSil = TimeSpan.FromSeconds 0.5

    type Stream with
        member x.CacheAsMemoryStream() =
            let cacheStream = new MemoryStream()
            x.CopyTo cacheStream
            cacheStream.Position <- 0L
            cacheStream

module Audio =
    let [<Literal>] fs = 44100
    let [<Literal>] channels = 1
    let waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(fs, channels)

    let sampleToTime(sampleTime : int) =
        TimeSpan.FromSeconds(float sampleTime / float fs)

    let timeToSample(time : TimeSpan) =
        int(time.TotalSeconds * float fs)


