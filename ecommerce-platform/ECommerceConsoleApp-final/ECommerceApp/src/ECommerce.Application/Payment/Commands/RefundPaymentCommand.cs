using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Payment.Commands;

public record RefundPaymentCommand(Guid OrderId, Guid CustomerId) : IRequest<Result<PaymentDto>>;
