using Common.Domain.Entities;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    IMediator mediator) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(mb);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity).ToList();
        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());
        var result = await base.SaveChangesAsync(ct);
        foreach (var ev in events) await mediator.Publish(ev, ct);
        return result;
    }
}

public sealed class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        b.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        b.Property(u => u.PhoneNumber).HasMaxLength(25);
        b.Property(u => u.Role).IsRequired().HasMaxLength(50);
        b.Property(u => u.PasswordHash).IsRequired();
        b.HasQueryFilter(u => !u.IsDeleted);
        b.Property(u => u.Version).IsConcurrencyToken();
        b.Ignore(u => u.DomainEvents);
    }
}
