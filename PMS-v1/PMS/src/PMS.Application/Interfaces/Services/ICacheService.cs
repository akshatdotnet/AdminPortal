namespace PMS.Application.Interfaces.Services;

/// <summary>
/// Abstraction over IMemoryCache — keeps Application layer
/// free of Microsoft.Extensions.Caching dependencies.
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiry = null,
        TimeSpan? slidingExpiry = null);

    void Remove(string key);
    void RemoveByPrefix(string prefix);
}