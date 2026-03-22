#!/bin/bash
# Run all microservices locally (requires .NET 8 SDK and running Docker)
set -e

echo "=================================================="
echo " ShopHub E-Commerce Platform — Local Runner"
echo "=================================================="

# Start PostgreSQL and Redis
echo "Starting PostgreSQL and Redis..."
docker compose up -d postgres redis
echo "Waiting for databases to be ready..."
sleep 8

BASE=$(pwd)

run_service() {
    local name=$1
    local project=$2
    local port=$3
    echo "Starting $name on port $port..."
    cd "$BASE/$project"
    ASPNETCORE_URLS="http://localhost:$port" dotnet run --no-build &
    cd "$BASE"
    sleep 2
}

# Build all first
echo ""
echo "Building solution..."
dotnet build ECommerceApp.sln -c Release -v quiet

echo ""
echo "Starting services..."
run_service "Identity API"   "src/Services/Identity/Identity.Api"         5001
run_service "Product API"    "src/Services/ProductAPI/Product.Api"        5002
run_service "Order API"      "src/Services/OrderAPI/Order.Api"            5003
run_service "Cart API"       "src/Services/ShoppingCartAPI/Cart.Api"      5004
run_service "Coupon API"     "src/Services/CouponAPI/Coupon.Api"          5005
run_service "Payment API"    "src/Services/PaymentAPI/Payment.Api"        5006
run_service "Email Service"  "src/Services/EmailService/Email.Api"        5007

echo ""
echo "=================================================="
echo " ALL SERVICES RUNNING"
echo "=================================================="
echo ""
echo " Identity API  -> http://localhost:5001/swagger"
echo " Product API   -> http://localhost:5002/swagger"
echo " Order API     -> http://localhost:5003/swagger"
echo " Cart API      -> http://localhost:5004/swagger"
echo " Coupon API    -> http://localhost:5005/swagger"
echo " Payment API   -> http://localhost:5006/swagger"
echo " Email Service -> http://localhost:5007/swagger"
echo ""
echo " Press Ctrl+C to stop all services"
echo ""

wait
