module NuGetPlus.TickSpec.Tests.Fixtures

open NUnit.Framework
open System.IO
open System.Reflection
open TickSpec

let assembly = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(assembly)

/// Inherit from FeatureFixture to define a feature fixture
[<AbstractClass>]
[<TestFixture>]
type FeatureFixture(source : string) = 
    
    [<Test>][<TestCaseSource("Scenarios")>]
    member this.TestScenario(scenario : Scenario) = 
        if scenario.Tags |> Seq.exists((=) "ignore") then 
            raise(new IgnoreException("Ignored: " + scenario.Name))
        try 
            scenario.Action.Invoke()
        with
        | ex as TargetInvocationException -> raise ex.InnerException 

    member this.Scenarios = 
        let s = File.OpenText(Path.Combine(@"..\..\", source))
        definitions.GenerateScenarios(source, s)

type ProjectFixture() = 
    inherit FeatureFixture("ProjectFeature.txt")

type SolutionFixture() = 
    inherit FeatureFixture("BatchFeature.txt")