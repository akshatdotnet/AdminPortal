using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;

namespace Product.Application.Commands;

// ── CREATE PRODUCT ────────────────────────────────────────────
public sealed record CreateProductCommand(
    string Name, string Description, string Sku,
    decimal Price, string Currency, int StockQuantity,
    Guid CategoryId, string? Brand) : IRequest<Result<Guid>>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator(IProductRepository repo)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50)
            .MustAsync(async (sku, ct) => !await repo.SkuExistsAsync(sku, ct))
            .WithMessage("SKU already exists.");
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public sealed class CreateProductCommandHandler(
    IProductRepository repo, IUnitOfWorkProduct uow)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = Product.Domain.Entities.Product.Create(
            cmd.Name, cmd.Description, cmd.Sku, cmd.Price,
            cmd.Currency, cmd.StockQuantity, cmd.CategoryId, cmd.Brand);
        repo.Add(product);
        await uow.SaveChangesAsync(ct);
        return Result.Success(product.Id);
    }
}

// ── UPDATE PRODUCT ────────────────────────────────────────────
public sealed record UpdateProductCommand(
    Guid ProductId, string Name, string Description,
    decimal Price, string Currency, decimal? SalePrice,
    string? Brand) : IRequest<Result>;

public sealed class UpdateProductCommandHandler(
    IProductRepository repo, IUnitOfWorkProduct uow)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (p is null) return Result.Failure(Error.NotFound("Product", cmd.ProductId));
        p.UpdateDetails(cmd.Name, cmd.Description, cmd.Price, cmd.SalePrice, cmd.Brand);
        repo.Update(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── ADJUST STOCK ──────────────────────────────────────────────
public sealed record AdjustStockCommand(
    Guid ProductId, int Delta, string Reason) : IRequest<Result>;

public sealed class AdjustStockCommandHandler(
    IProductRepository repo, IUnitOfWorkProduct uow)
    : IRequestHandler<AdjustStockCommand, Result>
{
    public async Task<Result> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (p is null) return Result.Failure(Error.NotFound("Product", cmd.ProductId));
        try
        {
            p.AdjustStock(cmd.Delta);
            repo.Update(p);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("Stock", ex.Message));
        }
    }
}

// ── CREATE CATEGORY ───────────────────────────────────────────
public sealed record CreateCategoryCommand(
    string Name, string? Description, Guid? ParentCategoryId) : IRequest<Result<Guid>>;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository repo, IUnitOfWorkProduct uow)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        var cat = Category.Create(cmd.Name, cmd.Description, cmd.ParentCategoryId);
        repo.Add(cat);
        await uow.SaveChangesAsync(ct);
        return Result.Success(cat.Id);
    }
}
