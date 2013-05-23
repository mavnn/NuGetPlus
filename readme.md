# Repeatable NuGet actions

Frustrated with broken hintpaths and wrong versions of dlls being referenced?

Need to install dependencies from the commandline without firing up Visual Studio?

Want to downgrade the version of a NuGet package you're using without pain?

This could be the wrapper for you, when it's finished.

# Command line options available for NuGetPlus.exe

    --action <string>: Specify an action: Install, Remove or Update
    --projectfile <string>: Path to project file to update.
    --packageid <string>: NuGet package id for action.
    --version <string>: Optional specific version of package.

Want to call them from code? Reference the dll and be amazed by the fact that the logic works the same way as the command line executable. Astonishing!

# This is very much pre-production

Only the install command has been tested so far, and it doesn't always correctly interpret the config setting for package location.