public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Guest User";
    public string FullName { get; set; } = "Guest User";
    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? MpinHash { get; set; }
    public string Type { get; set; } = "S"; // B2B | B2C | Student 
    public string Role { get; set; } = "User"; // Admin | Manager | User
    public string? Permission { get; set; }
    public string? ResetToken { get; set; }
    public string? ResetTokenExpiry { get; set; }

   
}
