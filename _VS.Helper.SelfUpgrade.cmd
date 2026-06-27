@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
cd /d "E:\CS\26\VS.Helper"
set "LOG=E:\CS\26\VS.Helper\_VS.Helper.SelfUpgrade.log"
set "EXPECTED_VERSION=2026.2.1.74"
echo VS.Helper Self Upgrade > "%LOG%"
echo Mode: install only, no uninstall >> "%LOG%"
echo Expected Identity Version: %EXPECTED_VERSION% >> "%LOG%"
echo Started: %date% %time% >> "%LOG%"
echo. >> "%LOG%"
echo VS.Helper Self Upgrade
echo Version: %EXPECTED_VERSION%
echo.
echo Cleaning old VSIX packages...
echo Cleaning old VSIX packages... >> "%LOG%"
if exist .\bin del /s /q .\bin\*.vsix >> "%LOG%" 2>&1
if exist .\obj del /s /q .\obj\*.vsix >> "%LOG%" 2>&1
del /q .\*.vsix >> "%LOG%" 2>&1
echo Building VSIX...
echo Building solution... >> "%LOG%"
set "MSBUILD="
for /f "delims=" %%M in ('where MSBuild.exe 2^>nul') do if not defined MSBUILD set "MSBUILD=%%M"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD goto build_with_dotnet
echo MSBuild: %MSBUILD% >> "%LOG%"
"%MSBUILD%" "E:\CS\26\VS.Helper\VS.Helper.slnx" "/t:Build;GeneratePkgDef;CreateVsixContainer" /p:Configuration=Debug /p:TargetFramework=net48 /p:DeployExtension=false /nologo >> "%LOG%" 2>&1
if errorlevel 1 goto build_failed
goto build_done
:build_with_dotnet
echo MSBuild.exe not found, fallback to dotnet build... >> "%LOG%"
dotnet build "E:\CS\26\VS.Helper\VS.Helper.slnx" /p:Configuration=Debug /p:DeployExtension=true /p:CreateVsixContainer=true >> "%LOG%" 2>&1
if errorlevel 1 goto build_failed
:build_done
echo. >> "%LOG%"
echo Searching freshly built VSIX in bin and solution root... >> "%LOG%"
set "VSIX="
for /f "usebackq delims=" %%F in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '.\bin' -Recurse -Filter '*.vsix' -ErrorAction SilentlyContinue ^| Sort-Object LastWriteTime -Descending ^| Select-Object -First 1 -ExpandProperty FullName"`) do set "VSIX=%%F"
if not defined VSIX for /f "usebackq delims=" %%F in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '.' -File -Filter '*.vsix' -ErrorAction SilentlyContinue ^| Sort-Object LastWriteTime -Descending ^| Select-Object -First 1 -ExpandProperty FullName"`) do set "VSIX=%%F"
if not defined VSIX goto no_vsix
echo VSIX: %VSIX% >> "%LOG%"
echo Extracting Identity Version from VSIX... >> "%LOG%"
set "VSIX_VERSION="
for /f "usebackq delims=" %%V in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $z=[IO.Compression.ZipFile]::OpenRead($env:VSIX); $e=$z.Entries ^| Where-Object { $_.FullName -like '*extension.vsixmanifest' -or $_.FullName -like '*source.extension.vsixmanifest' } ^| Select-Object -First 1; if($null -eq $e){ $z.Dispose(); exit 8 }; $r=New-Object IO.StreamReader($e.Open()); $xml=[xml]$r.ReadToEnd(); $r.Dispose(); $z.Dispose(); $ns=New-Object Xml.XmlNamespaceManager($xml.NameTable); $ns.AddNamespace('v','http://schemas.microsoft.com/developer/vsx-schema/2011'); $id=$xml.SelectSingleNode('//v:Identity',$ns); if($null -eq $id){ exit 9 }; $id.Version"`) do set "VSIX_VERSION=%%V"
if not defined VSIX_VERSION goto bad_vsix_version
echo Actual VSIX Identity Version: %VSIX_VERSION% >> "%LOG%"
if /i not "%VSIX_VERSION%"=="%EXPECTED_VERSION%" goto version_mismatch
set "VSIXINSTALLER="
for /f "delims=" %%I in ('where VSIXInstaller.exe 2^>nul') do if not defined VSIXINSTALLER set "VSIXINSTALLER=%%I"
if not defined VSIXINSTALLER if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" for /f "usebackq delims=" %%I in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -find Common7\IDE\VSIXInstaller.exe`) do if not defined VSIXINSTALLER set "VSIXINSTALLER=%%I"
if not defined VSIXINSTALLER if exist "%ProgramFiles%\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe" set "VSIXINSTALLER=%ProgramFiles%\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe"
if not defined VSIXINSTALLER if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe" set "VSIXINSTALLER=%ProgramFiles(x86)%\Microsoft Visual Studio\18\Community\Common7\IDE\VSIXInstaller.exe"
if not defined VSIXINSTALLER if exist "%ProgramFiles%\Microsoft Visual Studio\17\Community\Common7\IDE\VSIXInstaller.exe" set "VSIXINSTALLER=%ProgramFiles%\Microsoft Visual Studio\17\Community\Common7\IDE\VSIXInstaller.exe"
if not defined VSIXINSTALLER if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\17\Community\Common7\IDE\VSIXInstaller.exe" set "VSIXINSTALLER=%ProgramFiles(x86)%\Microsoft Visual Studio\17\Community\Common7\IDE\VSIXInstaller.exe"
if not defined VSIXINSTALLER goto no_installer
echo VSIXInstaller: %VSIXINSTALLER% >> "%LOG%"
echo.
echo Close Visual Studio now. Installer will start after devenv.exe exits.
echo Waiting for Visual Studio close... >> "%LOG%"
:wait_devenv
tasklist /FI "IMAGENAME eq devenv.exe" 2>nul | find /I "devenv.exe" >nul
if not errorlevel 1 (
  timeout /t 2 /nobreak >nul
  goto wait_devenv
)
echo Visual Studio closed: %date% %time% >> "%LOG%"
echo Launching VSIXInstaller install only... >> "%LOG%"
echo Installing VS.Helper %VSIX_VERSION%...
"%VSIXINSTALLER%" "%VSIX%" >> "%LOG%" 2>&1
set "INSTALL_EXIT=%ERRORLEVEL%"
echo Installer exit code: %INSTALL_EXIT% >> "%LOG%"
echo Done. Installer exit code: %INSTALL_EXIT%
exit /b %INSTALL_EXIT%
:build_failed
echo BUILD FAILED: %date% %time% >> "%LOG%"
echo Build failed. See: %LOG%
exit /b 1
:no_vsix
echo VSIX NOT FOUND IN BIN OR SOLUTION ROOT >> "%LOG%"
echo VSIX not found. See: %LOG%
exit /b 2
:bad_vsix_version
echo COULD NOT READ VSIX IDENTITY VERSION >> "%LOG%"
echo Could not read VSIX version. See: %LOG%
exit /b 4
:version_mismatch
echo VERSION MISMATCH. Expected %EXPECTED_VERSION%, actual %VSIX_VERSION% >> "%LOG%"
echo Version mismatch. See: %LOG%
exit /b 5
:no_installer
echo VSIXInstaller.exe NOT FOUND >> "%LOG%"
echo VSIXInstaller.exe not found. See: %LOG%
exit /b 3
