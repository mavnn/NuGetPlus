module NuGetPlus.TickSpec.Tests.ProjectDefinitions

open TickSpec
open NUnit.Framework
open System
open System.IO
open ReferenceManagement

let testProjectDir =
    let TestProjectDirName = "TestProjects"
    let rec inner (dir : DirectoryInfo) = 
        if Seq.length <| dir.EnumerateDirectories(TestProjectDirName) > 0 then
            DirectoryInfo(Path.Combine(dir.FullName, TestProjectDirName))
        else
            if dir.Parent = null then
                failwith "Could not find the TestProjects"
            else
                inner dir.Parent
    inner (DirectoryInfo("."))

let workingDir = DirectoryInfo(Path.Combine(testProjectDir.FullName, "WorkingDirectory"))

let ensureWorkingDirectory () =
    if not workingDir.Exists then
        workingDir.Create()

let packagesDir = DirectoryInfo(Path.Combine(testProjectDir.FullName, "packages"))

let cleanWorkingPackagesDirectory () =
    if packagesDir.Exists then
        packagesDir.Delete(true)

type State =
    {
        project : FileInfo
        package : string
    }

let mutable state = { project = FileInfo("."); package = "" }

let [<Given>] ``a (.*) with (packages|no packages)`` (projType:string) (hasPackages:string) = 
    let midFix =
        match hasPackages with
        | "packages" -> ".WithPackages."
        | "no packages" -> ".NoPackages."
        | _ -> failwith "Unknown package option"
    let example = 
        testProjectDir.GetFiles("*.*", SearchOption.AllDirectories)
        |> Seq.filter (fun fi -> fi.Name = projType + midFix + (projType.ToLower()))
        |> Seq.head
    ensureWorkingDirectory ()
    cleanWorkingPackagesDirectory ()
    let destination = Path.Combine(workingDir.FullName, Guid.NewGuid().ToString(), example.Name)
    let projDir = Directory.CreateDirectory(Path.GetDirectoryName destination)
    let nugetConfig = FileInfo(Path.Combine(testProjectDir.FullName, "nuget.config"))
    nugetConfig.CopyTo(Path.Combine(projDir.FullName, "nuget.config")) |> ignore
    state <- { state with project = example.CopyTo destination }
      
let [<When>] ``I install (.*)`` (packageId:string) =  
    state <- { state with package = packageId }
    InstallReference state.project.FullName packageId

let [<When>] ``remove (.*)`` (packageId:string) =  
    state <- { state with package = packageId }
    RemoveReference state.project.FullName packageId
      
let [<Then>] ``the package (should|should not) be installed in the right directory`` (should : string) =
    let packagesDir = DirectoryInfo(Path.Combine(state.project.Directory.FullName, "packages"))
    if packagesDir.Exists then
        let isDir = packagesDir.GetDirectories() |> Seq.exists (fun di -> di.Name.StartsWith state.package)
        match should with
        | "should" -> Assert.IsTrue isDir
        | "should not" -> Assert.IsFalse isDir
        | _ -> failwith "Unknown should option"
    else
        match should with
        | "should" -> Assert.Fail()
        | "should not" -> ()
        | _ -> failwith "Unknown should option"

let [<Then>] ``the reference (should|should not) be added to the project file`` (should : string) =     
    state.project.OpenText().ReadToEnd().Contains(state.package)
    |>
        match should with
        | "should" -> Assert.IsTrue
        | "should not" -> Assert.IsFalse
        | _ -> failwith "Unknown should option"

let [<Then>] ``the package (should|should not) be added to the packages.config file`` (should : string) =     
    let packagesConfig = FileInfo(Path.Combine(state.project.Directory.FullName, "packages.config"))
    packagesConfig.OpenText().ReadToEnd().Contains(state.package)
    |>
        match should with
        | "should" -> Assert.IsTrue
        | "should not" -> Assert.IsFalse
        | _ -> failwith "Unknown should option"