using Common.Domain.Entities;

namespace Identity.Domain.Entities;

public sealed record UserRegisteredEvent(
    Guid UserId, string Email, string FullName) : BaseDomainEvent;

public sealed record EmailConfirmedEvent(
    Guid UserId, string Email) : BaseDomainEvent;

public sealed record PasswordChangedEvent(
    Guid UserId, string Email) : BaseDomainEvent;
