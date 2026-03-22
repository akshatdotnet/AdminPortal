namespace BookingSystem.Core.Events;

// All domain events - published to message bus for async processing
public record BookingCreatedEvent(Guid BookingId, Guid CustomerId, Guid VenueId, DateTime SlotDate, decimal Amount, DateTime OccurredAt);
public record BookingConfirmedEvent(Guid BookingId, Guid CustomerId, string CustomerEmail, DateTime SlotDate, DateTime OccurredAt);
public record BookingCancelledEvent(Guid BookingId, Guid CustomerId, string Reason, DateTime OccurredAt);
public record OrderCreatedEvent(Guid OrderId, Guid BookingId, Guid CustomerId, decimal Amount, DateTime OccurredAt);
public record OrderPaidEvent(Guid OrderId, Guid BookingId, string PaymentReference, DateTime OccurredAt);
public record OrderFailedEvent(Guid OrderId, Guid BookingId, string Reason, DateTime OccurredAt);
public record NotificationEvent(string To, string Subject, string Body, string Channel, DateTime OccurredAt); // Channel: Email|SMS
