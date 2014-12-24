module NuGetPlay.Main

open System
open NuGet
open NuGetPlus
open NuGetPlus.ProjectManagement
open Nessos.UnionArgParser

type Argument = 
    | Action of string
    | File of string
    | PackageId of string
    | Version of string
    interface IArgParserTemplate with
        member s.Usage = 
            match s with
            | Action _ -> 
                "Specify an action: Scan, Install, Remove, Restore, Update, SolutionRestore or SolutionUpdate"
            | File _ -> "Path to project/solution file."
            | PackageId _ -> "NuGet package id for action."
            | Version _ -> "Optional specific version of package."

type ActionType = 
    | Install
    | Remove
    | Restore
    | Update
    | Scan
    | SolutionRestore
    | SolutionUpdate

let processAction(a : string) = 
    match a.ToLower() with
    | "install" -> Install
    | "remove" -> Remove
    | "restore" -> Restore
    | "update" -> Update
    | "solutionrestore" -> SolutionRestore
    | "solutionupdate" -> SolutionUpdate
    | "scan" -> Scan
    | _ -> 
        failwith 
            "Invalid Action; please use scan, install, remove, restore, update, solutionrestore or solutionupdate."

let processProjectFile(f : string) = 
    match IO.File.Exists(f) with
    | true -> IO.Path.GetFullPath f
    | false -> failwith "File does not exist."

let processVersion(v : string) = SemanticVersion(v)

[<EntryPoint>]
let main argv = 
    try 
        let parser = UnionArgParser.Create<Argument>()
        let results = parser.Parse()
        let action = results.PostProcessResult(<@ Action @>, processAction)
        let file = 
            results.PostProcessResult(<@ File @>, processProjectFile)
        let maybePackage = results.TryGetResult <@ PackageId @>
        let version = results.TryPostProcessResult (<@ Version @>, processVersion)
        printfn "Action type: %A" action
        printfn "File: %s" file
        match maybePackage with
        | Some package -> printfn "Package ID: %s" package
        | None -> ()
        match version with
        | None -> ()
        | Some v -> printfn "Version: %A" v
        let checkPackage mp = 
            match mp with
            | None -> failwith "A package is required for this action"
            | Some p -> p
        match action with
        | Install -> 
            match version with
            | None -> InstallReference file (checkPackage maybePackage)
            | Some v -> 
                InstallReferenceOfSpecificVersion file
                    (checkPackage maybePackage) v
        | Update -> 
            match version with
            | None -> UpdateReference file (checkPackage maybePackage)
            | Some v -> 
                UpdateReferenceToSpecificVersion file
                    (checkPackage maybePackage) v
        | Remove -> RemoveReference file (checkPackage maybePackage)
        | Restore -> 
            match maybePackage with
            | Some package -> 
                failwith 
                    "PackageId provided for restore action - restore will always restore the whole packages.config"
            | None -> ()
            RestoreReferences file
        | SolutionRestore ->
            match maybePackage with
            | Some package -> 
                failwith 
                    "PackageId provided for solution restore action - restore will always restore the whole solution."
            | None -> ()
            SolutionManagement.RestorePackages file
        | SolutionUpdate ->
            match version with
            | None -> SolutionManagement.UpdateReference file (checkPackage maybePackage)
            | Some v -> 
                SolutionManagement.UpdateReferenceToSpecificVersion file
                    (checkPackage maybePackage) v
        | Scan ->
            match maybePackage with
            | Some package -> 
                failwith 
                    "Scan takes only a solution file as a parameter."
            | None -> ()
            SolutionManagement.Scan file
            |> Seq.iter (fun (id, versions) -> printfn "Package %s has multiple versions: %A" id (Seq.toList versions))
        0
    with
    | ex -> 
        printfn "%A" ex
        1