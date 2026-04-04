@echo off
echo ==============================================
echo  ShopHub - Starting All Microservices
echo ==============================================

REM Make sure Docker is running first
echo Starting PostgreSQL and Redis via Docker...
docker compose up -d postgres redis
timeout /t 10 /nobreak > nul
echo Databases ready.

REM Start each service in its own window
echo Starting Identity API on port 5001...
start "Identity API :5001" cmd /k "cd src\Services\Identity\Identity.Api && dotnet run"

timeout /t 3 /nobreak > nul
echo Starting Product API on port 5002...
start "Product API :5002" cmd /k "cd src\Services\ProductAPI\Product.Api && dotnet run"

timeout /t 3 /nobreak > nul
echo Starting Order API on port 5003...
start "Order API :5003" cmd /k "cd src\Services\OrderAPI\Order.Api && dotnet run"

timeout /t 3 /nobreak > nul
echo Starting Cart API on port 5004...
start "Cart API :5004" cmd /k "cd src\Services\ShoppingCartAPI\Cart.Api && dotnet run"

timeout /t 3 /nobreak > nul
echo Starting Coupon API on port 5005...
start "Coupon API :5005" cmd /k "cd src\Services\CouponAPI\Coupon.Api && dotnet run"

timeout /t 3 /nobreak > nul
echo Starting Payment API on port 5006...
start "Payment API :5006" cmd /k "cd src\Services\PaymentAPI\Payment.Api && dotnet run"

timeout /t 3 /nobreak > nul
echo Starting Email Service on port 5007...
start "Email Service :5007" cmd /k "cd src\Services\EmailService\Email.Api && dotnet run"

echo.
echo ==============================================
echo  ALL SERVICES STARTING IN SEPARATE WINDOWS
echo ==============================================
echo.
echo  Identity API  -^> http://localhost:5001/swagger
echo  Product API   -^> http://localhost:5002/swagger
echo  Order API     -^> http://localhost:5003/swagger
echo  Cart API      -^> http://localhost:5004/swagger
echo  Coupon API    -^> http://localhost:5005/swagger
echo  Payment API   -^> http://localhost:5006/swagger
echo  Email Service -^> http://localhost:5007/swagger
echo.
echo  Wait ~30 seconds for all services to finish startup.
pause
