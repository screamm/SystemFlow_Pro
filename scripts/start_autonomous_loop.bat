@echo off
echo ========================================
echo Claude Code Autonomous Loop Launcher
echo ========================================
echo.

:: Check if goal was provided
if "%~1"=="" (
    echo Usage: start_autonomous_loop.bat "Your Goal" [hours] [context_file]
    echo.
    echo Examples:
    echo   start_autonomous_loop.bat "Optimize all application performance" 5
    echo   start_autonomous_loop.bat "Create comprehensive test suite" 3 custom_context.md
    echo.
    pause
    exit /b 1
)

:: Set parameters
set GOAL=%~1
set HOURS=%~2
if "%HOURS%"=="" set HOURS=5
set CONTEXT=%~3
if "%CONTEXT%"=="" set CONTEXT=loop_context.md

echo Starting autonomous loop:
echo - Goal: %GOAL%
echo - Duration: %HOURS% hours
echo - Context: %CONTEXT%
echo.
echo Press Ctrl+C to stop the loop at any time.
echo ========================================
echo.

:: Run PowerShell script
powershell -ExecutionPolicy Bypass -File "%~dp0claude_autonomous_loop.ps1" -Goal "%GOAL%" -Hours %HOURS% -ContextFile "%CONTEXT%"

pause