module Vogen.Client.Views.Commands

open Doaz.Reactive
open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open Vogen.Client.Controls
open Vogen.Client.Model
open Vogen.Client.ViewModel


let New = RoutedUICommand()
let Open = RoutedUICommand()
let Save = RoutedUICommand()
let SaveAs = RoutedUICommand()
let Import = RoutedUICommand()
let Export = RoutedUICommand()
let Exit = RoutedUICommand()

let Undo = RoutedUICommand()
let Redo = RoutedUICommand()
let Cut = RoutedUICommand()
let Copy = RoutedUICommand()
let Paste = RoutedUICommand()
let Delete = RoutedUICommand()
let SelectAll = RoutedUICommand()
let BlurUtt = RoutedUICommand()

let SetGrid = RoutedUICommand()

let EditTimeSig = RoutedUICommand()
let EditTempo = RoutedUICommand()
let EditLyrics = RoutedUICommand()
let SetUttLanguage = RoutedUICommand()
let SetUttSinger = RoutedUICommand()

let Synth = RoutedUICommand()
let Resynth = RoutedUICommand()
let ClearSynth = RoutedUICommand()
let PlayStop = RoutedUICommand()
let Play = RoutedUICommand()
let Stop = RoutedUICommand()


