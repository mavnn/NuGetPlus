module ReferenceManagement
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text.RegularExpressions
open NuGet
open Microsoft.Build.Evaluation

let GetManager (projectName : string) =
    if String.IsNullOrWhiteSpace projectName then raise <| ArgumentException("projectName cannot be empty")
    let projectDir = Path.GetFullPath <| IO.Path.GetDirectoryName projectName
    let settings = Settings.LoadDefaultSettings(PhysicalFileSystem projectDir)
    let repositoryPath = settings.GetRepositoryPath()
    printfn "repo path: %s" repositoryPath
    let defaultPackageSource = PackageSource "https://nuget.org/api/v2/"
    let packageSourceProvider = PackageSourceProvider (settings, [defaultPackageSource])
    let remoteRepository = packageSourceProvider.GetAggregate(PackageRepositoryFactory())
    let logger = { new ILogger 
                        with 
                            member x.Log(level, message, parameters) = Console.WriteLine("[{0}] {1}", level.ToString(), String.Format(message, parameters))
                            member x.ResolveFileConflict(message) = FileConflictResolution() }
    let packageManager = PackageManager(
                                            remoteRepository, 
                                            DefaultPackagePathResolver(PhysicalFileSystem repositoryPath), 
                                            PhysicalFileSystem repositoryPath,
                                            PackageReferenceRepository(PhysicalFileSystem projectDir, SharedPackageRepository repositoryPath))
    packageManager.Logger <- logger
    packageManager


let AddFilesToProj packageInstallPath (package : IPackage) (project : IProjectSystem) =
    let grabCompatible f name =
        match project.TryGetCompatibleItems(f ()) with
        | (true, result) -> result
        | (false, _) -> failwith "Failed to get compatible %s." name
    let assemblyReferences =
        grabCompatible (fun () -> package.AssemblyReferences) "assembly references"
    let frameworkReferences =
        grabCompatible (fun () -> package.FrameworkAssemblies) "framework assemblies"
    let contentFiles =
        grabCompatible (package.GetContentFiles) "content files"
    let buildFiles =
        grabCompatible (package.GetBuildFiles) "build files"
    if
        (Seq.isEmpty assemblyReferences 
        && Seq.isEmpty frameworkReferences
        && Seq.isEmpty contentFiles
        && Seq.isEmpty buildFiles)
        && not (Seq.isEmpty package.AssemblyReferences && Seq.isEmpty package.FrameworkAssemblies && Seq.isEmpty (package.GetContentFiles()) && Seq.isEmpty (package.GetBuildFiles()))
        then
        failwith "Unable to find compatible items for framework %s in package %s." (project.TargetFramework.FullName) (package.GetFullName())
    let filteredAssemblyReferences =
        match package.PackageAssemblyReferences with
        | null -> assemblyReferences
        | par ->
            match project.TryGetCompatibleItems(par) with
            | (true, items) ->
                Seq.filter (fun assembly -> not <| Seq.exists (fun (pr : PackageReferenceSet) -> pr.References.Contains(assembly.Name, StringComparer.OrdinalIgnoreCase)) items) assemblyReferences
            | (false, _) ->
                assemblyReferences
    let fileTransformers : IDictionary<string, IPackageFileTransformer> =
        dict [(".transform", XmlTransformer() :> IPackageFileTransformer);(".pp", Preprocessor() :> IPackageFileTransformer)]
    project.AddFiles(contentFiles, fileTransformers)
    assemblyReferences
    |> Seq.filter (fun a -> not <| a.IsEmptyFolder())
    |> Seq.iter (fun a ->
                        let refPath = Path.Combine(packageInstallPath, a.Path)
                        if project.ReferenceExists(a.Name) then project.RemoveReference(a.Name)
                        project.AddReference(refPath, Stream.Null))

let UpdateReferenceToSpecificVersion projectName packageId (version : SemanticVersion) =
    let pm = GetManager projectName
    pm.UpdatePackage(packageId, version, true, true)

let UpdateReference projectName (packageId : string) =
    let pm = GetManager projectName
    pm.UpdatePackage(packageId, true, false)

let private getInstallManager projectName =
    let manager = GetManager projectName
    let project = ProjectSystem(projectName) :> IProjectSystem
    manager.PackageInstalling.Add(fun ev -> AddFilesToProj ev.InstallPath ev.Package project)
    manager

let InstallReferenceOfSpecificVersion projectName packageId (version : SemanticVersion) =
    let manager = getInstallManager projectName
    manager.InstallPackage(packageId, version, false, true)

let InstallReference projectName packageId =
    let manager = getInstallManager projectName
    manager.InstallPackage packageId


let RemoveReference projectName (packageId : string) =
    let manager = GetManager projectName
    manager.UninstallPackage packageId