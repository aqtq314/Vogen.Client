namespace Doaz.Reactive.Controls

open Doaz.Reactive
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Data
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Shapes


type TrivialPanel() =
    inherit Panel()

    override x.MeasureOverride availableSize =
        let availableSize = Size(infinity, infinity)

        for child in x.InternalChildren do
            child.Measure availableSize

        Size()

    override x.ArrangeOverride finalSize =
        for child in x.InternalChildren do
            let childDesiredSize = child.DesiredSize
            let childX, childY =
                match child with
                | :? FrameworkElement as child ->
                    (match child.HorizontalAlignment with
                        | HorizontalAlignment.Center -> half -childDesiredSize.Width
                        | HorizontalAlignment.Right -> -childDesiredSize.Width
                        | _ -> 0.0),
                    (match child.VerticalAlignment with
                        | VerticalAlignment.Center -> half -childDesiredSize.Height
                        | VerticalAlignment.Bottom -> -childDesiredSize.Height
                        | _ -> 0.0)
                | _ -> 0.0, 0.0
            child.Arrange(Rect(Point(childX, childY), childDesiredSize))

        finalSize

type BasicPanel() =
    inherit Panel()

    override x.MeasureOverride availableSize =
        let mutable maxChildWidth = 0.0
        let mutable maxChildHeight = 0.0
        for child in x.InternalChildren do
            child.Measure availableSize
            let childDesiredSize = child.DesiredSize
            maxChildWidth <- max maxChildWidth childDesiredSize.Width
            maxChildHeight <- max maxChildHeight childDesiredSize.Height
        Size(maxChildWidth, maxChildHeight)

    override x.ArrangeOverride finalSize =
        let childArrangeRect = Rect(Point(), finalSize)
        for child in x.InternalChildren do
            child.Arrange childArrangeRect
        finalSize

