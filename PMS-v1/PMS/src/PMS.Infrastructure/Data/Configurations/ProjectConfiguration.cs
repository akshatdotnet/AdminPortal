using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Constants;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(DomainConstants.Project.NameMaxLength);

        builder.Property(p => p.Description)
            .HasMaxLength(DomainConstants.Project.DescriptionMaxLength);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()   // store enum as string in DB
            .HasMaxLength(20);

        builder.Property(p => p.StartDate).IsRequired();

        // Audit columns
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.IsDeleted);

        // Relationships
        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict); // prevent cascade deletes
    }
}