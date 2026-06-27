@echo off
cls

echo ========================================
echo VS.Helper Green Line Clean Build
echo ========================================

echo.
echo [1/5] Removing bin / obj / .vs ...
for /d /r %%d in (bin,obj) do @if exist "%%d" rmdir /s /q "%%d"
if exist ".vs" rmdir /s /q ".vs"

echo.
echo [2/5] dotnet clean ...
dotnet clean
if errorlevel 1 goto fail

echo.
echo [3/5] dotnet restore ...
dotnet restore
if errorlevel 1 goto fail

echo.
echo [4/5] dotnet build ...
dotnet build
if errorlevel 1 goto fail

echo.
echo [5/5] SUCCESS: build completed with 0 errors.
exit /b 0

:fail
echo.
echo FAILED: clean build stopped with errors.
exit /b 1
