namespace PMS.Domain.Enums;

/// <summary>
/// Using fully qualified name to avoid collision with System.Threading.Tasks
/// </summary>
public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    OnHold = 3,
    Cancelled = 4
}