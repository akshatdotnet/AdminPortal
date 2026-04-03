using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Payment.Commands;

public record ProcessPaymentCommand(Guid OrderId, Guid CustomerId, PaymentMethod PaymentMethod)
    : IRequest<Result<PaymentDto>>;
