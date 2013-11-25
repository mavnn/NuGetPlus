[<TickSpec.StepScope(Feature = "Project level operations should work")>]
module NuGetPlus.TickSpec.Tests.ProjectDefinitions

open TickSpec
open NUnit.Framework
open System
open System.IO
open Microsoft.Build.Evaluation
open NuGetPlus.ProjectManagement

type State = 
    { project : FileInfo;
      package : string;
      expectedVersion : Option<string> }

let mutable state = 
    { project = FileInfo(".");
      package = "";
      expectedVersion = None }

[<AfterScenario>]
let TearDownScenario() = 
    let projFiles = workingDir.GetFileSystemInfos "*.*proj"
    projFiles 
    |> Seq.iter
           (fun proj -> 
               ProjectCollection.GlobalProjectCollection.GetLoadedProjects
                   (proj.FullName) 
               |> Seq.iter 
                      ProjectCollection.GlobalProjectCollection.UnloadProject)
    let CleanDir(dir : DirectoryInfo) = 
        if dir.Exists then 
            // This often throws a random error as it tries to delete the directory
            // before file deletion has finished. So we try again.
            try 
                dir.Delete(true)
            with
            | _ -> 
                Threading.Thread.Sleep(200)
                if dir.Exists then dir.Delete(true)
        dir.Create()
    CleanDir workingDir
    CleanDir packagesDir
    state <- { project = FileInfo(".");
               package = "";
               expectedVersion = None }

type MidFix =
    | HasPackages of string
    | VisualStudioVersion of string

let constructWorkingProject projType midFix destinationDir shared = 
    let midFixString = 
        match midFix with
        | HasPackages hasPackages ->
            match hasPackages with
            | "packages" -> ".WithPackages."
            | "no packages" -> ".NoPackages."
            | _ -> failwith "Unknown package option"
        | VisualStudioVersion version ->
            "." + version + "."
    let example = 
        testProjectDir.GetFiles("*.*", SearchOption.AllDirectories)
        |> Seq.filter
               (fun fi -> fi.Name = projType + midFixString + (projType.ToLower()))
        |> Seq.head
    ensureWorkingDirectory()
    let destination = Path.Combine(destinationDir, example.Name)
    let projDir = DirectoryInfo(Path.GetDirectoryName destination)
    if projDir.Exists then projDir.Delete(true)
    projDir.Create()
    if shared then 
        let nugetConfig = 
            FileInfo(Path.Combine(testProjectDir.FullName, "nuget.config"))
        let destination = Path.Combine(projDir.Parent.FullName, "nuget.config")
        if not <| File.Exists destination then 
            nugetConfig.CopyTo(destination) |> ignore
    else 
        let nugetConfig = 
            FileInfo(Path.Combine(testProjectDir.FullName, "nuget.config"))
        nugetConfig.CopyTo(Path.Combine(projDir.FullName, "nuget.config")) 
        |> ignore
    example.Directory.GetFiles() 
    |> Seq.iter
           (fun fi -> 
               fi.CopyTo
                   (Path.Combine(Path.GetDirectoryName destination, fi.Name)) 
               |> ignore)
    state <- { state with project = FileInfo(destination) }

[<Given>]
let ``a (\w*) with (packages|no packages)`` (projType : string) 
    (hasPackages : string) = 
    let destinationDir = 
        Path.Combine(workingDir.FullName, Guid.NewGuid().ToString())
    constructWorkingProject projType (HasPackages hasPackages) destinationDir false

[<Given>]
let ``a (\w*) (\w*) Visual Studio package`` (projType : string) (version : string) =
    let destinationDir =
        Path.Combine(workingDir.FullName, Guid.NewGuid().ToString())
    constructWorkingProject projType (VisualStudioVersion version) destinationDir false

[<Given>]
let ``a (|restored )(\w*) (\w*) with (packages|no packages)`` (restored : string) 
    (ordinal : string) (projType : string) (hasPackages : string) = 
    let destinationDir = Path.Combine(workingDir.FullName, ordinal)
    constructWorkingProject projType (HasPackages hasPackages) destinationDir true
    if restored = "restored " then RestoreReferences state.project.FullName

[<When>]
let ``I install (\S*) version (\S*)`` (packageId : string) (version : string) = 
    state <- { state with package = packageId;
                          expectedVersion = Some version }
    InstallReferenceOfSpecificVersion state.project.FullName packageId 
        (NuGet.SemanticVersion(version))

[<When>]
let ``I install (\S*)$``(packageId : string) = 
    state <- { state with package = packageId }
    InstallReference state.project.FullName packageId

[<When>]
let ``remove (.*)``(packageId : string) = 
    state <- { state with package = packageId }
    RemoveReference state.project.FullName packageId

[<When>]
let ``I restore a project with (.*)``(packageId : string) = 
    state <- { state with package = packageId }
    RestoreReferences state.project.FullName

[<When>]
let ``I update (\S*)$``(packageId : string) = 
    state <- { state with expectedVersion = None }
    UpdateReference state.project.FullName packageId

[<When>]
let ``I update (\S*) to version (.*)$`` (packageId : string) (version : string) = 
    state <- { state with expectedVersion = Some version }
    UpdateReferenceToSpecificVersion state.project.FullName packageId 
        (NuGet.SemanticVersion(version))

[<When>]
let ``I delete the (\S*) package``(package : string) = 
    let packagesDir = 
        DirectoryInfo
            (Path.Combine(state.project.Directory.FullName, "packages"))
    if packagesDir.Exists then 
        let dir = 
            packagesDir.GetDirectories() 
            |> Seq.find(fun di -> di.Name.StartsWith state.package)
        dir.Delete(true)

[<Then>]
let ``(the package|\S*) (should|should not) be installed in the (right|shared) directory`` (package : string) 
    (should : string) (shared : string) = 
    let packagesDir = 
        match shared with
        | "right" -> 
            DirectoryInfo
                (Path.Combine(state.project.Directory.FullName, "packages"))
        | "shared" -> 
            DirectoryInfo
                (Path.Combine
                     (state.project.Directory.Parent.FullName, "packages"))
        | _ -> failwith "Unknown local repository type"
    if packagesDir.Exists then 
        let packageName = 
            match package with
            | "the package" -> state.package
            | s -> s
        let isDir = 
            match state.expectedVersion with
            | None -> 
                packagesDir.GetDirectories() 
                |> Seq.exists(fun di -> di.Name.StartsWith(state.package, StringComparison.CurrentCultureIgnoreCase))
            | Some version -> 
                packagesDir.GetDirectories() 
                |> Seq.exists(fun di -> di.Name = packageName + "." + version)
        match should with
        | "should" -> Assert.IsTrue isDir
        | "should not" -> Assert.IsFalse isDir
        | _ -> failwith "Unknown should option"
    else 
        match should with
        | "should" -> Assert.Fail()
        | "should not" -> Assert.IsTrue true
        | _ -> failwith "Unknown should option"

[<Then>]
let ``(the reference|\S*) (should|should not) be added to the project file`` (reference : string) 
    (should : string) = 
    let referenceName = 
        match reference with
        | "the reference" -> state.package
        | s -> s

    let content = projectReferences state.project

    let errMsg() = sprintf "\nExpected to find reference to '%s'\nActual rerfrences:\n'%s'" referenceName (String.Join("', '", content))
    
    content 
    |> Seq.exists(fun c -> c.Equals(referenceName, StringComparison.CurrentCultureIgnoreCase))
    |> match should with
        | "should" -> fun b -> Assert.IsTrue(b, errMsg())
        | "should not" -> fun b -> Assert.IsFalse(b, errMsg())
        | _ -> failwith "Unknown should option"

[<Then>]
let ``the package (should|should not) be added to the packages.config file``(should : string) = 
    let packagesConfig = 
        FileInfo
            (Path.Combine(state.project.Directory.FullName, "packages.config"))
    let content = 
        using (packagesConfig.OpenText()) (fun config -> config.ReadToEnd())
    content.Contains(state.package) |> match should with
                                       | "should" -> Assert.IsTrue
                                       | "should not" -> Assert.IsFalse
                                       | _ -> failwith "Unknown should option"