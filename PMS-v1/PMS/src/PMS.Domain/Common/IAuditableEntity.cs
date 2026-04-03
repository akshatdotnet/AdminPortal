namespace PMS.Domain.Common;

/// <summary>
/// Marks an entity as auditable — tracks who created/modified it and when.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}