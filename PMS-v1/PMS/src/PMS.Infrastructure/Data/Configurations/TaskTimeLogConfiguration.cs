using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Constants;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations;

public class TaskTimeLogConfiguration : IEntityTypeConfiguration<TaskTimeLog>
{
    public void Configure(EntityTypeBuilder<TaskTimeLog> builder)
    {
        builder.ToTable("TaskTimeLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.StartTime).IsRequired();

        builder.Property(l => l.Notes)
            .HasMaxLength(DomainConstants.TimeLog.NotesMaxLength);

        // Audit columns
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.IsDeleted).HasDefaultValue(false);

        // Indexes
        builder.HasIndex(l => l.TaskId);
        builder.HasIndex(l => new { l.TaskId, l.EndTime });

        // Ignore computed properties
        builder.Ignore(l => l.TotalHours);
        builder.Ignore(l => l.FormattedDuration);
        builder.Ignore(l => l.IsRunning);
    }
}