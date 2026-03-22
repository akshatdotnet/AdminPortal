# ShopHub — Start All Services (PowerShell)
# Run from the shop/ root directory

Write-Host "Starting PostgreSQL and Redis..." -ForegroundColor Cyan
docker compose up -d postgres redis
Start-Sleep -Seconds 10
Write-Host "Databases ready." -ForegroundColor Green

$services = @(
    @{ Name="Identity API";    Path="src\Services\Identity\Identity.Api";          Port=5001 },
    @{ Name="Product API";     Path="src\Services\ProductAPI\Product.Api";         Port=5002 },
    @{ Name="Order API";       Path="src\Services\OrderAPI\Order.Api";             Port=5003 },
    @{ Name="Cart API";        Path="src\Services\ShoppingCartAPI\Cart.Api";       Port=5004 },
    @{ Name="Coupon API";      Path="src\Services\CouponAPI\Coupon.Api";           Port=5005 },
    @{ Name="Payment API";     Path="src\Services\PaymentAPI\Payment.Api";         Port=5006 },
    @{ Name="Email Service";   Path="src\Services\EmailService\Email.Api";         Port=5007 }
)

foreach ($svc in $services) {
    Write-Host "Starting $($svc.Name) on port $($svc.Port)..." -ForegroundColor Yellow
    $env:ASPNETCORE_URLS = "http://localhost:$($svc.Port)"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$($svc.Path)'; `$env:ASPNETCORE_URLS='http://localhost:$($svc.Port)'; dotnet run" -WindowStyle Normal
    Start-Sleep -Seconds 3
}

Write-Host ""
Write-Host "All services launched!" -ForegroundColor Green
Write-Host "Swagger UIs (wait ~30s for startup):" -ForegroundColor Cyan
Write-Host "  Identity  -> http://localhost:5001/swagger"
Write-Host "  Product   -> http://localhost:5002/swagger"
Write-Host "  Order     -> http://localhost:5003/swagger"
Write-Host "  Cart      -> http://localhost:5004/swagger"
Write-Host "  Coupon    -> http://localhost:5005/swagger"
Write-Host "  Payment   -> http://localhost:5006/swagger"
Write-Host "  Email     -> http://localhost:5007/swagger"
