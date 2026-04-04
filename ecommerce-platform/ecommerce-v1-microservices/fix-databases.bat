@echo off
echo ================================================
echo  ShopHub - Create all databases
echo ================================================
echo.
echo This creates all 5 service databases in PostgreSQL.
echo Run this ONCE after "docker compose up -d postgres".
echo.

REM Wait a few seconds in case postgres just started
timeout /t 5 /nobreak > nul

echo Creating databases...
docker exec shophub-postgres psql -U ecommerce -c "CREATE DATABASE identity_db;" 2>nul
docker exec shophub-postgres psql -U ecommerce -c "CREATE DATABASE product_db;"  2>nul
docker exec shophub-postgres psql -U ecommerce -c "CREATE DATABASE order_db;"    2>nul
docker exec shophub-postgres psql -U ecommerce -c "CREATE DATABASE coupon_db;"   2>nul
docker exec shophub-postgres psql -U ecommerce -c "CREATE DATABASE payment_db;"  2>nul

echo.
echo Verifying...
docker exec shophub-postgres psql -U ecommerce -c "\l" | findstr "_db"
echo.
echo Done! All databases created.
echo You can now run: dotnet run
pause
