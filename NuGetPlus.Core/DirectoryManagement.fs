module NuGetPlus.DirectoryManagement

open System.IO
open System.Collections.Concurrent
open NuGet

let GetProjects dir = 
    let baseDirectory = DirectoryInfo(dir)
    baseDirectory.EnumerateFiles("*.*proj", SearchOption.AllDirectories)
    |> Seq.map (fun fi -> fi.FullName)

let GetRestorePackages dir = 
    GetProjects dir
    |> Seq.map
           (fun projName -> 
               let repoPath = GetRepositoryPath projName
               ProjectManagement.GetRestorePackages projName 
               |> Seq.map(fun package -> repoPath, package))
    |> Seq.concat

let RestorePackages dir = 
    let packages = GetRestorePackages dir |> Set.ofSeq
    BatchOperations.RestorePackages packages