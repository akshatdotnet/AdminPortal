using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int ResetTokenExpiryMinutes = 60;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        // 1. Find user by email or mobile
        var user = await _userRepository.GetByEmailOrMobileAsync(dto.EmailOrMobile);
        if (user is null)
            return Fail("Invalid email/mobile or password.");

        // 2. Check lockout
        if (user.IsLockedOut)
            return new AuthResultDto
            {
                IsLockedOut = true,
                ErrorMessage = $"Account locked. Try again after {user.LockoutUntil!.Value:hh:mm tt}."
            };

        // 3. Verify password
        if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            int remaining = MaxFailedAttempts - user.FailedLoginAttempts;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0;
                await _userRepository.UpdateAsync(user);
                return new AuthResultDto
                {
                    IsLockedOut = true,
                    ErrorMessage = $"Too many failed attempts. Account locked for {LockoutMinutes} minutes."
                };
            }

            await _userRepository.UpdateAsync(user);
            return new AuthResultDto
            {
                ErrorMessage = "Invalid email/mobile or password.",
                RemainingAttempts = remaining > 0 ? remaining : 0
            };
        }

        // 4. Check active
        if (!user.IsActive)
            return Fail("Your account has been deactivated. Contact support.");

        // 5. Reset failed attempts and update last login
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return new AuthResultDto
        {
            IsSuccess = true,
            UserName  = user.Name,
            UserEmail = user.Email,
            UserRole  = user.Role
        };
    }

    public async Task<AuthResultDto> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        // Always return success to prevent user enumeration (security best practice)
        if (user is null || !user.IsActive)
            return new AuthResultDto { IsSuccess = true };

        // Invalidate any existing tokens for this user
        await _tokenRepository.InvalidateAllForUserAsync(user.Id);

        // Generate a cryptographically secure token
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var resetToken = new PasswordResetToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = tokenHash,
            Email     = user.Email,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };
        await _tokenRepository.AddAsync(resetToken);

        // Send email with raw token (only raw token leaves the server, hash is stored)
        var resetLink = $"/Account/ResetPassword?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(user.Email)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);

        return new AuthResultDto { IsSuccess = true };
    }

    public async Task<AuthResultDto> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var tokenHash = HashToken(dto.Token);
        var storedToken = await _tokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken is null || !storedToken.IsValid ||
            !storedToken.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
            return Fail("This reset link is invalid or has expired. Please request a new one.");

        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user is null || !user.IsActive)
            return Fail("User not found.");

        // Hash and save new password
        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        await _userRepository.UpdateAsync(user);

        // Invalidate the used token
        await _tokenRepository.MarkUsedAsync(storedToken.Id);

        return new AuthResultDto { IsSuccess = true, UserEmail = user.Email };
    }

    public async Task<bool> ValidateResetTokenAsync(string token, string email)
    {
        var tokenHash = HashToken(token);
        var storedToken = await _tokenRepository.GetByTokenHashAsync(tokenHash);
        return storedToken is not null
            && storedToken.IsValid
            && storedToken.Email.Equals(email, StringComparison.OrdinalIgnoreCase);
    }




    // ── Helpers ──────────────────────────────────────────────

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash  = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static AuthResultDto Fail(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };

    // ─────────────────────────────────────────────────────────────────
    // AuthResult is likely already in your project.
    // Ensure it has at minimum:
    //   bool   IsSuccess
    //   string? ErrorMessage
    // ─────────────────────────────────────────────────────────────────

    // ── ADD to AuthService.cs ─────────────────────────────────────────
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        // 1. Duplicate email check
        var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingByEmail != null)
            return new AuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "An account with this email already exists."
            };

        // 2. Duplicate mobile check
        var fullMobile = (dto.CountryCode + dto.MobileNumber).Replace(" ", "").Replace("-", "");
        var existingByMobile = await _userRepository.GetByEmailOrMobileAsync(fullMobile);
        if (existingByMobile != null)
            return new AuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "This mobile number is already registered."
            };

        // 3. Hash password (BCrypt — same pattern as your LoginAsync)
        //var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var passwordHash = dto.Password;

        // 4. Build new AppUser — mirrors MockUserStore field names
        var newUser = new AppUser
        {
            Id = Guid.NewGuid(),
            //FirstName = dto.FirstName.Trim(),
            //LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            MobileNumber = fullMobile,
            PasswordHash = passwordHash,
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await _userRepository.AddAsync(newUser);

        return new AuthResultDto { IsSuccess = true };
    }

}
