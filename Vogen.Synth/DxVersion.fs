module Vogen.Synth.DxVersion

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime.InteropServices


[<Struct; StructLayout(LayoutKind.Sequential)>]
type DXDIAG_INIT_PARAMS =
    val mutable dwSize : int
    val mutable dwDxDiagHeaderVersion : uint
    val mutable bAllowWHQLChecks : bool
    val mutable pReserved : nativeint

[<ComImport>]
[<Guid("7D0F462F-4064-4862-BC7F-933E5058C10F")>]
[<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>]
[<AllowNullLiteral>]
type IDxDiagContainer =
    abstract EnumChildContainerNames : dwIndex : uint * pwszContainer : string * cchContainer : uint -> unit
    abstract EnumPropNames : dwIndex : uint * pwszPropName : string * cchPropName : uint -> unit
    abstract GetChildContainer : pwszContainer : string * [<Out>] ppInstance : byref<IDxDiagContainer> -> unit
    abstract GetNumberOfChildContainers : [<Out>] pdwCount : byref<uint> -> unit
    abstract GetNumberOfProps : [<Out>] pdwCount : byref<uint> -> unit
    abstract GetProp : pwszPropName : string * [<Out>] pvarProp : byref<obj> -> unit

[<ComImport>]
[<Guid("9C6B4CB0-23F8-49CC-A3ED-45A55000A6D2")>]
[<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>]
[<AllowNullLiteral>]
type IDxDiagProvider =
    abstract Initialize : [<In; Out>] pParams : byref<DXDIAG_INIT_PARAMS> -> unit
    abstract GetRootContainer : [<Out>] ppInstance : outref<IDxDiagContainer> -> unit

[<AutoOpen>]
module DxDiagContainerExtensions =
    type IDxDiagContainer with
        member x.GetProperty<'a> pwszPropName =
            let variant = x.GetProp pwszPropName
            Convert.ChangeType(variant, typeof<'a>) :?> 'a

let getDxVersion() =
    let typeDxDiagProvider = Type.GetTypeFromCLSID(Guid "A65B8071-3BFE-4213-9A5B-491DA4461CA7")
    let provider0 = Activator.CreateInstance typeDxDiagProvider
    let provider = Activator.CreateInstance typeDxDiagProvider :?> IDxDiagProvider

    let initParams =
        DXDIAG_INIT_PARAMS(
            dwSize = Marshal.SizeOf<DXDIAG_INIT_PARAMS>(),
            dwDxDiagHeaderVersion = 111u)
    provider.Initialize(ref initParams)

    let rootContainer = provider.GetRootContainer()
    let systemInfoContainer = rootContainer.GetChildContainer "DxDiag_SystemInfo"

    let versionMajor = systemInfoContainer.GetProperty<int> "dwDirectXVersionMajor"
    let versionMinor = systemInfoContainer.GetProperty<int> "dwDirectXVersionMinor"
    let versionLetter = systemInfoContainer.GetProperty<string> "szDirectXVersionLetter"
    let isDebug = systemInfoContainer.GetProperty<bool> "bDebug"

    //let _ = Marshal.ReleaseComObject systemInfoContainer
    //let _ = Marshal.ReleaseComObject rootContainer
    //let _ = Marshal.ReleaseComObject provider

    //printfn "DirectX Version: %A" (Version(versionMajor, versionMinor))
    Version(versionMajor, versionMinor)


