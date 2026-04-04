using Common.Domain.Entities;

namespace Identity.Domain.Entities;

public sealed class ApplicationUser : BaseEntity
{
    private ApplicationUser() { }

    public string Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = UserRoles.Customer;
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;

    public static ApplicationUser Create(string email, string firstName, string lastName,
        string phoneNumber, string passwordHash, string role = UserRoles.Customer)
    {
        var user = new ApplicationUser
        {
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHash,
            Role = role
        };
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.FullName));
        return user;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        AddDomainEvent(new EmailConfirmedEvent(Id, Email));
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public void RecordFailedLogin(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public void UpdateProfile(string firstName, string lastName, string phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        SetUpdated(Email);
    }
}

public static class UserRoles
{
    public const string Admin    = "Admin";
    public const string Customer = "Customer";
    public const string Vendor   = "Vendor";
    public static readonly IReadOnlyList<string> All = new[] { Admin, Customer, Vendor };
}
