namespace Doaz.Reactive.Math

open System
open System.Collections.Generic


[<AutoOpen>]
module Math =
    let [<Literal>] pi = Math.PI

    let [<Literal>] eps = 1e-8

    let inline lerp (a : ^a) (b : ^a) amount : ^a =
        amount * (b - a) + a

    let inline unlerp (a : ^a) (b : ^a) (value : ^a) =
        (value - a) / (b - a)

    let inline intr x = int(round x)

    let inline half x = x / 2.0

    let inline sqr v = v * v

    let inline cube v = v * v * v

    let inline inv v = 1.0 / v

    let [<NoDynamicInvocation>] inline (/%) dividend divisor =
        dividend / divisor, dividend % divisor

    let inline clamp minValue maxValue value = value |> min maxValue |> max minValue

    let inline between minValue maxValue value = value >= minValue && value < maxValue

    let inline invlerp (a : ^a, b : ^a, c : ^a) amount : ^a =
        let one = LanguagePrimitives.GenericOne
        let y = b |> unlerp a c
        let k = sqrt ((one - y) / y)
        let result = amount / (k * k * (one - amount) + amount)
        result |> lerp a c

    let inline lerp2 (w1 : ^b, f1 : ^b -> ^a) (w2 : ^b, f2 : ^b -> ^a) (amount : ^b) : ^a =
        let sep = w1 / (w1 + w2)
        if amount < sep then
            f1(amount / sep)
        else
            f2((amount - sep) / (LanguagePrimitives.GenericOne - sep))

    let inline lerp3 (w1, f1) (w2, f2) (w3, f3) amount =
        amount |> lerp2
            (w1, f1)
            (w2 + w3, lerp2 (w2, f2) (w3, f3))

    let inline toDeg angle = angle * 180.0 / pi

    let inline angleOfDeg angleDeg = angleDeg * pi / 180.0

    let inline roundTo unitValue (value : float) =
        round (value / unitValue) * unitValue

    let inline floorTo unitValue (value : float) =
        floor (value / unitValue) * unitValue

    let inline ceilTo unitValue (value : float) =
        ceil (value / unitValue) * unitValue

    let normalizeAngle value =
        if Double.IsNaN value || Double.IsInfinity value then
            0.0
        else
            let n = value % (pi * 2.0)
            if n <= -pi then
                n + (pi * 2.0)
            elif n > pi then
                n - (pi * 2.0)
            else n

    type [<RequireQualifiedAccess>] Quadrant =
        | Q1 | Q2 | Q3 | Q4 | XPos | XNeg | YPos | YNeg

        static member ofAngle angle =
            let angle = normalizeAngle angle
            if   angle < -pi / 2.0 then Quadrant.Q3
            elif angle = -pi / 2.0 then Quadrant.YNeg
            elif angle < 0.0       then Quadrant.Q4
            elif angle = 0.0       then Quadrant.XPos
            elif angle < pi / 2.0  then Quadrant.Q1
            elif angle = pi / 2.0  then Quadrant.YPos
            elif angle < pi        then Quadrant.Q2
            else                        Quadrant.XNeg


[<Struct>]
type VectorF =
    val X : float
    val Y : float
    new (x, y) = { X = x; Y = y }

    override x.ToString () =
        sprintf "<%A, %A>" x.X x.Y

    member x.Magnitude =
        sqrt (sqr x.X + sqr x.Y)

    member x.MagnitudeSquared =
        sqr x.X + sqr x.Y

    member x.Angle =
        atan2 x.Y x.X

    member x.AngleInDegrees =
        x.Angle * 180.0 / pi

    member x.Scale (scaleX, scaleY) =
        VectorF (x.X * scaleX, x.Y * scaleY)

    member x.Rotate angle =
        let cosang = cos angle
        let sinang = sin angle
        VectorF (
            x.X * cosang - x.Y * sinang,
            x.X * sinang + x.Y * cosang)

    member x.Rotate90 () =
        VectorF (-x.Y, x.X)

    member x.RotateNeg90 () =
        VectorF (x.Y, -x.X)

    member x.Multiply k =
        VectorF (x.X * k, x.Y * k)

    member x.Divide k =
        VectorF (x.X / k, x.Y / k)

    member x.Normalized =
        let mag = x.Magnitude
        VectorF (x.X / mag, x.Y / mag)

    static member (+) (u : VectorF, v : VectorF) =
        VectorF (u.X + v.X, u.Y + v.Y)

    static member (-) (u : VectorF, v : VectorF) =
        VectorF (u.X - v.X, u.Y - v.Y)

    static member (~-) (u : VectorF) =
        VectorF (-u.X, -u.Y)

    static member (*) (u : VectorF, k) =
        u.Multiply k

    static member (*) (k, u : VectorF) =
        u.Multiply k

    static member (/) (u : VectorF, k) =
        u.Divide k

    static member ( *. ) (u : VectorF, v : VectorF) =
        u.X * v.X + u.Y * v.Y

    static member ( *! ) (u : VectorF, v : VectorF) =
        u.X * v.Y - u.Y * v.X

    static member (=~) (u : VectorF, v : VectorF) =
        let d = u - v in d.MagnitudeSquared < sqr eps

    static member (<>~) (u : VectorF, v : VectorF) =
        let d = u - v in d.MagnitudeSquared >= sqr eps

    static member Zero = VectorF (0.0, 0.0)

    static member ofPolar (angle, mag) =
        VectorF (mag * cos angle, mag * sin angle)

    static member inline xOf (v : VectorF) = v.X
    static member inline yOf (v : VectorF) = v.Y

    static member inline dot (u : VectorF) (v : VectorF) = u *. v
    static member inline dotSelf (u : VectorF) = u.MagnitudeSquared

    static member inline angleOf (v : VectorF) = v.Angle
    static member inline angleDegOf (v : VectorF) = v.AngleInDegrees
    static member inline magOf (v : VectorF) = v.Magnitude
    static member inline magSquaredOf (v : VectorF) = v.MagnitudeSquared

    static member inline scale (scaleX, scaleY) (v : VectorF) = v.Scale (scaleX, scaleY)
    static member inline rotate angle (v : VectorF) = v.Rotate angle
    static member inline rotate90 (v : VectorF) = v.Rotate90 ()
    static member inline rotateNeg90 (v : VectorF) = v.RotateNeg90 ()
    static member inline normalize (v : VectorF) = v.Normalized

    static member elementwiseDiv (u : VectorF) (v : VectorF) = u.X / v.X, u.Y / v.Y

    static member proj (onto : VectorF) (v : VectorF) = onto * ((v *. onto) / onto.MagnitudeSquared)
    static member projAmount (onto : VectorF) (v : VectorF) = (v *. onto) / onto.MagnitudeSquared

    static member angleBetween (u : VectorF) (v : VectorF) = acos (u *. v / u.Magnitude / v.Magnitude)

    static member intersection (p1 : VectorF) (p2 : VectorF) (q1 : VectorF) (q2 : VectorF) =
        let d1 = p1.X * p2.Y - p1.Y * p2.X
        let d2 = q1.X * q2.Y - q1.Y * q2.X
        let denom = (p1.X - p2.X) * (q1.Y - q2.Y) - (p1.Y - p2.Y) * (q1.X - q2.X)
        let rx = (d1 * (q1.X - q2.X) - (p1.X - p2.X) * d2) / denom
        let ry = (d1 * (q1.Y - q2.Y) - (p1.Y - p2.Y) * d2) / denom
        VectorF (rx, ry)

    static member distinctOne(vs : seq<VectorF>) =
        let vs = List(vs)
        if vs.Count = 0 then None
        else
            let avgPos = Seq.sum vs / float vs.Count
            if vs |> Seq.exists(fun v -> v <>~ avgPos) then None
            else Some avgPos


[<AutoOpen>]
module VectorExt =
    let inline vec x y = VectorF (float x, float y)
    let inline vecu x = VectorF (float x, float x)
    let inline vecp angle mag = VectorF.ofPolar (float angle, float mag)
    let inline unvec (v : VectorF) = v.X, v.Y
    let vecz = VectorF.Zero


[<Struct>]
type RectF =
    val X : float
    val Y : float
    val R : float
    val B : float
    new (x, y, r, b) = { X = x; Y = y; R = r; B = b }

    member x.Width =  x.R - x.X
    member x.Height = x.B - x.Y
    member x.Left =   x.X
    member x.Top =    x.Y
    member x.Right =  x.R
    member x.Bottom = x.B

    member x.TopLeft     = vec x.X x.Y
    member x.TopRight    = vec x.R x.Y
    member x.BottomLeft  = vec x.X x.B
    member x.BottomRight = vec x.R x.B

    member x.Center =
        vec ((x.X + x.R) / 2.0) ((x.Y + x.B) / 2.0)

    member x.FourCorners =
        [| vec x.X x.Y; vec x.R x.Y; vec x.X x.B; vec x.R x.B |]

    member x.Contains (v : VectorF) =
        v.X >= x.X && v.X <= x.R && v.Y >= x.Y && v.Y <= x.B

    member x.Expand points =
        (x, points)
        ||> Seq.fold (fun (x : RectF) (v : VectorF) ->
            RectF (min x.X v.X, min x.Y v.Y, max x.R v.X, max x.B v.Y))

    member x.Inflate (dx, dy) =
        RectF (x.X - dx, x.Y - dy, x.R + dx, x.B + dy)

    member x.Move (d : VectorF) =
        RectF (x.X + d.X, x.Y + d.Y, x.R + d.X, x.B + d.Y)

    static member (+) (x : RectF, d : VectorF) =
        RectF (x.X + d.X, x.Y + d.Y, x.R + d.X, x.B + d.Y)

    static member (+) (d : VectorF, x : RectF) =
        RectF (x.X + d.X, x.Y + d.Y, x.R + d.X, x.B + d.Y)

    static member (-) (x : RectF, d : VectorF) =
        RectF (x.X - d.X, x.Y - d.Y, x.R - d.X, x.B - d.Y)

    static member Zero = RectF (0.0, 0.0, 0.0, 0.0)

    static member ofBounds (ux, uy, vx, vy) =
        RectF (min ux vx, min uy vy, max ux vx, max uy vy)

    static member ofSize (x, y, width, height) =
        RectF (x, y, x + width, y + height)

    static member ofVec (v : VectorF) =
        RectF (v.X, v.Y, v.X, v.Y)

    static member ofVecs (u : VectorF, v : VectorF) =
        RectF (min u.X v.X, min u.Y v.Y, max u.X v.X, max u.Y v.Y)

    static member ofVecParams (v : VectorF, [<ParamArray>] vs : _ []) =
        let r = RectF (v.X, v.Y, v.X, v.Y)
        r.Expand vs

    static member ofVecArray vecs =
        match vecs with
        | [| |] -> raise (ArgumentException "Input array empty")
        | _ ->
            let v0 : VectorF = vecs.[0]
            let r0 = RectF (v0.X, v0.Y, v0.X, v0.Y)
            r0.Expand (Seq.skip 1 vecs)

    static member ofCenter (c : VectorF, w, h) =
        RectF (c.X - w, c.Y - h, c.X + w, c.Y + h)

    static member hasIntersection (r : RectF) (s : RectF) =
        r.X < s.R && r.R > s.X && r.Y < s.B && r.B > s.Y

    static member union (r : RectF) (s : RectF) =
        RectF (min r.X s.X, min r.Y s.Y, max r.R s.R, max r.B s.B)

    static member inline contains p (r : RectF) = r.Contains p
    static member inline expand points (r : RectF) = r.Expand points
    static member inline move d (r : RectF) = r.Move d


[<AutoOpen>]
module RectExt =
    let inline rect x y x2 y2 = RectF (float x, float y, float x2, float y2)
    let inline rects x y w h = RectF.ofSize (float x, float y, float w, float h)
    let inline rectsu x y l = RectF.ofSize (float x, float y, float l, float l)
    let inline recto w h = RectF.ofSize (0.0, 0.0, float w, float h)
    let inline rectv u v = RectF.ofVecs (u, v)
    let inline rectc c w h = RectF.ofCenter (c, float w, float h)
    let rectz = RectF (0.0, 0.0, 0.0, 0.0)


type MatrixF (sourceSeq) =
    let arr = array2D sourceSeq
    member x.Length1 = Array2D.length1 arr
    member x.Length2 = Array2D.length2 arr
    member x.Item with get (i, j) = Array2D.get arr i j


module RootFinding =
    let rec newton iterationCount f f' x : float =
        if iterationCount > 0 then
            newton (iterationCount - 1) f f' (x - f x / f' x)
        else
            x

