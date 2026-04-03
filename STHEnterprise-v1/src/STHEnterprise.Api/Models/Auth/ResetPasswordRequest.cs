using System.ComponentModel.DataAnnotations;

public class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; }

    [Required, MinLength(6)]
    public string NewPassword { get; set; }
}
