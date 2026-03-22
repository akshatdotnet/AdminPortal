using Common.Domain.Entities;
using Common.Domain.Events;
using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// ══════════════════════════════════════════════════════════════
// DOMAIN
// ══════════════════════════════════════════════════════════════
namespace Payment.Domain.Entities;

public sealed class PaymentRecord : BaseEntity
{
    private PaymentRecord() { }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentGateway Gateway { get; private set; }
    public string? GatewayPaymentId { get; private set; }
    public string? GatewaySessionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public RefundRecord? Refund { get; private set; }

    public static PaymentRecord Create(
        Guid orderId, Guid customerId,
        decimal amount, string currency,
        PaymentGateway gateway)
    {
        var payment = new PaymentRecord
        {
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            Currency = currency,
            Gateway = gateway
        };

        payment.AddDomainEvent(new PaymentInitiatedEvent(payment.Id, orderId, amount));
        return payment;
    }

    public void SetGatewaySession(string sessionId, string paymentId)
    {
        GatewaySessionId = sessionId;
        GatewayPaymentId = paymentId;
    }

    public void MarkSucceeded(string gatewayPaymentId)
    {
        Status = PaymentStatus.Succeeded;
        GatewayPaymentId = gatewayPaymentId;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentSucceededEvent(Id, OrderId, CustomerId, Amount));
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentFailedEvent(Id, OrderId, reason));
    }

    public void InitiateRefund(decimal refundAmount, string reason)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Can only refund succeeded payments.");
        if (refundAmount > Amount)
            throw new InvalidOperationException("Refund amount exceeds payment amount.");

        Refund = new RefundRecord(refundAmount, reason, DateTime.UtcNow);
        Status = PaymentStatus.RefundInitiated;
        AddDomainEvent(new RefundInitiatedEvent(Id, OrderId, refundAmount));
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
        ProcessedAt = DateTime.UtcNow;
    }
}

public sealed record RefundRecord(decimal Amount, string Reason, DateTime InitiatedAt);

public enum PaymentStatus { Pending, Succeeded, Failed, RefundInitiated, Refunded }
public enum PaymentGateway { Stripe, PayPal, Razorpay }

// ── Domain Events ─────────────────────────────────────────────
public sealed record PaymentInitiatedEvent(Guid PaymentId, Guid OrderId, decimal Amount) : BaseDomainEvent;
public sealed record PaymentSucceededEvent(Guid PaymentId, Guid OrderId, Guid CustomerId, decimal Amount) : BaseDomainEvent;
public sealed record PaymentFailedEvent(Guid PaymentId, Guid OrderId, string Reason) : BaseDomainEvent;
public sealed record RefundInitiatedEvent(Guid PaymentId, Guid OrderId, decimal Amount) : BaseDomainEvent;

// ══════════════════════════════════════════════════════════════
// APPLICATION
// ══════════════════════════════════════════════════════════════
namespace Payment.Application.Commands;

// CREATE PAYMENT SESSION (Stripe Checkout / PayPal Order)
public sealed record CreatePaymentSessionCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Gateway,
    string SuccessUrl,
    string CancelUrl) : IRequest<Result<PaymentSessionDto>>;

public sealed class CreatePaymentSessionValidator : AbstractValidator<CreatePaymentSessionCommand>
{
    public CreatePaymentSessionValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Gateway).Must(g => Enum.TryParse<PaymentGateway>(g, true, out _))
            .WithMessage("Invalid payment gateway. Use: Stripe, PayPal, Razorpay");
        RuleFor(x => x.SuccessUrl).NotEmpty().Must(Uri.IsWellFormedUriString,
            (s, f) => f.Must((url) => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("SuccessUrl must be a valid URL."));
    }
}

public sealed class CreatePaymentSessionCommandHandler(
    IPaymentRepository paymentRepo,
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWorkPayment uow) :
    IRequestHandler<CreatePaymentSessionCommand, Result<PaymentSessionDto>>
{
    public async Task<Result<PaymentSessionDto>> Handle(
        CreatePaymentSessionCommand cmd, CancellationToken ct)
    {
        var gateway = Enum.Parse<PaymentGateway>(cmd.Gateway, ignoreCase: true);
        var paymentRecord = PaymentRecord.Create(
            cmd.OrderId, cmd.CustomerId, cmd.Amount, cmd.Currency, gateway);

        var gatewayService = gatewayFactory.Create(gateway);
        var session = await gatewayService.CreateSessionAsync(new CreateSessionRequest(
            cmd.OrderId, cmd.Amount, cmd.Currency, cmd.SuccessUrl, cmd.CancelUrl), ct);

        if (!session.IsSuccess)
            return Result.Failure<PaymentSessionDto>(session.Error);

        paymentRecord.SetGatewaySession(session.Value.SessionId, session.Value.PaymentIntentId);
        paymentRepo.Add(paymentRecord);
        await uow.SaveChangesAsync(ct);

        return Result.Success(new PaymentSessionDto(
            paymentRecord.Id,
            session.Value.CheckoutUrl,
            session.Value.SessionId,
            session.Value.PaymentIntentId));
    }
}

// HANDLE WEBHOOK — called by Stripe/PayPal/Razorpay
public sealed record HandlePaymentWebhookCommand(
    string Gateway,
    string Payload,
    string Signature) : IRequest<r>;

public sealed class HandlePaymentWebhookCommandHandler(
    IPaymentRepository paymentRepo,
    IPaymentGatewayFactory gatewayFactory,
    IOrderServiceClient orderClient,
    IUnitOfWorkPayment uow) :
    IRequestHandler<HandlePaymentWebhookCommand, Result>
{
    public async Task<r> Handle(HandlePaymentWebhookCommand cmd, CancellationToken ct)
    {
        var gateway = Enum.Parse<PaymentGateway>(cmd.Gateway, ignoreCase: true);
        var gatewayService = gatewayFactory.Create(gateway);

        var eventResult = await gatewayService.ParseWebhookEventAsync(
            cmd.Payload, cmd.Signature, ct);
        if (!eventResult.IsSuccess)
            return Result.Failure(eventResult.Error);

        var webhookEvent = eventResult.Value;
        var payment = await paymentRepo.GetByGatewayPaymentIdAsync(webhookEvent.PaymentIntentId, ct);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment", webhookEvent.PaymentIntentId));

        switch (webhookEvent.EventType)
        {
            case "payment.succeeded":
                payment.MarkSucceeded(webhookEvent.PaymentIntentId);
                await orderClient.ConfirmPaymentAsync(payment.OrderId, webhookEvent.PaymentIntentId, ct);
                break;
            case "payment.failed":
                payment.MarkFailed(webhookEvent.FailureReason ?? "Unknown failure");
                break;
            case "refund.succeeded":
                payment.MarkRefunded();
                break;
        }

        paymentRepo.Update(payment);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// INITIATE REFUND
public sealed record InitiateRefundCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason) : IRequest<r>;

public sealed class InitiateRefundCommandHandler(
    IPaymentRepository paymentRepo,
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWorkPayment uow) :
    IRequestHandler<InitiateRefundCommand, Result>
{
    public async Task<r> Handle(InitiateRefundCommand cmd, CancellationToken ct)
    {
        var payment = await paymentRepo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment", cmd.PaymentId));

        try
        {
            var gatewayService = gatewayFactory.Create(payment.Gateway);
            var refundResult = await gatewayService.RefundAsync(
                payment.GatewayPaymentId!, cmd.Amount, ct);

            if (!refundResult.IsSuccess)
                return Result.Failure(refundResult.Error);

            payment.InitiateRefund(cmd.Amount, cmd.Reason);
            paymentRepo.Update(payment);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("Refund", ex.Message));
        }
    }
}

// ── Interfaces ────────────────────────────────────────────────
public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentRecord?> GetByGatewayPaymentIdAsync(string paymentId, CancellationToken ct = default);
    void Add(PaymentRecord record);
    void Update(PaymentRecord record);
}

public interface IUnitOfWorkPayment
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPaymentGatewayFactory
{
    IPaymentGatewayService Create(PaymentGateway gateway);
}

public interface IPaymentGatewayService
{
    Task<Result<GatewaySession>> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct);
    Task<Result<WebhookEvent>> ParseWebhookEventAsync(string payload, string signature, CancellationToken ct);
    Task<r> RefundAsync(string paymentId, decimal amount, CancellationToken ct);
}

public interface IOrderServiceClient
{
    Task ConfirmPaymentAsync(Guid orderId, string paymentIntentId, CancellationToken ct);
}

// ── DTOs & Value Objects ──────────────────────────────────────
public sealed record PaymentSessionDto(Guid PaymentId, string CheckoutUrl, string SessionId, string PaymentIntentId);
public sealed record CreateSessionRequest(Guid OrderId, decimal Amount, string Currency, string SuccessUrl, string CancelUrl);
public sealed record GatewaySession(string SessionId, string PaymentIntentId, string CheckoutUrl);
public sealed record WebhookEvent(string EventType, string PaymentIntentId, string? FailureReason);

// ══════════════════════════════════════════════════════════════
// API CONTROLLER
// ══════════════════════════════════════════════════════════════
namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Create a checkout session for an order.</summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(PaymentSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreatePaymentSessionCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>Webhook from Stripe/PayPal/Razorpay — do not call manually.</summary>
    [HttpPost("webhook/{gateway}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Webhook(
        string gateway, CancellationToken ct)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString()
            ?? Request.Headers["X-PayPal-Transmission-Sig"].ToString()
            ?? string.Empty;

        var result = await mediator.Send(
            new HandlePaymentWebhookCommand(gateway, payload, signature), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error.Message);
    }

    /// <summary>Initiate a refund (Admin only).</summary>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Refund(
        Guid id, [FromBody] RefundRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new InitiateRefundCommand(id, request.Amount, request.Reason), ct);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    private ObjectResult Problem(Error error) => Problem(
        detail: error.Message,
        statusCode: error.Code.Contains("NotFound") ? 404 : 400,
        title: error.Code);
}

public sealed record RefundRequest(decimal Amount, string Reason);
