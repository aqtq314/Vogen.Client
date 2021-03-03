module Vogen.Client.ViewModel.DesignerModels

open Newtonsoft.Json
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Windows
open Vogen.Client.Controls
open Vogen.Client.Model


let comp, audioLib =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream @"Vogen.Client.ViewModel.testComp.vog"
    FilePackage.read stream

let programModel = ProgramModel()
do  programModel.Load comp audioLib


