VS.Helper 39 patch notes
========================

Build Zip is now wired to the NEW ZIP ENGINE for both visible command and legacy CreateZipCommand route.

What changed:
- Commands/BuildZipCommand.cs is a thin Visual Studio command only.
- Core/Zip/* contains the real archive pipeline.
- Core/LegacyCommands/CreateZipCommand.cs now calls ZipBuildService instead of ZipFile.CreateFromDirectory(solutionDir,...).
- VSCommandTable.vsct was restored so the commands are visible under Tools.
- Create Zip Config writes VS.Helper.Zip.xml for the new scheme.
- Build Zip auto-creates VS.Helper.Zip.xml on first run if missing.
- Archive contains _VS.Helper.ZipManifest.txt when IncludeManifest=true.
- Default excludes: bin, obj, .vs, .git, packages, *.zip, *.vsix, *.user, *.suo, logs.
- Self Upgrade / Run Swarm / Evolve Swarm / Build Solution remain visible commands in the menu.

Config variables:
- $(SolutionName)
- $(SolutionDir)
- $(Date)
- $(Time)
