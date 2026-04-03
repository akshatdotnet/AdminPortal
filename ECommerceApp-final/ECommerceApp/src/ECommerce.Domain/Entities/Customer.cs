using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Customer : Entity
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Email Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    private Customer() { }

    public static Customer Create(string firstName, string lastName, string email, string? phone = null)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = new Email(email),
            Phone = phone,
            CreatedAt = DateTime.UtcNow
        };
    }
}
