namespace Doaz.Reactive

open System


type SuccessFailure<'s, 'f> =
    | Success of value : 's
    | Failure of exn : 'f

type ('s, 'f) sf = SuccessFailure<'s, 'f>


module Sf =
    let bind binder sf =
        match sf with
        | Success value -> binder value
        | Failure exn -> Failure exn

    let map mapper sf =
        match sf with
        | Success value -> Success (mapper value)
        | Failure exn -> Failure exn

    let toOption sf =
        match sf with
        | Success value -> Some value
        | Failure exn -> None


type SuccessFailureBuilder () =
    member x.Bind (m, f) = Sf.bind f m
    member x.Return m = Success m

[<AutoOpen>]
module SfUtil =
    let sf = SuccessFailureBuilder ()


