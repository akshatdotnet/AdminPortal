using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasConversion<string>();
        builder.Property(p => p.Method).HasConversion<string>();
        builder.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.HasIndex(p => p.IdempotencyKey).IsUnique();
        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
        builder.Ignore(p => p.DomainEvents);
    }
}
