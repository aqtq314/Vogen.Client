namespace Doaz.Reactive

open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text
open System.Threading.Tasks
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Data
open System.Windows.Documents
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Navigation
open System.Windows.Shapes

#nowarn "62"


[<AutoOpen>]
module WpfUtil =
    let inline tryCast<'a when 'a : null> (value : obj) =
        match value with
        | :? 'a as value -> value
        | _ -> null

    let inline freeze (g : #Freezable) = g.Freeze ()

    type ModifierKeys with
        member x.IsNone  = x = ModifierKeys.None
        member x.IsCtrl  = x = ModifierKeys.Control
        member x.IsAlt   = x = ModifierKeys.Alt
        member x.IsShift = x = ModifierKeys.Shift
        member x.IsCtrlDown = (x &&& ModifierKeys.Control) <> ModifierKeys.None
        member x.IsAltDown = (x &&& ModifierKeys.Alt) <> ModifierKeys.None
        member x.IsShiftDown = (x &&& ModifierKeys.Shift) <> ModifierKeys.None

    type VectorF with
        member x.AsWpfPoint = System.Windows.Point (x.X, x.Y)

        member x.AsWpfVector = System.Windows.Vector (x.X, x.Y)

        static member ofWpfPoint (p : System.Windows.Point) =
            VectorF (p.X, p.Y)

        static member ofWpfVector (p : System.Windows.Vector) =
            VectorF (p.X, p.Y)

    type RectF with
        member x.WpfSize = System.Windows.Size (x.R - x.X, x.B - x.Y)

        member x.AsWpfRect = System.Windows.Rect (x.X, x.Y, x.Width, x.Height)

        static member ofWpfRect (r : System.Windows.Rect) =
            RectF (r.X, r.Y, r.Right, r.Bottom)



module BooleanBoxes =
    let falseBox = (UIElement.FocusableProperty.GetMetadata typeof<UIElement>).DefaultValue
    let trueBox = (UIElement.FocusableProperty.GetMetadata typeof<Control>).DefaultValue


module Dp =
    type MetaFlags = FrameworkPropertyMetadataOptions

    type ('a, 'owner) Meta when 'owner :> DependencyObject private (meta : FrameworkPropertyMetadata) =
        new (initval : 'a) =
            Meta (FrameworkPropertyMetadata (initval))

        new (initval : 'a, flags : MetaFlags) =
            Meta (FrameworkPropertyMetadata (initval, flags))

        new (initval : 'a, flags : MetaFlags, propChanged) =
            Meta (
                FrameworkPropertyMetadata (
                    initval, flags,
                    PropertyChangedCallback (fun d e -> propChanged (d :?> 'owner) (e.OldValue :?> 'a, e.NewValue :?> 'a))))

        new (initval : 'a, flags : MetaFlags, propChanged, coerce : _ -> _ -> 'a) =
            Meta (
                FrameworkPropertyMetadata (
                    initval, flags,
                    PropertyChangedCallback (fun d e -> propChanged (d :?> 'owner) (e.OldValue :?> 'a, e.NewValue :?> 'a)),
                    CoerceValueCallback (fun d v -> coerce (d :?> 'owner) (v :?> 'a) |> box)))

        new (initval : 'a, flags : MetaFlags, propChanged, coerce : _ -> _ -> 'a, forbidAnimation) =
            Meta (
                FrameworkPropertyMetadata (
                    initval, flags,
                    PropertyChangedCallback (fun d e -> propChanged (d :?> 'owner) (e.OldValue :?> 'a, e.NewValue :?> 'a)),
                    CoerceValueCallback (fun d v -> coerce (d :?> 'owner) (v :?> 'a) |> box),
                    forbidAnimation))

        new (initval : 'a, flags : MetaFlags, propChanged, coerce : _ -> _ -> 'a, forbidAnimation, initTrigger) =
            Meta (
                FrameworkPropertyMetadata (
                    initval, flags,
                    PropertyChangedCallback (fun d e -> propChanged (d :?> 'owner) (e.OldValue :?> 'a, e.NewValue :?> 'a)),
                    CoerceValueCallback (fun d v -> coerce (d :?> 'owner) (v :?> 'a) |> box),
                    forbidAnimation, initTrigger))

        new (initval : 'a, propChanged) =
            Meta (
                FrameworkPropertyMetadata (
                    initval,
                    PropertyChangedCallback (fun d e -> propChanged (d :?> 'owner) (e.OldValue :?> 'a, e.NewValue :?> 'a))))

        new (initval : 'a, propChanged, coerce : _ -> _ -> 'a) =
            Meta (
                FrameworkPropertyMetadata (
                    initval,
                    PropertyChangedCallback (fun d e -> propChanged (d :?> 'owner) (e.OldValue :?> 'a, e.NewValue :?> 'a)),
                    CoerceValueCallback (fun d v -> coerce (d :?> 'owner) (v :?> 'a) |> box)))

        member x.WpfMeta = meta

    let inline reg<'a, 'owner when 'owner :> DependencyObject> name (meta : ('a, 'owner) Meta) =
        DependencyProperty.Register (
            name, typeof<'a>, typeof<'owner>, meta.WpfMeta)

    let inline regr<'a, 'owner when 'owner :> DependencyObject> name (meta : ('a, 'owner) Meta) =
        DependencyProperty.RegisterReadOnly (
            name, typeof<'a>, typeof<'owner>, meta.WpfMeta)

    let inline rega<'a, 'owner when 'owner :> DependencyObject> name (meta : ('a, DependencyObject) Meta) =
        DependencyProperty.RegisterAttached (
            name, typeof<'a>, typeof<'owner>, meta.WpfMeta)

    let inline regar<'a, 'owner when 'owner :> DependencyObject> name (meta : ('a, DependencyObject) Meta) =
        DependencyProperty.RegisterAttachedReadOnly (
            name, typeof<'a>, typeof<'owner>, meta.WpfMeta)


type ValueConverter =
    static member Create(forward : Func<'a, 'b>) =
        { new IValueConverter with
            member x.Convert(v, targetType, p, culture) = box(forward.Invoke(unbox v))
            member x.ConvertBack(v, targetType, p, culture) = raise(NotImplementedException()) }

    static member Create(forward : Func<'a, 'p, 'b>) =
        { new IValueConverter with
            member x.Convert(v, targetType, p, culture) = box(forward.Invoke(unbox v, unbox p))
            member x.ConvertBack(v, targetType, p, culture) = raise(NotImplementedException()) }

    static member Create(forward : Func<'a, 'b>, backward : Func<'b, 'a>) =
        { new IValueConverter with
            member x.Convert(v, targetType, p, culture) = box(forward.Invoke(unbox v))
            member x.ConvertBack(v, targetType, p, culture) = box(backward.Invoke(unbox v)) }

    static member Create(forward : Func<'a, 'p, 'b>, backward : Func<'b, 'p2, 'a>) =
        { new IValueConverter with
            member x.Convert(v, targetType, p, culture) = box(forward.Invoke(unbox v, unbox p))
            member x.ConvertBack(v, targetType, p, culture) = box(backward.Invoke(unbox v, unbox p)) }

    static member CreateMulti(forward : Func<obj [], 'b>) =
        { new IMultiValueConverter with
            member x.Convert(vs, targetType, p, culture) = box(forward.Invoke(vs))
            member x.ConvertBack(v, targetTypes, p, culture) = raise(NotImplementedException()) }

    static member CreateMulti(forward : Func<obj [], 'b>, backward : Func<'b, obj []>) =
        { new IMultiValueConverter with
            member x.Convert(vs, targetType, p, culture) = box(forward.Invoke(vs))
            member x.ConvertBack(v, targetTypes, p, culture) = backward.Invoke(unbox v) }

    static member CreateMulti(forward : Func<obj [], 'p, 'b>) =
        { new IMultiValueConverter with
            member x.Convert(vs, targetType, p, culture) = box(forward.Invoke(vs, unbox p))
            member x.ConvertBack(v, targetTypes, p, culture) = raise(NotImplementedException()) }

    static member CreateMulti(forward : Func<obj [], 'p, 'b>, backward : Func<'b, 'p2, obj []>) =
        { new IMultiValueConverter with
            member x.Convert(vs, targetType, p, culture) = box(forward.Invoke(vs, unbox p))
            member x.ConvertBack(v, targetTypes, p, culture) = backward.Invoke(unbox v, unbox p) }


[<Extension>]
type ExtensionMethods =
    [<Extension>]
    static member Frozen (freezable : #Freezable) =
        freezable.Freeze ()
        freezable

    [<Extension>]
    static member AsColor (colorCode : uint32) =
        Color.FromArgb (
            byte ((colorCode >>> 24) &&& 0xFFu),
            byte ((colorCode >>> 16) &&& 0xFFu),
            byte ((colorCode >>> 8) &&& 0xFFu),
            byte ((colorCode >>> 0) &&& 0xFFu))



