using AdminPortal.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

/// <summary>
/// Stub account/auth controller.
/// Extend this with JWT / cookie auth as needed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountController : ControllerBase
{
    /// <summary>Health check / ping.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { Message = "AdminPortal API is running.", Timestamp = DateTime.UtcNow });

    /// <summary>
    /// Stub login — returns a success flag.
    /// Replace with real auth (JWT, cookie, etc.) as needed.
    /// </summary>
    [HttpPost("login")]
    public ActionResult<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Hardcoded stub — swap for real user lookup
        if (request.Email == "admin@vaishnomart.com" && request.Password == "Admin@123")
        {
            return Ok(new ApiResponse<LoginResponse>
            {
                Data = new LoginResponse
                {
                    Email = request.Email,
                    Name = "Ravi Kumar",
                    Role = "Admin",
                    Token = "stub-token-replace-with-jwt"
                },
                Message = "Login successful."
            });
        }

        return Unauthorized(new ApiResponse<LoginResponse>
        {
            Success = false,
            Message = "Invalid email or password."
        });
    }
}

public record LoginRequest(string Email, string Password);

public class LoginResponse
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Token { get; set; } = "";
}
