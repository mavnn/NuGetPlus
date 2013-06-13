# Repeatable NuGet actions

Frustrated with broken hintpaths and wrong versions of dlls being referenced?

Need to install dependencies from the commandline without firing up Visual Studio?

Want to downgrade the version of a NuGet package you're using without pain?

This could be the wrapper for you. Code is at an alpha status at the moment.

# Command line options available for ngp.exe

    --action <string>: Specify an action: Install, Remove, Restore or Update
    --projectfile <string>: Path to project file to update.
    --packageid <string>: NuGet package id for action.
    --version <string>: Optional specific version of package.

Want to call them from code? Reference the dll and be amazed by the fact that the logic works the same way as the command line executable. Astonishing!

# Get involved!

Pull requests gratefully accepted. The code was hacked together in a hurry as I learnt how NuGet had
been built, so it could definitely be cleaned up.

There's also a bunch of other functionality that is... a bit ropey... in the normal NuGet client,
search as package search for packages with a lot of versions that could be added.

# Continuous Integration and Issue Tracking provided by CodeBetter

<div style="background: #00578e url('http://www.jetbrains.com/img/banners/Codebetter.png') no-repeat 0 50%; margin:0;padding:0;text-decoration:none;text-indent:0;letter-spacing:-0.001em; width:728px; height:90px"> <a href="http://www.jetbrains.com/youtrack" title="YouTrack by JetBrains" style="margin: 60px 0 0 190px;padding: 0; float: left;font-size: 14px; background-image:none;border:0;color: #acc4f9; font-family: trebuchet ms,arial,sans-serif;font-weight: normal;text-align:left;">keyboard-centric bug tracker</a> <a href="http://www.jetbrains.com/teamcity" title="TeamCity by JetBrains" style="margin:0 0 0 400px;padding:60px 0 2px 0;font-size:14px; background-image:none;border:0;display:block; color: #acc4f9; font-family: trebuchet ms,arial,sans-serif;font-weight: normal;text-align:left;">continuous integration server</a> </div>
