# ShopHub - Complete Local Setup & Testing Guide

## Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` should show 9.x)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (running)
- Any terminal (CMD, PowerShell, or bash)

---

## Step 1 — Extract & Open

Extract the archive, then open a terminal **in the `shop/` folder**:
```
cd path	o\shop
```

---

## Step 2 — Start Databases (Docker)

```cmd
docker compose up -d postgres redis
```

Wait ~10 seconds. To verify:
```cmd
docker compose ps
```
Both `shophub-postgres` and `shophub-redis` should show `healthy`.

---

## Step 3 — Run Identity Service (first)

Open a terminal and run:
```cmd
cd src\Services\Identity\Identity.Api
dotnet run
```

Wait until you see:
```
Now listening on: http://localhost:5001
```

Open: **http://localhost:5001/swagger**

---

## Step 4 — Test Identity with ONE Click (Demo Endpoint)

In Swagger, find **POST /api/v1/demo/complete-flow** and click **Execute**.

This single call:
1. Registers an Admin user
2. Registers a Customer user
3. Logs in as Customer
4. Refreshes the token
5. Verifies duplicate registration is rejected

All 5 steps return `"success": true` and `"allPassed": true` if Identity is working.

---

## Step 5 — Run All Services

**Option A: Double-click** `START-ALL.bat` (opens each service in its own window)

**Option B: Run individually** (one terminal per service):
```cmd
cd src\Services\ProductAPI\Product.Api   && dotnet run   # :5002
cd src\Services\OrderAPI\Order.Api       && dotnet run   # :5003
cd src\Services\ShoppingCartAPI\Cart.Api && dotnet run   # :5004
cd src\Services\CouponAPI\Coupon.Api     && dotnet run   # :5005
cd src\Services\PaymentAPI\Payment.Api   && dotnet run   # :5006
```

---

## Step 6 — Test Full Workflow via Swagger

### 6a. Register & Login
```
POST http://localhost:5001/api/v1/auth/register
{
  "email": "admin@shop.com",
  "password": "Admin@123!",
  "firstName": "Admin",
  "lastName": "User",
  "phoneNumber": "+14155550001",
  "role": "Admin"
}
```
Copy the `accessToken` from the response.

In Swagger, click **Authorize** (top right) and paste: `Bearer <your-token>`

### 6b. Create Category
```
POST http://localhost:5002/api/v1/categories
{ "name": "Electronics", "description": "Electronic devices" }
```
Copy the returned `categoryId`.

### 6c. Create Product
```
POST http://localhost:5002/api/v1/products
{
  "name": "MacBook Pro 14",
  "description": "Apple M3 chip",
  "sku": "MBP14-M3",
  "price": 1999.99,
  "currency": "USD",
  "stockQuantity": 50,
  "categoryId": "<categoryId>",
  "brand": "Apple"
}
```
Copy the returned `productId`.

### 6d. Create Coupon
```
POST http://localhost:5005/api/v1/coupons
{
  "code": "SAVE100",
  "description": "$100 off orders over $500",
  "discountType": "FixedAmount",
  "discountValue": 100,
  "validFrom": "2024-01-01T00:00:00Z",
  "validTo": "2030-12-31T23:59:59Z",
  "minimumOrderAmount": 500
}
```

### 6e. Register Customer & Get Token
```
POST http://localhost:5001/api/v1/auth/register
{
  "email": "john@test.com",
  "password": "John@123!",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+14155551234"
}
```

### 6f. Add to Cart (with customer token)
```
POST http://localhost:5004/api/v1/cart/items
{
  "productId": "<productId>",
  "productName": "MacBook Pro 14",
  "sku": "MBP14-M3",
  "unitPrice": 1999.99,
  "quantity": 1
}
```

### 6g. Apply Coupon to Cart
```
POST http://localhost:5004/api/v1/cart/coupon
{ "couponCode": "SAVE100" }
```

### 6h. Place Order
```
POST http://localhost:5003/api/v1/orders
{
  "customerId": "<userId from login response>",
  "shippingFullName": "John Doe",
  "shippingStreet": "123 Main St",
  "shippingCity": "New York",
  "shippingState": "NY",
  "shippingPostalCode": "10001",
  "shippingCountry": "US",
  "shippingPhone": "+14155551234",
  "items": [{
    "productId": "<productId>",
    "productName": "MacBook Pro 14",
    "sku": "MBP14-M3",
    "unitPrice": 1999.99,
    "quantity": 1
  }],
  "couponCode": "SAVE100"
}
```
Copy the `orderId`.

### 6i. Create Payment Session
```
POST http://localhost:5006/api/v1/payments/sessions
{
  "orderId": "<orderId>",
  "customerId": "<userId>",
  "amount": 1979.99,
  "currency": "USD",
  "gateway": "Stripe",
  "successUrl": "http://localhost/success",
  "cancelUrl": "http://localhost/cancel"
}
```
Copy the `paymentIntentId`.

### 6j. Confirm Payment (simulates Stripe webhook)
```
POST http://localhost:5003/api/v1/orders/<orderId>/confirm-payment
{ "paymentIntentId": "<paymentIntentId>" }
```

### 6k. Ship Order (admin token required)
```
POST http://localhost:5003/api/v1/orders/<orderId>/ship
{ "trackingNumber": "1Z999AA1012345678", "carrier": "UPS" }
```

### 6l. Verify Final State
```
GET http://localhost:5003/api/v1/orders/<orderId>
```
Status should be `Shipped` with your tracking number.

---

## Run Unit Tests

```cmd
dotnet test tests\Identity.Tests
dotnet test tests\Order.Tests
```

---

## Troubleshooting

| Error | Fix |
|-------|-----|
| `Connection refused :5432` | `docker compose up -d postgres redis` |
| `Database does not exist` | Wait 10s after Docker starts, then run service again |
| `401 Unauthorized` | Token expired — login again and paste new token in Swagger Authorize |
| Port already in use | Another process on that port — `netstat -ano | findstr :5001` |
| `dotnet: command not found` | Install .NET 9 SDK from microsoft.com/dotnet |

---

## Swagger URLs

| Service  | URL |
|----------|-----|
| Identity | http://localhost:5001/swagger |
| Product  | http://localhost:5002/swagger |
| Order    | http://localhost:5003/swagger |
| Cart     | http://localhost:5004/swagger |
| Coupon   | http://localhost:5005/swagger |
| Payment  | http://localhost:5006/swagger |
