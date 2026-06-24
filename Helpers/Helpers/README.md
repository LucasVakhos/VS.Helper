# VS.Helper

Visual Studio extension with project helper commands.

## Commands

### Insert file path comment
Adds a comment with the relative file path at the top of the active code file.

### Build ZIP
Builds a ZIP archive from the opened solution.  
If `VS.Helper.Zip.xml` exists next to the `.sln` / `.slnx`, it is used as the include/exclude config.

### Create ZIP Config
Creates or opens `VS.Helper.Zip.xml`.

The current config root is:

```xml
<VSHelperZip>
```

The config contains both ZIP settings and Git credentials section.

### Commit + Sync Git
Runs:

```text
git add -A
git commit -m "SolutionName yyyy-MM-dd HH:mm:ss"
git pull --rebase
git push
```

GitHub token is stored in `VS.Helper.Zip.xml`.

Put a new token into:

```xml
<Token>ghp_xxx</Token>
```

On the next run it is encrypted via Windows DPAPI and moved to:

```xml
<TokenProtected>...</TokenProtected>
```

`TokenProtected` can be decrypted only by the same Windows user on the same machine.

### VSHelper Tools
One command opens a dialog with VSHelper-style operations:

- Delete empty lines
- Delete `#region` / `#endregion`
- Find and replace
- Find value/class and copy matched files
- Clear duplicate `using`
- Collect namespaces
- Collect using packages
- Delete `.bak` files
- Delete files not included in `.csproj`
- Compare/sync project with sample project
- Convert old `.csproj` to basic SDK-style project
- Normalize method signatures
- Restore `.cs` files from `.bak`
- Add `//relative\path.cs` comment to `.cs` files
- Create/update `VS.Helper.Zip.xml` and Git section

Use **Dry Run** before destructive operations.

## Config example

```xml
<?xml version="1.0" encoding="utf-8"?>
<VSHelperZip>
  <Root>$(SolutionDir)</Root>
  <OutputDir>$(SolutionDir)</OutputDir>
  <ArchiveName>$(SolutionName).zip</ArchiveName>
  <StartProject>RhymeContest.Blazor.Server\RhymeContest.Blazor.Server.csproj</StartProject>

  <Git>
    <UserName>YOUR_GITHUB_LOGIN</UserName>
    <Token></Token>
    <TokenProtected></TokenProtected>
  </Git>

  <Include>
    <Path>RhymeContest.sln</Path>
    <Path>RhymeContest.Blazor.Server</Path>
    <Path>RhymeContest.Module</Path>
    <Path>RhymeContest.Module.Blazor</Path>
    <Path>Directory.Build.props</Path>
    <Path>Directory.Packages.props</Path>
    <Path>NuGet.config</Path>
    <Path>README.md</Path>
  </Include>

  <Exclude>
    <Path>**/bin/**</Path>
    <Path>**/obj/**</Path>
    <Path>**/.vs/**</Path>
    <Path>**/.git/**</Path>
    <Path>**/node_modules/**</Path>
    <Path>**/packages/**</Path>
    <Path>**/*.user</Path>
    <Path>**/*.suo</Path>
    <Path>**/*.pdb</Path>
    <Path>**/*.cache</Path>
    <Path>**/*.log</Path>
    <Path>**/appsettings.Development.json</Path>
    <Path>**/appsettings.Production.json</Path>
    <Path>**/*.db</Path>
    <Path>**/*.sqlite</Path>
  </Exclude>
</VSHelperZip>
```

## Security

Add local config to `.gitignore`:

```gitignore
VS.Helper.Zip.xml
```

Do not commit plain GitHub tokens.

## Notes

This migration ports VSHelper’s main workflow into VS.Helper as one VS command with a combo dialog.  
The old WinForms/DevExpress `FileScanner` UI is not embedded; the VS extension has its own lightweight dialog.


## VS меню

После установки расширения в меню **Tools** должны быть 5 команд:

1. Create File Comment
2. Build ZIP
3. Commit + Sync Git
4. Create ZIP Config
5. VSHelper Tools

Если видны только 4 команды, значит в проект не попал `VSCommandTable.vsct` или Visual Studio держит старую установленную версию расширения. Удали старую версию из Extensions, закрой VS, очисти `bin/obj`, пересобери и установи новый VSIX.


## VS.Helper 2.0.4

- VSIX version bumped to 2.0.4.0.
- Menu table synchronized: 5 commands under Tools.
- VSHelper Tools command id: 0x0500.
