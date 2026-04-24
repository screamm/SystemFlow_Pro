@echo off
setlocal enabledelayedexpansion

REM SystemFlow Pro ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â unified build script.
REM Replaces build_release_v1.0.2.bat .. build_release_v1.0.8.bat.
REM
REM Usage: build.bat [version] [--no-compress]
REM
REM Examples:
REM   build.bat 1.1.0                Build release zip
REM   build.bat 1.1.0 --no-compress  Build, keep uncompressed folder only
REM
REM Requires: .NET 9 SDK (pinned via global.json).
REM Note: builds are unsigned ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â SystemFlow Pro is distributed as portable
REM open source. End users may see a SmartScreen warning on first run
REM (documented in README and FAQ).

if "%~1"=="" (
    echo Usage: build.bat [version] [--no-compress]
    exit /b 1
)

set VERSION=%~1
set COMPRESS=1

:parse_args
shift
if "%~1"=="" goto end_parse
if /I "%~1"=="--no-compress" set COMPRESS=0
goto parse_args
:end_parse

set OUTPUT_DIR=publish\v%VERSION%
set ZIP_NAME=SystemFlow-Pro-v%VERSION%-win-x64.zip
set ZIP_PATH=releases\%ZIP_NAME%

echo ============================================================
echo Building SystemFlow Pro v%VERSION%
echo   Compress: %COMPRESS%
echo   Output:   %OUTPUT_DIR%
echo ============================================================
echo.

echo [1/5] Cleaning previous build...
dotnet clean SystemMonitorApp.csproj -c Release --nologo --verbosity quiet
if errorlevel 1 (
    echo ERROR: Clean failed.
    exit /b 1
)

REM Split semver into numeric core (Major.Minor.Patch) and full (with -suffix).
REM AssemblyVersion/FileVersion accept only numeric 4-part values.
for /f "tokens=1 delims=-" %%A in ("%VERSION%") do set VERSION_CORE=%%A

echo [2/5] Publishing self-contained single-file...
dotnet publish SystemMonitorApp.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:Version=%VERSION% ^
    -p:AssemblyVersion=%VERSION_CORE%.0 ^
    -p:FileVersion=%VERSION_CORE%.0 ^
    -p:InformationalVersion=%VERSION% ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o %OUTPUT_DIR% ^
    --nologo
if errorlevel 1 (
    echo ERROR: Publish failed.
    exit /b 1
)

echo [3/4] Copying license documents...
if exist LICENSE (copy /Y LICENSE %OUTPUT_DIR%\LICENSE.txt >nul)
if exist THIRD_PARTY_LICENSES.txt (copy /Y THIRD_PARTY_LICENSES.txt %OUTPUT_DIR%\ >nul)
if exist PRIVACY.md (copy /Y PRIVACY.md %OUTPUT_DIR%\ >nul)

if "%COMPRESS%"=="1" (
    echo [4/4] Creating zip...
    if not exist releases mkdir releases
    if exist %ZIP_PATH% del %ZIP_PATH%
    powershell -NoProfile -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%ZIP_PATH%' -Force"
    if errorlevel 1 (
        echo ERROR: Zip creation failed.
        exit /b 1
    )
    echo   Created: %ZIP_PATH%
) else (
    echo [4/4] Skipping zip ^(--no-compress^)
)

echo.
echo ============================================================
echo Build complete: v%VERSION%
echo   Executable: %OUTPUT_DIR%\SystemFlow-Pro.exe
if "%COMPRESS%"=="1" echo   Distribution: %ZIP_PATH%
echo ============================================================
endlocal
