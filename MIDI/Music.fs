module Music

open System

let gcd (a: bigint) (b: bigint) =
    let rec g a b = if b = 0I then abs a else g b (a % b)
    g (abs a) (abs b)

let lcm x y = if x = 0I || y = 0I then 0I else abs (x / (gcd x y) * y)

type Beat(n: bigint, d: bigint) =

    let normalize (n: bigint) (d: bigint) =
        if d = 0I then invalidArg "d" "Denominator cannot be zero."
        let sign = if d < 0I then -1I else 1I
        let n' = n * sign
        let d' = abs d
        let g = gcd n' d'
        (n' / g, d' / g)

    let num, den = normalize n d

    member _.Numerator = num
    member _.Denominator = den

    override _.Equals(obj) =
        match obj with
        | :? Beat as b -> num = b.Numerator && den = b.Denominator
        | _ -> false

    override _.GetHashCode() =
        hash (num, den)

    interface IComparable with
        member _.CompareTo(obj) =
            match obj with
            | :? Beat as b ->
                compare (num * b.Denominator) (b.Numerator * den) // compare cross-multiplied values
            | _ -> invalidArg "obj" "Cannot compare with non-Beat"

    static member (+) (x: Beat, y: Beat) =
        Beat(x.Numerator * y.Denominator + y.Numerator * x.Denominator,
             x.Denominator * y.Denominator)

    static member (-) (x: Beat, y: Beat) =
        Beat(x.Numerator * y.Denominator - y.Numerator * x.Denominator,
             x.Denominator * y.Denominator)

    static member (*) (x: Beat, y: Beat) =
        Beat(x.Numerator * y.Numerator,
             x.Denominator * y.Denominator)

    static member (*) (x: Beat, y: int) =
        Beat(x.Numerator * bigint y, x.Denominator)

    static member (/) (x: Beat, y: Beat) =
        if y.Numerator = 0I then invalidArg "b" "Division by zero."
        Beat(x.Numerator * y.Denominator,
             x.Denominator * y.Numerator)

    member this.ToFloat() = float this.Numerator / float this.Denominator

    override this.ToString() = sprintf "%A/%A" this.Numerator this.Denominator

type Note<'a> = 'a
type Notes<'a> = (Note<'a> list * Beat)
type Music<'a> = Notes<'a> list

let commonBeat sequence =
    Beat(1, sequence |> Seq.map (fun (_, i: Beat) -> i.Denominator) |> Seq.reduce lcm)

let single<'a> note (beat: Beat) : Music<'a> = [([note], beat)]

let repeated<'a> times note (start: Beat) (interval: Beat) : Music<'a> =
    List.init times (fun n -> let beat = start + interval * n in [note], beat)

let combine<'a> (m: Music<'a>) (n: Music<'a>) : Music<'a> =
    let combine' (m': Music<'a>, n': Music<'a>) =
        match m', n' with
        | (mh :: mt), (nh :: nt) ->
            let mb = snd mh
            let nb = snd nh
            if mb < nb then Some (mh, (mt, n'))
            elif nb < mb then Some (nh, (m', nt))
            else Some ((fst mh @ fst nh, mb), (mt, nt))
        | _, [] | [], _ -> None
    List.unfold combine' (m, n)
