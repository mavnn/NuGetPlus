[<AutoOpen>]
module NuGetPlus.TickSpec.Tests.TestUtilities
open System
open System.IO

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
