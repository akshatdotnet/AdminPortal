using PMS.Domain.Common;
using PMS.Domain.Constants;

namespace PMS.Domain.Entities;

/// <summary>
/// Represents a system user who can be assigned to tasks.
/// Authentication is out of scope for this module — this is the domain user model.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = default!;

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();

    // ── Computed ──────────────────────────────────────────────────────────────
    public string FullName => $"{FirstName} {LastName}".Trim();
}