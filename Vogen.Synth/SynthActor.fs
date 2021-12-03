module Vogen.Synth.SynthActor

open System
open System.Collections.Generic
open System.Collections.Immutable


let create() =
    let tryWithResult f =
        try Ok(f())
        with ex -> Error ex

    MailboxProcessor.Start(fun inbox ->
        let rec handle() = async {
            match! inbox.Receive() with
            | Choice1Of3(reply : AsyncReplyChannel<_>, romScheme, uttDur, chars) ->
                reply.Reply(tryWithResult(fun () -> Prosody.run romScheme uttDur chars))

            | Choice2Of3(reply : AsyncReplyChannel<_>, romScheme, chars) ->
                reply.Reply(tryWithResult(fun () -> F0.run romScheme chars))

            | Choice3Of3(reply : AsyncReplyChannel<_>, romScheme, voiceLibId, f0, chars) ->
                reply.Reply(tryWithResult(fun () -> Acoustics.run romScheme voiceLibId f0 chars))

            return! handle() }
            
        handle())

let post buildMessage (synthActor : MailboxProcessor<_>) = async {
    let! synthResult = synthActor.PostAndAsyncReply buildMessage
    match synthResult with
    | Ok outChars -> return outChars
    | Error(ex : exn) -> return raise(AggregateException(ex)) }


