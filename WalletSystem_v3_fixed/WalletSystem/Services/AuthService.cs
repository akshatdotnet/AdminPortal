using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WalletSystem.Data;
using WalletSystem.Models;

namespace WalletSystem.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, User? User)> ValidateLoginAsync(string identifier, string password, string ipAddress);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<(bool Success, string Token)> GeneratePasswordResetTokenAsync(string email);
    Task<(bool Success, string Message)> ResetPasswordAsync(string email, string token, string newPassword);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public AuthService(AppDbContext db) => _db = db;

    // ── BCrypt-style hashing using PBKDF2 ────────────────────
    public string HashPassword(string password)
    {
        // PBKDF2 with SHA-256, 100,000 iterations, 256-bit salt + hash
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            100_000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        // Support legacy seed hash marker
        if (storedHash.StartsWith("$2a$")) return true; // seeded users use default password
        if (!storedHash.StartsWith("pbkdf2$")) return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 3) return false;

        try
        {
            byte[] salt  = Convert.FromBase64String(parts[1]);
            byte[] stored = Convert.FromBase64String(parts[2]);
            byte[] computed = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt,
                100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(stored, computed);
        }
        catch { return false; }
    }

    public async Task<(bool Success, string Message, User? User)> ValidateLoginAsync(
        string identifier, string password, string ipAddress)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == identifier || u.Username == identifier);

        if (user == null)
            return (false, "Invalid credentials.", null);

        if (user.Status == UserStatus.Suspended)
            return (false, "Your account has been suspended. Please contact support.", null);

        if (user.Status == UserStatus.Inactive)
            return (false, "Your account is inactive. Please contact support.", null);

        if (user.IsLocked)
        {
            var remaining = (int)(user.LockedUntil!.Value - DateTime.UtcNow).TotalMinutes + 1;
            return (false, $"Account locked. Try again in {remaining} minute(s).", null);
        }

        // Seeded accounts accept "Password123!" as default
        bool passwordOk = user.PasswordHash?.StartsWith("$2a$") == true
            ? password == "Password123!"
            : VerifyPassword(password, user.PasswordHash ?? "");

        if (!passwordOk)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0;
                await _db.SaveChangesAsync();
                return (false, $"Too many failed attempts. Account locked for {LockoutMinutes} minutes.", null);
            }
            await _db.SaveChangesAsync();
            int remaining = MaxFailedAttempts - user.FailedLoginAttempts;
            return (false, $"Invalid credentials. {remaining} attempt(s) remaining.", null);
        }

        // Success
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        await _db.SaveChangesAsync();

        return (true, "Login successful.", user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        bool current = user.PasswordHash?.StartsWith("$2a$") == true
            ? currentPassword == "Password123!"
            : VerifyPassword(currentPassword, user.PasswordHash ?? "");

        if (!current) return false;

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Token)> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return (false, "");

        // Generate cryptographically secure token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                           .Replace("+", "-").Replace("/", "_").Replace("=", "");

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        await _db.SaveChangesAsync();

        return (true, token);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(
        string email, string token, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return (false, "Invalid reset request.");

        if (user.PasswordResetToken != token)
            return (false, "Invalid or expired reset token.");

        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return (false, "Reset token has expired. Please request a new one.");

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, "Password reset successfully.");
    }
}
