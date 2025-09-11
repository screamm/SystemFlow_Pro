@echo off
echo ========================================
echo  SystemFlow Pro v1.0.8 Release Build  
echo ========================================
echo.

:: Set build configuration
set CONFIG=Release
set OUTPUT_DIR=releases\v1.0.8

:: Create release directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo [1/4] Cleaning previous builds...
dotnet clean --configuration %CONFIG%
if %ERRORLEVEL% neq 0 goto :error

echo [2/4] Restoring dependencies...
dotnet restore
if %ERRORLEVEL% neq 0 goto :error

echo [3/4] Building SystemFlow Pro v1.0.8...
dotnet build --configuration %CONFIG% --no-restore --verbosity minimal
if %ERRORLEVEL% neq 0 goto :error

echo [4/4] Publishing release package...
dotnet publish --configuration %CONFIG% --framework net9.0-windows --output "%OUTPUT_DIR%" --no-build --verbosity minimal
if %ERRORLEVEL% neq 0 goto :error

echo.
echo ========================================
echo  SystemFlow Pro v1.0.8 Build Complete
echo ========================================
echo.
echo Release location: %OUTPUT_DIR%
echo Executable: %OUTPUT_DIR%\SystemFlow-Pro.exe
echo.

:: Show file size
for %%F in ("%OUTPUT_DIR%\SystemFlow-Pro.exe") do echo File size: %%~zF bytes

echo.
echo Features in v1.0.8:
echo - Fixed GPU fan detection on modern Windows 11 systems
echo - Improved hardware sensor detection logic
echo - Better percentage to RPM conversion for modern GPUs
echo - Enhanced null checking for sensor values
echo - All previous v1.0.7 features included
echo.
echo Build completed successfully!
goto :end

:error
echo.
echo ========================================
echo  Build Failed!
echo ========================================
echo Error occurred during build process.
echo Check the output above for details.
echo.
pause
exit /b 1

:end
pause