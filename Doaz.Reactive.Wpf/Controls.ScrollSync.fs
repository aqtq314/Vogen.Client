namespace Doaz.Reactive.Controls

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input


type internal OffsetContainer(offset, [<ParamArray>] scrollViewers : _ []) =
    member val Offset : float = offset with get, set
    member val ScrollViewers : List<ScrollViewer> = List<_>(scrollViewers)

type ScrollSync private() =
    inherit DependencyObject()

    static let verticalScrollGroups = Dictionary<string, OffsetContainer>()     // potential memory leak
    static let horizontalScrollGroups = Dictionary<string, OffsetContainer>()   // potential memory leak

    static let scrollViewerHorizontalScrollChanged = ScrollChangedEventHandler(fun sender (e : ScrollChangedEventArgs) ->
        if sender :? ScrollViewer && e.HorizontalChange <> 0.0 then
            let changedSv = sender :?> ScrollViewer
            let groupName = ScrollSync.GetHorizontalScrollGroup changedSv
            match horizontalScrollGroups.TryGetValue groupName |> Option.ofByRef with
            | Some group when group.Offset <> changedSv.HorizontalOffset ->
                group.Offset <- changedSv.HorizontalOffset
                group.ScrollViewers |> Seq.iter(fun sv ->
                    if sv.HorizontalOffset <> changedSv.HorizontalOffset then
                        sv.ScrollToHorizontalOffset changedSv.HorizontalOffset)
            | _ -> ())

    static let scrollViewerVerticalScrollChanged = ScrollChangedEventHandler(fun sender (e : ScrollChangedEventArgs) ->
        if sender :? ScrollViewer && e.VerticalChange <> 0.0 then
            let changedSv = sender :?> ScrollViewer
            let groupName = ScrollSync.GetVerticalScrollGroup changedSv
            match verticalScrollGroups.TryGetValue groupName |> Option.ofByRef with
            | Some group when group.Offset <> changedSv.VerticalOffset ->
                group.Offset <- changedSv.VerticalOffset
                group.ScrollViewers |> Seq.iter(fun sv ->
                    if sv.VerticalOffset <> changedSv.VerticalOffset then
                        sv.ScrollToVerticalOffset changedSv.VerticalOffset)
            | _ -> ())

    static let addToHorizontalScrollGroup groupName (sv : ScrollViewer) =
        match horizontalScrollGroups.TryGetValue groupName |> Option.ofByRef with
        | None ->
            horizontalScrollGroups.Add(groupName, OffsetContainer(sv.HorizontalOffset, sv))
        | Some group ->
            sv.ScrollToHorizontalOffset group.Offset
            group.ScrollViewers.Add sv
        sv.ScrollChanged.AddHandler scrollViewerHorizontalScrollChanged

    static let addToVerticalScrollGroup groupName (sv : ScrollViewer) =
        match verticalScrollGroups.TryGetValue groupName |> Option.ofByRef with
        | None ->
            verticalScrollGroups.Add(groupName, OffsetContainer(sv.VerticalOffset, sv))
        | Some group ->
            sv.ScrollToVerticalOffset group.Offset
            group.ScrollViewers.Add sv
        sv.ScrollChanged.AddHandler scrollViewerVerticalScrollChanged

    static let removeFromHorizontalScrollGroup groupName (sv : ScrollViewer) =
        match horizontalScrollGroups.TryGetValue groupName |> Option.ofByRef with
        | None -> ()
        | Some group ->
            group.ScrollViewers.Remove sv |> ignore
            if group.ScrollViewers.Count = 0 then
                horizontalScrollGroups.Remove groupName |> ignore
        sv.ScrollChanged.RemoveHandler scrollViewerHorizontalScrollChanged

    static let removeFromVerticalScrollGroup groupName (sv : ScrollViewer) =
        match verticalScrollGroups.TryGetValue groupName |> Option.ofByRef with
        | None -> ()
        | Some group ->
            group.ScrollViewers.Remove sv |> ignore
            if group.ScrollViewers.Count = 0 then
                verticalScrollGroups.Remove groupName |> ignore
        sv.ScrollChanged.RemoveHandler scrollViewerVerticalScrollChanged

    static member GetHorizontalScrollGroup(d : DependencyObject) = d.GetValue ScrollSync.HorizontalScrollGroupProperty :?> string
    static member SetHorizontalScrollGroup(d : DependencyObject, value : string) = d.SetValue(ScrollSync.HorizontalScrollGroupProperty, value)
    static member OnHorizontalScrollGroupChanged d (oldValue, newValue) =
        match d with
        | :? ScrollViewer as sv ->
            removeFromHorizontalScrollGroup oldValue sv
            addToHorizontalScrollGroup newValue sv
        | _ -> ()
    static member val HorizontalScrollGroupProperty =
        Dp.rega<string, ScrollSync> "HorizontalScrollGroup"
            (Dp.Meta(String.Empty, ScrollSync.OnHorizontalScrollGroupChanged))

    static member GetVerticalScrollGroup(d : DependencyObject) = d.GetValue ScrollSync.VerticalScrollGroupProperty :?> string
    static member SetVerticalScrollGroup(d : DependencyObject, value : string) = d.SetValue(ScrollSync.VerticalScrollGroupProperty, value)
    static member OnVerticalScrollGroupChanged d (oldValue, newValue) =
        match d with
        | :? ScrollViewer as sv ->
            removeFromVerticalScrollGroup oldValue sv
            addToVerticalScrollGroup newValue sv
        | _ -> ()
    static member val VerticalScrollGroupProperty =
        Dp.rega<string, ScrollSync> "VerticalScrollGroup"
            (Dp.Meta(String.Empty, ScrollSync.OnVerticalScrollGroupChanged))


