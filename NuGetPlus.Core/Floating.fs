module NuGetPlus.Floating

open NuGet
open NuGetPlus.RepositoryManagement

type FloatingOptions =
    {
        IgnoreDependencies : bool
        AllowPrerelease : bool
        SemanticVersion : Option<SemanticVersion>
        VersionedDirectory : bool
    }

let DefaultOptions =
    {
        IgnoreDependencies = false
        AllowPrerelease = false
        SemanticVersion = None
        VersionedDirectory = true
    }

let GetFloatingManager repoPath versionedDirectory =
    let (RepositoryPath dir) = repoPath
    let settings = Settings.LoadDefaultSettings(PhysicalFileSystem(dir))
    if versionedDirectory then
        GetFlatManager repoPath settings
    else
        GetRawManager repoPath settings

let Install directory optionSetter packageId =
    let options = optionSetter DefaultOptions
    let manager = GetFloatingManager (RepositoryPath directory) options.VersionedDirectory
    let package =
        match options.SemanticVersion with
        | Some v ->
            manager.SourceRepository.FindPackage(packageId, v, options.AllowPrerelease, false)
        | None ->
            manager.SourceRepository.FindPackage(packageId, VersionSpec(), options.AllowPrerelease, false)
    manager.InstallPackage(package, options.IgnoreDependencies, options.AllowPrerelease)
