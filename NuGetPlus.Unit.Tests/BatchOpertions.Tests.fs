module NuGetPlus.Unit.Tests.BatchOperations

open NuGetPlus.BatchOperations

open NUnit.Framework
open FsUnit
open System.Diagnostics

module TestModule =
    let CallerName () =
        StackTrace().GetFrame(1).GetMethod().ReflectedType.FullName

[<Test>]
let ``Does reflection do what we expect`` () =
    let expected = "NuGetPlus.Unit.Tests.BatchOperations"
    TestModule.CallerName () |> should equal expected

