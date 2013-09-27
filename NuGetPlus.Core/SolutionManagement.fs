module NuGetPlus.SolutionManagement

open System.IO
open System.Text.RegularExpressions
open System.Collections.Concurrent
open NuGet

let ProjectExtractor = 
    Regex
        (@"Project\(""\{.*\}""\) = "".*"", ""(?<proj>.*\..*proj)"", "".*""", 
         RegexOptions.Compiled)

let GetProjects sln = 
    let baseDirectory = FileInfo(sln).DirectoryName
    let lines = File.ReadAllLines(sln)
    lines
    |> Seq.filter(fun line -> line.StartsWith("Project"))
    |> Seq.filter ProjectExtractor.IsMatch
    |> Seq.map
           (fun line -> 
               Path.Combine
                   (baseDirectory, 
                    ProjectExtractor.Match(line).Groups.["proj"].Value))

let GetRestorePackages sln = 
    GetProjects sln
    |> Seq.map
           (fun projName -> 
               let repoPath = GetRepositoryPath projName
               ProjectManagement.GetRestorePackages projName 
               |> Seq.map(fun package -> repoPath, package))
    |> Seq.concat

let RestorePackages sln = 
    let packages = GetRestorePackages sln |> Set.ofSeq
    BatchOperations.RestorePackages packages

let Scan =
    GetRestorePackages
    >> Seq.map (fun (_, p) -> p)
    >> BatchOperations.ScanPackages
