@echo off
title ShopHub - Start All Services
color 0A
cd /d %~dp0

echo ================================================
echo   ShopHub - Starting Infrastructure
echo ================================================
echo.
echo [1/3] Stopping any existing containers...
docker compose down 2>nul
echo.

echo [2/3] Starting PostgreSQL and Redis...
docker compose up -d postgres redis
echo Waiting for PostgreSQL to become healthy (up to 60s)...
timeout /t 5 /nobreak > nul

:wait_loop
docker inspect shophub-postgres --format "{{.State.Health.Status}}" 2>nul | findstr "healthy" > nul
if errorlevel 1 (
    echo   still waiting...
    timeout /t 3 /nobreak > nul
    goto wait_loop
)
echo PostgreSQL is healthy.
echo.

echo [3/3] Creating service databases...
docker compose up postgres-init
echo.

echo ================================================
echo   Starting microservices (each in own window)
echo ================================================
echo.

start "Identity  :5001" cmd /k "cd src\Services\Identity\Identity.Api && dotnet run"
timeout /t 5 /nobreak > nul

start "Product   :5002" cmd /k "cd src\Services\ProductAPI\Product.Api && dotnet run"
timeout /t 4 /nobreak > nul

start "Order     :5003" cmd /k "cd src\Services\OrderAPI\Order.Api && dotnet run"
timeout /t 4 /nobreak > nul

start "Cart      :5004" cmd /k "cd src\Services\ShoppingCartAPI\Cart.Api && dotnet run"
timeout /t 4 /nobreak > nul

start "Coupon    :5005" cmd /k "cd src\Services\CouponAPI\Coupon.Api && dotnet run"
timeout /t 4 /nobreak > nul

start "Payment   :5006" cmd /k "cd src\Services\PaymentAPI\Payment.Api && dotnet run"
timeout /t 4 /nobreak > nul

echo.
echo ================================================
echo   ALL SERVICES LAUNCHING
echo ================================================
echo   Wait ~30s for all services to finish startup.
echo.
echo   Identity  -^> http://localhost:5001/swagger
echo   Product   -^> http://localhost:5002/swagger
echo   Order     -^> http://localhost:5003/swagger
echo   Cart      -^> http://localhost:5004/swagger
echo   Coupon    -^> http://localhost:5005/swagger
echo   Payment   -^> http://localhost:5006/swagger
echo.
echo   DEMO endpoint (runs full Identity flow):
echo   POST http://localhost:5001/api/v1/demo/complete-flow
echo.
pause
