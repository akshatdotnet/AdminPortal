using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;
using System.Reflection;

namespace PMS.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTask> Tasks { get; set; }
    public DbSet<TaskTimeLog> TimeLogs { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // ── Global query filters — soft delete ────────────────────────────────
        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => !p.IsDeleted);

        modelBuilder.Entity<ProjectTask>()
            .HasQueryFilter(t => !t.IsDeleted);

        modelBuilder.Entity<TaskTimeLog>()
            .HasQueryFilter(l => !l.IsDeleted);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);
    }

    // ── Auto-set audit fields on SaveChanges ──────────────────────────────────
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity &&
                        e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
                // Prevent accidental overwrite of CreatedAt
                entry.Property(nameof(Domain.Common.BaseEntity.CreatedAt)).IsModified = false;
            }
        }
    }
}