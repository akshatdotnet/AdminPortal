namespace ECommerce.Application.Common.Interfaces;

/// <summary>Unit of Work — commits all repository changes in a single transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
