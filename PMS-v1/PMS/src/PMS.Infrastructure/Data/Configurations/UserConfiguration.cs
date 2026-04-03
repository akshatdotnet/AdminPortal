using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Constants;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(DomainConstants.User.NameMaxLength);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(DomainConstants.User.NameMaxLength);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(DomainConstants.User.EmailMaxLength);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        // Audit columns
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
    }
}