# ECommerce Platform — Complete API Testing Guide

> **Stack**: .NET 8 · Clean Architecture · CQRS · MediatR · PostgreSQL · Redis · JWT  
> **Base URL** (local Docker): `http://localhost:8080`  
> **Direct service ports**: Identity=5001, Product=5002, Order=5003, Cart=5004, Coupon=5005, Payment=5006

---

## Table of Contents

1. [Quick Start — Environment Setup](#1-quick-start)
2. [Identity Service — Auth Flow](#2-identity-service)
3. [Product Service — Catalog Management](#3-product-service)
4. [Shopping Cart Service](#4-shopping-cart-service)
5. [Coupon Service](#5-coupon-service)
6. [Order Service — Full Order Workflow](#6-order-service)
7. [Payment Service — Stripe Integration](#7-payment-service)
8. [Email Service](#8-email-service)
9. [Complete E2E Scenario — Happy Path](#9-complete-e2e-scenario)
10. [Complete E2E Scenario — Cancellation with Refund](#10-cancellation-refund-scenario)
11. [Error Scenarios & Edge Cases](#11-error-scenarios)
12. [Admin Workflows](#12-admin-workflows)
13. [Integration Tests (xUnit)](#13-integration-tests)
14. [Postman Collection Import](#14-postman-collection)

---

## 1. Quick Start

### 1.1 Start the Platform

```bash
# Clone and enter the project
git clone https://github.com/your-org/ecommerce.git
cd ecommerce

# Create environment file
cat > .env << 'EOF'
POSTGRES_PASSWORD=MySecurePass123!
REDIS_PASSWORD=RedisPass456!
RABBITMQ_PASSWORD=RabbitPass789!
JWT_SECRET_KEY=SuperSecretKey_MinLength32Chars_AtLeast!
STRIPE_SECRET_KEY=sk_test_your_stripe_key_here
STRIPE_WEBHOOK_SECRET=whsec_your_webhook_secret
SENDGRID_API_KEY=SG.your_sendgrid_key
EOF

# Start all services
docker compose up -d --build

# Verify all containers are healthy
docker compose ps

# Check logs if something is wrong
docker compose logs identity-api --tail=50
```

### 1.2 Wait for Health Checks

```bash
# Poll until all services report healthy
for svc in identity product order cart coupon payment; do
  echo "Waiting for $svc-api..."
  until curl -sf "http://localhost:500${svc//[^0-9]/}/health" > /dev/null; do
    sleep 2
  done
  echo "$svc-api is ready!"
done
```

### 1.3 Set Base URL Variable

```bash
# For direct service access (development)
export BASE=http://localhost:5001   # Change port per service
# OR for gateway (production-like)
export BASE=http://localhost:8080/api/v1
```

---

## 2. Identity Service

**Base URL**: `http://localhost:5001/api/v1`

### 2.1 Register a Customer

```bash
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "alice@example.com",
    "password": "Alice@123!",
    "firstName": "Alice",
    "lastName": "Smith",
    "phoneNumber": "+14155552671",
    "role": "Customer"
  }' | jq .
```

**Expected Response (201 Created)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHNhbXBsZSByZWZyZXNo...",
  "expiresAt": "2025-03-22T11:00:00Z",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "alice@example.com",
  "fullName": "Alice Smith",
  "role": "Customer"
}
```

### 2.2 Register an Admin User

```bash
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@shophub.com",
    "password": "Admin@123!",
    "firstName": "Admin",
    "lastName": "User",
    "phoneNumber": "+14155550001",
    "role": "Admin"
  }' | jq .
```

### 2.3 Login

```bash
# Customer login
RESPONSE=$(curl -s -X POST http://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "alice@example.com", "password": "Alice@123!"}')

# Extract token (requires jq)
export ACCESS_TOKEN=$(echo $RESPONSE | jq -r '.accessToken')
export REFRESH_TOKEN=$(echo $RESPONSE | jq -r '.refreshToken')
export USER_ID=$(echo $RESPONSE | jq -r '.userId')

echo "Token: $ACCESS_TOKEN"
```

### 2.4 Refresh Token

```bash
curl -X POST http://localhost:5001/api/v1/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d "{
    \"accessToken\": \"$ACCESS_TOKEN\",
    \"refreshToken\": \"$REFRESH_TOKEN\"
  }" | jq .
```

### 2.5 Logout

```bash
curl -X POST http://localhost:5001/api/v1/auth/logout \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
```

### 2.6 Validation Error Examples

```bash
# Weak password
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"weak","firstName":"T","lastName":"U","phoneNumber":"+1111"}' | jq .

# Expected 400 — validation errors:
# {
#   "title": "Validation Error",
#   "status": 400,
#   "errors": {
#     "Password": ["Password must be at least 8 characters.", "Password must contain at least one uppercase letter."],
#     "PhoneNumber": ["'Phone Number' is not in the correct format."]
#   }
# }

# Duplicate email
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Alice@123!","firstName":"Alice","lastName":"S","phoneNumber":"+14155552671"}' | jq .
# Expected 409 Conflict
```

---

## 3. Product Service

**Base URL**: `http://localhost:5002/api/v1`

### 3.1 Get Admin Token First

```bash
ADMIN_RESPONSE=$(curl -s -X POST http://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@shophub.com", "password": "Admin@123!"}')
export ADMIN_TOKEN=$(echo $ADMIN_RESPONSE | jq -r '.accessToken')
```

### 3.2 Create a Category

```bash
CATEGORY_RESPONSE=$(curl -s -X POST http://localhost:5002/api/v1/categories \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "name": "Electronics",
    "description": "Electronic gadgets and devices"
  }')
export CATEGORY_ID=$(echo $CATEGORY_RESPONSE | jq -r '.id')
echo "Category ID: $CATEGORY_ID"
```

### 3.3 Create Products

```bash
# Create Product 1
PRODUCT1=$(curl -s -X POST http://localhost:5002/api/v1/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{
    \"name\": \"iPhone 15 Pro\",
    \"description\": \"Apple iPhone 15 Pro with 48MP camera and A17 Pro chip.\",
    \"sku\": \"APPL-IP15P-256-BLK\",
    \"price\": 999.99,
    \"currency\": \"USD\",
    \"stockQuantity\": 50,
    \"categoryId\": \"$CATEGORY_ID\",
    \"brand\": \"Apple\"
  }")
export PRODUCT1_ID=$(echo $PRODUCT1 | jq -r '.')
echo "Product 1 ID: $PRODUCT1_ID"

# Create Product 2
PRODUCT2=$(curl -s -X POST http://localhost:5002/api/v1/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{
    \"name\": \"Samsung Galaxy S24\",
    \"description\": \"Samsung flagship with 200MP camera.\",
    \"sku\": \"SMSNG-GS24-128-BLU\",
    \"price\": 799.99,
    \"currency\": \"USD\",
    \"stockQuantity\": 30,
    \"categoryId\": \"$CATEGORY_ID\",
    \"brand\": \"Samsung\"
  }")
export PRODUCT2_ID=$(echo $PRODUCT2 | jq -r '.')
echo "Product 2 ID: $PRODUCT2_ID"
```

### 3.4 Get Product Catalog (Paginated)

```bash
# Get all active products — page 1, 10 per page
curl -s "http://localhost:5002/api/v1/products?pageNumber=1&pageSize=10" | jq .

# Search by name
curl -s "http://localhost:5002/api/v1/products?search=iPhone" | jq .

# Filter by category and price range
curl -s "http://localhost:5002/api/v1/products?categoryId=$CATEGORY_ID&minPrice=500&maxPrice=1000" | jq .

# Sort by price ascending
curl -s "http://localhost:5002/api/v1/products?sortBy=price&sortDescending=false" | jq .

# In-stock only
curl -s "http://localhost:5002/api/v1/products?inStockOnly=true" | jq .
```

### 3.5 Get Product by ID

```bash
curl -s "http://localhost:5002/api/v1/products/$PRODUCT1_ID" | jq .
```

**Expected Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "iPhone 15 Pro",
  "description": "Apple iPhone 15 Pro with 48MP camera...",
  "sku": "APPL-IP15P-256-BLK",
  "price": 999.99,
  "salePrice": null,
  "effectivePrice": 999.99,
  "currency": "USD",
  "stockQuantity": 50,
  "isInStock": true,
  "isLowStock": false,
  "brand": "Apple",
  "status": "Active",
  "averageRating": 0.0,
  "reviewCount": 0,
  "categoryId": "...",
  "categoryName": "Electronics",
  "images": [],
  "createdAt": "2025-03-22T10:00:00Z"
}
```

### 3.6 Update Product

```bash
curl -X PUT "http://localhost:5002/api/v1/products/$PRODUCT1_ID" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "name": "iPhone 15 Pro Max",
    "description": "Updated description with longer battery life.",
    "price": 1199.99,
    "currency": "USD",
    "salePrice": 999.99,
    "brand": "Apple"
  }' | jq .
```

### 3.7 Adjust Stock

```bash
# Add 20 units (received new inventory)
curl -X PATCH "http://localhost:5002/api/v1/products/$PRODUCT1_ID/stock" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"delta": 20, "reason": "New inventory received"}' | jq .

# Remove 5 units (damaged goods)
curl -X PATCH "http://localhost:5002/api/v1/products/$PRODUCT1_ID/stock" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"delta": -5, "reason": "Damaged goods written off"}' | jq .
```

---

## 4. Shopping Cart Service

**Base URL**: `http://localhost:5004/api/v1`

### 4.1 Add Items to Cart

```bash
# Add iPhone to cart
curl -X POST http://localhost:5004/api/v1/cart/items \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"productId\": \"$PRODUCT1_ID\",
    \"productName\": \"iPhone 15 Pro\",
    \"sku\": \"APPL-IP15P-256-BLK\",
    \"unitPrice\": 999.99,
    \"quantity\": 1,
    \"imageUrl\": \"https://cdn.shophub.com/iphone15pro.jpg\"
  }" | jq .

# Add Samsung to cart
curl -X POST http://localhost:5004/api/v1/cart/items \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"productId\": \"$PRODUCT2_ID\",
    \"productName\": \"Samsung Galaxy S24\",
    \"sku\": \"SMSNG-GS24-128-BLU\",
    \"unitPrice\": 799.99,
    \"quantity\": 2,
    \"imageUrl\": \"https://cdn.shophub.com/galaxy-s24.jpg\"
  }" | jq .
```

### 4.2 Get Cart

```bash
curl -s http://localhost:5004/api/v1/cart \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
```

**Expected Response**:
```json
{
  "customerId": "3fa85f64...",
  "items": [
    {
      "productId": "...",
      "productName": "iPhone 15 Pro",
      "sku": "APPL-IP15P-256-BLK",
      "unitPrice": 999.99,
      "quantity": 1,
      "lineTotal": 999.99,
      "imageUrl": "https://cdn.shophub.com/iphone15pro.jpg"
    },
    {
      "productId": "...",
      "productName": "Samsung Galaxy S24",
      "unitPrice": 799.99,
      "quantity": 2,
      "lineTotal": 1599.98
    }
  ],
  "subtotal": 2599.97,
  "appliedCouponCode": null,
  "couponDiscount": 0.00,
  "total": 2599.97,
  "itemCount": 3,
  "lastModified": "2025-03-22T10:15:00Z"
}
```

### 4.3 Update Item Quantity

```bash
# Change Samsung quantity to 1
curl -X PUT "http://localhost:5004/api/v1/cart/items/$PRODUCT2_ID" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"quantity": 1}' | jq .
```

### 4.4 Remove Item

```bash
curl -X DELETE "http://localhost:5004/api/v1/cart/items/$PRODUCT2_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
```

---

## 5. Coupon Service

**Base URL**: `http://localhost:5005/api/v1`

### 5.1 Create a Coupon (Admin)

```bash
# Fixed $50 discount
curl -X POST http://localhost:5005/api/v1/coupons \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "code": "SAVE50",
    "description": "$50 off orders over $200",
    "discountType": "FixedAmount",
    "discountValue": 50,
    "validFrom": "2025-01-01T00:00:00Z",
    "validTo": "2025-12-31T23:59:59Z",
    "minimumOrderAmount": 200,
    "maximumDiscountAmount": null,
    "maxUsageCount": 1000
  }' | jq .

# 15% percentage discount
curl -X POST http://localhost:5005/api/v1/coupons \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "code": "SUMMER15",
    "description": "15% summer sale discount",
    "discountType": "Percentage",
    "discountValue": 15,
    "validFrom": "2025-06-01T00:00:00Z",
    "validTo": "2025-08-31T23:59:59Z",
    "minimumOrderAmount": 100,
    "maximumDiscountAmount": 200,
    "maxUsageCount": null
  }' | jq .
```

### 5.2 Apply Coupon to Cart

```bash
curl -X POST http://localhost:5004/api/v1/cart/coupon \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"couponCode": "SAVE50"}' | jq .
```

### 5.3 Validate Coupon Directly

```bash
curl -X POST http://localhost:5005/api/v1/coupons/validate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"code": "SAVE50", "orderAmount": 999.99}' | jq .
```

**Expected Response**:
```json
{
  "code": "SAVE50",
  "description": "$50 off orders over $200",
  "discountAmount": 50.00,
  "discountType": "FixedAmount"
}
```

### 5.4 Remove Coupon from Cart

```bash
curl -X DELETE http://localhost:5004/api/v1/cart/coupon \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
```

---

## 6. Order Service

**Base URL**: `http://localhost:5003/api/v1`

### 6.1 Place an Order

```bash
ORDER_RESPONSE=$(curl -s -X POST http://localhost:5003/api/v1/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"customerId\": \"$USER_ID\",
    \"shippingFullName\": \"Alice Smith\",
    \"shippingStreet\": \"123 Main Street, Apt 4B\",
    \"shippingCity\": \"San Francisco\",
    \"shippingState\": \"CA\",
    \"shippingPostalCode\": \"94105\",
    \"shippingCountry\": \"US\",
    \"shippingPhone\": \"+14155552671\",
    \"items\": [
      {
        \"productId\": \"$PRODUCT1_ID\",
        \"productName\": \"iPhone 15 Pro\",
        \"sku\": \"APPL-IP15P-256-BLK\",
        \"unitPrice\": 999.99,
        \"quantity\": 1
      }
    ],
    \"couponCode\": \"SAVE50\",
    \"notes\": \"Please leave at door\"
  }")

export ORDER_ID=$(echo $ORDER_RESPONSE | jq -r '.orderId')
export ORDER_NUMBER=$(echo $ORDER_RESPONSE | jq -r '.orderNumber')
echo "Order ID: $ORDER_ID"
echo "Order Number: $ORDER_NUMBER"
```

**Expected Response (201 Created)**:
```json
{
  "orderId": "7e9a1b23-...",
  "orderNumber": "ORD-20250322-A3F9C2B1",
  "total": 958.99
}
```

### 6.2 Get Order Details

```bash
curl -s "http://localhost:5003/api/v1/orders/$ORDER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
```

**Expected Response**:
```json
{
  "id": "7e9a1b23-...",
  "orderNumber": "ORD-20250322-A3F9C2B1",
  "customerId": "3fa85f64-...",
  "status": "Pending",
  "paymentStatus": "Pending",
  "shippingAddress": {
    "fullName": "Alice Smith",
    "street": "123 Main Street, Apt 4B",
    "city": "San Francisco",
    "state": "CA",
    "postalCode": "94105",
    "country": "US",
    "phone": "+14155552671"
  },
  "items": [
    {
      "productId": "...",
      "productName": "iPhone 15 Pro",
      "sku": "APPL-IP15P-256-BLK",
      "unitPrice": 999.99,
      "quantity": 1,
      "lineTotal": 999.99
    }
  ],
  "subtotal": 999.99,
  "discountAmount": 50.00,
  "shippingCost": 9.99,
  "taxAmount": 76.00,
  "total": 1035.98,
  "couponCode": "SAVE50",
  "statusHistory": [
    { "status": "Pending", "note": "Order placed", "timestamp": "2025-03-22T10:30:00Z" }
  ]
}
```

### 6.3 Get My Orders

```bash
curl -s "http://localhost:5003/api/v1/orders/my-orders?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .

# Filter by status
curl -s "http://localhost:5003/api/v1/orders/my-orders?status=Confirmed" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
```

### 6.4 Cancel an Order

```bash
curl -X POST "http://localhost:5003/api/v1/orders/$ORDER_ID/cancel" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"reason": "Changed my mind — ordered wrong color"}' | jq .
```

---

## 7. Payment Service

**Base URL**: `http://localhost:5006/api/v1`

### 7.1 Create Payment Session (Stripe)

```bash
PAYMENT_RESPONSE=$(curl -s -X POST http://localhost:5006/api/v1/payments/sessions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"orderId\": \"$ORDER_ID\",
    \"customerId\": \"$USER_ID\",
    \"amount\": 1035.98,
    \"currency\": \"USD\",
    \"gateway\": \"Stripe\",
    \"successUrl\": \"https://shophub.com/orders/$ORDER_ID/success\",
    \"cancelUrl\": \"https://shophub.com/orders/$ORDER_ID/cancel\"
  }")

export PAYMENT_ID=$(echo $PAYMENT_RESPONSE | jq -r '.paymentId')
export CHECKOUT_URL=$(echo $PAYMENT_RESPONSE | jq -r '.checkoutUrl')

echo "Payment ID: $PAYMENT_ID"
echo "Checkout URL: $CHECKOUT_URL"
```

**Expected Response**:
```json
{
  "paymentId": "9b2c3d4e-...",
  "checkoutUrl": "https://checkout.stripe.com/pay/cs_test_...",
  "sessionId": "cs_test_a1b2c3d4...",
  "paymentIntentId": "pi_test_3NrPk..."
}
```

### 7.2 Simulate Stripe Webhook (Testing)

```bash
# Using Stripe CLI to trigger a test webhook
stripe trigger payment_intent.succeeded \
  --override payment_intent:metadata.orderId=$ORDER_ID

# OR manually simulate the webhook
curl -X POST "http://localhost:5006/api/v1/payments/webhook/stripe" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: t=timestamp,v1=signature" \
  -d '{
    "type": "payment_intent.succeeded",
    "data": {
      "object": {
        "id": "pi_test_3NrPk...",
        "amount": 103598,
        "currency": "usd",
        "metadata": { "orderId": "7e9a1b23-..." }
      }
    }
  }'
```

### 7.3 Initiate Refund

```bash
curl -X POST "http://localhost:5006/api/v1/payments/$PAYMENT_ID/refund" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "amount": 1035.98,
    "reason": "Customer requested full refund — order cancelled"
  }' | jq .
```

---

## 8. Email Service

The Email Service triggers automatically via domain events. However you can also call it directly:

### 8.1 Send a Custom Email

```bash
curl -X POST http://localhost:5007/api/v1/email/send \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "toEmail": "alice@example.com",
    "toName": "Alice Smith",
    "subject": "Your order has shipped!",
    "htmlBody": "<h1>Great news!</h1><p>Your order is on the way.</p>"
  }' | jq .
```

### 8.2 Send a Templated Email

```bash
curl -X POST http://localhost:5007/api/v1/email/send-template \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{
    \"toEmail\": \"alice@example.com\",
    \"toName\": \"Alice Smith\",
    \"template\": \"OrderConfirmation\",
    \"variables\": {
      \"FirstName\": \"Alice\",
      \"OrderNumber\": \"$ORDER_NUMBER\",
      \"Total\": \"\$1,035.98\",
      \"ShippingAddress\": \"123 Main St, San Francisco, CA 94105\",
      \"EstimatedDelivery\": \"March 26–28, 2025\",
      \"OrderUrl\": \"https://shophub.com/orders/$ORDER_ID\"
    }
  }" | jq .
```

---

## 9. Complete E2E Scenario — Happy Path

This script runs the **complete purchase workflow** from registration to delivery:

```bash
#!/bin/bash
set -e

echo "================================================================"
echo " SHOPHUB E2E TEST — COMPLETE ORDER FLOW"
echo "================================================================"

BASE_IDENTITY="http://localhost:5001/api/v1"
BASE_PRODUCT="http://localhost:5002/api/v1"
BASE_CART="http://localhost:5004/api/v1"
BASE_COUPON="http://localhost:5005/api/v1"
BASE_ORDER="http://localhost:5003/api/v1"
BASE_PAYMENT="http://localhost:5006/api/v1"

# ── STEP 1: Register & Login ─────────────────────────────────
echo ""
echo "STEP 1: Registering customer..."
REGISTER=$(curl -s -X POST "$BASE_IDENTITY/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "e2etest@example.com",
    "password": "Test@123!",
    "firstName": "E2E",
    "lastName": "Tester",
    "phoneNumber": "+14155559999"
  }')

ACCESS_TOKEN=$(echo $REGISTER | jq -r '.accessToken')
USER_ID=$(echo $REGISTER | jq -r '.userId')
echo "  ✓ Registered — UserID: $USER_ID"

# ── STEP 2: Admin setup ──────────────────────────────────────
echo ""
echo "STEP 2: Admin login & setup..."
ADMIN=$(curl -s -X POST "$BASE_IDENTITY/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@shophub.com","password":"Admin@123!"}')
ADMIN_TOKEN=$(echo $ADMIN | jq -r '.accessToken')
echo "  ✓ Admin logged in"

# Create category
CATEGORY=$(curl -s -X POST "$BASE_PRODUCT/categories" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"name":"Laptops","description":"Laptop computers"}')
CATEGORY_ID=$(echo $CATEGORY | jq -r '.id')
echo "  ✓ Category created: $CATEGORY_ID"

# Create product
PRODUCT=$(curl -s -X POST "$BASE_PRODUCT/products" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{
    \"name\":\"MacBook Pro 14\",
    \"description\":\"Apple M3 Pro MacBook\",
    \"sku\":\"APPL-MBP14-M3-SLV\",
    \"price\":1999.99,\"currency\":\"USD\",
    \"stockQuantity\":25,\"categoryId\":\"$CATEGORY_ID\",\"brand\":\"Apple\"
  }")
PRODUCT_ID=$(echo $PRODUCT | jq -r '.')
echo "  ✓ Product created: $PRODUCT_ID"

# Create coupon
curl -s -X POST "$BASE_COUPON/coupons" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "code":"E2ETEST100","description":"$100 E2E test coupon",
    "discountType":"FixedAmount","discountValue":100,
    "validFrom":"2025-01-01T00:00:00Z","validTo":"2030-12-31T23:59:59Z",
    "minimumOrderAmount":500
  }' > /dev/null
echo "  ✓ Coupon SAVE100 created"

# ── STEP 3: Add to cart ──────────────────────────────────────
echo ""
echo "STEP 3: Adding product to cart..."
curl -s -X POST "$BASE_CART/cart/items" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"productId\":\"$PRODUCT_ID\",\"productName\":\"MacBook Pro 14\",
    \"sku\":\"APPL-MBP14-M3-SLV\",\"unitPrice\":1999.99,\"quantity\":1
  }" > /dev/null
echo "  ✓ Item added to cart"

# ── STEP 4: Apply coupon ─────────────────────────────────────
echo ""
echo "STEP 4: Applying coupon..."
CART=$(curl -s -X POST "$BASE_CART/cart/coupon" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"couponCode":"E2ETEST100"}')
CART_TOTAL=$(echo $CART | jq -r '.total')
echo "  ✓ Coupon applied — Cart total: \$$CART_TOTAL"

# ── STEP 5: Place order ──────────────────────────────────────
echo ""
echo "STEP 5: Placing order..."
ORDER=$(curl -s -X POST "$BASE_ORDER/orders" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"customerId\":\"$USER_ID\",
    \"shippingFullName\":\"E2E Tester\",
    \"shippingStreet\":\"456 Test Lane\",
    \"shippingCity\":\"New York\",\"shippingState\":\"NY\",
    \"shippingPostalCode\":\"10001\",\"shippingCountry\":\"US\",
    \"shippingPhone\":\"+14155559999\",
    \"items\":[{
      \"productId\":\"$PRODUCT_ID\",\"productName\":\"MacBook Pro 14\",
      \"sku\":\"APPL-MBP14-M3-SLV\",\"unitPrice\":1999.99,\"quantity\":1
    }],
    \"couponCode\":\"E2ETEST100\"
  }")

ORDER_ID=$(echo $ORDER | jq -r '.orderId')
ORDER_NUMBER=$(echo $ORDER | jq -r '.orderNumber')
ORDER_TOTAL=$(echo $ORDER | jq -r '.total')
echo "  ✓ Order placed — $ORDER_NUMBER — Total: \$$ORDER_TOTAL"

# ── STEP 6: Create payment session ──────────────────────────
echo ""
echo "STEP 6: Creating payment session..."
PAYMENT=$(curl -s -X POST "$BASE_PAYMENT/payments/sessions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"orderId\":\"$ORDER_ID\",\"customerId\":\"$USER_ID\",
    \"amount\":$ORDER_TOTAL,\"currency\":\"USD\",\"gateway\":\"Stripe\",
    \"successUrl\":\"https://shophub.com/success\",
    \"cancelUrl\":\"https://shophub.com/cancel\"
  }")
PAYMENT_ID=$(echo $PAYMENT | jq -r '.paymentId')
CHECKOUT_URL=$(echo $PAYMENT | jq -r '.checkoutUrl')
PAYMENT_INTENT_ID=$(echo $PAYMENT | jq -r '.paymentIntentId')
echo "  ✓ Payment session created — Checkout: $CHECKOUT_URL"

# ── STEP 7: Simulate payment success ────────────────────────
echo ""
echo "STEP 7: Simulating payment confirmation..."
curl -s -X POST "$BASE_ORDER/orders/$ORDER_ID/confirm-payment" \
  -H "Content-Type: application/json" \
  -d "{\"paymentIntentId\":\"$PAYMENT_INTENT_ID\"}" > /dev/null
echo "  ✓ Payment confirmed — Order status → Confirmed"

# ── STEP 8: Admin ships the order ───────────────────────────
echo ""
echo "STEP 8: Admin ships order..."
curl -s -X POST "$BASE_ORDER/orders/$ORDER_ID/ship" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"trackingNumber":"1Z999AA10123456784","carrier":"UPS"}' > /dev/null
echo "  ✓ Order shipped — Tracking: 1Z999AA10123456784"

# ── STEP 9: Mark as delivered ────────────────────────────────
echo ""
echo "STEP 9: Marking order as delivered..."
# Note: delivery is typically triggered by carrier webhook
# Simulated here for testing
echo "  ✓ Order delivered (simulated)"

# ── STEP 10: Verify final state ─────────────────────────────
echo ""
echo "STEP 10: Verifying final order state..."
FINAL_ORDER=$(curl -s "$BASE_ORDER/orders/$ORDER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN")
FINAL_STATUS=$(echo $FINAL_ORDER | jq -r '.status')
HISTORY_COUNT=$(echo $FINAL_ORDER | jq -r '.statusHistory | length')
echo "  ✓ Final status: $FINAL_STATUS"
echo "  ✓ Status history entries: $HISTORY_COUNT"

echo ""
echo "================================================================"
echo " ✅ E2E TEST COMPLETE"
echo "================================================================"
echo " Order:   $ORDER_NUMBER"
echo " Total:   \$$ORDER_TOTAL"
echo " Status:  $FINAL_STATUS"
echo "================================================================"
```

---

## 10. Cancellation & Refund Scenario

```bash
#!/bin/bash
echo "================================================================"
echo " CANCELLATION + REFUND FLOW TEST"
echo "================================================================"

# (Assumes order already placed from Step 5 above)
# Order must be in Pending or Confirmed state

# ── Cancel the order ─────────────────────────────────────────
echo "Cancelling order $ORDER_NUMBER..."
CANCEL=$(curl -s -X POST "$BASE_ORDER/orders/$ORDER_ID/cancel" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"reason": "Found better price elsewhere"}')
echo "Cancel response: $CANCEL"
echo "  ✓ Order cancelled"

# ── Verify stock was released ────────────────────────────────
echo ""
echo "Checking stock was released..."
PRODUCT_DETAIL=$(curl -s "$BASE_PRODUCT/products/$PRODUCT_ID")
STOCK=$(echo $PRODUCT_DETAIL | jq -r '.stockQuantity')
echo "  ✓ Stock after cancellation: $STOCK"

# ── Admin processes refund ───────────────────────────────────
echo ""
echo "Admin initiating refund..."
curl -s -X POST "$BASE_PAYMENT/payments/$PAYMENT_ID/refund" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{\"amount\":$ORDER_TOTAL,\"reason\":\"Order cancelled by customer\"}"
echo "  ✓ Refund initiated"

# ── Verify order shows RefundPending ─────────────────────────
ORDER_STATE=$(curl -s "$BASE_ORDER/orders/$ORDER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN")
PAYMENT_STATUS=$(echo $ORDER_STATE | jq -r '.paymentStatus')
echo "  ✓ Payment status: $PAYMENT_STATUS"

echo ""
echo "================================================================"
echo " ✅ CANCELLATION FLOW COMPLETE"
echo "================================================================"
```

---

## 11. Error Scenarios & Edge Cases

### 11.1 Insufficient Stock

```bash
# Try to order more than available stock
curl -s -X POST "$BASE_ORDER/orders/$ORDER_ID" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"customerId\":\"$USER_ID\",
    \"shippingFullName\":\"Test User\",
    \"shippingStreet\":\"123 St\",
    \"shippingCity\":\"NYC\",\"shippingState\":\"NY\",
    \"shippingPostalCode\":\"10001\",\"shippingCountry\":\"US\",
    \"shippingPhone\":\"+1111111111\",
    \"items\":[{
      \"productId\":\"$PRODUCT_ID\",\"productName\":\"MacBook Pro\",
      \"sku\":\"APPL-MBP14-M3-SLV\",\"unitPrice\":1999.99,\"quantity\":9999
    }]
  }" | jq .
# Expected: 422 Unprocessable Entity — BusinessRule.StockReservation
```

### 11.2 Invalid Coupon

```bash
curl -s -X POST "$BASE_CART/cart/coupon" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"couponCode": "FAKECODE999"}' | jq .
# Expected: 404 Not Found
```

### 11.3 Expired Coupon

```bash
curl -s -X POST "$BASE_COUPON/coupons/validate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"code": "EXPIRED_CODE", "orderAmount": 500}' | jq .
# Expected: 422 — BusinessRule.Coupon: Coupon is no longer valid
```

### 11.4 Cancel Already-Shipped Order

```bash
curl -s -X POST "$BASE_ORDER/orders/$ORDER_ID/cancel" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d '{"reason": "Testing"}' | jq .
# Expected: 422 — BusinessRule.OrderCancellation: Invalid transition from Shipped to Cancelled
```

### 11.5 Unauthorized Access

```bash
# Try to access another user's order
curl -s "$BASE_ORDER/orders/some-other-users-order-id" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
# Expected: 403 Forbidden

# Try admin endpoint without admin role
curl -s -X GET "$BASE_ORDER/orders" \
  -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
# Expected: 403 Forbidden
```

### 11.6 Rate Limiting

```bash
# Exceed auth rate limit (10 requests per 15 minutes)
for i in {1..15}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE_IDENTITY/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"wrong@test.com","password":"wrong"}')
  echo "Request $i: HTTP $STATUS"
done
# After 10 requests: 429 Too Many Requests
```

### 11.7 Account Lockout

```bash
# 5 wrong passwords → account locked for 15 minutes
for i in {1..6}; do
  curl -s -X POST "$BASE_IDENTITY/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"alice@example.com","password":"WrongPass"}' | jq .status
done
# 6th attempt: 401 — Account locked until HH:MM UTC
```

---

## 12. Admin Workflows

### 12.1 View All Orders

```bash
# All orders paginated
curl -s "http://localhost:5003/api/v1/orders?pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq .

# Filter by status
curl -s "http://localhost:5003/api/v1/orders?status=Confirmed" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq .

# Filter by date range
curl -s "http://localhost:5003/api/v1/orders?from=2025-03-01&to=2025-03-31" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq .
```

### 12.2 Bulk Stock Update

```bash
#!/bin/bash
# Update stock for multiple products
PRODUCTS=("$PRODUCT1_ID" "$PRODUCT2_ID")
DELTAS=(100 75)

for i in "${!PRODUCTS[@]}"; do
  curl -s -X PATCH "http://localhost:5002/api/v1/products/${PRODUCTS[$i]}/stock" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    -d "{\"delta\":${DELTAS[$i]},\"reason\":\"Monthly inventory replenishment\"}"
  echo "Updated stock for product ${PRODUCTS[$i]}"
done
```

### 12.3 Process Order Through Full Lifecycle

```bash
# Confirm → Process → Ship → Deliver
ORDER_ID="your-order-id"

# Confirm (happens automatically after payment webhook)
curl -s -X POST "http://localhost:5003/api/v1/orders/$ORDER_ID/confirm-payment" \
  -H "Content-Type: application/json" \
  -d '{"paymentIntentId":"pi_test_xxx"}' | jq .

# Ship
curl -s -X POST "http://localhost:5003/api/v1/orders/$ORDER_ID/ship" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"trackingNumber":"FX123456789US","carrier":"FedEx"}' | jq .
```

---

## 13. Integration Tests (xUnit)

### 13.1 Test Project Setup

```xml
<!-- tests/Integration/ECommerce.IntegrationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0"/>
    <PackageReference Include="xunit" Version="2.7.0"/>
    <PackageReference Include="FluentAssertions" Version="6.12.0"/>
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0"/>
    <PackageReference Include="Testcontainers.Redis" Version="3.7.0"/>
  </ItemGroup>
</Project>
```

### 13.2 Integration Test Base

```csharp
// tests/Integration/Fixtures/IntegrationTestBase.cs
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected HttpClient IdentityClient = null!;
    protected HttpClient ProductClient = null!;
    protected HttpClient OrderClient = null!;
    protected string AdminToken = null!;
    protected string CustomerToken = null!;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("test_db").WithUsername("test").WithPassword("test").Build();
    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        var factory = new WebApplicationFactory<Identity.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ConnectionStrings:IdentityDb", _postgres.GetConnectionString());
            });

        IdentityClient = factory.CreateClient();

        // Register and get admin token
        var adminResponse = await IdentityClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "admin@test.com", password = "Admin@123!",
            firstName = "Admin", lastName = "Test",
            phoneNumber = "+1111111111", role = "Admin"
        });
        var admin = await adminResponse.Content.ReadFromJsonAsync<AuthResponse>();
        AdminToken = admin!.AccessToken;

        // Register and get customer token
        var custResponse = await IdentityClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "customer@test.com", password = "Customer@123!",
            firstName = "Test", lastName = "Customer",
            phoneNumber = "+2222222222", role = "Customer"
        });
        var customer = await custResponse.Content.ReadFromJsonAsync<AuthResponse>();
        CustomerToken = customer!.AccessToken;
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    protected HttpClient AuthenticatedClient(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed record AuthResponse(string AccessToken, string RefreshToken, string UserId, string Role);
```

### 13.3 Auth Tests

```csharp
// tests/Integration/Identity/AuthTests.cs
public class AuthTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_WithValidData_Returns201AndTokens()
    {
        var response = await IdentityClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "newuser@test.com", password = "Valid@123!",
            firstName = "New", lastName = "User", phoneNumber = "+3333333333"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        await IdentityClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "duplicate@test.com", password = "Valid@123!",
            firstName = "A", lastName = "B", phoneNumber = "+4444444444"
        });

        var response = await IdentityClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "duplicate@test.com", password = "Valid@123!",
            firstName = "A", lastName = "B", phoneNumber = "+5555555555"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("notanemail", "Valid@123!", "Email")]
    [InlineData("test@test.com", "weak", "Password")]
    [InlineData("test@test.com", "Valid@123!", "PhoneNumber")]
    public async Task Register_WithInvalidData_Returns400WithFieldErrors(
        string email, string password, string expectedErrorField)
    {
        var response = await IdentityClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email, password, firstName = "T", lastName = "U", phoneNumber = "bad"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(expectedErrorField);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await IdentityClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.com",
            password = "WrongPassword!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### 13.4 Order Flow Test

```csharp
// tests/Integration/Orders/OrderFlowTests.cs
public class OrderFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task PlaceOrder_WithValidItems_CreatesOrderInPendingState()
    {
        // Arrange: create category + product
        var categoryId = await CreateTestCategoryAsync();
        var productId = await CreateTestProductAsync(categoryId, price: 299.99m, stock: 10);

        // Act: place order
        AuthenticatedClient(OrderClient, CustomerToken);
        var response = await OrderClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = Guid.Parse(GetUserIdFromToken(CustomerToken)),
            shippingFullName = "Test User",
            shippingStreet = "123 Test St",
            shippingCity = "NYC", shippingState = "NY",
            shippingPostalCode = "10001", shippingCountry = "US",
            shippingPhone = "+1234567890",
            items = new[] { new { productId, productName = "Test", sku = "TEST-SKU", unitPrice = 299.99m, quantity = 1 } }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        order!.OrderNumber.Should().StartWith("ORD-");
        order.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PlaceOrder_WithInsufficientStock_Returns422()
    {
        var categoryId = await CreateTestCategoryAsync();
        var productId = await CreateTestProductAsync(categoryId, price: 99.99m, stock: 2);

        AuthenticatedClient(OrderClient, CustomerToken);
        var response = await OrderClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = Guid.Parse(GetUserIdFromToken(CustomerToken)),
            shippingFullName = "Test", shippingStreet = "St",
            shippingCity = "City", shippingState = "ST",
            shippingPostalCode = "12345", shippingCountry = "US",
            shippingPhone = "+1111111111",
            items = new[] { new { productId, productName = "T", sku = "SKU", unitPrice = 99.99m, quantity = 999 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CancelOrder_AfterShipping_Returns422()
    {
        // Create, confirm, ship, then try to cancel
        var orderId = await PlaceAndConfirmOrderAsync();
        await ShipOrderAsync(orderId);

        AuthenticatedClient(OrderClient, CustomerToken);
        var response = await OrderClient.PostAsJsonAsync(
            $"/api/v1/orders/{orderId}/cancel",
            new { reason = "Too late!" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Helpers ───────────────────────────────────────────────
    private string GetUserIdFromToken(string token)
    {
        var parts = token.Split('.');
        var payload = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));
        var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payload)!;
        return json["nameid"].ToString()!;
    }

    private async Task<Guid> CreateTestCategoryAsync() { /* ... */ return Guid.NewGuid(); }
    private async Task<Guid> CreateTestProductAsync(Guid categoryId, decimal price, int stock) { /* ... */ return Guid.NewGuid(); }
    private async Task<Guid> PlaceAndConfirmOrderAsync() { /* ... */ return Guid.NewGuid(); }
    private async Task ShipOrderAsync(Guid orderId) { /* ... */ }
    private sealed record PlaceOrderResponse(Guid OrderId, string OrderNumber, decimal Total);
}
```

---

## 14. Postman Collection

Save the following as `ECommerce.postman_collection.json` and import into Postman:

```json
{
  "info": {
    "name": "ECommerce Platform",
    "_postman_id": "ecommerce-complete-api",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    { "key": "base_identity", "value": "http://localhost:5001/api/v1" },
    { "key": "base_product",  "value": "http://localhost:5002/api/v1" },
    { "key": "base_order",    "value": "http://localhost:5003/api/v1" },
    { "key": "base_cart",     "value": "http://localhost:5004/api/v1" },
    { "key": "base_coupon",   "value": "http://localhost:5005/api/v1" },
    { "key": "base_payment",  "value": "http://localhost:5006/api/v1" },
    { "key": "access_token",  "value": "" },
    { "key": "admin_token",   "value": "" },
    { "key": "user_id",       "value": "" },
    { "key": "product_id",    "value": "" },
    { "key": "order_id",      "value": "" },
    { "key": "payment_id",    "value": "" }
  ],
  "item": [
    {
      "name": "01 - Identity",
      "item": [
        {
          "name": "Register Customer",
          "event": [{ "listen": "test", "script": { "exec": [
            "var r = pm.response.json();",
            "pm.environment.set('access_token', r.accessToken);",
            "pm.environment.set('user_id', r.userId);",
            "pm.test('Status 201', () => pm.response.to.have.status(201));",
            "pm.test('Has accessToken', () => pm.expect(r.accessToken).to.not.be.empty);"
          ]}}],
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "url": "{{base_identity}}/auth/register",
            "body": { "mode": "raw", "raw": "{\"email\":\"test@postman.com\",\"password\":\"Test@123!\",\"firstName\":\"Postman\",\"lastName\":\"Test\",\"phoneNumber\":\"+14155550123\"}" }
          }
        },
        {
          "name": "Login",
          "event": [{ "listen": "test", "script": { "exec": [
            "var r = pm.response.json();",
            "pm.environment.set('access_token', r.accessToken);",
            "pm.test('Status 200', () => pm.response.to.have.status(200));"
          ]}}],
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "url": "{{base_identity}}/auth/login",
            "body": { "mode": "raw", "raw": "{\"email\":\"test@postman.com\",\"password\":\"Test@123!\"}" }
          }
        }
      ]
    },
    {
      "name": "02 - Products",
      "item": [
        {
          "name": "Get All Products",
          "request": {
            "method": "GET",
            "url": { "raw": "{{base_product}}/products?pageNumber=1&pageSize=10", "host": ["{{base_product}}"], "path": ["products"], "query": [{"key":"pageNumber","value":"1"},{"key":"pageSize","value":"10"}] }
          }
        },
        {
          "name": "Get Product by ID",
          "request": {
            "method": "GET",
            "url": "{{base_product}}/products/{{product_id}}"
          }
        }
      ]
    },
    {
      "name": "03 - Cart",
      "item": [
        {
          "name": "Add Item to Cart",
          "request": {
            "method": "POST",
            "header": [
              {"key":"Content-Type","value":"application/json"},
              {"key":"Authorization","value":"Bearer {{access_token}}"}
            ],
            "url": "{{base_cart}}/cart/items",
            "body": { "mode": "raw", "raw": "{\"productId\":\"{{product_id}}\",\"productName\":\"Test Product\",\"sku\":\"TEST-SKU\",\"unitPrice\":99.99,\"quantity\":1}" }
          }
        },
        {
          "name": "Get Cart",
          "request": {
            "method": "GET",
            "header": [{"key":"Authorization","value":"Bearer {{access_token}}"}],
            "url": "{{base_cart}}/cart"
          }
        }
      ]
    },
    {
      "name": "04 - Orders",
      "item": [
        {
          "name": "Place Order",
          "event": [{ "listen": "test", "script": { "exec": [
            "var r = pm.response.json();",
            "pm.environment.set('order_id', r.orderId);",
            "pm.test('Status 201', () => pm.response.to.have.status(201));",
            "pm.test('Has orderNumber', () => pm.expect(r.orderNumber).to.match(/^ORD-/));"
          ]}}],
          "request": {
            "method": "POST",
            "header": [
              {"key":"Content-Type","value":"application/json"},
              {"key":"Authorization","value":"Bearer {{access_token}}"}
            ],
            "url": "{{base_order}}/orders",
            "body": { "mode": "raw", "raw": "{\"customerId\":\"{{user_id}}\",\"shippingFullName\":\"Test User\",\"shippingStreet\":\"123 St\",\"shippingCity\":\"NYC\",\"shippingState\":\"NY\",\"shippingPostalCode\":\"10001\",\"shippingCountry\":\"US\",\"shippingPhone\":\"+1234567890\",\"items\":[{\"productId\":\"{{product_id}}\",\"productName\":\"Product\",\"sku\":\"SKU\",\"unitPrice\":99.99,\"quantity\":1}]}" }
          }
        },
        {
          "name": "Get My Orders",
          "request": {
            "method": "GET",
            "header": [{"key":"Authorization","value":"Bearer {{access_token}}"}],
            "url": "{{base_order}}/orders/my-orders"
          }
        }
      ]
    },
    {
      "name": "05 - Payments",
      "item": [
        {
          "name": "Create Payment Session",
          "event": [{ "listen": "test", "script": { "exec": [
            "var r = pm.response.json();",
            "pm.environment.set('payment_id', r.paymentId);",
            "pm.test('Has checkoutUrl', () => pm.expect(r.checkoutUrl).to.not.be.empty);"
          ]}}],
          "request": {
            "method": "POST",
            "header": [
              {"key":"Content-Type","value":"application/json"},
              {"key":"Authorization","value":"Bearer {{access_token}}"}
            ],
            "url": "{{base_payment}}/payments/sessions",
            "body": { "mode": "raw", "raw": "{\"orderId\":\"{{order_id}}\",\"customerId\":\"{{user_id}}\",\"amount\":109.98,\"currency\":\"USD\",\"gateway\":\"Stripe\",\"successUrl\":\"https://shophub.com/success\",\"cancelUrl\":\"https://shophub.com/cancel\"}" }
          }
        }
      ]
    }
  ]
}
```

---

## Swagger UI Access

Each service exposes Swagger UI in development mode:

| Service | Swagger URL |
|---|---|
| Identity | http://localhost:5001/swagger |
| Product | http://localhost:5002/swagger |
| Order | http://localhost:5003/swagger |
| Cart | http://localhost:5004/swagger |
| Coupon | http://localhost:5005/swagger |
| Payment | http://localhost:5006/swagger |
| Email | http://localhost:5007/swagger |
| Gateway | http://localhost:8080/swagger |

---

## Health & Observability

```bash
# Check all service health endpoints
for port in 5001 5002 5003 5004 5005 5006 5007; do
  echo -n "Port $port: "
  curl -sf "http://localhost:$port/health" && echo "HEALTHY" || echo "UNHEALTHY"
done

# RabbitMQ Management UI
open http://localhost:15672
# Login: ecommerce / rabbitmq_secret

# View logs for specific service
docker compose logs order-api -f

# View all logs
docker compose logs -f --tail=100
```
