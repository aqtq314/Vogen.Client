module Vogen.Synth.Acoustics

open Doaz.Reactive
open Microsoft.ML.OnnxRuntime
open Microsoft.ML.OnnxRuntime.Tensors
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.IO.Compression
open System.Linq
open System.Text
open Vogen.Synth.TimeTable

#nowarn "40"


type VoiceLibMetadata = {
    [<JsonProperty("name",    Required=Required.DisallowNull)>] Name : string
    [<JsonProperty("id",      Required=Required.DisallowNull)>] Id : string
    [<JsonProperty("version", Required=Required.DisallowNull)>] Version : string
    [<JsonProperty("by",      Required=Required.DisallowNull)>] By : string }

let voiceLibs =
    Directory.GetFiles(".", "*.voglib")
    |> Array.choose(fun voiceLibPath ->
        try use zipArchive = ZipFile.OpenRead voiceLibPath
            use metaStream = (zipArchive.GetEntry "meta.json").Open()
            use metaReader = new StreamReader(metaStream, Encoding.UTF8)
            let meta = JsonConvert.DeserializeObject<VoiceLibMetadata>(metaReader.ReadToEnd())
            let model = Lazy<_>.Create <| fun () ->
                use zipArchive = ZipFile.OpenRead voiceLibPath
                use onnxStream = (zipArchive.GetEntry "model.onnx").Open()
                InferenceSession.ofStream onnxStream
            Some {| Path = voiceLibPath; Meta = meta; Model = model |}
        with ex ->
            eprintfn "Error loading voicelib %s" voiceLibPath
            eprintfn "%s" ex.Message
            None)
    |> Array.groupBy(fun voiceLib -> voiceLib.Meta.Id)
    |> Array.map(fun (voiceLibId, voiceLibs) ->
        voiceLibId, voiceLibs |> Array.maxBy(fun voiceLib -> voiceLib.Meta.Version))
    |> Dict.ofSeq

let run romScheme voiceLibId (f0 : float32 [])(chars : IReadOnlyList<TimeTable.TChar>) =
    let phs = chars |> Seq.collect(fun ch -> ch.Ipa) |> Array.ofSeq

    let phDurs = phs |> Array.map(fun ph -> int64(ph.Off - ph.On))
    let phSyms = phs |> Array.map(fun ph ->
        if isNull ph.Ph then ""
        elif ph.Ph.Contains ':' then ph.Ph
        else $"{romScheme}:{ph.Ph}")

    let breAmp : float32 [] = Array.zeroCreate f0.Length

    let xs = [|
        NamedOnnxValue.CreateFromTensor("phDurs", phDurs.ToTensor().Reshape(ReadOnlySpan([| 1; phDurs.Length |])))
        NamedOnnxValue.CreateFromTensor("phs", phSyms.ToTensor().Reshape(ReadOnlySpan([| 1; phSyms.Length |])))
        NamedOnnxValue.CreateFromTensor("f0", f0.ToTensor().Reshape(ReadOnlySpan([| 1; f0.Length |])))
        NamedOnnxValue.CreateFromTensor("breAmp", breAmp.ToTensor().Reshape(ReadOnlySpan([| 1; breAmp.Length |]))) |]

    // run model
    let model = voiceLibs.[voiceLibId].Model.Value
    use ys = model.Run xs
    let ys = ys.ToArray()
    let mgc = Array2D.ofDenseTensor(ys.[0].Value :?> DenseTensor<float32>)
    let bap = Array2D.ofDenseTensor(ys.[1].Value :?> DenseTensor<float32>)

    mgc, bap


