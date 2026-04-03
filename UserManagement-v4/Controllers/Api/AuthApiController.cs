using Microsoft.AspNetCore.Mvc;
using UserManagement.DTOs;
using UserManagement.Helpers;
using UserManagement.Services;
using UserManagement.ViewModels;

namespace UserManagement.Controllers.Api
{
    /// <summary>
    /// REST API surface for authentication.
    ///
    /// Routes
    ///   POST   /api/auth/login    – validate credentials, open session
    ///   POST   /api/auth/logout   – destroy session
    ///   GET    /api/auth/me       – return current user from session
    ///
    /// Notes
    ///   • No [ValidateAntiForgeryToken] – API clients (SPA, mobile, Postman)
    ///     cannot supply CSRF tokens.  Protect against CSRF with SameSite=Strict
    ///     session cookies or, better, migrate to JWT Bearer tokens.
    ///   • All responses use the built-in Problem Details format (RFC 7807) for
    ///     errors, and a typed DTO for success payloads – never raw strings.
    ///   • Session is still used here so the MVC and API surfaces share auth
    ///     state.  When you add JWT support, remove session writes from Login()
    ///     and issue a signed token instead.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            IAuthService authService,
            ILogger<AuthApiController> logger)
        {
            _authService = authService;
            _logger      = logger;
        }

        // ── POST /api/auth/login ──────────────────────────────────────────────

        /// <summary>Validate credentials and open an authenticated session.</summary>
        /// <param name="dto">Username/email + password</param>
        /// <returns>200 AuthResponseDto | 400 validation errors | 401 bad creds</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            // ModelState is checked automatically by [ApiController];
            // this guard is explicit for clarity and unit-test predictability.
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            SessionUserViewModel? user;
            try
            {
                user = await _authService.ValidateLoginAsync(dto.Username, dto.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login service error for user {Username}", dto.Username);
                return Problem(
                    title:      "Login failed",
                    detail:     "An unexpected error occurred. Please try again.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            if (user is null)
            {
                // Use 401 (not 400) – the request was well-formed, credentials just wrong.
                // Do NOT reveal which field was incorrect (username vs. password) –
                // that aids enumeration attacks.
                _logger.LogWarning("Failed login attempt for identifier {Username}", dto.Username);
                return Problem(
                    title:      "Invalid credentials",
                    detail:     "The username or password is incorrect.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Write session (shared with MVC controllers)
            HttpContext.Session.SetObjectAsJson("CurrentUser", user);

            // Fire-and-forget: update last-login timestamp
            _ = _authService.UpdateLastLoginAsync(user.Id);

            _logger.LogInformation("User {Username} (Id={Id}) logged in", user.Username, user.Id);

            return Ok(MapToDto(user));
        }

        // ── POST /api/auth/logout ─────────────────────────────────────────────

        /// <summary>Destroy the current session.</summary>
        /// <returns>204 No Content</returns>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {
            var user = HttpContext.Session.GetObjectFromJson<SessionUserViewModel>("CurrentUser");

            HttpContext.Session.Clear();

            if (user is not null)
                _logger.LogInformation("User {Username} (Id={Id}) logged out", user.Username, user.Id);

            // 204: action was completed, no body to return
            return NoContent();
        }

        // ── GET /api/auth/me ──────────────────────────────────────────────────

        /// <summary>Return the currently authenticated user's profile.</summary>
        /// <returns>200 AuthResponseDto | 401 if not logged in</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public IActionResult Me()
        {
            var user = HttpContext.Session.GetObjectFromJson<SessionUserViewModel>("CurrentUser");

            if (user is null)
                return Problem(
                    title:      "Not authenticated",
                    detail:     "No active session. Please log in.",
                    statusCode: StatusCodes.Status401Unauthorized);

            return Ok(MapToDto(user));
        }


        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Message = "API is working 🚀",
                Timestamp = DateTime.UtcNow
            });
        }


        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Map the internal session model to the public API DTO.
        /// Keeps internal fields (permissions, etc.) out of the API response.
        /// </summary>
        private static AuthResponseDto MapToDto(SessionUserViewModel user) => new()
        {
            Id       = user.Id,
            FullName = user.FullName,
            Username = user.Username,
            Email    = user.Email,
            Roles    = user.Roles,
            // Token and TokenExpiresAt left null until JWT is wired up
        };
    }
}
