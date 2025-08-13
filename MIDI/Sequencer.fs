module Sequencer

open System
open System.Threading
open System.Threading.Tasks

type Sequence = ((unit -> unit) list * TimeSpan) seq

let play (cancel: CancellationToken) (sequence: Sequence) =
    let start = DateTime.UtcNow
    let rec play' sequence =
        let rec next (offset: TimeSpan) sequence =
            task {
                let delay = (start + offset) - DateTime.UtcNow
                if delay > TimeSpan.Zero then
                    printfn "Waiting for %A %A" offset delay
                    do! Task.Delay(delay, cancel)
                if not cancel.IsCancellationRequested then
                    play' sequence
            } |> ignore
        if not cancel.IsCancellationRequested then
            if not (Seq.isEmpty sequence) then
                let (actions, offset) = Seq.head sequence
                List.iter (fun f -> f()) actions
                next offset (Seq.tail sequence)
    sequence |> play'
