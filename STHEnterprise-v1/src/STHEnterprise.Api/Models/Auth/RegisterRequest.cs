using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required]
    public string FullName { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; }

    // Optional: Admin can assign
    public string Role { get; set; } = "User";
}
