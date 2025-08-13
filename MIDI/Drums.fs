module Drums

type Articulation =
    | Center
    | Edge
    | Rimshot
    | RimOnly
    | Sidestick
    | Muted
    | Bow
    | Bell

type Instrument =
    | Snare
    | HighTom
    | MidTom
    | LowTom
    | FloorTom
    | Kick
    | Hat
    | Ride
    | Crash1
    | Crash2

type Drum = Instrument * Articulation * float

let drum velocity articulation instrument : Drum = (instrument, articulation, velocity)


