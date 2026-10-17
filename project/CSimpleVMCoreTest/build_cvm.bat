@echo off
REM Build the C VM vcxproj using VS MSBuild (found via vswhere).
REM Usage: build_cvm.bat <vcxproj-path> <Configuration>

setlocal enabledelayedexpansion

set "VCXPROJ=%~1"
set "CONFIG=%~2"

if "%VCXPROJ%"=="" (
    echo Error: missing vcxproj path argument
    exit /b 1
)
if "%CONFIG%"=="" (
    echo Error: missing configuration argument
    exit /b 1
)

set "PF86=%ProgramFiles(x86)%"
if not exist "%PF86%" set "PF86=%ProgramW6432%"

set "VSWHERE=%PF86%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo Error: vswhere.exe not found at "%VSWHERE%"
    exit /b 1
)

REM Use temp file to avoid for/f backtick quoting issues with spaces in paths
set "TMPFILE=%TEMP%\vswhere_out_%RANDOM%.txt"
"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" > "%TMPFILE%" 2>&1

set "MSBUILD="
for /f "usebackq delims=" %%i in ("%TMPFILE%") do (
    if not defined MSBUILD set "MSBUILD=%%i"
)

if exist "%TMPFILE%" del "%TMPFILE%"

if not defined MSBUILD (
    echo Error: MSBuild.exe not found via vswhere
    exit /b 1
)

if not exist "%MSBUILD%" (
    echo Error: MSBuild.exe not found at "%MSBUILD%"
    exit /b 1
)

echo [build_cvm] Building: "%MSBUILD%" "%VCXPROJ%" /p:Configuration=%CONFIG% /p:Platform=x64 /v:minimal
"%MSBUILD%" "%VCXPROJ%" /p:Configuration=%CONFIG% /p:Platform=x64 /v:minimal
exit /b %ERRORLEVEL%
