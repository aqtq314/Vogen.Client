namespace Vogen.Client.ViewModel

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
open System.Reflection
open System.Text
open System.Text.RegularExpressions
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

    type JsonConvert with
        static member SerializeObjectFormatted value =
            let jStr =
                use stringWriter = new StringWriter()
                use jWriter = new JsonTextWriter(stringWriter, Indentation = 2, Formatting = Formatting.Indented)
                let jSerializer = JsonSerializer.CreateDefault()
                jSerializer.Serialize(jWriter, value)
                stringWriter.ToString()
            Regex.Replace(jStr, @"(?<![\}\]],)(?<!\[)\r\n *(?!.+[\[\{])", " ")

module Audio =
    let playbackWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(fs, channels)

    let sampleToTime(sampleTime : int) =
        TimeSpan.FromSeconds(float sampleTime / float fs)

    let timeToSample(time : TimeSpan) =
        int(time.TotalSeconds * float fs)

    let inline pulseToSample bpm0 pulses =
        pulses |> Midi.toTimeSpan bpm0 |> timeToSample

    let inline sampleToPulse bpm0 sampleTime =
        sampleTime |> sampleToTime |> Midi.ofTimeSpan bpm0


