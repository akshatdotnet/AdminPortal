using Common.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;

namespace Product.Infrastructure.Persistence;

public sealed class ProductDbContext(
    DbContextOptions<ProductDbContext> options,
    IMediator mediator) : DbContext(options)
{
    public DbSet<Product.Domain.Entities.Product> Products   => Set<Product.Domain.Entities.Product>();
    public DbSet<Category>     Categories  => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
        base.OnModelCreating(mb);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();
        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());
        var result = await base.SaveChangesAsync(ct);
        foreach (var ev in events) await mediator.Publish(ev, ct);
        return result;
    }
}

public sealed class ProductConfig : IEntityTypeConfiguration<Product.Domain.Entities.Product>
{
    public void Configure(EntityTypeBuilder<Product.Domain.Entities.Product> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        b.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        b.HasIndex(p => p.Sku).IsUnique();
        b.Property(p => p.Price).HasColumnType("decimal(18,2)");
        b.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
        b.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        b.HasQueryFilter(p => !p.IsDeleted);
        b.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
        b.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Ignore(p => p.DomainEvents);
    }
}

public sealed class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).IsRequired().HasMaxLength(100);
        b.Property(c => c.Slug).IsRequired().HasMaxLength(120);
        b.HasIndex(c => c.Slug).IsUnique();
        b.Ignore(c => c.DomainEvents);
    }
}

public sealed class ProductImageConfig : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.Url).IsRequired().HasMaxLength(1000);
        b.Ignore(i => i.DomainEvents);
    }
}
