module Vogen.Client.ViewModel.AutoUpdater

open Doaz.Reactive
open FSharp.Data
open FSharp.Text
open Newtonsoft.Json
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Net
open System.Reflection
open System.Threading.Tasks
open System.Text
open System.Text.RegularExpressions


type GitHubReleases = JsonProvider<"gitReleaseJsons\\github-releases.json">
type GiteeReleases = JsonProvider<"gitReleaseJsons\\gitee-releases.json">
let cacheDir = Path.GetFullPath "autoUpdateCache"

let parseVersion versionText =
    let m = Regex.Match(versionText, @"^v((?<major>\d+))?(\.(?<minor>\d+))?(\.(?<build>\d+))?(\.(?<rev>\d+))?(\-(?<preRelease>[a-z]+))?$")
    if m.Success then
        let major = Int32.TryParse m.Groups.["major"].Value |> Option.ofByRef |> Option.defaultValue 0
        let minor = Int32.TryParse m.Groups.["minor"].Value |> Option.ofByRef |> Option.defaultValue 0
        let build = Int32.TryParse m.Groups.["build"].Value |> Option.ofByRef |> Option.defaultValue 0
        let rev   = Int32.TryParse m.Groups.["rev"].Value   |> Option.ofByRef |> Option.defaultValue 0
        let preReleaseStr = m.Groups.["preRelease"].Value
        Some((major, minor, build, rev), preReleaseStr)
    else
        None

let rec checkGitHubUpdates appVersion = async {
    try let! releaseData = GitHubReleases.AsyncGetSamples()
        let releases =
            releaseData
            |> Array.choose(fun releaseSample -> maybe {
                let! version, preReleaseStr = parseVersion releaseSample.TagName
                let! downloadUrl, fileName = releaseSample.Assets |> Array.tryPick(fun asset -> maybe {
                    let! downloadUrl = Some asset.BrowserDownloadUrl |> Option.filter(not << String.IsNullOrEmpty)
                    let! fileName = Some asset.Name |> Option.filter(not << String.IsNullOrEmpty)
                    return downloadUrl, fileName })
                return
                    {|  Version = version; TagName = releaseSample.TagName; PreRelease = releaseSample.Prerelease
                        DownloadUrl = downloadUrl; FileName = fileName |} })
        return findNewerRelease appVersion releases
    with ex ->
        return None }

and checkGiteeUpdates appVersion = async {
    try let! releaseData = GiteeReleases.AsyncGetSamples()
        let releases =
            releaseData
            |> Array.choose(fun releaseSample -> maybe {
                let! version, preReleaseStr = parseVersion releaseSample.TagName
                let! downloadUrl, fileName = releaseSample.Assets |> Array.tryPick(fun asset -> maybe {
                    let! downloadUrl = Some asset.BrowserDownloadUrl |> Option.filter(not << String.IsNullOrEmpty)
                    let! fileName = asset.Name |> Option.filter(not << String.IsNullOrEmpty)
                    return downloadUrl, fileName })
                return
                    {|  Version = version; TagName = releaseSample.TagName; PreRelease = releaseSample.Prerelease
                        DownloadUrl = downloadUrl; FileName = fileName |} })
        return findNewerRelease appVersion releases
    with ex ->
        return None }

and findNewerRelease appVersion releases =
    releases
    |> Seq.filter(fun release -> not release.PreRelease)
    |> Seq.sortByDescending(fun release -> release.Version)
    |> Seq.tryHead
    |> Option.filter(fun release -> release.Version > appVersion)

let checkUpdates() =
    let entryAssembly = Assembly.GetEntryAssembly()
    let appVersionStr = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
    let appVersion, preReleaseStr = parseVersion $"v{appVersionStr}" |> Option.get
    async {
        let! newReleaseOp = Async.Choice [| checkGitHubUpdates appVersion; checkGiteeUpdates appVersion |]
        match newReleaseOp with
        | None ->
            return new Task(Action(ignore))
        | Some newRelease ->
            let _ = Directory.CreateDirectory cacheDir
            let cacheFilePath = Path.Join(cacheDir, newRelease.FileName)
            let tempFilePath = Path.Join(cacheDir, $"__temp.{Path.GetRandomFileName()}.{newRelease.FileName}")
            if not(File.Exists cacheFilePath) then
                use webc = new WebClient()
                do! webc.AsyncDownloadFile(Uri(newRelease.DownloadUrl), tempFilePath)
                File.Move(tempFilePath, cacheFilePath, true)
            return new Task(fun () ->
                Process.Start(@"powershell", String.Join("; ", [|
                    $@"Write-Host '正在等待解压缩{newRelease.FileName}...'"
                    $@"wait-process vogen.client -erroraction silentlycontinue"
                    $@"expand-archive -literalpath {cacheFilePath} {appDir} -force"
                    $@"Write-Host '已成功更新至{newRelease.TagName}，按任意键退出。'"
                    $@"$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')" |])) |> ignore) }
    |> Async.StartAsTask


