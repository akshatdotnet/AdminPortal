using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Payment.Commands;

public class RefundPaymentCommandHandler(
    IOrderRepository orders,
    IPaymentRepository payments,
    ICustomerRepository customers,
    IPaymentGateway gateway,
    IEmailNotificationService email,
    IUnitOfWork uow,
    ILogger<RefundPaymentCommandHandler> logger) : IRequestHandler<RefundPaymentCommand, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Failure<PaymentDto>("Order not found.");
        if (order.CustomerId != cmd.CustomerId) return Result.Failure<PaymentDto>("Unauthorized.");
        if (order.Status != OrderStatus.Cancelled)
            return Result.Failure<PaymentDto>("Only cancelled orders can be refunded.");

        var payment = await payments.GetByOrderIdAsync(cmd.OrderId, ct);
        if (payment is null) return Result.Failure<PaymentDto>("No payment found for this order.");
        if (payment.Status != PaymentStatus.Captured)
            return Result.Failure<PaymentDto>("Payment is not in a refundable state.");

        var refundResult = await gateway.RefundAsync(
            new RefundRequest(order.Id, payment.GatewayTransactionId!, payment.Amount.Amount), ct);

        if (!refundResult.Success)
            return Result.Failure<PaymentDto>($"Refund failed: {refundResult.FailureReason}");

        payment.MarkRefunded();
        order.MarkRefunded();
        // Both are tracked → auto-detected, no explicit Update() needed

        await uow.SaveChangesAsync(ct);

        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is not null)
        {
            await email.SendAsync(new NotificationMessage(
                customer.Email.Value,
                $"Refund Initiated - {order.OrderNumber}",
                $"Refund of {payment.Amount} for order {order.OrderNumber} has been initiated.\n" +
                $"Refund ID: {refundResult.RefundId}\nExpected in 3-5 business days."), ct);
        }

        logger.LogInformation("Refund processed for order {OrderId}. RefundId: {RefundId}", cmd.OrderId, refundResult.RefundId);

        return Result.Success(new PaymentDto(payment.Id, payment.OrderId, payment.Amount.Amount,
            payment.Amount.Currency, payment.Method.ToString(), payment.Status.ToString(),
            payment.GatewayTransactionId, payment.CreatedAt));
    }
}
