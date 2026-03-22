@echo off
REM fix-packages.bat - fixes EF Core version conflict
REM Run from the shop\ root directory

echo Patching .csproj files to fix NU1605 and security warnings...
echo.

REM Use PowerShell's replace from CMD
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-ChildItem -Recurse -Filter '*.csproj' | ForEach-Object { $c = Get-Content $_.FullName -Raw; $orig = $c; $c = $c -replace 'Version=""8\.0\.0""', 'Version=""8.0.10""'; $c = $c -replace 'Version=""8\.0\.2""', 'Version=""8.0.10""'; $c = $c -replace 'Version=""7\.3\.1""', 'Version=""8.3.2""'; if ($c -ne $orig) { Set-Content $_.FullName $c -NoNewline; Write-Host 'Fixed:' $_.Name } }"

echo.
echo Done! Now run: dotnet restore
pause
