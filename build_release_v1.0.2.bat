@echo off
echo Bygger SystemFlow Pro v1.0.2 Release med GPU RPM och temperatur fixes...
echo.

REM Rensa gamla builds först
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "obj\Release" rmdir /s /q "obj\Release"

echo Rensar gamla artifacts...
echo.

REM Försök med Visual Studio 2022 Community
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    echo Använder Visual Studio 2022 Community MSBuild...
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Release /restore
    goto :check_result
)

REM Försök med Visual Studio 2022 Professional
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    echo Använder Visual Studio 2022 Professional MSBuild...
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Release /restore
    goto :check_result
)

REM Försök med Build Tools
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    echo Använder Build Tools MSBuild...
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SystemMonitorApp.csproj /p:Configuration=Release /restore
    goto :check_result
)

echo VARNING: Kunde inte hitta MSBuild. Öppna projektet i Visual Studio istället.
echo Dubbelklicka på SystemMonitorApp.csproj för att öppna i Visual Studio.
pause
exit /b 1

:check_result
echo.
if exist "bin\Release\net9.0-windows\SystemFlow-Pro.exe" (
    echo ✅ LYCKADES! SystemFlow Pro v1.0.2 Release är nu kompilerad.
    echo.
    echo Skapar release-mapp...
    if not exist "releases" mkdir "releases"
    if not exist "releases\v1.0.2" mkdir "releases\v1.0.2"
    
    echo Kopierar filer till release-mappen...
    xcopy "bin\Release\net9.0-windows\*" "releases\v1.0.2\" /s /e /i /y
    
    echo.
    echo 🎉 SystemFlow Pro v1.0.2 Release färdig!
    echo.
    echo Fixes i denna version:
    echo • GPU RPM x100 problem fixat (äldre system)
    echo • CPU temperatur text klippning fixat
    echo • Förbättrad sensor validering
    echo • Bättre formatering för äldre hårdvara
    echo.
    echo Release finns i: releases\v1.0.2\
    echo.
    echo Vill du starta den nya versionen? (J/N)
    set /p choice=
    if /i "%choice%"=="J" start "" "releases\v1.0.2\SystemFlow-Pro.exe"
    if /i "%choice%"=="j" start "" "releases\v1.0.2\SystemFlow-Pro.exe"
) else (
    echo ❌ Något gick fel vid byggandet.
    echo Försök öppna SystemMonitorApp.csproj i Visual Studio och bygg manuellt.
    if exist "bin\Release\net9.0-windows\" dir "bin\Release\net9.0-windows\"
)

echo.
pause