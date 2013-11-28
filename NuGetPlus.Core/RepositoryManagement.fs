[<AutoOpen>]
module NuGetPlus.RepositoryManagement

open System
open System.IO
open NuGet

let private inferRepositoryDirectory projectDir = 
    let rec inner(currentDir : DirectoryInfo) = 
        if Array.length(currentDir.GetFiles("*.sln")) > 0 then Some currentDir
        else if currentDir.Parent <> null then inner currentDir.Parent
        else None
    let solutionDir = inner <| DirectoryInfo(projectDir)
    match solutionDir with
    | Some dir -> Path.Combine(dir.FullName, "packages")
    | None -> Path.Combine(projectDir, "packages")

type RepositoryPath = | RepositoryPath of string

let GetRepositoryPath projectName = 
    let projectDir = Path.GetFullPath projectName |> Path.GetDirectoryName
    let settings = Settings.LoadDefaultSettings(PhysicalFileSystem projectDir)
    match settings.GetRepositoryPath() with
    | null -> RepositoryPath <| inferRepositoryDirectory projectDir
    | s -> RepositoryPath s

let private getManager (RepositoryPath repositoryPath) (settings : ISettings) local = 
    if String.IsNullOrWhiteSpace repositoryPath then 
        raise <| ArgumentException("Repository Path cannot be empty")
    printfn "repo path: %s" repositoryPath
    let defaultPackageSource = PackageSource "https://nuget.org/api/v2/"
    let packageSourceProvider = 
        PackageSourceProvider(settings, [defaultPackageSource])
    let remoteRepository = 
        packageSourceProvider.GetAggregate(PackageRepositoryFactory())
    let localRepository = local repositoryPath
    let logger = 
        { new ILogger with
              member x.Log(level, message, parameters) = 
                  Console.WriteLine
                      ("[{0}] {1}", level.ToString(), 
                       String.Format(message, parameters))
              member x.ResolveFileConflict(message) = FileConflictResolution() }
    let packageManager = 
        PackageManager
            (remoteRepository, 
             DefaultPackagePathResolver(PhysicalFileSystem repositoryPath), 
             PhysicalFileSystem repositoryPath, localRepository)
    packageManager.Logger <- logger
    packageManager

let GetRawManager repositoryPath settings = 
    getManager repositoryPath settings (fun rp -> SharedPackageRepository(rp))

let GetFlatManager repositoryPath settings =
    getManager repositoryPath settings (fun rp -> LocalPackageRepository(DefaultPackagePathResolver(rp, false), PhysicalFileSystem(rp)))

type RestorePackage = 
    { Id : string;
      Version : SemanticVersion }
