[<AutoOpen>]
module XmlTransformer
open System
open System.Collections.Generic
open System.IO
open System.Xml.Linq
open NuGet

type XmlTransformer () =
    let (configMappings : IDictionary<XName, System.Action<XElement, XElement>>) = dict [(XName.Get "configSections", Action<XElement, XElement>(fun parent element -> parent.AddFirst(element)))]
    interface IPackageFileTransformer with
        member x.TransformFile(file, targetPath, projectSystem) =
            let pp = Preprocessor() :> IPackageFileTransformer
            pp.TransformFile(file, targetPath, projectSystem)
            let reader = new StreamReader(projectSystem.OpenFile(targetPath))
            let xml = XElement.Parse(reader.ReadToEnd())
            reader.Dispose()
            let doc =
                match projectSystem.FileExists targetPath with
                | true ->
                    use configStream = projectSystem.OpenFile(targetPath)
                    XDocument.Load(configStream, LoadOptions.PreserveWhitespace)
                | false ->
                    let xDoc = XDocument(XElement(xml.Name))
                    projectSystem.AddFile(targetPath, fun (stream : Stream) -> xDoc.Save(stream))
                    xDoc
            doc.Root.MergeWith(xml, configMappings) |> ignore
            projectSystem.AddFile(targetPath, fun (stream : Stream) -> doc.Save(stream))
        member x.RevertFile(file, targetPath, matchingFiles, projectSystem) =
            raise <| NotImplementedException("RevertFile for XmlTransformer not implemented")