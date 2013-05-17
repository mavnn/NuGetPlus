open System
open NuGet
open ReferenceManagement
open UnionArgParser
type Argument =
    | [<MandatoryAttribute>] Action of string
    | [<MandatoryAttribute>] ProjectFile of string
    | [<MandatoryAttribute>] PackageId of string
    | Version of string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Action _ -> "Specify an action: Install, Remove or Update"
                | ProjectFile _ -> "Path to project file to update."
                | PackageId _ -> "NuGet package id for action."
                | Version _ -> "Optional specific version of package."

type ActionType =
    | Install
    | Remove
    | Update

let processAction (a : string) =
    match a.ToLower() with
    | "install" -> Install
    | "remove" -> Remove
    | "update" -> Update
    | _ -> failwith "Invalid Action; please use install, remove or update."

let processProjectFile (f : string) =
    match IO.File.Exists(f) with
    | true -> f
    | false -> failwith "ProjectFile does not exist."

let processVersion (v : string) =
    SemanticVersion(v)    

[<EntryPoint>]
let main argv = 
    try
        let parser = UnionArgParser<Argument>()
        let results = parser.Parse()
        let action = results.PostProcessResult <@ Action @> processAction
        let proj = results.PostProcessResult <@ ProjectFile @> processProjectFile
        let package = results.GetResult <@ PackageId @>
        let version = results.TryPostProcessResult <@ Version @> processVersion

        printfn "Action type: %A" action
        printfn "Project file: %s" proj
        printfn "Package ID: %s" package
        match version with
        | None -> ()
        | Some v -> printfn "Version: %A" v

        match action with
        | Install ->
            match version with
            | None -> InstallReference proj package
            | Some v -> InstallReferenceOfSpecificVersion proj package v
        | Update ->
            match version with
            | None -> UpdateReference proj package
            | Some v -> UpdateReferenceToSpecificVersion proj package v
        | Remove ->
            RemoveReference proj package
        0
    with 
    | ex ->
        printfn "%A" ex
        1
