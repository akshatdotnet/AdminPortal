using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentEntity = ECommerce.Domain.Entities.Payment;

namespace ECommerce.Application.Payment.Commands;

public class ProcessPaymentCommandHandler(
    IOrderRepository orders,
    IPaymentRepository payments,
    ICustomerRepository customers,
    IPaymentGateway gateway,
    IEmailNotificationService email,
    ISmsNotificationService sms,
    IUnitOfWork uow,
    ILogger<ProcessPaymentCommandHandler> logger) : IRequestHandler<ProcessPaymentCommand, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result.Failure<PaymentDto>("Order not found.");
        if (order.CustomerId != cmd.CustomerId)
            return Result.Failure<PaymentDto>("Unauthorized.");
        if (order.Status != OrderStatus.Pending)
            return Result.Failure<PaymentDto>($"Order is in '{order.Status}' status and cannot be paid.");

        // Idempotency: reject duplicate payment for same order
        var existing = await payments.GetByOrderIdAsync(order.Id, ct);
        if (existing?.Status == PaymentStatus.Captured)
            return Result.Failure<PaymentDto>("Payment already processed for this order.");

        var payment = PaymentEntity.Create(order.Id, order.TotalAmount, cmd.PaymentMethod);
        await payments.AddAsync(payment, ct);
        // payment is now tracked as Added — all subsequent mutations are detected automatically

        var gatewayResult = await gateway.ChargeAsync(new PaymentRequest(
            order.Id, order.TotalAmount.Amount, order.TotalAmount.Currency,
            cmd.PaymentMethod, payment.IdempotencyKey), ct);

        if (gatewayResult.Success)
        {
            payment.MarkCaptured(gatewayResult.TransactionId!);
            order.ConfirmPayment();
            // Both payment and order are tracked → no explicit Update() calls needed

            var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
            if (customer is not null)
            {
                await email.SendAsync(new NotificationMessage(
                    customer.Email.Value,
                    $"Payment Receipt - {order.OrderNumber}",
                    $"Payment of {order.TotalAmount} received.\nTransaction ID: {gatewayResult.TransactionId}"), ct);

                if (customer.Phone is not null)
                    await sms.SendAsync(customer.Phone,
                        $"Payment confirmed for order {order.OrderNumber}. Txn: {gatewayResult.TransactionId}", ct);
            }

            logger.LogInformation("Payment captured for order {OrderId}. TxnId: {TxnId}", order.Id, gatewayResult.TransactionId);
        }
        else
        {
            payment.MarkFailed(gatewayResult.FailureReason ?? "Unknown error");
            logger.LogWarning("Payment failed for order {OrderId}: {Reason}", order.Id, gatewayResult.FailureReason);
        }

        await uow.SaveChangesAsync(ct);

        return gatewayResult.Success
            ? Result.Success(new PaymentDto(payment.Id, payment.OrderId, payment.Amount.Amount,
                payment.Amount.Currency, payment.Method.ToString(), payment.Status.ToString(),
                payment.GatewayTransactionId, payment.CreatedAt))
            : Result.Failure<PaymentDto>($"Payment failed: {gatewayResult.FailureReason}");
    }
}
