module Vogen.Synth.SynthActor

open System


let create() =
    MailboxProcessor.Start(fun inbox ->
        let rec handle() = async {
            match! inbox.Receive() with
            | (romScheme, uttDur, chars), (reply : AsyncReplyChannel<_>) ->
                try let outChars = Prosody.run romScheme uttDur chars
                    reply.Reply(Ok outChars)
                with | ex ->
                    reply.Reply(Error ex)
            return! handle() }
            
        handle())


