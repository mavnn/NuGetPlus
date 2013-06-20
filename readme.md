## Repeatable NuGet actions

This project aims to provide a NuGet wrapper with the following functionality, both via dll and via an easy to use command line interface:

1.    Reliable installation of NuGet references to C#, VB.net and F# projects.
1.    Reliable upgrades of NuGet references in all project types.
1.    Reliable downgrades of NuGet references in all project types. It’s painful to try a new release/pre-release of a NuGet package across a solution and then discover that you have to manually downgrade all of the projects separately if you decide not to take the new version.
1.    Reliable removal of NuGet references turns out to be a requirement of reliable downgrades.
1.    Sane solution wide management of references. Due to the way project references work, we need an easy way to ensure that all of the projects in a solution use the same version of any particular NuGet reference, and to check that this will not case any version conflicts. So ideally, upgrade and downgrade commands will run against a solution.

As a 'meta-goal' we aim to be a dropin replacement, respecting things like nuget.config files in the same way as the official NuGet clients (with an exception for where those clients are subject to bugs).

There is a blog post that contains [more explaination on the project aims](http://mikehadlow.blogspot.co.uk/2013/06/guest-post-working-around-fnuget.html).

## Command line options available for ngp.exe

    --action <string>: Specify an action: Install, Remove, Restore or Update
    --projectfile <string>: Path to project file to update.
    --packageid <string>: NuGet package id for action.
    --version <string>: Optional specific version of package.

ngp.exe is a thin wrapper around the underlying dll, allowing the same operations to be called trivially from code:

```fsharp
// This is F# code but the dll can be referenced from other .net languages too.
let packageName = "myPackage"
let packageVersion = NuGet.SemanticVersion("10.0.2.0")
let projectName = "myProject.fs"

// Methods available are:
InstallReference projectName packageName
InstallReferenceOfSpecificVersion projectName packageName packageVersion
UpdateReference projectName packageName
UpdateReferenceToSpecificVersion projectName packageName packageVersion
RemoveReference projectName packageName
RestoreReferences projectName
```

## Get involved!

Issues and the current roadmap can be found at the [NuGetPlus YouTrack Instance](http://nugetplus.myjetbrains.com).

Pull requests gratefully accepted. The code was hacked together in a hurry as I learnt how NuGet had
been built, so it could definitely be cleaned up.

## Continuous Integration and Issue Management provided by CodeBetter and JetBrains

[NuGetPlus TeamCity Project](http://teamcity.codebetter.com/project.html?projectId=project363) is kindly provided by [CodeBetter](http://codebetter.com/) and [JetBrains](http://www.jetbrains.com/).

[NuGetPlus YouTrack Instance](http://nugetplus.myjetbrains.com) provided by [JetBrains](http://www.jetbrains.com/) under their [OSS Project License](http://www.jetbrains.com/youtrack/buy/buy.jsp).

Many thanks to both CodeBetter and JetBrains for provided these services.

![YouTrack and TeamCity](http://www.jetbrains.com/img/banners/Codebetter300x250.png) 
