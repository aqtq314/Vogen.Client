module Vogen.Client.ViewModel.DesignerModels

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Windows
open Vogen.Client.JsonModels


let jComp =
    use reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream @"Vogen.Client.ViewModel.testComp.json")
    reader.ReadToEnd()
    |> JsonConvert.DeserializeObject<Comp>

let workspace = WorkspaceModel()
do  workspace.LoadFromJson jComp


