@echo off
title BookingSystem API
cd /d "%~dp0"

echo.
echo  ============================================
echo    Booking System  .NET 8 + EF Core
echo  ============================================
echo.

where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET 8 SDK not found.
    echo Download: https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)

echo .NET SDK version:
dotnet --version
echo.

echo [1/3] Restoring packages...
dotnet restore src\BookingSystem.API\BookingSystem.API.csproj
if %ERRORLEVEL% NEQ 0 ( echo RESTORE FAILED & pause & exit /b 1 )

echo [2/3] Building...
dotnet build src\BookingSystem.API\BookingSystem.API.csproj -c Debug
if %ERRORLEVEL% NEQ 0 ( echo BUILD FAILED & pause & exit /b 1 )

echo [3/3] Launching API via dotnet exec...
echo.
echo   Swagger UI  --  http://localhost:5000
echo   Health      --  http://localhost:5000/health
echo   Full Demo   --  POST http://localhost:5000/api/demo/full-flow
echo   Events Log  --  GET  http://localhost:5000/api/events
echo.
echo   Press Ctrl+C to stop
echo.

:: Delete old DB so EnsureCreated runs fresh (safe to remove this line after first run)
if exist src\BookingSystem.API\bin\Debug\net8.0\bookingsystem.db (
    del /f src\BookingSystem.API\bin\Debug\net8.0\bookingsystem.db
    echo   Old database deleted - will be recreated with seed data
    echo.
)

dotnet exec src\BookingSystem.API\bin\Debug\net8.0\BookingSystem.API.dll
pause
