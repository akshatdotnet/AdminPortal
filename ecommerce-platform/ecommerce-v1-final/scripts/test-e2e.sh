#!/bin/bash
# End-to-end test script — run AFTER services are up
set -e

echo "=================================================="
echo " ShopHub E2E Test — Complete Order Flow"
echo "=================================================="

B1="http://localhost:5001/api/v1"
B2="http://localhost:5002/api/v1"
B3="http://localhost:5003/api/v1"
B4="http://localhost:5004/api/v1"
B5="http://localhost:5005/api/v1"
B6="http://localhost:5006/api/v1"

# ── STEP 1: Admin Setup ───────────────────────────────────────
echo ""
echo "STEP 1: Register Admin..."
ADMIN_RESP=$(curl -sf -X POST "$B1/auth/register"   -H "Content-Type: application/json"   -d '{"email":"admin@shophub.com","password":"Admin@123!","firstName":"Admin","lastName":"User","phoneNumber":"+14155550001","role":"Admin"}' ||   curl -sf -X POST "$B1/auth/login"   -H "Content-Type: application/json"   -d '{"email":"admin@shophub.com","password":"Admin@123!"}')
ADMIN_TOKEN=$(echo $ADMIN_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")
echo "  Admin token obtained"

# ── STEP 2: Register Customer ─────────────────────────────────
echo ""
echo "STEP 2: Register Customer..."
CUST_RESP=$(curl -sf -X POST "$B1/auth/register"   -H "Content-Type: application/json"   -d '{"email":"customer@test.com","password":"Cust@123!","firstName":"John","lastName":"Doe","phoneNumber":"+14155551234"}' ||   curl -sf -X POST "$B1/auth/login"   -H "Content-Type: application/json"   -d '{"email":"customer@test.com","password":"Cust@123!"}')
ACCESS_TOKEN=$(echo $CUST_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")
USER_ID=$(echo $CUST_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['userId'])")
echo "  Customer token obtained. UserID: $USER_ID"

# ── STEP 3: Create Category ───────────────────────────────────
echo ""
echo "STEP 3: Create Category..."
CAT_RESP=$(curl -sf -X POST "$B2/categories"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ADMIN_TOKEN"   -d '{"name":"Electronics","description":"Electronic devices"}')
CATEGORY_ID=$(echo $CAT_RESP | python3 -c "import sys,json; print(json.load(sys.stdin))")
echo "  Category ID: $CATEGORY_ID"

# ── STEP 4: Create Product ────────────────────────────────────
echo ""
echo "STEP 4: Create Product..."
PROD_RESP=$(curl -sf -X POST "$B2/products"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ADMIN_TOKEN"   -d "{"name":"MacBook Pro 14","description":"Apple M3 MacBook","sku":"APPL-MBP14-M3","price":1999.99,"currency":"USD","stockQuantity":50,"categoryId":"$CATEGORY_ID","brand":"Apple"}")
PRODUCT_ID=$(echo $PROD_RESP | python3 -c "import sys,json; print(json.load(sys.stdin))")
echo "  Product ID: $PRODUCT_ID"

# ── STEP 5: Create Coupon ─────────────────────────────────────
echo ""
echo "STEP 5: Create Coupon SAVE100..."
curl -sf -X POST "$B5/coupons"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ADMIN_TOKEN"   -d '{"code":"SAVE100","description":"$100 off","discountType":"FixedAmount","discountValue":100,"validFrom":"2025-01-01T00:00:00Z","validTo":"2030-12-31T23:59:59Z","minimumOrderAmount":500}' > /dev/null
echo "  Coupon created"

# ── STEP 6: Add to Cart ───────────────────────────────────────
echo ""
echo "STEP 6: Add product to cart..."
curl -sf -X POST "$B4/cart/items"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ACCESS_TOKEN"   -d "{"productId":"$PRODUCT_ID","productName":"MacBook Pro 14","sku":"APPL-MBP14-M3","unitPrice":1999.99,"quantity":1}" > /dev/null
echo "  Item added to cart"

# ── STEP 7: Apply Coupon ──────────────────────────────────────
echo ""
echo "STEP 7: Apply coupon to cart..."
CART=$(curl -sf -X POST "$B4/cart/coupon"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ACCESS_TOKEN"   -d '{"couponCode":"SAVE100"}')
CART_TOTAL=$(echo $CART | python3 -c "import sys,json; print(json.load(sys.stdin)['total'])")
echo "  Cart total after coupon: \$$CART_TOTAL"

# ── STEP 8: Place Order ───────────────────────────────────────
echo ""
echo "STEP 8: Place order..."
ORDER_RESP=$(curl -sf -X POST "$B3/orders"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ACCESS_TOKEN"   -d "{"customerId":"$USER_ID","shippingFullName":"John Doe","shippingStreet":"123 Main St","shippingCity":"New York","shippingState":"NY","shippingPostalCode":"10001","shippingCountry":"US","shippingPhone":"+14155551234","items":[{"productId":"$PRODUCT_ID","productName":"MacBook Pro 14","sku":"APPL-MBP14-M3","unitPrice":1999.99,"quantity":1}],"couponCode":"SAVE100"}")
ORDER_ID=$(echo $ORDER_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['orderId'])")
ORDER_NUMBER=$(echo $ORDER_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['orderNumber'])")
ORDER_TOTAL=$(echo $ORDER_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['total'])")
echo "  Order placed: $ORDER_NUMBER | Total: \$$ORDER_TOTAL"

# ── STEP 9: Create Payment Session ────────────────────────────
echo ""
echo "STEP 9: Create payment session..."
PAY_RESP=$(curl -sf -X POST "$B6/payments/sessions"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ACCESS_TOKEN"   -d "{"orderId":"$ORDER_ID","customerId":"$USER_ID","amount":$ORDER_TOTAL,"currency":"USD","gateway":"Stripe","successUrl":"https://shophub.com/success","cancelUrl":"https://shophub.com/cancel"}")
PAYMENT_ID=$(echo $PAY_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['paymentId'])")
CHECKOUT_URL=$(echo $PAY_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['checkoutUrl'])")
PI_ID=$(echo $PAY_RESP | python3 -c "import sys,json; print(json.load(sys.stdin)['paymentIntentId'])")
echo "  Payment session: $PAYMENT_ID"
echo "  Checkout URL: $CHECKOUT_URL"

# ── STEP 10: Confirm Payment ──────────────────────────────────
echo ""
echo "STEP 10: Confirm payment (simulate webhook)..."
curl -sf -X POST "$B3/orders/$ORDER_ID/confirm-payment"   -H "Content-Type: application/json"   -d "{"paymentIntentId":"$PI_ID"}" > /dev/null
echo "  Payment confirmed!"

# ── STEP 11: Ship Order ───────────────────────────────────────
echo ""
echo "STEP 11: Admin ships order..."
curl -sf -X POST "$B3/orders/$ORDER_ID/ship"   -H "Content-Type: application/json"   -H "Authorization: Bearer $ADMIN_TOKEN"   -d '{"trackingNumber":"1Z999AA10123456784","carrier":"UPS"}' > /dev/null
echo "  Order shipped!"

# ── STEP 12: Verify Final State ───────────────────────────────
echo ""
echo "STEP 12: Verify final order state..."
FINAL=$(curl -sf "$B3/orders/$ORDER_ID"   -H "Authorization: Bearer $ACCESS_TOKEN")
STATUS=$(echo $FINAL | python3 -c "import sys,json; print(json.load(sys.stdin)['status'])")
TRACKING=$(echo $FINAL | python3 -c "import sys,json; print(json.load(sys.stdin)['trackingNumber'])")
echo "  Final status: $STATUS | Tracking: $TRACKING"

echo ""
echo "=================================================="
echo " E2E TEST COMPLETE"
echo "=================================================="
echo " Order:    $ORDER_NUMBER"
echo " Total:    \$$ORDER_TOTAL"
echo " Status:   $STATUS"
echo " Tracking: $TRACKING"
echo "=================================================="
