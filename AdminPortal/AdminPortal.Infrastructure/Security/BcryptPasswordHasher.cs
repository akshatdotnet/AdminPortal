using AdminPortal.Application.Interfaces;

namespace AdminPortal.Infrastructure.Security;

/// <summary>
/// BCrypt password hasher. Work factor 12 is the recommended balance
/// between security and performance (adds ~300ms per hash — intentional).
/// Replace BCrypt.Net-Next NuGet with your preferred lib when integrating.
/// For mock purposes this uses SHA-256 with a salt prefix (no external dep needed).
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        // Mock implementation: In production use BCrypt.Net-Next
        // Install-Package BCrypt.Net-Next
        // return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        return MockHash(password);
    }

    public bool Verify(string password, string hash)
    {
        // In production: return BCrypt.Net.BCrypt.Verify(password, hash);
        return MockHash(password) == hash;
    }

    // ── Mock (no external dep) — Replace with BCrypt in production ──
    private static string MockHash(string password)
    {
        var salted = $"shopzo_salt_{password}_2024";
        var bytes  = System.Text.Encoding.UTF8.GetBytes(salted);
        var hash   = System.Security.Cryptography.SHA256.HashData(bytes);
        return "$mock$" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
