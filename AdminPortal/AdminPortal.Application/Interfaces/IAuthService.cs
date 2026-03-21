using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto dto);
    Task<AuthResultDto> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<AuthResultDto> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> ValidateResetTokenAsync(string token, string email);

    Task<AuthResultDto> RegisterAsync(RegisterDto dto);
}
