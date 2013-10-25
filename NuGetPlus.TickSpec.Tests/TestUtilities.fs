[<AutoOpen>]
module NuGetPlus.TickSpec.Tests.TestUtilities

open System
open System.IO
open System.Xml

let testProjectDir = 
    let TestProjectDirName = "TestProjects"
    let rec inner(dir : DirectoryInfo) = 
        if Seq.length <| dir.EnumerateDirectories(TestProjectDirName) > 0 then 
            DirectoryInfo(Path.Combine(dir.FullName, TestProjectDirName))
        else if dir.Parent = null then 
            failwith "Could not find the TestProjects"
        else inner dir.Parent
    inner(DirectoryInfo("."))

let workingDir = 
    DirectoryInfo(Path.Combine(testProjectDir.FullName, "WorkingDirectory"))

let ensureWorkingDirectory() = 
    if not workingDir.Exists then workingDir.Create()

let packagesDir = 
    DirectoryInfo(Path.Combine(testProjectDir.FullName, "packages"))

let projectReferences (project:FileInfo) =
        let x = using (project.OpenText()) (fun proj -> proj.ReadToEnd())
        let xml = XmlDocument()
        let ns = XmlNamespaceManager(xml.NameTable)
        ns.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003")
        xml.LoadXml(x) 
        xml.SelectNodes(@"//x:Reference", ns)
        |> Seq.cast<XmlNode>
        |> Seq.map(fun n -> n.Attributes.["Include"].Value)
        |> Seq.map(fun s -> s.Split([|','|]).[0])
