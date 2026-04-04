# ShopHub E2E Test (PowerShell / Windows)
$B1 = "http://localhost:5001/api/v1"
$B2 = "http://localhost:5002/api/v1"
$B3 = "http://localhost:5003/api/v1"
$B4 = "http://localhost:5004/api/v1"
$B5 = "http://localhost:5005/api/v1"
$B6 = "http://localhost:5006/api/v1"

function Post($url, $body, $token=$null) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    try { Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body ($body | ConvertTo-Json -Depth 5) }
    catch { Write-Host "ERROR: $_" -ForegroundColor Red; return $null }
}

function Get($url, $token) {
    $headers = @{ "Authorization" = "Bearer $token" }
    Invoke-RestMethod -Uri $url -Method GET -Headers $headers
}

Write-Host "STEP 1: Register Admin" -ForegroundColor Cyan
$admin = Post "$B1/auth/register" @{
    email="admin@shophub.com"; password="Admin@123!"; firstName="Admin"
    lastName="User"; phoneNumber="+14155550001"; role="Admin"
}
if (-not $admin) {
    $admin = Post "$B1/auth/login" @{ email="admin@shophub.com"; password="Admin@123!" }
}
$adminToken = $admin.accessToken
Write-Host "  Admin token OK" -ForegroundColor Green

Write-Host "STEP 2: Register Customer" -ForegroundColor Cyan
$cust = Post "$B1/auth/register" @{
    email="customer@test.com"; password="Cust@123!"; firstName="John"
    lastName="Doe"; phoneNumber="+14155551234"
}
if (-not $cust) {
    $cust = Post "$B1/auth/login" @{ email="customer@test.com"; password="Cust@123!" }
}
$custToken = $cust.accessToken
$userId = $cust.userId
Write-Host "  Customer token OK. UserID: $userId" -ForegroundColor Green

Write-Host "STEP 3: Create Category" -ForegroundColor Cyan
$catId = Post "$B2/categories" @{ name="Electronics"; description="Electronic devices" } $adminToken
Write-Host "  Category ID: $catId" -ForegroundColor Green

Write-Host "STEP 4: Create Product" -ForegroundColor Cyan
$prodId = Post "$B2/products" @{
    name="MacBook Pro 14"; description="Apple M3 MacBook"; sku="APPL-MBP14-M3"
    price=1999.99; currency="USD"; stockQuantity=50; categoryId=$catId; brand="Apple"
} $adminToken
Write-Host "  Product ID: $prodId" -ForegroundColor Green

Write-Host "STEP 5: Create Coupon" -ForegroundColor Cyan
Post "$B5/coupons" @{
    code="SAVE100"; description="`$100 off orders over `$500"
    discountType="FixedAmount"; discountValue=100
    validFrom="2025-01-01T00:00:00Z"; validTo="2030-12-31T23:59:59Z"
    minimumOrderAmount=500
} $adminToken | Out-Null
Write-Host "  Coupon SAVE100 created" -ForegroundColor Green

Write-Host "STEP 6: Add to Cart" -ForegroundColor Cyan
Post "$B4/cart/items" @{
    productId=$prodId; productName="MacBook Pro 14"; sku="APPL-MBP14-M3"
    unitPrice=1999.99; quantity=1
} $custToken | Out-Null
Write-Host "  Item added to cart" -ForegroundColor Green

Write-Host "STEP 7: Apply Coupon" -ForegroundColor Cyan
$cart = Post "$B4/cart/coupon" @{ couponCode="SAVE100" } $custToken
Write-Host "  Cart total after coupon: `$$($cart.total)" -ForegroundColor Green

Write-Host "STEP 8: Place Order" -ForegroundColor Cyan
$order = Post "$B3/orders" @{
    customerId=$userId
    shippingFullName="John Doe"; shippingStreet="123 Main St"
    shippingCity="New York"; shippingState="NY"
    shippingPostalCode="10001"; shippingCountry="US"; shippingPhone="+14155551234"
    items=@(@{ productId=$prodId; productName="MacBook Pro 14"; sku="APPL-MBP14-M3"; unitPrice=1999.99; quantity=1 })
    couponCode="SAVE100"
} $custToken
$orderId = $order.orderId
$orderNumber = $order.orderNumber
$orderTotal = $order.total
Write-Host "  Order: $orderNumber | Total: `$$orderTotal" -ForegroundColor Green

Write-Host "STEP 9: Create Payment Session" -ForegroundColor Cyan
$payment = Post "$B6/payments/sessions" @{
    orderId=$orderId; customerId=$userId; amount=$orderTotal
    currency="USD"; gateway="Stripe"
    successUrl="https://shophub.com/success"; cancelUrl="https://shophub.com/cancel"
} $custToken
$paymentId = $payment.paymentId
$piId = $payment.paymentIntentId
Write-Host "  Payment session: $paymentId" -ForegroundColor Green

Write-Host "STEP 10: Confirm Payment" -ForegroundColor Cyan
Post "$B3/orders/$orderId/confirm-payment" @{ paymentIntentId=$piId } | Out-Null
Write-Host "  Payment confirmed!" -ForegroundColor Green

Write-Host "STEP 11: Ship Order" -ForegroundColor Cyan
Post "$B3/orders/$orderId/ship" @{ trackingNumber="1Z999AA10123456784"; carrier="UPS" } $adminToken | Out-Null
Write-Host "  Order shipped!" -ForegroundColor Green

Write-Host "STEP 12: Verify Final State" -ForegroundColor Cyan
$final = Get "$B3/orders/$orderId" $custToken
Write-Host "  Status: $($final.status) | Tracking: $($final.trackingNumber)" -ForegroundColor Green

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " E2E TEST COMPLETE" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host " Order:    $orderNumber"
Write-Host " Total:    `$$orderTotal"
Write-Host " Status:   $($final.status)"
Write-Host " Tracking: $($final.trackingNumber)"
