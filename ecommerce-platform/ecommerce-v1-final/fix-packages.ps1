# fix-packages.ps1
# Run from the shop\ root directory: powershell -ExecutionPolicy Bypass -File fix-packages.ps1
# Fixes NU1605 (EF Core version conflict) and NU1903 (security vulnerability) errors

Write-Host "Fixing NuGet package versions in all .csproj files..." -ForegroundColor Cyan

$replacements = @(
    # EF Core: 8.0.0 -> 8.0.10 (fixes NU1605 downgrade conflict with Npgsql)
    @{ From = 'Include="Microsoft.EntityFrameworkCore" Version="8.0.0"';   To = 'Include="Microsoft.EntityFrameworkCore" Version="8.0.10"' },
    @{ From = 'Include="Microsoft.EntityFrameworkCore" Version="8.0.1"';   To = 'Include="Microsoft.EntityFrameworkCore" Version="8.0.10"' },
    @{ From = 'Include="Microsoft.EntityFrameworkCore" Version="8.0.2"';   To = 'Include="Microsoft.EntityFrameworkCore" Version="8.0.10"' },

    # EF Design: same fix
    @{ From = 'Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0"'; To = 'Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10"' },
    @{ From = 'Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.2"'; To = 'Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10"' },

    # Npgsql: 8.0.2 -> 8.0.10 (fixes GHSA-x9vc-6hfv-hg8c)
    @{ From = 'Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.2"'; To = 'Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10"' },

    # JWT: 7.x -> 8.3.2 (fixes GHSA-59j7-ghrg-fj52)
    @{ From = 'Include="System.IdentityModel.Tokens.Jwt" Version="7.3.1"';  To = 'Include="System.IdentityModel.Tokens.Jwt" Version="8.3.2"' },
    @{ From = 'Include="Microsoft.IdentityModel.Tokens" Version="7.3.1"';   To = 'Include="Microsoft.IdentityModel.Tokens" Version="8.3.2"' },

    # JwtBearer: 8.0.0 -> 8.0.10 (fixes GHSA-qj66-m88j-hmgj via Memory fix)
    @{ From = 'Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0"'; To = 'Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10"' },

    # StackExchange Redis cache: 8.0.0 -> 8.0.10
    @{ From = 'Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0"'; To = 'Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.10"' },

    # MediatR
    @{ From = 'Include="MediatR" Version="12.2.0"'; To = 'Include="MediatR" Version="12.4.1"' },

    # FluentValidation
    @{ From = 'Include="FluentValidation" Version="11.9.0"';      To = 'Include="FluentValidation" Version="11.11.0"' },
    @{ From = 'Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0"'; To = 'Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0"' }
)

$csprojFiles = Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.FullName -notlike "*\obj\*" }
Write-Host "Found $($csprojFiles.Count) .csproj files" -ForegroundColor Gray

$fixedCount = 0
foreach ($file in $csprojFiles) {
    $content  = Get-Content $file.FullName -Raw
    $original = $content

    foreach ($r in $replacements) {
        $content = $content.Replace($r.From, $r.To)
    }

    if ($content -ne $original) {
        Set-Content $file.FullName $content -NoNewline
        Write-Host "  Fixed: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
}

Write-Host ""
Write-Host "Done! Fixed $fixedCount files." -ForegroundColor Green
Write-Host ""
Write-Host "Now run: dotnet restore" -ForegroundColor Cyan
