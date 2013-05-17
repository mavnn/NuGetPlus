module ReferenceManagement
open System
open NuGet
open Microsoft.Build.Evaluation

type Managers =
    {
        PackageManager : PackageManager
        ProjectManager : ProjectManager
    }

let GetManagers (projectName : string) =
    if String.IsNullOrWhiteSpace projectName then raise <| ArgumentException("projectName cannot be empty")
    let projectSystem = new ProjectSystem(projectName)
    let fileSystem = new PhysicalFileSystem(IO.Path.GetFullPath <| IO.Path.GetDirectoryName projectName)
    let (settings : ISettings) = Settings.LoadDefaultSettings(fileSystem)
    let repositoryPath = settings.GetValue("config", "repositoryPath")
    printfn "repo path: %s" repositoryPath
    let sharedRepositoryFileSystem = new PhysicalFileSystem(repositoryPath)
    let pathResolver = new DefaultPackagePathResolver(repositoryPath)
    let defaultPackageSource = new PackageSource("https://nuget.org/api/v2/")
    let packageSourceProvider = new PackageSourceProvider(settings, [defaultPackageSource])
    let remoteRepository = packageSourceProvider.GetAggregate(new PackageRepositoryFactory())
    let sharedPackageRepository = new SharedPackageRepository(pathResolver, sharedRepositoryFileSystem, fileSystem)
    let localRepository = new PackageReferenceRepository(projectSystem, sharedPackageRepository)
    {
        PackageManager = new PackageManager(remoteRepository, pathResolver, fileSystem, localRepository)
        ProjectManager = new ProjectManager(remoteRepository, pathResolver, projectSystem, localRepository)
    }

let UpdateReferenceToSpecificVersion projectName packageId (version : SemanticVersion) =
    let pm = (GetManagers projectName).PackageManager
    pm.UpdatePackage(packageId, version, true, true)

let UpdateReference projectName (packageId : string) =
    let pm = (GetManagers projectName).PackageManager
    pm.UpdatePackage(packageId, true, false)

let InstallReferenceOfSpecificVersion projectName packageId (version : SemanticVersion) =
    let managers = GetManagers projectName
    managers.PackageManager.InstallPackage(packageId, version, true, true)
    managers.ProjectManager.AddPackageReference(packageId, version, false, true)

let InstallReference projectName packageId =
    let managers = GetManagers projectName
    managers.PackageManager.InstallPackage packageId
    managers.ProjectManager.AddPackageReference packageId

let RemoveReference projectName (packageId : string) =
    let managers = GetManagers projectName
    managers.ProjectManager.RemovePackageReference packageId
    managers.PackageManager.UninstallPackage packageId
