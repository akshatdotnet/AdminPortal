using System.ComponentModel.DataAnnotations;

namespace WalletSystem.ViewModels;

// ─── Auth ─────────────────────────────────────────────────────────────────────

public class LoginVM
{
    [Required(ErrorMessage = "Username or email is required")]
    [Display(Name = "Username or Email")]
    public string Identifier { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = "";
}

public class ResetPasswordVM
{
    [Required]
    public string Token { get; set; } = "";

    [Required]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";
}

// ─── Profile ──────────────────────────────────────────────────────────────────

public class ProfileVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public decimal? WalletBalance { get; set; }
    public string? WalletCurrency { get; set; }
    public int TransactionCount { get; set; }
    public string Initials { get; set; } = "";
}

public class EditProfileVM
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Required, Phone, MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = "";

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(200)]
    [Display(Name = "Avatar URL")]
    public string? AvatarUrl { get; set; }

    [MaxLength(100)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
}

public class ChangePasswordVM
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = "";
}
