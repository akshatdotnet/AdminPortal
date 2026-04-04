using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    // ── Auth ─────────────────────────────────────────────────
    [MaxLength(256)]
    public string? PasswordHash { get; set; }

    public bool IsEmailVerified { get; set; } = false;

    // Password reset
    [MaxLength(128)]
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Security
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    [MaxLength(45)]
    public string? LastLoginIp { get; set; }

    // ── Profile ──────────────────────────────────────────────
    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(200)]
    public string? AvatarUrl { get; set; }

    [MaxLength(100)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual Wallet? Wallet { get; set; }

    // Helpers
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    public string Initials => string.Concat(FullName.Split(' ').Where(p => p.Length > 0).Take(2).Select(p => p[0])).ToUpper();
}

public enum UserStatus { Active, Inactive, Suspended }
public enum UserRole   { User, Admin }
