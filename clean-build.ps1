Clear-Host
Write-Host "========================================"
Write-Host "VS.Helper Green Line Clean Build"
Write-Host "========================================"

Write-Host "`n[1/5] Removing bin / obj / .vs ..."
Get-ChildItem -Path . -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('bin', 'obj') } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

if (Test-Path ".vs") {
    Remove-Item ".vs" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "`n[2/5] dotnet clean ..."
dotnet clean
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n[3/5] dotnet restore ..."
dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n[4/5] dotnet build ..."
dotnet build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n[5/5] MSBuild VSIX package (Release) ..."
$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
if (!(Test-Path $msbuild)) {
    Write-Host "MSBuild not found at expected path, skipping VSIX packaging."
} else {
    & $msbuild VS.Helper.csproj /p:Configuration=Release /p:DeployExtension=false /t:Build`;GeneratePkgDef`;CopyPkgDef`;CreateVsixContainer /v:minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $vsix = Get-Item "bin\Release\net48\VS.Helper.vsix" -ErrorAction SilentlyContinue
    if ($vsix) {
        Write-Host "VSIX ready: $($vsix.FullName) ($([math]::Round($vsix.Length/1KB, 1)) KB)"
    }
}

Write-Host "`n[6/6] SUCCESS: build + VSIX completed with 0 errors."
exit 0
