#r    "./tools/fake/fakelib.dll"
open Fake
open System.IO
open System.Text.RegularExpressions

let packExe () =
    let tools = environVar "tools"
    let versionRegex = Regex(@"AssemblyInformationalVersion\(\""(?<version>.*)\""\)")
    let version = 
        ReadFileAsString ("NuGetPlus.Console" @@ "AssemblyInfo.fs")
        |> versionRegex.Match
        |> (fun m -> m.Groups.["version"].Value)
    let setNugetParams param =
         { param with 
                    Version = version
                    Project = "NuGetPlus"
                    OutputPath = "." @@ "output"
                    WorkingDir = "."
                    ToolPath = tools @@ "NuGet" @@ "NuGet.exe"
                    AccessKey = environVarOrDefault "apikey" ""
                    PublishUrl = environVarOrDefault "pushurl" ""
                    NoPackageAnalysis = true
                    Publish = not isLocalBuild }
    NuGetPack setNugetParams ("NuGetPlus" @@ "ngp.nuspec")
    if not isLocalBuild then
        NuGetPublish setNugetParams

let Default = Target "Default" packExe

RunParameterTargetOrDefault "Target" "Default"
