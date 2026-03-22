@echo off
title ShopHub - Starting All Services
color 0A

echo ================================================
echo   ShopHub E-Commerce Platform
echo   Starting all microservices...
echo ================================================
echo.

cd /d %~dp0

echo [1/2] Starting PostgreSQL + Redis via Docker...
docker compose up -d postgres redis
echo Waiting 10 seconds for databases to initialize...
timeout /t 10 /nobreak > nul
echo Databases ready.
echo.

echo [2/2] Starting microservices (each in own window)...

start "Identity  :5001" cmd /k "cd src\Services\Identity\Identity.Api && dotnet run"
timeout /t 4 /nobreak > nul

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
echo   ALL SERVICES LAUNCHED
echo ================================================
echo.
echo   Wait ~30 seconds for services to finish startup.
echo   Then open these Swagger UIs:
echo.
echo   Identity  -^> http://localhost:5001/swagger
echo   Product   -^> http://localhost:5002/swagger
echo   Order     -^> http://localhost:5003/swagger
echo   Cart      -^> http://localhost:5004/swagger
echo   Coupon    -^> http://localhost:5005/swagger
echo   Payment   -^> http://localhost:5006/swagger
echo.
echo   DEMO ENDPOINT (tests Identity in one click):
echo   POST http://localhost:5001/api/v1/demo/complete-flow
echo.
pause
