[<TickSpec.StepScope(Feature = "Solution level operations should work")>]
module NuGetPlus.TickSpec.Tests.SolutionStepDefinitions

open FsUnit
open TickSpec
open NUnit.Framework
open System
open System.IO
open Microsoft.Build.Evaluation
open NuGetPlus.SolutionManagement

type State = 
    { Solution : FileInfo;
      ProjectList : seq<string> }

let mutable state = 
    { Solution = FileInfo(".");
      ProjectList = Seq.empty }

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
    if workingDir.Exists then 
        // This often throws a random error as it tries to delete the directory
        // before file deletion has finished. So we try again.
        try 
            workingDir.Delete(true)
        with
        | _ -> 
            Threading.Thread.Sleep(200)
            workingDir.Delete(true)
    workingDir.Create()

let constructWorkingSolution name destinationDir shared = 
    let example = 
        testProjectDir.GetFiles(name, SearchOption.AllDirectories) |> Seq.head
    let exampleDir = example.Directory
    ensureWorkingDirectory()
    let moveFile fromDir toDir name = 
        File.Copy(Path.Combine(fromDir, name), Path.Combine(toDir, name), true)
    let rec moveDirectoryContents (fromDir : DirectoryInfo) 
            (toDir : DirectoryInfo) = 
        fromDir.GetFiles() 
        |> Seq.iter
               (fun file -> moveFile fromDir.FullName toDir.FullName file.Name)
        fromDir.GetDirectories() 
        |> Seq.iter
               (fun dir -> 
                   moveDirectoryContents dir 
                       (fromDir.GetDirectories(dir.Name) |> Seq.head))
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
    state <- { state with Solution = example }

[<Given>]
let ``a solution called (.*)``(solution : string) = 
    constructWorkingSolution solution 
        (Path.Combine(workingDir.FullName, "TestSolution")) false

[<When>]
let ``I ask for the project list``() = 
    state <- { state with ProjectList = GetProjects state.Solution.FullName }

[<Then>]
let ``the project list should contain (.*)``(projectName : string) = 
    state.ProjectList
    |> Seq.map(fun name -> FileInfo(name).Name)
    |> should contain projectName

[<Then>]
let ``(.*) should be in a directory called (.*)`` (projectName : string) 
    (directoryName : string) = 
    state.ProjectList
    |> Seq.find(fun name -> name.Contains(projectName))
    |> fun p -> FileInfo(p).Directory.Name
    |> should equal directoryName