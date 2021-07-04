namespace Doaz.Reactive

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Windows


type WpfOption<'a> (value : 'a option) =
    member x.IsSome = Option.isSome value
    member x.IsNone = Option.isNone value
    member x.Value = Option.get value
    member x.ValueOrNull =
        match value with
        | Some value -> box value
        | None -> null

    member x.AsOption = value

    member x.AsSeq =
        match value with
        | None -> Seq.empty
        | Some value -> Seq.singleton value

    member x.AsBoxed =
        WpfOption (Option.map box value)

    member x.Count = Option.count value

    override x.ToString () =
        match value with
        | Some value -> sprintf "WpfSome (%O)" value
        | None -> sprintf "WpfNone"


[<AutoOpen>]
module WpfOptionUtil =
    let WpfNone<'a> = WpfOption<'a> (None)

    let WpfSome value = WpfOption (Some value)

    let (|WpfNone|WpfSome|) (v : WpfOption<_>) =
        if v.IsSome then
            WpfSome v.Value
        else
            WpfNone


module WpfOption =
    let inline get (v : WpfOption<_>) = v.Value

    let inline isNone (v : WpfOption<_>) = v.IsNone
    let inline isSome (v : WpfOption<_>) = v.IsSome

    let map mapper v =
        match v with
        | WpfNone -> WpfNone
        | WpfSome v -> WpfSome (mapper v)

    let bind binder v =
        match v with
        | WpfNone -> WpfNone
        | WpfSome v -> binder v

    let bindNone fallback v =
        match v with
        | WpfNone -> fallback
        | WpfSome v -> WpfSome v

    let iter iterator v =
        match v with
        | WpfNone -> ()
        | WpfSome v -> iterator v

    let fold folder init v =
        match v with
        | WpfNone -> init
        | WpfSome v -> folder init v

    let ofOption v =
        match v with
        | None -> WpfNone
        | Some v -> WpfSome v

    let toOption v =
        match v with
        | WpfNone -> None
        | WpfSome v -> Some v

    let getOrFallbackTo fallback v =
        match v with
        | WpfNone -> fallback
        | WpfSome v -> v

    let structuralEquals x y =
        match x, y with
        | WpfNone, WpfNone -> true
        | WpfSome x, WpfSome y -> x = y
        | _, _ -> false


type WpfOptionBuilder () =
    member x.Bind (m, f) =
        WpfOption.bind f m

    member x.Return m =
        WpfSome m

    member x.ReturnFrom m =
        m : WpfOption<_>

    member x.Zero () =
        WpfSome ()


type WpfOptionIterBuilder () =
    member x.Bind (m, f) =
        match m with
        | WpfSome m -> f m
        | WpfNone -> ()

    member x.Zero () =
        ()


[<AutoOpen>]
module WpfOptionBuilderUtil =
    let wpfOption = WpfOptionBuilder ()
    let wpfOptionIter = WpfOptionIterBuilder ()


