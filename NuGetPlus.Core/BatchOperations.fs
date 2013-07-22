module NuGetPlus.BatchOperations

open System.IO
open System.Collections.Concurrent
open NuGet

type PackageInRepo = 
    { RepoPath : string;
      Package : RestorePackage;
      Projects : List<string> }

type RepositoryInfo = 
    { RepoPath : RepositoryPath;
      Manager : PackageManager;
      Queue : BlockingCollection<RestorePackage> }

let ScanPackages packages = 
    packages
    |> Seq.groupBy(fun p -> p.Id)
    |> Seq.map(fun (id, packages) -> 
               (id, 
                packages
                |> Seq.map(fun p -> p.Version)
                |> Seq.distinct
                |> Seq.sort))
    |> Seq.filter(fun (id, versions) -> Seq.length versions > 1)

let RestorePackages packages = 
    let repositories = 
        packages
        |> Set.map(fun (repoPath, _) -> repoPath)
        |> Set.ofSeq
    let counter = ref(Set.count packages)
    let lockCounter = obj()
    use completeQueue = new BlockingCollection<string>()
    let managers = 
        repositories 
        |> Set.fold 
               (fun map repo -> 
                   Map.add repo 
                       { RepoPath = repo;
                         Manager = 
                             (GetRawManager repo 
                              <| Settings.LoadDefaultSettings
                                     (PhysicalFileSystem
                                          (match repo with
                                           | RepositoryPath r -> r)));
                         Queue = new BlockingCollection<RestorePackage>() } map) 
               Map.empty
    packages 
    |> Set.iter
           (fun (repoPath, package) -> managers.[repoPath].Queue.Add package)
    repositories
    |> Seq.map(fun repo -> 
               let rec inner() = 
                   async { 
                       let package = managers.[repo].Queue.Take()
                       managers.[repo]
                           .Manager.InstallPackage(package.Id, package.Version, 
                                                   true, true)
                       let count = 
                           lock lockCounter (fun () -> 
                                   let count = !counter - 1
                                   counter := count
                                   count)
                       if count = 0 then completeQueue.Add("")
                       else return! inner() }
               inner())
    |> Seq.iter Async.Start
    completeQueue.Take() |> ignore