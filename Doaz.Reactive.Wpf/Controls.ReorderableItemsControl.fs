namespace Doaz.Reactive.Controls

open Doaz.Reactive
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Data
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Animation

#nowarn "40"
#nowarn "21"


type private ReorderableItemsDragEventArgs<'i> =
    | ReorderDragStarted of sender : 'i

type [<StyleTypedPropertyAttribute (Property = "ItemContainerStyle", StyleTargetType = typeof<ReorderableItemPresenter>)>] [<AllowNullLiteral>]
    ReorderableItemsControl () =
    inherit ItemsControl ()

    override x.GetContainerForItemOverride () =
        ReorderableItemPresenter () :> _

    override x.IsItemItsOwnContainerOverride item =
        item :? ReorderableItemPresenter


and [<TemplatePart (Name = "PART_Grip", Type = typeof<Thumb>)>] [<AllowNullLiteral>]
    ReorderableItemPresenter () =
    inherit ContentControl ()

    let mutable PART_Grip : Thumb = null

    member x.GetParentReorderableItemsControl () =
        ItemsControl.ItemsControlFromItemContainer x |> tryCast<ReorderableItemsControl>

    member private x.CreateOrGetTranslateTransform () =
        match x.RenderTransform with
        | :? TranslateTransform as transform -> transform
        | _ ->
            let transform = TranslateTransform (0.0, 0.0)
            x.RenderTransform <- transform
            transform

    member private x.AnimateTranslateYToZero ?fromValue =
        let translateTransform = x.CreateOrGetTranslateTransform ()

        let fromValue = defaultArg fromValue translateTransform.Y
        translateTransform.BeginAnimation (
            TranslateTransform.YProperty,
            DoubleAnimation (fromValue, 0.0, Duration (TimeSpan.FromMilliseconds 300.0), FillBehavior.Stop, EasingFunction = CubicEase ()),
            HandoffBehavior.SnapshotAndReplace)
        translateTransform.Y <- 0.0

    member x.GripDragBehavior =
        let rec noDrag = behavior {
            let! (e : BehaviorDragEventArgs) = ()

            match e.EventType with
            | DragStartEvent offset ->
                let translateTransform = x.CreateOrGetTranslateTransform ()
                translateTransform.Y <- translateTransform.Y
                translateTransform.BeginAnimation (TranslateTransform.YProperty, null, HandoffBehavior.SnapshotAndReplace)

                return! drag

            | _ ->
                return! noDrag }

        and drag = behavior {
            let! e = ()
            match e.EventType with
            | DragDeltaEvent delta ->
                let translateTransform = x.CreateOrGetTranslateTransform ()
                translateTransform.Y <- translateTransform.Y + delta.Y

                let parent = x.GetParentReorderableItemsControl ()
                match parent.ItemsSource with
                | :? IReorderableCollection as collection ->
                    let index = parent.ItemContainerGenerator.IndexFromContainer x

                    if translateTransform.Y > 0.0 then
                        let newIndex, newTranslateY =
                            (index, translateTransform.Y)
                            |> fix (fun f (newIndex, newTranslateY) ->
                                if newIndex + 1 < parent.Items.Count then
                                    let nextContainer =
                                        parent.ItemContainerGenerator.ContainerFromIndex (newIndex + 1)
                                        |> tryCast<ReorderableItemPresenter>
                                    if (nextContainer |> isNotNull) && newTranslateY > nextContainer.ActualHeight / 2.0 then
                                        nextContainer.AnimateTranslateYToZero (
                                            (nextContainer.CreateOrGetTranslateTransform ()).Y + x.ActualHeight)
                                        f (newIndex + 1, newTranslateY - nextContainer.ActualHeight)
                                    else
                                        newIndex, newTranslateY
                                else
                                    newIndex, newTranslateY)

                        if index <> newIndex then
                            collection.Move index newIndex
                            translateTransform.Y <- newTranslateY

                    elif translateTransform.Y < 0.0 then
                        let newIndex, newTranslateY =
                            (index, translateTransform.Y)
                            |> fix (fun f (newIndex, newTranslateY) ->
                                if newIndex - 1 >= 0 then
                                    let nextContainer =
                                        parent.ItemContainerGenerator.ContainerFromIndex (newIndex - 1)
                                        |> tryCast<ReorderableItemPresenter>
                                    if (nextContainer |> isNotNull) && newTranslateY < -nextContainer.ActualHeight / 2.0 then
                                        nextContainer.AnimateTranslateYToZero (
                                            (nextContainer.CreateOrGetTranslateTransform ()).Y - x.ActualHeight)
                                        f (newIndex - 1, newTranslateY + nextContainer.ActualHeight)
                                    else
                                        newIndex, newTranslateY
                                else
                                    newIndex, newTranslateY)

                        if index <> newIndex then
                            collection.Move index newIndex
                            translateTransform.Y <- newTranslateY

                | _ -> ()

                return! drag

            | DragCompletedEvent ->
                x.AnimateTranslateYToZero ()
                return! noDrag

            | _ ->
                return! drag }

        Behavior.dragBehavior drag

    override x.OnApplyTemplate () =
        base.OnApplyTemplate ()

        if PART_Grip |> isNotNull then
            Behaviors.SetDragBehavior (PART_Grip, Behavior.none)

        PART_Grip <- x.GetTemplateChild "PART_Grip" |> tryCast<Thumb>

        if PART_Grip |> isNotNull then
            Behaviors.SetDragBehavior (PART_Grip, x.GripDragBehavior)



