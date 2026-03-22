using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Commands;

public sealed record CreatePaymentSessionCommand(
    Guid OrderId, Guid CustomerId, decimal Amount, string Currency,
    string Gateway, string SuccessUrl, string CancelUrl)
    : IRequest<Result<PaymentSessionDto>>;

public sealed class CreatePaymentSessionValidator
    : AbstractValidator<CreatePaymentSessionCommand>
{
    public CreatePaymentSessionValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Gateway)
            .Must(g => Enum.TryParse<PaymentGateway>(g, true, out _))
            .WithMessage("Invalid gateway. Use: Stripe, PayPal, or Razorpay.");
    }
}

public sealed class CreatePaymentSessionHandler(
    IPaymentRepository repo,
    IPaymentGatewayService gateway,
    IUnitOfWorkPayment uow)
    : IRequestHandler<CreatePaymentSessionCommand, Result<PaymentSessionDto>>
{
    public async Task<Result<PaymentSessionDto>> Handle(
        CreatePaymentSessionCommand cmd, CancellationToken ct)
    {
        var gw = Enum.Parse<PaymentGateway>(cmd.Gateway, ignoreCase: true);
        var record = PaymentRecord.Create(cmd.OrderId, cmd.CustomerId,
            cmd.Amount, cmd.Currency, gw);

        var sessionResult = await gateway.CreateSessionAsync(
            cmd.OrderId, cmd.Amount, cmd.Currency,
            cmd.SuccessUrl, cmd.CancelUrl, ct);

        if (!sessionResult.IsSuccess)
            return Result.Failure<PaymentSessionDto>(sessionResult.Error);

        record.SetSession(sessionResult.Value.SessionId,
            sessionResult.Value.PaymentIntentId,
            sessionResult.Value.CheckoutUrl);

        repo.Add(record);
        await uow.SaveChangesAsync(ct);

        return Result.Success(new PaymentSessionDto(record.Id,
            sessionResult.Value.CheckoutUrl,
            sessionResult.Value.SessionId,
            sessionResult.Value.PaymentIntentId));
    }
}

public sealed record HandleWebhookCommand(string Gateway, string Payload, string Signature)
    : IRequest<Result>;

public sealed class HandleWebhookHandler(
    IPaymentRepository repo,
    IOrderServiceClient orderClient,
    IUnitOfWorkPayment uow)
    : IRequestHandler<HandleWebhookCommand, Result>
{
    public async Task<Result> Handle(HandleWebhookCommand cmd, CancellationToken ct)
    {
        // Parse simplified test payload
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(cmd.Payload);
            var root = doc.RootElement;
            var eventType = root.GetProperty("type").GetString() ?? "";
            var piId = root.GetProperty("data").GetProperty("object")
                .GetProperty("id").GetString() ?? "";

            var record = await repo.GetByGatewayIdAsync(piId, ct);
            if (record is null)
                return Result.Failure(Error.NotFound("Payment", piId));

            if (eventType == "payment.succeeded")
            {
                record.MarkSucceeded(piId);
                await orderClient.ConfirmPaymentAsync(record.OrderId, piId, ct);
            }
            else if (eventType == "payment.failed")
            {
                record.MarkFailed("Payment declined");
            }

            repo.Update(record);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.BusinessRule("Webhook",
                $"Invalid payload: {ex.Message}"));
        }
    }
}

public sealed record InitiateRefundCommand(
    Guid PaymentId, decimal Amount, string Reason) : IRequest<Result>;

public sealed class InitiateRefundHandler(
    IPaymentRepository repo,
    IPaymentGatewayService gateway,
    IUnitOfWorkPayment uow)
    : IRequestHandler<InitiateRefundCommand, Result>
{
    public async Task<Result> Handle(InitiateRefundCommand cmd, CancellationToken ct)
    {
        var record = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (record is null)
            return Result.Failure(Error.NotFound("Payment", cmd.PaymentId));
        try
        {
            var refundResult = await gateway.RefundAsync(
                record.GatewayPaymentId!, cmd.Amount, ct);
            if (!refundResult.IsSuccess)
                return Result.Failure(refundResult.Error);
            record.InitiateRefund(cmd.Amount);
            repo.Update(record);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("Refund", ex.Message));
        }
    }
}
