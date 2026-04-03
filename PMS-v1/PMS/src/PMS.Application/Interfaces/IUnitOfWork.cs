using PMS.Application.Interfaces.Repositories;

namespace PMS.Application.Interfaces;

/// <summary>
/// Unit of Work — coordinates multiple repository operations in one transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    ITaskRepository Tasks { get; }
    ITimeLogRepository TimeLogs { get; }
    IUserRepository Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}