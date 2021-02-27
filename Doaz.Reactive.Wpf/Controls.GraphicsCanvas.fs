namespace Doaz.Reactive.Controls

open Doaz.Reactive
open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Linq
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


type ICoordinateConverter<'TIn, 'TOut> =
    abstract ConvertX : x : float -> float
    abstract ConvertY : y : float -> float
    abstract ConvertPoint : p : 'TIn -> 'TOut

module CoordinateConverter =
    let inline create xconv yconv outCons =
        { new ICoordinateConverter<_, _> with
            member x.ConvertX px = xconv px
            member x.ConvertY py = yconv py
            member x.ConvertPoint p =
                let px = (^a : (member X : float) p)
                let py = (^a : (member Y : float) p)
                outCons (xconv px) (yconv py) }


type CoordinateManager () as x =
    static member val DefaultExtent = rects (-1500) (-1000) 3000 2000
    static member val DefaultViewportSize = Size ()
    static member val DefaultViewportCenter = vecz

    member val Extent : RectF = CoordinateManager.DefaultExtent with get, set
    member val ViewportSize : Size = CoordinateManager.DefaultViewportSize with get, set
    member val ViewportCenter : VectorF = CoordinateManager.DefaultViewportCenter with get, set

    member val PhysicalToLogical : ICoordinateConverter<Point, VectorF> =
        CoordinateConverter.create
            (fun px -> px - (let s = x.ViewportSize in s.Width) / 2.0 + x.ViewportCenter.X)
            (fun py -> py - (let s = x.ViewportSize in s.Height) / 2.0 + x.ViewportCenter.Y)
            (fun x y -> vec x y)

    member val LogicalToPhysical : ICoordinateConverter<VectorF, Point> =
        CoordinateConverter.create
            (fun vx -> vx + (let s = x.ViewportSize in s.Width) / 2.0 - x.ViewportCenter.X)
            (fun vy -> vy + (let s = x.ViewportSize in s.Height) / 2.0 - x.ViewportCenter.Y)
            (fun x y -> Point (x, y))

    member val ScrollToLogical : ICoordinateConverter<Point, VectorF> =
        CoordinateConverter.create
            (fun px -> px + x.Extent.X + (let s = x.ViewportSize in s.Width) / 2.0)
            (fun py -> py + x.Extent.Y + (let s = x.ViewportSize in s.Height) / 2.0)
            (fun x y -> vec x y)

    member val LogicalToScroll : ICoordinateConverter<VectorF, Point> =
        CoordinateConverter.create
            (fun vx -> vx - x.Extent.X - (let s = x.ViewportSize in s.Width) / 2.0)
            (fun vy -> vy - x.Extent.Y - (let s = x.ViewportSize in s.Height) / 2.0)
            (fun x y -> Point (x, y))


type GraphicsCanvasMouseEventType =
    | CanvasMouseDownEvent of button : MouseButton * clickCount : int
    | CanvasMouseMoveEvent
    | CanvasMouseUpEvent

type GraphicsCanvasMouseHoverEventType =
    | CanvasMouseEnterEvent
    | CanvasMouseLeaveEvent

type GraphicsCanvasMouseEventArgs<'arg> (position, eventType) =
    member x.Position : VectorF = position
    member x.EventType : 'arg = eventType


type GraphicsCanvas () =
    inherit Panel ()

    static do UIElement.FocusableProperty.OverrideMetadata (typeof<GraphicsCanvas>, FrameworkPropertyMetadata BooleanBoxes.trueBox)

    let graphicsCanvasMouseEvent = Event<_> ()
    let graphicsCanvasMouseHoverEvent = Event<_> ()

    static let ZoomInversePropertyKey =
        Dp.regr<float, GraphicsCanvas> "ZoomInverse"
            (Dp.Meta (1.0))

    member val Coordinate = CoordinateManager ()

    static member val ExtentProperty =
        Dp.reg<RectF, GraphicsCanvas> "Extent"
            (Dp.Meta (CoordinateManager.DefaultExtent, fun (d : GraphicsCanvas) -> d.OnExtentChanged))

    static member val private ViewportSizePropertyKey =
        Dp.regr<Size, GraphicsCanvas> "ViewportSize"
            (Dp.Meta (CoordinateManager.DefaultViewportSize, fun (d : GraphicsCanvas) -> d.OnViewportSizeChanged))

    static member val ViewportSizeProperty =
        (GraphicsCanvas.ViewportSizePropertyKey : DependencyPropertyKey).DependencyProperty

    static member val ViewportCenterProperty =
        Dp.reg<VectorF, GraphicsCanvas> "ViewportCenter"
            (Dp.Meta (CoordinateManager.DefaultViewportCenter, fun (d : GraphicsCanvas) -> d.OnViewportCenterChanged))

    static member val ZoomProperty =
        Dp.reg<float, GraphicsCanvas> "Zoom"
            (Dp.Meta (1.0, fun (d : GraphicsCanvas) -> d.OnZoomChanged))

    static member val MaxZoomProperty =
        Dp.reg<float, GraphicsCanvas> "MaxZoom"
            (Dp.Meta (16.0))

    static member val MinZoomProperty =
        Dp.reg<float, GraphicsCanvas> "MinZoom"
            (Dp.Meta (0.125))

    static member ZoomInverseProperty = ZoomInversePropertyKey.DependencyProperty

    member x.Extent
        with get () = x.GetValue GraphicsCanvas.ExtentProperty :?> RectF
        and set (v : RectF) = x.SetValue (GraphicsCanvas.ExtentProperty, box v)

    member x.ViewportSize
        with get () = x.GetValue GraphicsCanvas.ViewportSizeProperty :?> Size
        and private set (v : Size) = x.SetValue (GraphicsCanvas.ViewportSizePropertyKey, box v)

    member x.ViewportCenter
        with get () = x.GetValue GraphicsCanvas.ViewportCenterProperty :?> VectorF
        and set (v : VectorF) = x.SetValue (GraphicsCanvas.ViewportCenterProperty, box v)

    member x.Zoom
        with get () = x.GetValue GraphicsCanvas.ZoomProperty :?> float
        and set (v : float) = x.SetValue (GraphicsCanvas.ZoomProperty, box v)

    member x.ZoomInverse
        with get () = x.GetValue GraphicsCanvas.ZoomInverseProperty :?> float
        and private set (v : float) = x.SetValue (ZoomInversePropertyKey, box v)

    member x.MaxZoom
        with get () = x.GetValue GraphicsCanvas.MaxZoomProperty :?> float
        and set (v : float) = x.SetValue (GraphicsCanvas.MaxZoomProperty, box v)

    member x.MinZoom
        with get () = x.GetValue GraphicsCanvas.MinZoomProperty :?> float
        and set (v : float) = x.SetValue (GraphicsCanvas.MinZoomProperty, box v)

    member private x.OnExtentChanged (oldValue, newValue) =
        x.Coordinate.Extent <- newValue
        x.InvalidateArrange ()

    member private x.OnViewportSizeChanged (oldValue, newValue) =
        x.Coordinate.ViewportSize <- newValue

    member private x.OnViewportCenterChanged (oldValue, newValue) =
        x.Coordinate.ViewportCenter <- newValue
        x.InvalidateArrange ()

    member private x.OnZoomChanged (oldValue, newValue) =
        x.ZoomInverse <- 1.0 / newValue
        x.LayoutTransform <- ScaleTransform (newValue, newValue)

    static member ZoomConverter =
        { new IValueConverter with
            member x.Convert (v, _, p, _) =
                let v : float = Convert.ToDouble v
                let p : float = Convert.ToDouble p
                v * p
                |> box

            member x.ConvertBack (v, _, p, _) =
                let v : float = Convert.ToDouble v
                let p : float = Convert.ToDouble p
                v / p
                |> box }

    override x.MeasureOverride availableSize =
        let availableSize = Size (infinity, infinity)

        x.InternalChildren
        |> Seq.cast<UIElement>
        |> Seq.iter (fun child -> child.Measure availableSize)

        Size ()

    override x.ArrangeOverride finalSize =
        x.ViewportSize <- finalSize
        let origin = x.Coordinate.LogicalToPhysical.ConvertPoint vecz

        x.InternalChildren
        |> Seq.cast<UIElement>
        |> Seq.iter (fun child -> child.Arrange (Rect (origin, child.DesiredSize)))

        x.ScrollOwner
        |> Option.iter (fun sc -> sc.InvalidateScrollInfo ())

        finalSize

    member val CanHorizontallyScroll = true with get, set
    member val CanVerticallyScroll = true with get, set
    member val ScrollOwner : ScrollViewer option = None with get, set

    member x.ExtentWidth  = let extent = x.Coordinate.Extent in extent.Width
    member x.ExtentHeight = let extent = x.Coordinate.Extent in extent.Height
    member x.ViewportWidth  = (let s = x.ViewportSize in s.Width)
    member x.ViewportHeight = (let s = x.ViewportSize in s.Height)
    member x.HorizontalOffset = x.Coordinate.LogicalToScroll.ConvertX (x.Coordinate.ViewportCenter.X)
    member x.VerticalOffset   = x.Coordinate.LogicalToScroll.ConvertY (x.Coordinate.ViewportCenter.Y)

    member x.SetHorizontalOffset offset =
        x.ViewportCenter <-
            vec (x.Coordinate.ScrollToLogical.ConvertX offset)
                (x.Coordinate.ViewportCenter.Y)

    member x.SetVerticalOffset offset =
        x.ViewportCenter <-
            vec (x.Coordinate.ViewportCenter.X)
                (x.Coordinate.ScrollToLogical.ConvertY offset)

    [<CLIEvent>]
    member x.GraphicsCanvasMouseEvent = graphicsCanvasMouseEvent.Publish

    [<CLIEvent>]
    member x.GraphicsCanvasMouseHoverEvent = graphicsCanvasMouseHoverEvent.Publish

    member val private MouseDownButton = None with get, set
    member val private MouseMiddleButtonDownPosition = Point () with get, set

    override x.OnMouseDown e =
        if not x.IsKeyboardFocused then
            Keyboard.Focus x |> ignore
        if not x.IsMouseCaptured then
            if x.CaptureMouse () then
                x.MouseDownButton <- Some e.ChangedButton
                if e.ChangedButton = MouseButton.Middle then
                    x.MouseMiddleButtonDownPosition <- e.GetPosition x
                else
                    graphicsCanvasMouseEvent.Trigger (
                        GraphicsCanvasMouseEventArgs (
                            x.Coordinate.PhysicalToLogical.ConvertPoint (e.GetPosition x),
                            CanvasMouseDownEvent (e.ChangedButton, e.ClickCount)))
                e.Handled <- true
        base.OnMouseDown e

    override x.OnMouseMove e =
        match x.MouseDownButton, x.IsMouseCaptured with
        | Some MouseButton.Middle, true ->
            let newPosition = e.GetPosition x
            let delta = newPosition - x.MouseMiddleButtonDownPosition
            x.SetHorizontalOffset (x.HorizontalOffset - delta.X)
            x.SetVerticalOffset (x.VerticalOffset - delta.Y)
            x.MouseMiddleButtonDownPosition <- newPosition
            e.Handled <- true

        | Some _, true

        | None, false ->
            graphicsCanvasMouseEvent.Trigger (
                GraphicsCanvasMouseEventArgs (
                    x.Coordinate.PhysicalToLogical.ConvertPoint (e.GetPosition x),
                    CanvasMouseMoveEvent))
            e.Handled <- true

        | _, _ -> ()

        base.OnMouseMove e

    override x.OnMouseUp e =
        x.MouseDownButton
        |> Option.iter (fun mouseDownButton ->
            if e.ChangedButton = mouseDownButton then
                x.ReleaseMouseCapture ()
                e.Handled <- true)

        base.OnMouseUp e

    override x.OnLostMouseCapture e =
        x.MouseDownButton
        |> Option.iter (fun mouseDownButton ->
            x.MouseDownButton <- None
            if mouseDownButton <> MouseButton.Middle then
                graphicsCanvasMouseEvent.Trigger (
                    GraphicsCanvasMouseEventArgs (
                        x.Coordinate.PhysicalToLogical.ConvertPoint (e.GetPosition x),
                        CanvasMouseUpEvent)))

        base.OnLostMouseCapture e

    override x.OnMouseWheel e =
        match Keyboard.Modifiers with
        | ModifierKeys.None ->
            x.SetVerticalOffset (x.VerticalOffset - float (sign e.Delta) * x.ViewportHeight * 0.1)
            e.Handled <- true

        | ModifierKeys.Shift ->
            x.SetHorizontalOffset (x.HorizontalOffset - float (sign e.Delta) * x.ViewportWidth * 0.1)
            e.Handled <- true

        | ModifierKeys.Control ->
            let zoom = x.Zoom
            let zooms =
                [| 0.8; 1.0; 1.1; 1.25; 1.5; 1.75; 2.0; 2.5; 3.0; 4.0; 5.0; 6.5; 8.0; 10.0 |]
                |> Array.map (fun v -> v * 10.0 ** floor (log10 zoom))
            let newZoom =
                if e.Delta < 0 then
                    zooms |> Array.findBack (fun v -> v < zoom)
                else
                    zooms |> Array.find (fun v -> v > zoom)
                |> clamp x.MinZoom x.MaxZoom
            let zoomFactor = newZoom / zoom

            let mousePos = x.Coordinate.PhysicalToLogical.ConvertPoint (e.GetPosition x)
            let newViewportCenter = (x.ViewportCenter - mousePos) / zoomFactor + mousePos

            x.Zoom <- newZoom
            x.ViewportCenter <- newViewportCenter

            e.Handled <- true

        | _ -> ()

        base.OnMouseWheel e

    override x.OnMouseEnter e =
        graphicsCanvasMouseHoverEvent.Trigger (
            GraphicsCanvasMouseEventArgs (
                x.Coordinate.PhysicalToLogical.ConvertPoint (e.GetPosition x),
                CanvasMouseEnterEvent))
        base.OnMouseEnter e

    override x.OnMouseLeave e =
        graphicsCanvasMouseHoverEvent.Trigger (
            GraphicsCanvasMouseEventArgs (
                x.Coordinate.PhysicalToLogical.ConvertPoint (e.GetPosition x),
                CanvasMouseLeaveEvent))
        base.OnMouseLeave e

    interface IScrollInfo with
        member x.CanHorizontallyScroll
            with get () = x.CanHorizontallyScroll
            and set v = x.CanHorizontallyScroll <- v

        member x.CanVerticallyScroll
            with get () = x.CanVerticallyScroll
            and set v = x.CanVerticallyScroll <- v

        member x.ScrollOwner
            with get () =
                match x.ScrollOwner with
                | Some sc -> sc
                | None -> null

            and set v =
                x.ScrollOwner <-
                    match v with
                    | null -> None
                    | _ -> Some v

        member x.ExtentHeight = x.ExtentHeight
        member x.ExtentWidth  = x.ExtentWidth
        member x.ViewportHeight = x.ViewportHeight
        member x.ViewportWidth  = x.ViewportWidth
        member x.HorizontalOffset = x.HorizontalOffset
        member x.VerticalOffset   = x.VerticalOffset

        member x.SetHorizontalOffset offset = x.SetHorizontalOffset offset
        member x.SetVerticalOffset offset = x.SetVerticalOffset offset

        member x.LineDown        () = x.SetVerticalOffset   (x.VerticalOffset   + x.ViewportHeight * 0.1)
        member x.LineLeft        () = x.SetHorizontalOffset (x.HorizontalOffset - x.ViewportWidth  * 0.1)
        member x.LineRight       () = x.SetHorizontalOffset (x.HorizontalOffset + x.ViewportWidth  * 0.1)
        member x.LineUp          () = x.SetVerticalOffset   (x.VerticalOffset   - x.ViewportHeight * 0.1)
        member x.MouseWheelDown  () = x.SetVerticalOffset   (x.VerticalOffset   + x.ViewportHeight * 0.1)
        member x.MouseWheelLeft  () = x.SetHorizontalOffset (x.HorizontalOffset - x.ViewportWidth  * 0.1)
        member x.MouseWheelRight () = x.SetHorizontalOffset (x.HorizontalOffset + x.ViewportWidth  * 0.1)
        member x.MouseWheelUp    () = x.SetVerticalOffset   (x.VerticalOffset   - x.ViewportHeight * 0.1)
        member x.PageDown        () = x.SetVerticalOffset   (x.VerticalOffset   + x.ViewportHeight * 0.9)
        member x.PageLeft        () = x.SetHorizontalOffset (x.HorizontalOffset - x.ViewportWidth  * 0.9)
        member x.PageRight       () = x.SetHorizontalOffset (x.HorizontalOffset + x.ViewportWidth  * 0.9)
        member x.PageUp          () = x.SetVerticalOffset   (x.VerticalOffset   - x.ViewportHeight * 0.9)

        member x.MakeVisible (visual, rectangle) = Rect ()


