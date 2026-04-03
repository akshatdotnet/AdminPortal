namespace PMS.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides primary key, audit fields, and soft-delete support.
/// </summary>
public abstract class BaseEntity : IAuditableEntity
{
    public int Id { get; set; }

    // ── Audit fields ──────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // ── Soft delete ───────────────────────────────────────────────────────────
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}