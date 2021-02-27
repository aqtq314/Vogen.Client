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


type RenderControl() =
    inherit FrameworkElement()

    static member val RenderProperty =
        Dp.reg<Action<DrawingContext>, RenderControl> "Render"
            (Dp.Meta (Action<_>(ignore), Dp.MetaFlags.AffectsRender))

    member x.Render
        with get () = x.GetValue RenderControl.RenderProperty :?> Action<DrawingContext>
        and set (v : Action<DrawingContext>) = x.SetValue (RenderControl.RenderProperty, box v)

    override x.OnRender dc =
        base.OnRender dc
        x.Render.Invoke dc


