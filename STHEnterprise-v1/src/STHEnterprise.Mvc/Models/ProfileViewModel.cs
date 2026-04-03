using System.ComponentModel.DataAnnotations;

public class ProfileViewModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Mobile Number")]
    public string Mobile { get; set; }
}
