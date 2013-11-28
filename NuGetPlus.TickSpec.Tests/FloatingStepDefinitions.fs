[<TickSpec.StepScope(Feature = "Floating operations should work")>]
module NuGetPlus.TickSpec.Tests.FloatingStepDefinitions

open System
open System.IO
open FsUnit
open TickSpec
open NUnit.Framework
open NuGet
open NuGetPlus.Floating
open TestUtilities

let getWorkingTestDir () =
    Path.Combine(workingDir.FullName, Guid.NewGuid().ToString())

type State =
    {
        WorkingDirectory : string
        Package : string
        Version : Option<SemanticVersion>
    }

let mutable state = { WorkingDirectory = ""; Package = ""; Version = None }

[<AfterScenario>]
let clean () =
    Directory.Delete(state.WorkingDirectory, true)
    state <- { WorkingDirectory = ""; Package = ""; Version = None }

[<Given>]
let ``a floating directory`` () =
    let workingDir = getWorkingTestDir ()
    Directory.CreateDirectory(workingDir) |> ignore
    let nugetConfig = 
        FileInfo(Path.Combine(testProjectDir.FullName, "nuget.config"))
    let destination = Path.Combine(workingDir, "nuget.config")
    if not <| File.Exists destination then 
        nugetConfig.CopyTo(destination) |> ignore
    state <- { state with WorkingDirectory = workingDir }    

[<When>]
let ``I install (\S+) with versioned directory`` (package : string) =
    Install state.WorkingDirectory id package
    state <- { state with Package = package }

[<When>]
let ``I install (\S+), version (\S+) with(|out)( | no dependencies and a )versioned directory``
        (package : string) (version : string) (versioned : string) (deps : string) =
    let ignoreDeps =
        match deps with
        | " no dependencies and a " -> true
        | _ -> false
    let versionedDir =
        match versioned with
        | "out" -> false
        | _ -> true
    Install state.WorkingDirectory
        (fun def -> 
                    { def with 
                            SemanticVersion = Some (SemanticVersion(version))
                            IgnoreDependencies = ignoreDeps
                            VersionedDirectory = versionedDir }) package
    state <- { state with Package = package; Version = Some (SemanticVersion(version)) }


let checkInstalled directory (package : string) postFix =
    DirectoryInfo(directory).GetDirectories()
    |> Seq.exists (fun d -> d.Name.ToLowerInvariant().StartsWith(package.ToLowerInvariant() + postFix))

let checkInstalledWithoutVersion directory (package : string) =
    DirectoryInfo(directory).GetDirectories()
    |> Seq.exists (fun d -> d.Name.ToLowerInvariant() = package.ToLowerInvariant())

[<Then>]
let ``the package should be installed with(|out) a versioned directory`` (versioned : string) =
    match versioned with
    | "out" ->
        checkInstalledWithoutVersion state.WorkingDirectory state.Package
    | _ ->
        let postFix =
            match state.Version with
            | None -> "."
            | Some v -> "." + v.ToString()
        checkInstalled state.WorkingDirectory state.Package postFix
    |> should be True

[<Then>]
let ``it's dependency (\S+) should( | not )be installed`` (package : string) (shouldBe : string) =
    let desired =
        match shouldBe with
        | " not " -> should be False
        | _ -> should be True
    checkInstalled state.WorkingDirectory package ""
    |> desired