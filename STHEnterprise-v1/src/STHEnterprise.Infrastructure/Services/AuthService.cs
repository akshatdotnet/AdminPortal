using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using STHEnterprise.Application.DTOs.Auth;
using STHEnterprise.Application.Interfaces;

namespace STHEnterprise.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const string MOCK_OTP = "123456";
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public void SendOtp(string phoneNumber)
    {
        // 🔥 MOCK OTP – log instead of SMS
        Console.WriteLine($"OTP for {phoneNumber} is {MOCK_OTP}");
    }

    public AuthResponseDto VerifyOtp(string phoneNumber, string otp)
    {
        if (otp != MOCK_OTP)
            throw new UnauthorizedAccessException("Invalid OTP");

        var token = GenerateJwt(phoneNumber);

        return new AuthResponseDto
        {
            PhoneNumber = phoneNumber,
            Name = "Suraj Tea House User",
            Token = token
        };
    }

    private string GenerateJwt(string phone)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, phone),
            new Claim(ClaimTypes.MobilePhone, phone)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
