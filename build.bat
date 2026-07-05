@echo off
REM Build script for UNI-T 161E Multimeter Application
REM This script builds the project and creates an executable

setlocal enabledelayedexpansion

echo.
echo ========================================
echo UNI-T 161E Multimeter - Build Script
echo ========================================
echo.

REM Check if dotnet CLI is installed
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET 6.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Display dotnet version
echo Detected .NET SDK:
dotnet --version
echo.

REM Clean previous build
echo [1/3] Cleaning previous build...
dotnet clean -c Release >nul 2>nul
if exist "bin\Release" rmdir /s /q "bin\Release" >nul 2>nul
echo Done.
echo.

REM Restore dependencies
echo [2/3] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Error: Failed to restore packages
    pause
    exit /b 1
)
echo Done.
echo.

REM Build the project
echo [3/3] Building Release executable...
dotnet publish -c Release -o "bin\Release\publish" --self-contained false
if %errorlevel% neq 0 (
    echo Error: Build failed
    pause
    exit /b 1
)
echo Done.
echo.

REM Check if executable was created
if exist "bin\Release\publish\UniT161E.exe" (
    echo.
    echo ========================================
    echo Build Successful!
    echo ========================================
    echo.
    echo Executable created at:
    echo bin\Release\publish\UniT161E.exe
    echo.
    echo File size: 
    for %%A in ("bin\Release\publish\UniT161E.exe") do echo %%~zA bytes
    echo.
    echo Note: .NET 6.0 Runtime must be installed on target systems
    echo Download from: https://dotnet.microsoft.com/download/dotnet/6.0
    echo.
    pause
) else (
    echo Error: Executable not found after build
    pause
    exit /b 1
)
