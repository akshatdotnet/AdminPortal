using ECommerce.Application.Orders.Queries;
using ECommerce.Application.Payment.Commands;
using ECommerce.Console.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Console.Handlers;

/// <summary>Handles payment and refund console interactions.</summary>
public class PaymentHandler(IMediator mediator)
{
    public async Task PayAsync(Guid customerId, CancellationToken ct)
    {
        var ordersResult = await mediator.Send(new GetOrdersQuery(customerId), ct);
        if (ordersResult.IsFailure) { ConsoleDisplayService.Error(ordersResult.Error); return; }

        var payable = ordersResult.Value!.Where(o => o.Status == "Pending").ToList();
        if (!payable.Any()) { ConsoleDisplayService.Info("No pending orders awaiting payment."); return; }

        ConsoleDisplayService.PrintOrders(payable);
        ConsoleDisplayService.Prompt("Select order number to pay");
        if (!int.TryParse(ConsoleDisplayService.ReadLine(), out var idx) || idx < 1 || idx > payable.Count)
        { ConsoleDisplayService.Error("Invalid selection."); return; }

        var order = payable[idx - 1];
        ConsoleDisplayService.PrintOrderDetail(order);

        System.Console.WriteLine();
        System.Console.WriteLine("  Select Payment Method:");
        System.Console.WriteLine("    [1]  Credit / Debit Card");
        System.Console.WriteLine("    [2]  Net Banking");
        System.Console.WriteLine("    [3]  UPI");
        System.Console.WriteLine("    [4]  Wallet");
        System.Console.WriteLine("    [5]  Cash on Delivery");
        ConsoleDisplayService.Prompt("Method");

        var method = ConsoleDisplayService.ReadLine() switch
        {
            "1" => PaymentMethod.CreditCard,
            "2" => PaymentMethod.NetBanking,
            "3" => PaymentMethod.UPI,
            "4" => PaymentMethod.Wallet,
            "5" => PaymentMethod.CashOnDelivery,
            _   => (PaymentMethod?)null
        };

        if (method is null) { ConsoleDisplayService.Error("Invalid payment method."); return; }

        ConsoleDisplayService.Info($"Processing payment of ₹{order.TotalAmount:N0} via {method}...");

        var result = await mediator.Send(new ProcessPaymentCommand(order.Id, customerId, method.Value), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        var payment = result.Value!;
        ConsoleDisplayService.Success($"Payment successful! Transaction ID: {payment.GatewayTransactionId}");
        ConsoleDisplayService.Info($"Amount   : ₹{payment.Amount:N0}");
        ConsoleDisplayService.Info($"Method   : {payment.Method}");
        ConsoleDisplayService.Info($"Status   : {payment.Status}");
        ConsoleDisplayService.PressAnyKey();
    }

    public async Task RefundAsync(Guid customerId, CancellationToken ct)
    {
        var ordersResult = await mediator.Send(new GetOrdersQuery(customerId), ct);
        if (ordersResult.IsFailure) { ConsoleDisplayService.Error(ordersResult.Error); return; }

        var refundable = ordersResult.Value!.Where(o => o.Status == "Cancelled").ToList();
        if (!refundable.Any()) { ConsoleDisplayService.Info("No cancelled orders eligible for refund."); return; }

        ConsoleDisplayService.PrintOrders(refundable);
        ConsoleDisplayService.Prompt("Select order number for refund");
        if (!int.TryParse(ConsoleDisplayService.ReadLine(), out var idx) || idx < 1 || idx > refundable.Count)
        { ConsoleDisplayService.Error("Invalid selection."); return; }

        var order = refundable[idx - 1];
        ConsoleDisplayService.Prompt($"Confirm refund for {order.OrderNumber} (₹{order.TotalAmount:N0})? (y/n)");
        if (ConsoleDisplayService.ReadLine().ToLower() != "y") return;

        var result = await mediator.Send(new RefundPaymentCommand(order.Id, customerId), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        ConsoleDisplayService.Success($"Refund initiated! Refund will be credited within 3-5 business days.");
        ConsoleDisplayService.PressAnyKey();
    }
}
