using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Users.FindAsync(new object[] { id }, ct);

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await context.Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public void Add(ApplicationUser user)    => context.Users.Add(user);
    public void Update(ApplicationUser user) => context.Users.Update(user);
}

public sealed class UnitOfWorkIdentity(IdentityDbContext context) : IUnitOfWorkIdentity
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
