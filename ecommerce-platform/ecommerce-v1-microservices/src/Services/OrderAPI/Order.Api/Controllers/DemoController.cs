using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Commands;
using Order.Application.DTOs;
using Order.Infrastructure.Persistence;

namespace Order.Api.Controllers;

/// <summary>
/// Demo endpoint - runs the complete Order workflow in one HTTP call.
/// No auth required. Tests all major states of the Order state machine.
/// POST /api/v1/demo/complete-flow
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/demo")]
[ApiVersion("1.0")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class DemoController(
    IMediator mediator,
    OrderQueryService queryService) : ControllerBase
{
    /// <summary>
    /// Runs the full Order service workflow:
    ///  1.  Place order with 2 items
    ///  2.  Verify order created with Pending status
    ///  3.  Try to ship before payment (must fail - invalid transition)
    ///  4.  Confirm payment -> status becomes Confirmed
    ///  5.  Verify status is Confirmed + PaymentStatus is Paid
    ///  6.  Ship the order
    ///  7.  Verify TrackingNumber saved + status is Shipped
    ///  8.  Deliver the order
    ///  9.  Verify status is Delivered + DeliveredAt timestamp set
    /// 10.  Place a second order then cancel it
    /// 11.  Verify cancellation reason saved + status is Cancelled
    /// 12.  Try to deliver a cancelled order (must fail)
    /// </summary>
    [HttpPost("complete-flow")]
    [ProducesResponseType(typeof(OrderDemoResult), 200)]
    public async Task<IActionResult> RunCompleteFlow(CancellationToken ct)
    {
        var customerId = Guid.NewGuid();
        var result     = new OrderDemoResult();

        // Step 1: Place first order
        var placeCmd = new PlaceOrderCommand(
            CustomerId: customerId,
            ShippingFullName:    "John Demo",
            ShippingStreet:      "123 Test Street",
            ShippingCity:        "Mumbai",
            ShippingState:       "MH",
            ShippingPostalCode:  "400001",
            ShippingCountry:     "India",
            ShippingPhone:       "+91-9999999999",
            Items: new[]
            {
                new OrderItemRequest(Guid.NewGuid(), "MacBook Pro 14", "APPL-MBP14", 1899.99m, 1),
                new OrderItemRequest(Guid.NewGuid(), "USB-C Hub",      "ACC-USBC01",   49.99m, 2)
            },
            CouponCode: null,
            Notes: "Demo order - please handle with care");

        var s1 = await mediator.Send(placeCmd, ct);
        result.Step1_PlaceOrder = Step(
            s1.IsSuccess,
            s1.IsSuccess
                ? $"Order placed: {s1.Value.OrderNumber}  Total: ${s1.Value.Total:F2}"
                : s1.Error.Message,
            s1.IsSuccess ? new { s1.Value.OrderId, s1.Value.OrderNumber, s1.Value.Total } : null);

        if (!s1.IsSuccess) return Ok(result.Fail("Order placement failed"));
        var orderId     = s1.Value.OrderId;
        var orderNumber = s1.Value.OrderNumber;

        // Step 2: Verify Pending status
        var dto2 = await queryService.GetOrderDtoAsync(orderId, ct);
        result.Step2_VerifyPending = Step(
            dto2?.Status == "Pending",
            dto2 is not null
                ? $"Status={dto2.Status}  Items={dto2.Items.Count()}  Total=${dto2.Total:F2}"
                : "Order not found after creation",
            dto2 is not null
                ? new { dto2.Status, dto2.PaymentStatus, ItemCount = dto2.Items.Count(), dto2.Total }
                : null);

        // Step 3: Try to ship before payment (must fail)
        var s3 = await mediator.Send(
            new ShipOrderCommand(orderId, "TRACK-999", "FedEx"), ct);
        result.Step3_ShipBeforePayment = Step(
            !s3.IsSuccess,
            !s3.IsSuccess
                ? $"Correctly rejected: {s3.Error.Message}"
                : "BUG: allowed ship on unpaid order",
            null);

        // Step 4: Confirm payment
        var paymentRef = $"PAY-{Guid.NewGuid():N}"[..20].ToUpper();
        var s4 = await mediator.Send(
            new ConfirmPaymentCommand(orderId, paymentRef), ct);
        result.Step4_ConfirmPayment = Step(
            s4.IsSuccess,
            s4.IsSuccess
                ? $"Payment confirmed: {paymentRef}"
                : s4.Error.Message,
            null);

        // Step 5: Verify Confirmed + Paid
        var dto5 = await queryService.GetOrderDtoAsync(orderId, ct);
        result.Step5_VerifyConfirmed = Step(
            dto5?.Status == "Confirmed" && dto5.PaymentStatus == "Paid",
            dto5 is not null
                ? $"Status={dto5.Status}  PaymentStatus={dto5.PaymentStatus}"
                : "Order not found",
            dto5 is not null
                ? new { dto5.Status, dto5.PaymentStatus, dto5.PaidAt }
                : null);

        // Step 6: Ship the order
        var tracking = $"SHIP-{Guid.NewGuid():N}"[..16].ToUpper();
        var s6 = await mediator.Send(
            new ShipOrderCommand(orderId, tracking, "DHL Express"), ct);
        result.Step6_ShipOrder = Step(
            s6.IsSuccess,
            s6.IsSuccess
                ? $"Shipped via DHL. Tracking: {tracking}"
                : s6.Error.Message,
            null);

        // Step 7: Verify Shipped + tracking number
        var dto7 = await queryService.GetOrderDtoAsync(orderId, ct);
        result.Step7_VerifyShipped = Step(
            dto7?.Status == "Shipped" && dto7.TrackingNumber == tracking,
            dto7 is not null
                ? $"Status={dto7.Status}  Tracking={dto7.TrackingNumber}  ShippedAt={dto7.ShippedAt}"
                : "Order not found",
            dto7 is not null
                ? new { dto7.Status, dto7.TrackingNumber, dto7.ShippedAt }
                : null);

        // Step 8: Deliver the order
        var s8 = await mediator.Send(new DeliverOrderCommand(orderId), ct);
        result.Step8_DeliverOrder = Step(
            s8.IsSuccess,
            s8.IsSuccess ? "Order delivered to customer" : s8.Error.Message,
            null);

        // Step 9: Verify Delivered + timestamp
        var dto9 = await queryService.GetOrderDtoAsync(orderId, ct);
        result.Step9_VerifyDelivered = Step(
            dto9?.Status == "Delivered" && dto9.DeliveredAt.HasValue,
            dto9 is not null
                ? $"Status={dto9.Status}  DeliveredAt={dto9.DeliveredAt}  History={dto9.StatusHistory.Count()} entries"
                : "Order not found",
            dto9 is not null
                ? new
                  {
                      dto9.Status,
                      dto9.DeliveredAt,
                      History = dto9.StatusHistory.Select(h => new { h.Status, h.Note, h.Timestamp })
                  }
                : null);

        // Step 10: Place second order and cancel it
        var s10 = await mediator.Send(
            placeCmd with { Notes = "Second order - will be cancelled" }, ct);
        result.Step10_PlaceSecondOrder = Step(
            s10.IsSuccess,
            s10.IsSuccess
                ? $"Second order placed: {s10.Value.OrderNumber}"
                : s10.Error.Message,
            s10.IsSuccess ? new { s10.Value.OrderId, s10.Value.OrderNumber } : null);

        if (!s10.IsSuccess) return Ok(result.Fail("Second order failed"));
        var orderId2 = s10.Value.OrderId;

        var s10c = await mediator.Send(
            new CancelOrderCommand(orderId2, customerId, "Customer changed their mind"), ct);
        result.Step10_PlaceSecondOrder = Step(
            s10.IsSuccess && s10c.IsSuccess,
            s10c.IsSuccess
                ? $"Second order placed ({s10.Value.OrderNumber}) and cancelled successfully"
                : s10c.Error.Message,
            s10.IsSuccess ? new { s10.Value.OrderId, s10.Value.OrderNumber } : null);

        // Step 11: Verify cancellation
        var dto11 = await queryService.GetOrderDtoAsync(orderId2, ct);
        result.Step11_VerifyCancelled = Step(
            dto11?.Status == "Cancelled" &&
            dto11.CancellationReason == "Customer changed their mind",
            dto11 is not null
                ? $"Status={dto11.Status}  Reason='{dto11.CancellationReason}'"
                : "Order not found",
            dto11 is not null
                //? new { dto11.Status, dto11.CancellationReason, dto11.StatusHistory.Count() }
                ? new { dto11.Status, HistoryCount = dto11.StatusHistory.Count() }
                : null);

        // Step 12: Try to deliver the cancelled order (must fail)
        var s12 = await mediator.Send(new DeliverOrderCommand(orderId2), ct);
        result.Step12_DeliverCancelled = Step(
            !s12.IsSuccess,
            !s12.IsSuccess
                ? $"Correctly rejected: {s12.Error.Message}"
                : "BUG: delivered a cancelled order",
            null);

        // Summary
        var steps = new[]
        {
            result.Step1_PlaceOrder,       result.Step2_VerifyPending,
            result.Step3_ShipBeforePayment,result.Step4_ConfirmPayment,
            result.Step5_VerifyConfirmed,  result.Step6_ShipOrder,
            result.Step7_VerifyShipped,    result.Step8_DeliverOrder,
            result.Step9_VerifyDelivered,  result.Step10_PlaceSecondOrder,
            result.Step11_VerifyCancelled, result.Step12_DeliverCancelled
        };
        result.TotalSteps  = steps.Length;
        result.PassedSteps = steps.Count(s => s?.Success == true);
        result.AllPassed   = result.PassedSteps == result.TotalSteps;

        return Ok(result);
    }

    private static StepResult Step(bool ok, string msg, object? data) =>
        new() { Success = ok, Message = msg, Data = data };
}

public sealed class OrderDemoResult
{
    public StepResult? Step1_PlaceOrder         { get; set; }
    public StepResult? Step2_VerifyPending       { get; set; }
    public StepResult? Step3_ShipBeforePayment   { get; set; }
    public StepResult? Step4_ConfirmPayment      { get; set; }
    public StepResult? Step5_VerifyConfirmed     { get; set; }
    public StepResult? Step6_ShipOrder           { get; set; }
    public StepResult? Step7_VerifyShipped       { get; set; }
    public StepResult? Step8_DeliverOrder        { get; set; }
    public StepResult? Step9_VerifyDelivered     { get; set; }
    public StepResult? Step10_PlaceSecondOrder   { get; set; }
    public StepResult? Step11_VerifyCancelled    { get; set; }
    public StepResult? Step12_DeliverCancelled   { get; set; }
    public int  TotalSteps  { get; set; }
    public int  PassedSteps { get; set; }
    public bool AllPassed   { get; set; }

    public OrderDemoResult Fail(string reason)
    {
        AllPassed = false;
        return this;
    }
}

public sealed class StepResult
{
    public bool    Success { get; init; }
    public string  Message { get; init; } = default!;
    public object? Data    { get; init; }
}
