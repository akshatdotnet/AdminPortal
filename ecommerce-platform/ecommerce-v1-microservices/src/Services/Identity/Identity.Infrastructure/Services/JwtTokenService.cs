using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _s = options.Value;

    public AuthResponseDto GenerateTokens(ApplicationUser user)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_s.ExpiryMinutes);
        var token   = CreateAccessToken(user, expiry);
        var refresh = CreateRefreshToken();
        return new AuthResponseDto(
            token, refresh, expiry,
            user.Id.ToString(), user.Email, user.FullName, user.Role);
    }

    public Guid? GetUserIdFromExpiredToken(string token)
    {
        var p = new TokenValidationParameters
        {
            ValidateIssuer           = true,  ValidIssuer    = _s.Issuer,
            ValidateAudience         = true,  ValidAudience  = _s.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(_s.SecretKey)),
            ValidateLifetime         = false   // allow expired tokens here
        };
        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, p, out _);

            // Use Claims directly — avoids dependency on AspNetCore extensions
            var idClaim = principal.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(idClaim, out var g) ? g : null;
        }
        catch { return null; }
    }

    private string CreateAccessToken(ApplicationUser user, DateTime expiry)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,           user.Email),
            new Claim(ClaimTypes.Name,            user.FullName),
            new Claim(ClaimTypes.Role,            user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_s.SecretKey));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt  = new JwtSecurityToken(
            _s.Issuer, _s.Audience, claims,
            expires: expiry, signingCredentials: cred);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static string CreateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)   => BCrypt.Net.BCrypt.HashPassword(password, 12);
    public bool   Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string SecretKey     { get; init; } = default!;
    public string Issuer        { get; init; } = default!;
    public string Audience      { get; init; } = default!;
    public int    ExpiryMinutes { get; init; } = 60;
}
