using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Constants;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations;

public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(DomainConstants.Task.TitleMaxLength);

        builder.Property(t => t.Description)
            .HasMaxLength(DomainConstants.Task.DescriptionMaxLength);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Audit columns
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.IsDeleted).HasDefaultValue(false);

        // Indexes
        builder.HasIndex(t => t.ProjectId);
        builder.HasIndex(t => t.AssignedUserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.IsDeleted);

        // Relationships
        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasMany(t => t.TimeLogs)
            .WithOne(l => l.Task)
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties — not mapped to columns
        builder.Ignore(t => t.TotalHoursLogged);
        builder.Ignore(t => t.HasActiveTimer);
    }
}