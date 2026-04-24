@echo off
echo Building SystemFlow Pro v1.0.3 Release with improved hardware detection...
echo.

REM Clean old builds first
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "obj\Release" rmdir /s /q "obj\Release"

echo Cleaning old artifacts...
echo.

REM Try Visual Studio 2022 Community
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    echo Using Visual Studio 2022 Community MSBuild...
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Release /restore
    goto :check_result
)

REM Try Visual Studio 2022 Professional
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    echo Using Visual Studio 2022 Professional MSBuild...
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Release /restore
    goto :check_result
)

REM Try Build Tools
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    echo Using Build Tools MSBuild...
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Release /restore
    goto :check_result
)

echo WARNING: Could not find MSBuild. Open the project in Visual Studio instead.
echo Double-click SystemMonitorApp.csproj to open in Visual Studio.
pause
exit /b 1

:check_result
echo.
if exist "bin\Release\net9.0-windows\SystemFlow-Pro.exe" (
    echo SUCCESS! SystemFlow Pro v1.0.3 Release is now compiled.
    echo.
    echo Creating release folder...
    if not exist "releases" mkdir "releases"
    if not exist "releases\v1.0.3" mkdir "releases\v1.0.3"

    echo Copying files to the release folder...
    xcopy "bin\Release\net9.0-windows\*" "releases\v1.0.3\" /s /e /i /y

    echo.
    echo SystemFlow Pro v1.0.3 Release ready!
    echo.
    echo New features in this version:
    echo - Improved hardware detection for older systems
    echo - Multiple detection strategies (LibreHardwareMonitor + WMI + temperature-based)
    echo - Better error messages with administrator status information
    echo - Improved compatibility with older GPU/CPU drivers
    echo - Temperature-based fan estimation for older hardware
    echo - Detailed detection status in the user interface
    echo.
    echo Release is located in: releases\v1.0.3\
    echo.
    echo Do you want to start the new version? (J/N)
    set /p choice=
    if /i "%choice%"=="J" start "" "releases\v1.0.3\SystemFlow-Pro.exe"
    if /i "%choice%"=="j" start "" "releases\v1.0.3\SystemFlow-Pro.exe"
) else (
    echo Something went wrong during the build.
    echo Try opening SystemMonitorApp.csproj in Visual Studio and build manually.
    if exist "bin\Release\net9.0-windows\" dir "bin\Release\net9.0-windows\"
)

echo.
pause
