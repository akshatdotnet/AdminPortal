using STHEnterprise.Application.DTOs.Auth;

namespace STHEnterprise.Application.Interfaces;

public interface IAuthService
{
    void SendOtp(string phoneNumber);
    AuthResponseDto VerifyOtp(string phoneNumber, string otp);
}
