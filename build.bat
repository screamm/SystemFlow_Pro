@echo off
echo Bygger SystemMonitor med den nya moderna designen...
echo.

REM Försök med Visual Studio 2022 Community
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    echo Använder Visual Studio 2022 Community MSBuild...
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Debug /restore
    goto :check_result
)

REM Försök med Visual Studio 2022 Professional
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    echo Använder Visual Studio 2022 Professional MSBuild...
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Debug /restore
    goto :check_result
)

REM Försök med Build Tools
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    echo Använder Build Tools MSBuild...
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Debug /restore
    goto :check_result
)

echo VARNING: Kunde inte hitta MSBuild. Öppna projektet i Visual Studio istället.
echo Dubbelklicka på SystemMonitorApp.csproj för att öppna i Visual Studio.
pause
exit /b 1

:check_result
echo.
if exist "bin\Debug\net9.0-windows\SystemMonitorApp.exe" (
    echo ✅ LYCKADES! Den nya moderna designen är nu kompilerad.
    echo.
    echo Startar applikationen med den nya designen...
    start "" "bin\Debug\net9.0-windows\SystemMonitorApp.exe"
) else (
    echo ❌ Något gick fel vid byggandet.
    echo Försök öppna SystemMonitorApp.csproj i Visual Studio och bygg manuellt.
)

echo.
pause 