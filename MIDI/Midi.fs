module Midi

open NAudio.Midi

type Change = {
    Value: int
    Controller: int}

type Note = int
type Velocity = float
type Midi =
    | NoteOn of Note * Velocity
    | NoteOff of Note
    | Control of Change

let devices () =
    for i in 0 .. MidiOut.NumberOfDevices - 1 do
        let info = MidiOut.DeviceInfo(i)
        printfn "Device %d: %s" i info.ProductName

type Device(index, channel) =
    let out = new MidiOut(index)
    do out.Reset()

    member this.Send(midi) =
        match midi with
        | NoteOn (note, velocity) ->
            //printfn "NoteOn %A (%A)" note velocity
            out.Send(MidiMessage.StartNote(note, int (velocity * 127.0), channel).RawData)
        | NoteOff note ->
            //printfn "NoteOff %A" note
            out.Send(MidiMessage.StopNote(note, 0, channel).RawData)
        | Control change ->
            //printfn "Control %A" change
            out.Send(MidiMessage.ChangeControl(change.Controller, change.Value, channel).RawData)
