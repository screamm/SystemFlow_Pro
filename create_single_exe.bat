@echo off
echo Building single-file SystemFlow Pro...
echo.

REM Create true single-file executable
& "C:\Program Files\dotnet\dotnet.exe" publish SystemMonitorApp.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -o publish-single-final

echo.
if exist "publish-single-final\SystemMonitorApp.exe" (
    echo SUCCESS: Single-file EXE created!
    dir "publish-single-final\SystemMonitorApp.exe"
    echo.
    echo File location: publish-single-final\SystemMonitorApp.exe
    echo Size: ~130MB (includes everything!)
    echo.
    echo Ready for distribution - just copy this ONE file!
) else (
    echo ERROR: Build failed!
)

pause 