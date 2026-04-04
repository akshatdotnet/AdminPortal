using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using ProductEntity   = Product.Domain.Entities.Product;
using CategoryEntity  = Product.Domain.Entities.Category;
using ProductImageEntity = Product.Domain.Entities.ProductImage;

namespace Product.Application.Commands;

// ═══════════════════════════════════════════════════════════
// CREATE CATEGORY
// ═══════════════════════════════════════════════════════════
public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentCategoryId) : IRequest<Result<Guid>>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository repo,
    IUnitOfWorkProduct uow)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCategoryCommand cmd, CancellationToken ct)
    {
        var slug = cmd.Name.Trim().ToLowerInvariant().Replace(" ", "-");
        if (await repo.SlugExistsAsync(slug, ct))
            return Result.Failure<Guid>(
                Error.Conflict("Category", $"Category '{cmd.Name}' already exists."));

        var cat = Category.Create(cmd.Name, cmd.Description, cmd.ParentCategoryId);
        repo.Add(cat);
        await uow.SaveChangesAsync(ct);
        return Result.Success(cat.Id);
    }
}

// ═══════════════════════════════════════════════════════════
// CREATE PRODUCT
// ═══════════════════════════════════════════════════════════
public sealed record CreateProductCommand(
    string  Name,
    string  Description,
    string  Sku,
    decimal Price,
    string  Currency,
    int     StockQuantity,
    Guid    CategoryId,
    string? Brand) : IRequest<Result<Guid>>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator(
        IProductRepository  productRepo,
        ICategoryRepository catRepo)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Sku)
            .NotEmpty().MaximumLength(50)
            .MustAsync(async (sku, ct) =>
                !await productRepo.SkuExistsAsync(sku, ct))
            .WithMessage(x => $"SKU '{x.Sku}' already exists.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3)
            .WithMessage("Currency must be a 3-letter ISO code (e.g. USD).");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .MustAsync(async (id, ct) =>
                await catRepo.GetByIdAsync(id, ct) != null)
            .WithMessage("Category not found.");
        RuleFor(x => x.Brand).MaximumLength(100).When(x => x.Brand != null);
    }
}

public sealed class CreateProductCommandHandler(
    IProductRepository repo,
    IUnitOfWorkProduct uow)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateProductCommand cmd, CancellationToken ct)
    {
        var product = Product.Domain.Entities.Product.Create(
            cmd.Name, cmd.Description, cmd.Sku,
            cmd.Price, cmd.Currency, cmd.StockQuantity,
            cmd.CategoryId, cmd.Brand);

        repo.Add(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success(product.Id);
    }
}

// ═══════════════════════════════════════════════════════════
// UPDATE PRODUCT
// ═══════════════════════════════════════════════════════════
public sealed record UpdateProductCommand(
    Guid    ProductId,
    string  Name,
    string  Description,
    decimal Price,
    decimal? SalePrice,
    string?  Brand,
    Guid    CategoryId) : IRequest<Result>;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator(ICategoryRepository catRepo)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.SalePrice)
            .GreaterThan(0)
            .LessThan(x => x.Price)
            .WithMessage("Sale price must be between 0 and the regular price.")
            .When(x => x.SalePrice.HasValue);
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .MustAsync(async (id, ct) =>
                await catRepo.GetByIdAsync(id, ct) != null)
            .WithMessage("Category not found.");
        RuleFor(x => x.Brand).MaximumLength(100).When(x => x.Brand != null);
    }
}

public sealed class UpdateProductCommandHandler(
    IProductRepository repo,
    IUnitOfWorkProduct uow)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(
        UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("ProductEntity", cmd.ProductId));

        try
        {
            product.UpdateDetails(
                cmd.Name, cmd.Description, cmd.Price,
                cmd.SalePrice, cmd.Brand, cmd.CategoryId, "api");
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.BusinessRule("ProductUpdate", ex.Message));
        }

        repo.Update(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════════════════════════════════════
// DELETE PRODUCT (soft delete)
// ═══════════════════════════════════════════════════════════
public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result>;

public sealed class DeleteProductCommandHandler(
    IProductRepository repo,
    IUnitOfWorkProduct uow)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(
        DeleteProductCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("ProductEntity", cmd.ProductId));

        product.Delete("api");
        repo.Update(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════════════════════════════════════
// ADJUST STOCK
// ═══════════════════════════════════════════════════════════
public sealed record AdjustStockCommand(
    Guid   ProductId,
    int    Delta,
    string Reason) : IRequest<Result>;

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.Delta).NotEqual(0)
            .WithMessage("Delta must be non-zero (use positive to add, negative to reduce).");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}

public sealed class AdjustStockCommandHandler(
    IProductRepository repo,
    IUnitOfWorkProduct uow)
    : IRequestHandler<AdjustStockCommand, Result>
{
    public async Task<Result> Handle(
        AdjustStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("ProductEntity", cmd.ProductId));

        try
        {
            product.AdjustStock(cmd.Delta, "api");
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("Stock", ex.Message));
        }

        repo.Update(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════════════════════════════════════
// ACTIVATE / DEACTIVATE
// ═══════════════════════════════════════════════════════════
public sealed record ChangeProductStatusCommand(
    Guid   ProductId,
    string Action) : IRequest<Result>;

public sealed class ChangeProductStatusCommandHandler(
    IProductRepository repo,
    IUnitOfWorkProduct uow)
    : IRequestHandler<ChangeProductStatusCommand, Result>
{
    public async Task<Result> Handle(
        ChangeProductStatusCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("ProductEntity", cmd.ProductId));

        switch (cmd.Action.ToLower())
        {
            case "activate":   product.Activate("api");   break;
            case "deactivate": product.Deactivate("api"); break;
            default:
                return Result.Failure(
                    Error.BusinessRule("ProductStatus",
                        "Action must be 'activate' or 'deactivate'."));
        }

        repo.Update(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
