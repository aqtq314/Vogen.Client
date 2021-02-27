namespace Doaz.Reactive.Controls

open Doaz.Reactive.Math
open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Media


type BehaviorMouseEventType =
    | MouseDownEvent of button : MouseButton * clickCount : int
    | MouseMoveEvent
    | MouseUpEvent of button : MouseButton

type BehaviorMouseEventArgs (pos, eventType, setHandled) =
    member x.Position : VectorF = pos
    member x.EventType : BehaviorMouseEventType = eventType
    member x.SetHandled () : unit = setHandled ()


type BehaviorPreviewMouseEventType =
    | PreviewMouseDownEvent of button : MouseButton * clickCount : int
    | PreviewMouseMoveEvent
    | PreviewMouseUpEvent of button : MouseButton

type BehaviorPreviewMouseEventArgs (pos, eventType, setHandled) =
    member x.Position : VectorF = pos
    member x.EventType : BehaviorPreviewMouseEventType = eventType
    member x.SetHandled () : unit = setHandled ()


type BehaviorKeyboardEventType =
    | KeyDownEvent
    | KeyUpEvent

type BehaviorKeyboardEventArgs (e, eventType) =
    member x.EventArgs : KeyEventArgs = e
    member x.EventType : BehaviorKeyboardEventType = eventType


type BehaviorPreviewKeyboardEventType =
    | PreviewKeyDownEvent
    | PreviewKeyUpEvent

type BehaviorPreviewKeyboardEventArgs (e, eventType) =
    member x.EventArgs : KeyEventArgs = e
    member x.EventType : BehaviorPreviewKeyboardEventType = eventType


type BehaviorDragEventType =
    | DragStartEvent of offset : VectorF
    | DragDeltaEvent of delta : VectorF
    | DragCompletedEvent

type BehaviorDragEventArgs (modifierKeys, eventType) =
    member x.Modifiers : ModifierKeys = modifierKeys
    member x.EventType : BehaviorDragEventType = eventType


type BehaviorMouseWheelEventArgs (delta, position, setHandled) =
    member x.Delta : int = delta
    member x.Position : Point = position
    member x.SetHandled () : unit = setHandled ()


type BehaviorClickEventArgs () =
    class end


type BehaviorMouseHoverEventType =
    | MouseEnterEvent
    | MouseLeaveEvent

type BehaviorMouseHoverEventArgs (isMouseOver, eventType) =
    member x.IsMouseOver : bool = isMouseOver
    member x.EventType : BehaviorMouseHoverEventType = eventType


type BehaviorValueChangedEventArgs<'a> (value) =
    member x.Value : 'a = value


type BehaviorCheckEventArgs () =
    class end


