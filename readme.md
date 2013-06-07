# Repeatable NuGet actions

Frustrated with broken hintpaths and wrong versions of dlls being referenced?

Need to install dependencies from the commandline without firing up Visual Studio?

Want to downgrade the version of a NuGet package you're using without pain?

This could be the wrapper for you, when it's finished.

# Command line options available for ngp.exe

    --action <string>: Specify an action: Install, Remove, Restore or Update
    --projectfile <string>: Path to project file to update.
    --packageid <string>: NuGet package id for action.
    --version <string>: Optional specific version of package.

Want to call them from code? Reference the dll and be amazed by the fact that the logic works the same way as the command line executable. Astonishing!

# This is very much pre-production

But most things should work. One caveat is that it requires a repositoryPath setting in a findable
nuget.config (it loads settings files the same way as NuGet does).

# Get involved!

Pull requests gratefully accepted. The code was hacked together in a hurry as I learnt how NuGet had
been built, so it could definitely be cleaned up.

There's also a bunch of other functionality that is... a bit ropey... in the normal NuGet client,
search as package search for packages with a lot of versions that could be added.
