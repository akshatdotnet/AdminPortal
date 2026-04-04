namespace ECommerce.Domain.Exceptions;

/// <summary>Thrown when a domain business rule is violated.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
