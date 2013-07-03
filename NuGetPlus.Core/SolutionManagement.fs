module NuGetPlus.SolutionManagement

open System.IO
open System.Text.RegularExpressions

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