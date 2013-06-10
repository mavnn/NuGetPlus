open System
open NuGet
open ReferenceManagement
open UnionArgParser

type Argument =
    | [<MandatoryAttribute>] Action of string
    | [<MandatoryAttribute>] ProjectFile of string
    | PackageId of string
    | Version of string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Action _ -> "Specify an action: Install, Remove, Restore or Update"
                | ProjectFile _ -> "Path to project file to update."
                | PackageId _ -> "NuGet package id for action."
                | Version _ -> "Optional specific version of package."

type ActionType =
    | Install
    | Remove
    | Restore
    | Update

let processAction (a : string) =
    match a.ToLower() with
    | "install" -> Install
    | "remove" -> Remove
    | "restore" -> Restore
    | "update" -> Update
    | _ -> failwith "Invalid Action; please use install, remove, restore or update."

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
        let maybePackage = results.TryGetResult <@ PackageId @>
        let version = results.TryPostProcessResult <@ Version @> processVersion

        printfn "Action type: %A" action
        printfn "Project file: %s" proj
        match maybePackage with
        | Some package ->
            printfn "Package ID: %s" package
        | None -> ()

        match version with
        | None -> ()
        | Some v -> printfn "Version: %A" v

        let checkPackage mp =
            match mp with
            | None ->
                failwith "A package is required for this action"
            | Some p -> p

        match action with
        | Install ->
            match version with
            | None -> InstallReference proj (checkPackage maybePackage)
            | Some v -> InstallReferenceOfSpecificVersion proj (checkPackage maybePackage) v
        | Update ->
            match version with
            | None -> UpdateReference proj (checkPackage maybePackage)
            | Some v -> UpdateReferenceToSpecificVersion proj (checkPackage maybePackage) v
        | Remove ->
            RemoveReference proj (checkPackage maybePackage)
        | Restore ->
            match maybePackage with
            | Some package -> failwith "PackageId provided for restore action - restore will always restore the whole packages.config"
            | None -> ()
            RestoreReferences proj
        0
    with 
    | ex ->
        printfn "%A" ex
        1
