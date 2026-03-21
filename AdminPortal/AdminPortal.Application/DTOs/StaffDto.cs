namespace AdminPortal.Application.DTOs;

public class StaffDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string Initials => string.Concat(Name.Split(' ').Select(n => n.FirstOrDefault())).ToUpper();
}

public class InviteStaffDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
