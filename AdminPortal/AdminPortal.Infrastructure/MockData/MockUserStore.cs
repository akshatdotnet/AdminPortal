using AdminPortal.Domain.Entities;

namespace AdminPortal.Infrastructure.MockData;

/// <summary>
/// In-memory user store. Pre-seeded with demo credentials:
///   Email: admin@shopzo.com | Password: Admin@1234
///   Email: manager@shopzo.com | Password: Manager@1234
/// </summary>
public static class MockUserStore
{
    private static string MockHash(string password)
    {
        var salted = $"shopzo_salt_{password}_2024";
        var bytes  = System.Text.Encoding.UTF8.GetBytes(salted);
        var hash   = System.Security.Cryptography.SHA256.HashData(bytes);
        return "$mock$" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static List<AppUser> Users { get; } = new()
    {
        new AppUser
        {
            Id              = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000001"),
            Name            = "Pradeep Gupta",
            Email           = "admin@shopzo.com",
            MobileNumber    = "+919876543210",
            PasswordHash    = MockHash("Admin@1234"),
            Role            = "Owner",
            IsActive        = true,
            IsEmailVerified = true,
            CreatedAt       = DateTime.UtcNow.AddYears(-2)
        },
        new AppUser
        {
            Id              = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000002"),
            Name            = "Meena Sharma",
            Email           = "manager@shopzo.com",
            MobileNumber    = "+919123456789",
            PasswordHash    = MockHash("Manager@1234"),
            Role            = "Manager",
            IsActive        = true,
            IsEmailVerified = true,
            CreatedAt       = DateTime.UtcNow.AddMonths(-8)
        }
    };

    public static List<PasswordResetToken> ResetTokens { get; } = new();
}
