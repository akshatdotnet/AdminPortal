using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.OwnsOne(c => c.Email, e =>
        {
            e.Property(x => x.Value).HasColumnName("Email").IsRequired().HasMaxLength(200);
        });
        builder.Ignore(c => c.FullName);
        builder.Ignore(c => c.DomainEvents);
    }
}
