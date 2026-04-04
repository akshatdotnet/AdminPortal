using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CustomerId).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // Do NOT configure a HasMany navigation here.
        // Cart.Items is a domain collection populated via explicit Include or separate queries.
        // Configuring HasMany without always using Include causes EF to treat the empty
        // collection as "no children exist" and attempt to orphan/delete existing CartItem rows.
        // CartItem → Cart relationship is configured on the CartItem side only.
        builder.Ignore(c => c.Items);     // EF does NOT manage this collection
        builder.Ignore(c => c.TotalAmount);
        builder.Ignore(c => c.TotalItems);
        builder.Ignore(c => c.DomainEvents);
    }
}
