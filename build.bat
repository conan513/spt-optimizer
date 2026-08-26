@echo off
setlocal
cd /d "%~dp0"

echo Running SPT Optimizer Build...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed with error code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo.
echo Build finished successfully.
