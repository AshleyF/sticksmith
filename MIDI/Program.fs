open System
open System.Threading
open System.IO
open NAudio.Midi
open System.IO.Ports
open Midi
open Drums

let beat (b: int) = Music.Beat(bigint b, 4) // 4:4 time

let kick = Music.repeated 1000 (drum 1.0 Center Kick) (beat 0) (beat 2)
let snare = Music.repeated 1000 (drum 0.5 Center Snare) (beat 1) (beat 2)
let hat = Music.repeated 2000 (drum 0.5 Edge Hat) (beat 0) (Music.Beat(1, 8))

let money =
    kick
    |> Music.combine<Drum> snare
    |> Music.combine<Drum> hat

let ghost =
    Music.repeated 1000 (drum 0.1 Center Snare) (Music.Beat(3, 8)) (beat 2)
    |> Music.combine<Drum> money

let extraKick =
    Music.repeated 1000 (drum 1.0 Center Kick) (Music.Beat(5, 8)) (beat 4)
    |> Music.combine<Drum> ghost

money |> Seq.take 10 |> List.ofSeq |> printfn "Money: %A"
money |> Seq.take 10 |> Music.commonBeat |> printfn "Common Beat: %A"

let cancellation = new CancellationTokenSource()
let device = new Midi.Device(1, 9)

let noteToMidiAction (note: Drum) () =
    let hit note v =
        device.Send(NoteOn (note, v))
        device.Send(NoteOff note)
    let openHat () = device.Send(Control { Value = 127; Controller = 4 })
    let closeHat () = device.Send(Control { Value = 0; Controller = 4 })
    match note with
    | Kick, Center, v -> hit 36 v
    | Snare, Center, v -> hit 38 v
    | Snare, Edge, v -> hit 33 v
    | HighTom, Center, v -> hit 48 v
    | MidTom, Center, v -> hit 47 v
    | LowTom, Center, v -> hit 45 v
    | FloorTom, Center, v -> hit 43 v
    | Hat, Edge, v -> hit 42 v
    | OpenHat, Edge, v -> openHat (); hit 42 v; closeHat ()
    | Crash1, Bow, v -> hit 49 v
    //printfn "Send %A %A" note DateTime.Now

let beatToTimeSpan (bmp: int) (beat: Music.Beat) =
    let ticksPerBeat = TimeSpan.FromMinutes(1.0).Ticks / int64 bmp
    TimeSpan.FromTicks(int64 (float ticksPerBeat * beat.ToFloat()))

let blackHoleSun =
    Music.combineAll
        [
            //1 2 3 4 1 2 3 4 1 2 3 4 1 2 3 4 1 2 3 4 1 2 3 4 1 2 3 4 1 2 3 4 
            ("*_______________________________________________________*_______" |> Music.score (drum 1.0 Bow Crash1) (Music.Beat(1, 8)))
            ("________*_______*_______*_______*_______*_______*_______________" |> Music.score (drum 1.0 Edge OpenHat) (Music.Beat(1, 8)))
            ("__******__******__******__******__******__******__******__*_*___" |> Music.score (drum 1.0 Edge Hat) (Music.Beat(1, 8)))
            ("____*_______*_______*_______*_______*_______*_______*___________" |> Music.score (drum 1.0 Center Snare) (Music.Beat(1, 8)))
            ("*_______*_______*_______*_*__*_**_______*____*_**__*_*_**____*__" |> Music.score (drum 1.0 Center Kick) (Music.Beat(1, 8)))
            ("______________________________________________________________*_" |> Music.score (drum 1.0 Center LowTom) (Music.Beat(1, 8)))
            ("______________________________________________________________*_" |> Music.score (drum 1.0 Center FloorTom) (Music.Beat(1, 8)))
        ]

//money
//ghost
//extraKick
blackHoleSun
//|> List.iter (printfn "WTF %A")
|> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat)
|> Sequencer.play cancellation.Token

Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token
Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token
Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token
Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token
Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token
Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token
Console.ReadLine() |> ignore
blackHoleSun |> Seq.map (fun (notes, beat) -> List.map noteToMidiAction notes, beatToTimeSpan 30 beat) |> Sequencer.play cancellation.Token

Console.ReadLine() |> ignore
cancellation.Cancel()
printfn "Press Enter again to really exit"
Console.ReadLine() |> ignore

[
    ([NoteOn (1, 1.0)], TimeSpan.FromSeconds(1.0))
    ([NoteOn (2, 1.0)], TimeSpan.FromSeconds(1.0))
    ([NoteOn (3, 1.0)], TimeSpan.FromSeconds(1.0))
    ([NoteOff 3], TimeSpan.FromSeconds(1.0))
    ([], TimeSpan.FromSeconds(3.0))
    ([Control { Value = 9; Controller = 7 }], TimeSpan.FromSeconds(1.0))
]
|> Seq.map (fun (ms, t) -> List.map (fun m -> fun () -> printfn "Send %A" m) ms, t)
//|> Seq.map (fun (ms, t) -> List.map (fun m -> fun () -> device.Send(m)) ms, t)
|> Sequencer.play cancellation.Token

type Velocity = double
type Articulation =
    | Hit
    | Center
    | OffCenter
    | CenterAlt
    | Edge
    | Rimshot
    | RimOnly
    | Sidestick
    | Muted
    | MutedAccent
    | Flam // ?
    | ClosedRoll
    | ForwardSwirl
    | BackwardSwirl
    | ForwardBrushTrigger
    | BackwardBrushTrigger
    | MutedAlt
    | Brushed
    | Bow
    | BowTip
    | BowShank
    | Bell
    | BellTip
    | BellShank
    | MutedHit
    | MutedTail
    | Crash
    | Crescendo // ?
    | FX
type HiHatArticulation =
    | SequencedHits
    | ClosedEdge
    | ClosedTip
    | TightEdge
    | TightTip
    | TightBell
    | ClosedBell
    | SeqHard
    | SeqSoft
    | OpenEdge0
    | OpenEdge1
    | OpenEdge2
    | OpenEdge3
    | OpenEdge4
    | OpenEdge5
    | OpenTip0
    | OpenTip1
    | OpenTip2
    | OpenTip3
    | OpenTip4
    | OpenTip5
    | OpenBell0
    | OpenBell1
    | OpenBell2
    | OpenBell3
    | OpenBell4
    | OpenBell5
    | ClosedShaft
    | TightShaft
    | OpenShaft0
    | OpenShaft1
    | OpenShaft2
    | OpenShaft3
    | OpenShaft4
    | OpenShaft5
    | Open1
    | Open2
    | Open3
    | Open4
    | OpenBell
    | OpenPedal
    | ClosedPedal
    | EdgeTrigger
    | TipTrigger
    | BellTrigger
    | ShaftTrigger
    | HatsTrig
    | HatsTipTrig
    | HatsBellTrig
    | HatsCtrl
type InstrumentType =
    | Cymbal
    | FX
    | HiHat
    | Kick
    | Percussion
    | Snare
    | Toms
type Instrument =
    | China
    | Claps
    | Cowbell
    | Crash
    | FloorTom
    | RackTom
    | Ride
    | Shakers
    | Snap
    | Splash
    | Spock
    | Stomp
    | Tambourine
    | Woodblock
    | Snare  of Articulation * Velocity
    | Tom    of Velocity
    | Floor  of Velocity
    | Hat    of Velocity
    | Cymbal of Velocity
    | Kick   of Velocity
type Tool =
    | Sticks
    | Brushes
    | Hands
    | Rods
    | Pedal
    | Mallets
type MIDI = { On: MidiMessage; Off: MidiMessage }

let elementToMidi channel element =
    let midi note velocity =
        { On  = MidiMessage.StartNote(note, int (velocity * 127.0), channel)
          Off = MidiMessage.StopNote(note, 0, channel) }
    match element with
    | Snare (Center, v) -> midi 38 v
    | Snare (Edge,   v) -> midi 33 v
    | Tom            v  -> midi 45 v
    | Floor          v  -> midi 43 v
    | Hat            v  -> midi 42 v
    | Cymbal         v  -> midi 49 v
    | Kick           v  -> midi 36 v

let drumMachine (delay, sequence) =
    for i in 0 .. MidiOut.NumberOfDevices - 1 do
        let info = MidiOut.DeviceInfo(i)
        printfn "Device %d: %s" i info.ProductName
    let deviceIndex = 1
    use midiOut = new MidiOut(deviceIndex)
    midiOut.Reset()
    let send (message: MidiMessage) = midiOut.Send(message.RawData)

    let channel = 9
    let toMidi = elementToMidi channel

    let playElements elements =
        for element in elements do
            let midi = toMidi element
            send midi.On
            send midi.Off
        Thread.Sleep(int delay)

    Seq.iter playElements sequence

let repeat count elements =
    Seq.replicate count elements |> Seq.concat
    
(*
let loadSample (file: string) =
    use reader = new WaveFileReader(file)
    let memoryStream = new MemoryStream()
    let buffer = Array.zeroCreate<byte> 6000
    let mutable bytesRead = reader.Read(buffer, 0, buffer.Length)
    while bytesRead > 0 do
        memoryStream.Write(buffer, 0, bytesRead)
        bytesRead <- reader.Read(buffer, 0, buffer.Length)
    memoryStream.Position <- 0L
    new RawSourceWaveStream(memoryStream, reader.WaveFormat)

let playSample (reader: RawSourceWaveStream) volume =
    let waveOut = new WaveOutEvent()
    reader.Position <- 0
    waveOut.Init(reader)
    waveOut.Play()
    waveOut.Volume <- volume
    async {
        while waveOut.PlaybackState = PlaybackState.Playing do
            do! Async.Sleep(100)
        waveOut.Dispose()
    } |> Async.Start

let snare = loadSample @"..\..\..\samples\rapsnaresX_01-short.wav"
let tom = loadSample @"..\..\..\samples\SnickR_08-regular.wav"
let hat = loadSample @"..\..\..\samples\ClosedHatsX_130-regular.wav"
let cymbal = loadSample @"..\..\..\samples\CymbalX_01-regular.wav"
let kick = loadSample @"..\..\..\samples\vkick_07-regular.wav"

let midiMessageReceived (args: MidiInMessageEventArgs) =
    printfn "%A" args.MidiEvent
    match args.MidiEvent with
    | :? NoteOnEvent as noteOn ->
        let position, color, sample = 
            match noteOn.NoteNumber with
            | 49 -> 10, ConsoleColor.Red,    hat
            | 48 -> 15, ConsoleColor.Green,  snare
            | 45 -> 20, ConsoleColor.Blue,   tom
            | 51 -> 25, ConsoleColor.Yellow, cymbal
            | _  ->  0, ConsoleColor.Gray,   snare
        let volume = (single noteOn.Velocity) / 127f
        //playSample sample volume
        //Console.SetCursorPosition(position, Console.WindowHeight - 2)
        let originalColor = Console.ForegroundColor
        //Console.ForegroundColor <- color
        //Console.Write('■')
        Console.ForegroundColor <- originalColor
    | _ -> ()

let onTimerElapsed (_: obj) (args: ElapsedEventArgs) =
    Console.SetCursorPosition(0, Console.WindowHeight - 1)
    Console.WriteLine(String.replicate Console.WindowWidth " ")

let startMetronome intervalMilliseconds =
    let timer = new Timer(float intervalMilliseconds)
    timer.Elapsed.AddHandler(onTimerElapsed)
    timer.AutoReset <- true // Fires repeatedly
    timer.Enabled <- true
    timer.Start()

let listMidiDevices () =
    if MidiIn.NumberOfDevices = 0 then
        printfn "No MIDI input devices found."
    else
        printfn "Available MIDI input devices:"
        for i in 0 .. MidiIn.NumberOfDevices - 1 do
            let info = MidiIn.DeviceInfo(i)
            printfn "%d: %s %s" i (info.Manufacturer.ToString()) (info.ProductName)
    if MidiIn.NumberOfDevices = 1 then 0 else -1

let getInputDevice () =
    let autoChosen = listMidiDevices()
    if autoChosen >= 0 then autoChosen else
        printf "Select a MIDI device index: "
        match Int32.TryParse(Console.ReadLine()) with
        | true, i when i >= 0 && i < MidiIn.NumberOfDevices -> i
        | _ -> failwith "Invalid device index."

let readMidiInput () =
    let deviceIndex = getInputDevice ()
    use midiIn = new MidiIn(deviceIndex)
    midiIn.MessageReceived.Add midiMessageReceived
    midiIn.Start()
    printfn "Listening for MIDI messages. Press Enter to exit..."

    while true do
        Console.ReadLine() |> ignore
        printfn "--------------------------------------------------"

let writeMidiOutput () =
    for i in 0 .. MidiOut.NumberOfDevices - 1 do
        let info = MidiOut.DeviceInfo(i)
        printfn "Device %d: %s" i info.ProductName
    let deviceIndex = 1
    use midiOut = new MidiOut(deviceIndex)
    midiOut.Reset()

    let note = 38        // Snare (D1)
    let channel = 9      // MIDI channel 10 (zero-based index)

    let noteOff = MidiMessage.StopNote(note, 0, channel)

    use port = new SerialPort("COM5", 115200, Parity.None, 8, StopBits.One)
    port.DtrEnable <- true
    port.RtsEnable <- true
    port.DataReceived.Add(fun _ ->
        while port.BytesToRead > 0 do
            let b = port.ReadByte();
            let velocity = b >>> 1;
            let noteOn = MidiMessage.StartNote(note, velocity, channel)
            midiOut.Send(noteOn.RawData)
            midiOut.Send(noteOff.RawData)
            printfn "Velocity: %i (%i)" velocity b)
    port.Open()

    Console.ReadLine() |> ignore
*)
let pullData () =
    use port = new SerialPort("COM5", 115200, Parity.None, 8, StopBits.One)
    port.DtrEnable <- true
    port.RtsEnable <- true
    use file = File.OpenWrite("drum.bin")
    //port.DataReceived.Add(fun _ ->
    //    while port.BytesToRead > 0 do
    //        let b = port.ReadByte();
    //        printf "%i " b
    //        file.WriteByte(b |> byte))
    port.Open()
    printfn "Pulling data..."
    let rec readHeader () =
        let b = port.ReadByte ()
        if b <> 255 then readHeader ()
    let mutable flag = true
    let mutable last = DateTime.Now
    Console.BufferWidth <- 256
    while not Console.KeyAvailable do
        readHeader () |> ignore
        let a = port.ReadByte();
        let b = port.ReadByte();
        let c = port.ReadByte();
        //if a > 20 || b > 20 || c > 20 then
            //let now = DateTime.Now
            //if flag && now - last > TimeSpan.FromMilliseconds(2000) then
            //    Console.Clear()
            //    flag <- false
            //    last <- now
        Console.WriteLine()
        Console.CursorLeft <- a
        Console.ForegroundColor <- ConsoleColor.Blue
        Console.Write('*')
        Console.CursorLeft <- b
        Console.ForegroundColor <- ConsoleColor.Green
        Console.Write('*')
        Console.CursorLeft <- c
        Console.ForegroundColor <- ConsoleColor.Red
        Console.Write('*')
            //printfn "%A %i %i %i" now a b c
        //else flag <- true
        //file.WriteByte(b |> byte)
    printf "Closing..."
    port.Close()
    port.Dispose()
    file.Close()
    file.Dispose()
    printfn "Done"

    (*
    for _ in 0 .. 100000 do
        for p in 0 .. 10 .. 127 do
            let cc = MidiMessage(0xB0 + channel, 16, p)
            midiOut.Send(cc.RawData)
            midiOut.Send(noteOn.RawData)
            Thread.Sleep(100) // Hold briefly
            midiOut.Send(noteOff.RawData)
            printfn "Snare %i" p
            Thread.Sleep(400)
    *)



//startMetronome 100
//readMidiInput ()
//writeMidiOutput ()
//pullData ()
//let hat = Hat 0.5
let hatAccent = Hat 1.0
//let kick = Kick 0.75
let snareCenter = Snare (Center, 0.6)
let snareEdge = Snare (Edge, 0.6)
let snareAccent = Snare (Center, 1.0)
let snareEdgeAccent = Snare (Edge, 1.0)
let snareCenterGhost = Snare (Center, 0.2)
let snareEdgeGhost = Snare (Edge, 0.2)
let cymbal = Cymbal 0.5
let tom = Tom 0.5
let floor = Floor 0.5

let notationToElement = function
        | 'H' -> [hatAccent]
        //| 'h' -> [hat]
        //| 'k' -> [kick]
        | 'S' -> [snareAccent]
        | 's' -> [snareCenter]
        | 'e' -> [snareEdge]
        | 'E' -> [snareEdgeAccent]
        | 'g' -> [snareCenterGhost]
        | 'b' -> [snareEdgeGhost]
        | 't' -> [tom]
        | 'f' -> [floor]
        | '.' -> []
        | c   -> failwith $"Invalid notation character: '{c}"

let makeBeat lineA lineB lineC =
    Seq.zip3 lineA lineB lineC
    |> Seq.map (fun (a, b, c) -> [a; b; c] |> Seq.map notationToElement |> Seq.concat)

let moneyBeat =
    100,
    makeBeat
        "H...h...H...h...H...h...H...h..."
        "........s...............s......."
        "k...............k..............."
    |> repeat 1000

let funBeat =
    100,
    makeBeat
        "h...h...h...h...h...h...h...h..."
        "........s.g...g.........s.g...g."
        "k...............k...k..........."
    |> repeat 1000

let paradiddle =
    200,
    makeBeat
        "h.hh.h..h.h....."
        ".g..s.gg....t.ss"
        "k.......k.k..f.."
    |> repeat 1000      

let simpleParadiddle =
    200,
    makeBeat
        "................"
        "E.g.b.b.S.b.g.g."
        "................"
    |> repeat 1000      

//moneyBeat
//funBeat
//paradiddle
//simpleParadiddle
//|> drumMachine

Console.ReadLine() |> ignore
cancellation.Cancel()
printfn "Press Enter again to really exit"
Console.ReadLine() |> ignore

