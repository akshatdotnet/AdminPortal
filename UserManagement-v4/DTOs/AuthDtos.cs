using System.ComponentModel.DataAnnotations;

namespace UserManagement.DTOs
{
    // ── Inbound ──────────────────────────────────────────────────────────────

    /// <summary>Payload accepted by POST /api/auth/login</summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    // ── Outbound ─────────────────────────────────────────────────────────────

    /// <summary>Returned on successful login</summary>
    public class AuthResponseDto
    {
        public int    Id       { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;

        /// <summary>Roles assigned to this user</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Reserved for future JWT token.
        /// Populate this when you add Microsoft.AspNetCore.Authentication.JwtBearer.
        /// </summary>
        public string? Token     { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
    }

    /// <summary>Standard envelope for all API error responses</summary>
    public class ApiErrorDto
    {
        public string Message { get; set; } = string.Empty;
        public string? Detail { get; set; }
    }
}
