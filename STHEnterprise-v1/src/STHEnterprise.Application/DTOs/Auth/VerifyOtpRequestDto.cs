namespace STHEnterprise.Application.DTOs.Auth;

public class VerifyOtpRequestDto
{
    public string PhoneNumber { get; set; } = "";
    public string Otp { get; set; } = "";
}
