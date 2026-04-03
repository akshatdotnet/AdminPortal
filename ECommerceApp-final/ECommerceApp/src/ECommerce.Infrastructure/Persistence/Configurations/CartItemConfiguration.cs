using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.CartId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPriceAmount).HasColumnName("UnitPrice").HasPrecision(18, 2);
        builder.Property(i => i.UnitPriceCurrency).HasColumnName("Currency").HasMaxLength(3);
        builder.Property(i => i.Quantity).IsRequired();

        // CartItem owns its CartId FK. Cascade delete is handled via DB-level FK constraint.
        builder.HasOne<Cart>().WithMany().HasForeignKey(i => i.CartId)
               .OnDelete(DeleteBehavior.Cascade);

        // Computed projections — not stored in DB
        builder.Ignore(i => i.UnitPrice);
        builder.Ignore(i => i.Subtotal);
        builder.Ignore(i => i.DomainEvents);
    }
}
