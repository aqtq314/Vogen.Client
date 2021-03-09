namespace Doaz.Reactive

open System


module Result =
    let ofOption error valueOp =
        match valueOp with
        | Some value -> Ok value
        | None -> Error error

    let toOption result =
        match result with
        | Ok value -> Some value
        | Error _ -> None

type ResultBuilder() =
    member x.Bind(m, f) = Result.bind f m
    member x.Bind((m, error), f) = m |> Result.ofOption error |> Result.bind f

    member x.Zero() = Ok()
    member x.Return v = Ok v
    member x.ReturnFrom(m : Result<_, _>) = m

    member x.Combine(m, f) = Result.bind f m
    member x.Delay(f : unit -> _) = f
    member x.Run f = f()

    member x.TryWith(m, h) =
        try x.ReturnFrom(m())
        with e -> h e

    member x.TryFinally(m, compensation) =
        try x.ReturnFrom(m())
        finally compensation()

    member x.Using(resource : #IDisposable, f) =
        x.TryFinally((fun () -> f resource), fun () -> resource.Dispose())

    member x.While(pred, f) =
        if not(pred()) then Ok() else
            f() |> ignore
            x.While(pred, f)

    member x.For(items : seq<_>, f) =
        x.Using(items.GetEnumerator(), fun enum -> x.While(enum.MoveNext, x.Delay(fun () -> f enum.Current)))

[<AutoOpen>]
module ResultUtil =
    let result = ResultBuilder()


