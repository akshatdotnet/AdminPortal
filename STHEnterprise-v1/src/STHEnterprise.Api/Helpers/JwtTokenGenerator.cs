//using STHEnterprise.Api.Helpers;

//public class JwtTokenGenerator : IJwtTokenGenerator
//{
//    public string GenerateToken(AppUser user)
//    {
//        // existing JWT logic
//        return "real-jwt-token";
//    }
//}



using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using STHEnterprise.Api.Helpers;

//using Microsoft.IdentityModel.Tokens; // Ensure this is included for token-related classes
//using System.IdentityModel.Tokens.Jwt; // Ensure this is included for JWT handling
//using System.Security.Claims; // Ensure this is included for Claims
//using System.Text; // Ensure this is included for Encoding

// Ensure the necessary NuGet package is installed:
// Install-Package System.IdentityModel.Tokens.Jwt
//Install - Package System.IdentityModel.Tokens.Jwt
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_config["Jwt:ExpiryMinutes"])),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
