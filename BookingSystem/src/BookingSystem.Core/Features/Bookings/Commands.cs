using BookingSystem.Core.Entities;
using MediatR;

namespace BookingSystem.Core.Features.Bookings;

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public record BookingDto(
    Guid Id, Guid CustomerId, string CustomerName,
    Guid VenueId, string VenueName,
    DateTime SlotDate, int GuestCount,
    BookingStatus Status, decimal TotalAmount,
    DateTime CreatedAt);

public record OrderDto(
    Guid Id, Guid BookingId, decimal Amount,
    OrderStatus Status, string? PaymentReference, DateTime CreatedAt);

// ─── BOOKING COMMANDS ─────────────────────────────────────────────────────────
public record CreateBookingCommand(
    Guid CustomerId, Guid VenueId,
    DateTime SlotDate, int GuestCount) : IRequest<BookingDto>;

public record ConfirmBookingCommand(Guid BookingId) : IRequest<BookingDto>;
public record CancelBookingCommand(Guid BookingId, string Reason) : IRequest<BookingDto>;

// ─── ORDER COMMANDS ───────────────────────────────────────────────────────────
public record CreateOrderCommand(Guid BookingId) : IRequest<OrderDto>;
public record ProcessPaymentCommand(Guid OrderId, string CardToken) : IRequest<OrderDto>;
public record RefundOrderCommand(Guid OrderId) : IRequest<OrderDto>;

// ─── QUERIES ──────────────────────────────────────────────────────────────────
public record GetBookingQuery(Guid BookingId) : IRequest<BookingDto?>;
public record GetCustomerBookingsQuery(Guid CustomerId) : IRequest<IReadOnlyList<BookingDto>>;
public record GetAvailableSlotsQuery(Guid VenueId, DateTime Date) : IRequest<IReadOnlyList<DateTime>>;
public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;
