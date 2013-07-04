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
    |> Seq.map
           (fun line -> 
               Path.Combine
                   (baseDirectory, 
                    ProjectExtractor.Match(line).Groups.["proj"].Value))

type private PackageInRepo = 
    { RepoPath : string;
      Package : RestorePackage;
      Projects : List<string> }

let GetRestorePackages sln = 
    GetProjects sln
    |> Seq.map
           (fun projName -> 
               let repoPath = GetRepositoryPath projName
               ProjectManagement.GetRestorePackages projName 
               |> Seq.map(fun package -> repoPath, package))
    |> Seq.concat

type private RepositoryInfo = 
    { RepoPath : RepositoryPath;
      Manager : PackageManager;
      Queue : BlockingCollection<RestorePackage> }

let RestorePackages sln = 
    let slnDir = Path.GetFullPath <| Path.GetDirectoryName sln
    let settings = Settings.LoadDefaultSettings(PhysicalFileSystem sln)
    let packages = GetRestorePackages sln |> Set.ofSeq
    let repositories = 
        packages
        |> Seq.map(fun (repoPath, _) -> repoPath)
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
                         Manager = (GetRawManager repo settings);
                         Queue = new BlockingCollection<RestorePackage>() } map) 
               Map.empty
    packages 
    |> Set.iter
           (fun (repoPath, package) -> managers.[repoPath].Queue.Add package)
    repositories
    |> Seq.map (fun repo ->
        let rec inner () =
            async {
                let package = managers.[repo].Queue.Take()
                managers.[repo].Manager.InstallPackage(package.Id, package.Version, true, true)
                let count = lock lockCounter (
                                                fun () ->
                                                    let count = !counter - 1
                                                    counter := count
                                                    count
                                                )
                if count = 0 then
                    completeQueue.Add("")
                else
                    return! inner ()
            }
        inner ())
    |> Seq.iter Async.Start
    completeQueue.Take() |> ignore