namespace Vogen.Client.ViewModel

open Doaz.Reactive
open System
open System.Collections.Generic
open System.Collections.Immutable


type UndoNodeDescription =
    | WriteNote
    | MouseMoveNote
    | MouseResizeNoteLeft
    | MouseResizeNoteRight
    | DeleteNote
    | CutNote
    | PasteNote
    | ModifyNoteContent
    | KeyboardNudgeNote

type UndoRedoStack<'TComp>() =
    let undoStack = rp([] : (UndoNodeDescription * 'TComp * 'TComp) list)
    let redoStack = rp []

    member x.UndoStack = undoStack :> ReactiveProperty<_>
    member x.RedoStack = redoStack :> ReactiveProperty<_>
    member val CanUndo = undoStack |> Rpo.map(fun undoStack -> not(List.isEmpty undoStack))
    member val CanRedo = redoStack |> Rpo.map(fun redoStack -> not(List.isEmpty redoStack))

    member x.PopUndo() =
        match !!undoStack with
        | [] -> raise(InvalidOperationException("Undo stack is empty."))
        | (_, undoComp, _) as node :: undoStackCont ->
            undoStack |> Rp.set undoStackCont
            redoStack |> Rp.set(node :: !!redoStack)
            undoComp

    member x.PopRedo() =
        match !!redoStack with
        | [] -> raise(InvalidOperationException("Redo stack is empty."))
        | (_, _, redoComp) as node :: redoStackCont ->
            undoStack |> Rp.set(node :: !!undoStack)
            redoStack |> Rp.set redoStackCont
            redoComp

    member x.PushUndo(nodeDesc, undoComp, redoComp) =
        undoStack |> Rp.set((nodeDesc, undoComp, redoComp) :: !!undoStack)
        redoStack |> Rp.set []

    member x.UpdateLatestRedo redoComp =
        match !!undoStack with
        | [] -> ()
        | (nodeDesc, undoComp, _) :: undoStackCont ->
            undoStack |> Rp.set((nodeDesc, undoComp, redoComp) :: undoStackCont)

    member x.Clear() =
        undoStack |> Rp.set []
        redoStack |> Rp.set []


