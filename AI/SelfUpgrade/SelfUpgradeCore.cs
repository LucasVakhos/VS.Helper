// <auto-split from VSHelper.AgentSwarm.Full.cs>
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VS.Helper.AI;

internal static class SelfUpgradeCore
{
    public static void RunDetached(string solutionDir, string solutionPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(solutionDir) || !Directory.Exists(solutionDir))
                return;

            if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
                return;

            string version = VersionBumpEngine.Bump(solutionDir);
            StartExternalBuildAndInstall(solutionDir, solutionPath, version);
        }
        catch (Exception ex)
        {
            TryWriteError(solutionDir, ex);
        }
    }

    private static void StartExternalBuildAndInstall(string solutionDir, string solutionPath, string version)
    {
        string scriptPath = Path.Combine(solutionDir, "_VS.Helper.SelfUpgrade.cmd");
        string logPath = Path.Combine(solutionDir, "_VS.Helper.SelfUpgrade.log");

        string script = BuildScript(solutionDir, solutionPath, version, logPath);
        File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            WorkingDirectory = solutionDir,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal
        });
    }

    private static string BuildScript(string solutionDir, string solutionPath, string version, string logPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("setlocal EnableExtensions EnableDelayedExpansion");
        sb.AppendLine("chcp 65001 >nul");
        sb.AppendLine("cd /d \"" + solutionDir + "\"");
        sb.AppendLine("set \"LOG=" + logPath + "\"");
        sb.AppendLine("set \"EXPECTED_VERSION=" + version + "\"");
        sb.AppendLine("echo VS.Helper Self Upgrade > \"%LOG%\"");
        sb.AppendLine("echo Mode: install only, no uninstall >> \"%LOG%\"");
        sb.AppendLine("echo Expected Identity Version: %EXPECTED_VERSION% >> \"%LOG%\"");
        sb.AppendLine("echo Started: %date% %time% >> \"%LOG%\"");
        sb.AppendLine("echo. >> \"%LOG%\"");
        sb.AppendLine("echo VS.Helper Self Upgrade");
        sb.AppendLine("echo Version: %EXPECTED_VERSION%");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Cleaning old VSIX packages...");
        sb.AppendLine("echo Cleaning old VSIX packages... >> \"%LOG%\"");
        sb.AppendLine("if exist .\\bin del /s /q .\\bin\\*.vsix >> \"%LOG%\" 2>&1");
        sb.AppendLine("if exist .\\obj del /s /q .\\obj\\*.vsix >> \"%LOG%\" 2>&1");
        sb.AppendLine("del /q .\\*.vsix >> \"%LOG%\" 2>&1");
        sb.AppendLine("echo Building VSIX...");
        sb.AppendLine("echo Building solution... >> \"%LOG%\"");
        sb.AppendLine("set \"MSBUILD=\"");
        sb.AppendLine("for /f \"delims=\" %%M in ('where MSBuild.exe 2^>nul') do if not defined MSBUILD set \"MSBUILD=%%M\"");
        sb.AppendLine("if not defined MSBUILD if exist \"%ProgramFiles%\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\" set \"MSBUILD=%ProgramFiles%\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\"");
        sb.AppendLine("if not defined MSBUILD if exist \"%ProgramFiles(x86)%\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\" set \"MSBUILD=%ProgramFiles(x86)%\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\"");
        sb.AppendLine("if not defined MSBUILD if exist \"%ProgramFiles%\\Microsoft Visual Studio\\17\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\" set \"MSBUILD=%ProgramFiles%\\Microsoft Visual Studio\\17\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\"");
        sb.AppendLine("if not defined MSBUILD if exist \"%ProgramFiles(x86)%\\Microsoft Visual Studio\\17\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\" set \"MSBUILD=%ProgramFiles(x86)%\\Microsoft Visual Studio\\17\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe\"");
        sb.AppendLine("if not defined MSBUILD goto build_with_dotnet");
        sb.AppendLine("echo MSBuild: %MSBUILD% >> \"%LOG%\"");
        sb.AppendLine("\"%MSBUILD%\" \"" + solutionPath + "\" \"/t:Build;GeneratePkgDef;CreateVsixContainer\" /p:Configuration=Debug /p:TargetFramework=net48 /p:DeployExtension=false /nologo >> \"%LOG%\" 2>&1");
        sb.AppendLine("if errorlevel 1 goto build_failed");
        sb.AppendLine("goto build_done");
        sb.AppendLine(":build_with_dotnet");
        sb.AppendLine("echo MSBuild.exe not found, fallback to dotnet build... >> \"%LOG%\"");
        sb.AppendLine("dotnet build \"" + solutionPath + "\" /p:Configuration=Debug /p:DeployExtension=true /p:CreateVsixContainer=true >> \"%LOG%\" 2>&1");
        sb.AppendLine("if errorlevel 1 goto build_failed");
        sb.AppendLine(":build_done");
        sb.AppendLine("echo. >> \"%LOG%\"");
        sb.AppendLine("echo Searching freshly built VSIX in bin and solution root... >> \"%LOG%\"");
        sb.AppendLine("set \"VSIX=\"");
        sb.AppendLine("for /f \"usebackq delims=\" %%F in (`powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-ChildItem -Path '.\\bin' -Recurse -Filter '*.vsix' -ErrorAction SilentlyContinue ^| Sort-Object LastWriteTime -Descending ^| Select-Object -First 1 -ExpandProperty FullName\"`) do set \"VSIX=%%F\"");
        sb.AppendLine("if not defined VSIX for /f \"usebackq delims=\" %%F in (`powershell -NoProfile -ExecutionPolicy Bypass -Command \"Get-ChildItem -Path '.' -File -Filter '*.vsix' -ErrorAction SilentlyContinue ^| Sort-Object LastWriteTime -Descending ^| Select-Object -First 1 -ExpandProperty FullName\"`) do set \"VSIX=%%F\"");
        sb.AppendLine("if not defined VSIX goto no_vsix");
        sb.AppendLine("echo VSIX: %VSIX% >> \"%LOG%\"");
        sb.AppendLine("echo Extracting Identity Version from VSIX... >> \"%LOG%\"");
        sb.AppendLine("set \"VSIX_VERSION=\"");
        sb.AppendLine("for /f \"usebackq delims=\" %%V in (`powershell -NoProfile -ExecutionPolicy Bypass -Command \"Add-Type -AssemblyName System.IO.Compression.FileSystem; $z=[IO.Compression.ZipFile]::OpenRead($env:VSIX); $e=$z.Entries ^| Where-Object { $_.FullName -like '*extension.vsixmanifest' -or $_.FullName -like '*source.extension.vsixmanifest' } ^| Select-Object -First 1; if($null -eq $e){ $z.Dispose(); exit 8 }; $r=New-Object IO.StreamReader($e.Open()); $xml=[xml]$r.ReadToEnd(); $r.Dispose(); $z.Dispose(); $ns=New-Object Xml.XmlNamespaceManager($xml.NameTable); $ns.AddNamespace('v','http://schemas.microsoft.com/developer/vsx-schema/2011'); $id=$xml.SelectSingleNode('//v:Identity',$ns); if($null -eq $id){ exit 9 }; $id.Version\"`) do set \"VSIX_VERSION=%%V\"");
        sb.AppendLine("if not defined VSIX_VERSION goto bad_vsix_version");
        sb.AppendLine("echo Actual VSIX Identity Version: %VSIX_VERSION% >> \"%LOG%\"");
        sb.AppendLine("if /i not \"%VSIX_VERSION%\"==\"%EXPECTED_VERSION%\" goto version_mismatch");
        sb.AppendLine("set \"VSIXINSTALLER=\"");
        sb.AppendLine("for /f \"delims=\" %%I in ('where VSIXInstaller.exe 2^>nul') do if not defined VSIXINSTALLER set \"VSIXINSTALLER=%%I\"");
        sb.AppendLine("if not defined VSIXINSTALLER if exist \"%ProgramFiles(x86)%\\Microsoft Visual Studio\\Installer\\vswhere.exe\" for /f \"usebackq delims=\" %%I in (`\"%ProgramFiles(x86)%\\Microsoft Visual Studio\\Installer\\vswhere.exe\" -latest -products * -find Common7\\IDE\\VSIXInstaller.exe`) do if not defined VSIXINSTALLER set \"VSIXINSTALLER=%%I\"");
        sb.AppendLine("if not defined VSIXINSTALLER if exist \"%ProgramFiles%\\Microsoft Visual Studio\\18\\Community\\Common7\\IDE\\VSIXInstaller.exe\" set \"VSIXINSTALLER=%ProgramFiles%\\Microsoft Visual Studio\\18\\Community\\Common7\\IDE\\VSIXInstaller.exe\"");
        sb.AppendLine("if not defined VSIXINSTALLER if exist \"%ProgramFiles(x86)%\\Microsoft Visual Studio\\18\\Community\\Common7\\IDE\\VSIXInstaller.exe\" set \"VSIXINSTALLER=%ProgramFiles(x86)%\\Microsoft Visual Studio\\18\\Community\\Common7\\IDE\\VSIXInstaller.exe\"");
        sb.AppendLine("if not defined VSIXINSTALLER if exist \"%ProgramFiles%\\Microsoft Visual Studio\\17\\Community\\Common7\\IDE\\VSIXInstaller.exe\" set \"VSIXINSTALLER=%ProgramFiles%\\Microsoft Visual Studio\\17\\Community\\Common7\\IDE\\VSIXInstaller.exe\"");
        sb.AppendLine("if not defined VSIXINSTALLER if exist \"%ProgramFiles(x86)%\\Microsoft Visual Studio\\17\\Community\\Common7\\IDE\\VSIXInstaller.exe\" set \"VSIXINSTALLER=%ProgramFiles(x86)%\\Microsoft Visual Studio\\17\\Community\\Common7\\IDE\\VSIXInstaller.exe\"");
        sb.AppendLine("if not defined VSIXINSTALLER goto no_installer");
        sb.AppendLine("echo VSIXInstaller: %VSIXINSTALLER% >> \"%LOG%\"");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Close Visual Studio now. Installer will start after devenv.exe exits.");
        sb.AppendLine("echo Waiting for Visual Studio close... >> \"%LOG%\"");
        sb.AppendLine(":wait_devenv");
        sb.AppendLine("tasklist /FI \"IMAGENAME eq devenv.exe\" 2>nul | find /I \"devenv.exe\" >nul");
        sb.AppendLine("if not errorlevel 1 (");
        sb.AppendLine("  timeout /t 2 /nobreak >nul");
        sb.AppendLine("  goto wait_devenv");
        sb.AppendLine(")");
        sb.AppendLine("echo Visual Studio closed: %date% %time% >> \"%LOG%\"");
        sb.AppendLine("echo Launching VSIXInstaller install only... >> \"%LOG%\"");
        sb.AppendLine("echo Installing VS.Helper %VSIX_VERSION%...");
        sb.AppendLine("\"%VSIXINSTALLER%\" \"%VSIX%\" >> \"%LOG%\" 2>&1");
        sb.AppendLine("set \"INSTALL_EXIT=%ERRORLEVEL%\"");
        sb.AppendLine("echo Installer exit code: %INSTALL_EXIT% >> \"%LOG%\"");
        sb.AppendLine("echo Done. Installer exit code: %INSTALL_EXIT%");
        sb.AppendLine("exit /b %INSTALL_EXIT%");
        sb.AppendLine(":build_failed");
        sb.AppendLine("echo BUILD FAILED: %date% %time% >> \"%LOG%\"");
        sb.AppendLine("echo Build failed. See: %LOG%");
        sb.AppendLine("exit /b 1");
        sb.AppendLine(":no_vsix");
        sb.AppendLine("echo VSIX NOT FOUND IN BIN OR SOLUTION ROOT >> \"%LOG%\"");
        sb.AppendLine("echo VSIX not found. See: %LOG%");
        sb.AppendLine("exit /b 2");
        sb.AppendLine(":bad_vsix_version");
        sb.AppendLine("echo COULD NOT READ VSIX IDENTITY VERSION >> \"%LOG%\"");
        sb.AppendLine("echo Could not read VSIX version. See: %LOG%");
        sb.AppendLine("exit /b 4");
        sb.AppendLine(":version_mismatch");
        sb.AppendLine("echo VERSION MISMATCH. Expected %EXPECTED_VERSION%, actual %VSIX_VERSION% >> \"%LOG%\"");
        sb.AppendLine("echo Version mismatch. See: %LOG%");
        sb.AppendLine("exit /b 5");
        sb.AppendLine(":no_installer");
        sb.AppendLine("echo VSIXInstaller.exe NOT FOUND >> \"%LOG%\"");
        sb.AppendLine("echo VSIXInstaller.exe not found. See: %LOG%");
        sb.AppendLine("exit /b 3");
        return sb.ToString();
    }

    private static void TryWriteError(string solutionDir, Exception ex)
    {
        try
        {
            string dir = string.IsNullOrWhiteSpace(solutionDir) ? Environment.CurrentDirectory : solutionDir;
            File.AppendAllText(
                Path.Combine(dir, "_VS.Helper.SelfUpgrade.error.log"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine);
        }
        catch
        {
            // Не даём Self Upgrade уронить или зависить Visual Studio.
        }
    }
}
