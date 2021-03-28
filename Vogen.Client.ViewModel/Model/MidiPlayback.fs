namespace Vogen.Client.Model

open Doaz.Reactive
open NAudio
open NAudio.Midi


type MidiPlaybackEngine() =
    let device =
        if MidiOut.NumberOfDevices = 0 then null else
            new MidiOut(0)

    do  if device <> null then
            device.Send(PatchChangeEvent(0L, 1, 53).GetAsShortMessage())

    member x.PlayPitch pitch =
        if device <> null then
            device.Send(NoteEvent(0L, 1, MidiCommandCode.NoteOn, pitch, 64).GetAsShortMessage())

    member x.StopPitch pitch =
        if device <> null then
            device.Send(NoteEvent(0L, 1, MidiCommandCode.NoteOff, pitch, 0).GetAsShortMessage())


