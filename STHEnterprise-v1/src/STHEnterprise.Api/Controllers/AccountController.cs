using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Api.Helpers;
using STHEnterprise.Application.DTOs.Account;
using STHEnterprise.Application.DTOs.Auth;
using STHEnterprise.Application.Interfaces;

/*
 * 
 ✅ Final Corrected Controller Summary
You should end up with:
| Action                 | HTTP     | Purpose              |
| ---------------------- | -------- | -------------------- |
| Login()                | GET      | Show login page      |
| Login(LoginViewModel)  | POST     | Email/password login |
| LoginWithPhone(string) | POST     | Phone login          |
| Register()             | GET      | Register page        |
| ForgotPassword()       | GET/POST | Password recovery    |
| Logout()               | GET      | Logout               | 

POST: api/v1/auth/send-otp
POST: api/v1/auth/verify-otp
POST: api/v1/auth/login-mpin
POST: api/v1/auth/register
POST: api/v1/auth/login
POST: api/v1/auth/reset-password 
POST  api/v1/auth/refresh-token
POST  api/v1/auth/logout

*/


[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/account")]
public class AccountController : ControllerBase
{
    private readonly IAuthService _authService;

    private readonly IJwtTokenGenerator _tokenGenerator;

    public AccountController(IJwtTokenGenerator tokenGenerator, IAuthService authService)
    {
        _tokenGenerator = tokenGenerator;
        _authService = authService;
    }

    // POST: api/v1/auth/send-otp
    [HttpPost("send-otp")]
    public IActionResult SendOtp([FromBody] SendOtpRequestDto request)
    {
        _authService.SendOtp(request.PhoneNumber);

        return Ok(new
        {
            success = true,
            message = "OTP sent successfully (mock)"
        });
    }

    // POST: api/v1/auth/verify-otp
    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var result = _authService.VerifyOtp(request.PhoneNumber, request.Otp);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request is null)
            return BadRequest("Invalid request");

        if (!InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
            return Unauthorized("Invalid credentials");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = _tokenGenerator.GenerateToken(user);

        var response = new LoginResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }
        };

        return Ok(response);
    }

    // =========================
    // REGISTER
    // =========================
    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        if (InMemoryUserStore.Users.ContainsKey(request.Email))
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "User already exists"
            });

        var user = new AppUser
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password)
        };

        InMemoryUserStore.Users[request.Email] = user;

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "User registered successfully"
        });
    }


    // =========================
    // FORGOT PASSWORD
    // =========================
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword(ForgotPasswordRequest request)
    {
        if (!InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "If the email exists, reset instructions have been sent"
            });

        user.ResetToken = Guid.NewGuid().ToString();
        user.ResetTokenExpiry = Convert.ToString( DateTime.UtcNow.AddMinutes(15));

        // In real system → send email
        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Password reset token generated",
            Data = new { user.ResetToken }
        });
    }

    // =========================
    // RESET PASSWORD
    // =========================
    [HttpPost("reset-password")]
    public IActionResult ResetPassword(ResetPasswordRequest request)
    {
        var user = InMemoryUserStore.Users.Values.FirstOrDefault(
            x => x.ResetToken == request.Token); 
                 //&&
                 //x.ResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Invalid or expired reset token"
            });

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Password reset successfully"
        });
    }



    //using Asp.Versioning;
    //using Microsoft.AspNetCore.Authorization;
    //using Microsoft.AspNetCore.Mvc;
    //using STHEnterprise.Api.Models;

    //namespace STHEnterprise.Api.Controllers.v1;

    //[ApiController]
    //[ApiVersion("1.0")]
    //[Route("api/v{version:apiVersion}/account")]
    //public class AccountController : ControllerBase
    //{
    //    private readonly JwtTokenGenerator _tokenGenerator;

    //    public AccountController(JwtTokenGenerator tokenGenerator)
    //    {
    //        _tokenGenerator = tokenGenerator;
    //    }

    //    // =====================================================
    //    // REGISTER
    //    // =====================================================
    //    /// <summary>
    //    /// Registers a new user (In-Memory store for demo)
    //    /// </summary>
    //    [HttpPost("register")]
    //    [AllowAnonymous]
    //    public IActionResult Register([FromBody] RegisterRequest request)
    //    {
    //        if (!ModelState.IsValid)
    //            return BadRequest(ModelState);

    //        if (InMemoryUserStore.Users.ContainsKey(request.Email))
    //            return BadRequest(ApiResponse.Fail("User already exists"));

    //        var user = new AppUser
    //        {
    //            Id = Guid.NewGuid().ToString(),
    //            FullName = request.FullName,
    //            Email = request.Email,
    //            Role = "User", // default role
    //            PasswordHash = PasswordHasher.Hash(request.Password)
    //        };

    //        InMemoryUserStore.Users.TryAdd(user.Email, user);

    //        return Ok(ApiResponse.Success("User registered successfully"));
    //    }

    //    // =====================================================
    //    // LOGIN
    //    // =====================================================
    //    /// <summary>
    //    /// Authenticates user and returns JWT token
    //    /// </summary>
    //    [HttpPost("login")]
    //    [AllowAnonymous]
    //    public IActionResult Login([FromBody] LoginRequest request)
    //    {
    //        if (!ModelState.IsValid)
    //            return BadRequest(ModelState);

    //        if (!InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
    //            return Unauthorized(ApiResponse.Fail("Invalid email or password"));

    //        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
    //            return Unauthorized(ApiResponse.Fail("Invalid email or password"));

    //        var token = _tokenGenerator.GenerateToken(user);

    //        return Ok(new
    //        {
    //            token,
    //            user = new
    //            {
    //                user.Id,
    //                user.FullName,
    //                user.Email,
    //                user.Role
    //            }
    //        });
    //    }

    //    // =====================================================
    //    // AUTHORIZATION TEST ENDPOINTS
    //    // =====================================================

    //    /// <summary>
    //    /// Accessible only by Admin role
    //    /// </summary>
    //    [Authorize(Roles = "Admin")]
    //    [HttpGet("admin-only")]
    //    public IActionResult AdminOnly()
    //    {
    //        return Ok("Admin access granted");
    //    }

    //    /// <summary>
    //    /// Accessible by Admin & Manager
    //    /// </summary>
    //    [Authorize(Roles = "Admin,Manager")]
    //    [HttpGet("admin-manager")]
    //    public IActionResult AdminManager()
    //    {
    //        return Ok("Admin & Manager access granted");
    //    }

    //    /// <summary>
    //    /// Accessible by Admin & User
    //    /// </summary>
    //    [Authorize(Roles = "Admin,User")]
    //    [HttpGet("admin-user")]
    //    public IActionResult AdminUser()
    //    {
    //        return Ok("Admin & User access granted");
    //    }

    //    /// <summary>
    //    /// Any authenticated user
    //    /// </summary>
    //    [Authorize]
    //    [HttpGet("authenticated")]
    //    public IActionResult Authenticated()
    //    {
    //        return Ok("User is authenticated");
    //    }

    //    // =====================================================
    //    // FORGOT PASSWORD
    //    // =====================================================
    //    /// <summary>
    //    /// Generates reset token (always returns success to prevent email enumeration)
    //    /// </summary>
    //    [HttpPost("forgot-password")]
    //    [AllowAnonymous]
    //    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
    //    {
    //        if (!ModelState.IsValid)
    //            return BadRequest(ModelState);

    //        if (InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
    //        {
    //            user.ResetToken = Guid.NewGuid().ToString();
    //            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

    //            // TODO: Send email in real system
    //        }

    //        return Ok(ApiResponse.Success(
    //            "If the email exists, reset instructions have been sent"
    //        ));
    //    }

    //    // =====================================================
    //    // RESET PASSWORD
    //    // =====================================================
    //    /// <summary>
    //    /// Resets password using reset token
    //    /// </summary>
    //    [HttpPost("reset-password")]
    //    [AllowAnonymous]
    //    public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
    //    {
    //        if (!ModelState.IsValid)
    //            return BadRequest(ModelState);

    //        var user = InMemoryUserStore.Users.Values.FirstOrDefault(u =>
    //            u.ResetToken == request.Token &&
    //            u.ResetTokenExpiry > DateTime.UtcNow);

    //        if (user == null)
    //            return BadRequest(ApiResponse.Fail("Invalid or expired reset token"));

    //        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
    //        user.ResetToken = null;
    //        user.ResetTokenExpiry = null;

    //        return Ok(ApiResponse.Success("Password reset successfully"));
    //    }
    //}





    // =========================
    // LOGIN
    // =========================

    //[HttpPost("login")]
    //public IActionResult Login(LoginRequest request,
    //                       [FromServices] JwtTokenGenerator tokenGenerator)
    //{
    //    if (!InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
    //        return Unauthorized("Invalid credentials");

    //    if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
    //        return Unauthorized("Invalid credentials");

    //    var token = tokenGenerator.GenerateToken(user);

    //    return Ok(new
    //    {
    //        token,
    //        user = new
    //        {
    //            user.Id,
    //            user.FullName,
    //            user.Email,
    //            user.Role
    //        }
    //    });
    //}





    //[HttpPost("login")]
    //[AllowAnonymous]
    //public IActionResult Login([FromBody] LoginRequest request)
    //{
    //    if (!InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
    //        return Unauthorized("Invalid credentials");

    //    if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
    //        return Unauthorized("Invalid credentials");

    //    var token = _tokenGenerator.GenerateToken(user);

    //    return Ok(new
    //    {
    //        token,
    //        user = new
    //        {
    //            user.Id,
    //            user.FullName,
    //            user.Email,
    //            user.Role
    //        }
    //    });
    //}



    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        return Ok("Admin access granted");
    }


    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("admin-manager")]
    public IActionResult AdminManager()
    {
        return Ok("Admin & Manager access");
    }

    [Authorize(Roles = "Admin,User")]
    [HttpGet("admin-User")]
    public IActionResult AdminUser()
    {
        return Ok("Admin & User access");
    }

    [Authorize]
    [HttpGet("authenticated")]
    public IActionResult Authenticated()
    {
        return Ok("User is authenticated");
    }


    //[HttpPost("login")]
    //public IActionResult Login(LoginRequest request)
    //{
    //    if (!InMemoryUserStore.Users.TryGetValue(request.Email, out var user))
    //        return Unauthorized(new ApiResponse
    //        {
    //            Success = false,
    //            Message = "Invalid email or password"
    //        });

    //    if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
    //        return Unauthorized(new ApiResponse
    //        {
    //            Success = false,
    //            Message = "Invalid email or password"
    //        });

    //    return Ok(new ApiResponse
    //    {
    //        Success = true,
    //        Message = "Login successful",
    //        Data = new
    //        {
    //            user.Id,
    //            user.FullName,
    //            user.Email
    //        }
    //    });
    //}

    
}


/*
 
🌐 7️⃣ API ENDPOINTS (READY)
Method	Endpoint
POST	/api/v1/account/register
POST	/api/v1/account/login
POST	/api/v1/account/forgot-password
POST	/api/v1/account/reset-password
 
🧪 SAMPLE PAYLOADS
Register
{
  "fullName": "John Doe",
  "email": "john@test.com",
  "password": "Password@123"
}

Login
{
  "email": "john@test.com",
  "password": "Password@123"
}

🏆 INTERVIEW-READY ONE-LINER

“The Account API supports secure registration, login, and password recovery using hashed credentials, token-based reset flow, and clean REST design.”

🚀 NEXT UP (OPTIONAL UPGRADES)

1️⃣ JWT Authentication
2️⃣ ASP.NET Identity integration
3️⃣ Email service (SendGrid)
4️⃣ Refresh tokens
5️⃣ Role-based authorization
6️⃣ Rate limiting

If you want JWT + Swagger auth next, just say the word 👍
 
 */