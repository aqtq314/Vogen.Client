namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.Immutable
open Vogen.Client.Controls


type UndoNodeDescription =
    | WriteNote
    | MouseDragNote of noteDragType : NoteDragType
    | DeleteNote
    | CutNote
    | PasteNote
    | ModifyNoteContent
    | KeyboardNudgeNote

type UndoRedoStack<'a>() =
    let undoStack = rp([] : (UndoNodeDescription * 'a * 'a) list)
    let redoStack = rp []

    member x.UndoStack = undoStack :> ReactiveProperty<_>
    member x.RedoStack = redoStack :> ReactiveProperty<_>
    member val CanUndo = undoStack |> Rpo.map(fun undoStack -> not(List.isEmpty undoStack))
    member val CanRedo = redoStack |> Rpo.map(fun redoStack -> not(List.isEmpty redoStack))

    member x.TryPopUndo() =
        match !!undoStack with
        | [] -> None
        | (_, undoItem, _) as node :: undoStackCont ->
            undoStack |> Rp.set undoStackCont
            redoStack |> Rp.set(node :: !!redoStack)
            Some undoItem

    member x.TryPopRedo() =
        match !!redoStack with
        | [] -> None
        | (_, _, redoItem) as node :: redoStackCont ->
            undoStack |> Rp.set(node :: !!undoStack)
            redoStack |> Rp.set redoStackCont
            Some redoItem

    member x.PushUndo(nodeDesc, undoItem, redoItem) =
        undoStack |> Rp.set((nodeDesc, undoItem, redoItem) :: !!undoStack)
        redoStack |> Rp.set []

    member x.UpdateLatestRedo redoItem =
        match !!undoStack with
        | [] -> ()
        | (nodeDesc, undoItem, _) :: undoStackCont ->
            undoStack |> Rp.set((nodeDesc, undoItem, redoItem) :: undoStackCont)

    member x.Clear() =
        undoStack |> Rp.set []
        redoStack |> Rp.set []


