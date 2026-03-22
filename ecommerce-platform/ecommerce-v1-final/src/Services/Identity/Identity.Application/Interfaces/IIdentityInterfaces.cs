using Common.Domain.Primitives;
using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    void Add(ApplicationUser user);
    void Update(ApplicationUser user);
}

public interface IUnitOfWorkIdentity
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    AuthResponseDto GenerateTokens(ApplicationUser user);
    Guid? GetUserIdFromExpiredToken(string token);
}
