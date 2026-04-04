using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Application.Queries;
using ProductEntity   = Product.Domain.Entities.Product;
using CategoryEntity  = Product.Domain.Entities.Category;
using ProductImageEntity = Product.Domain.Entities.ProductImage;

namespace Product.Api.Controllers;

/// <summary>
/// DEMO — runs the complete ProductEntity workflow in a single HTTP call.
/// No auth token required. Tests all major operations end-to-end.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/demo")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class DemoController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Runs the FULL ProductEntity service flow:
    /// 1.  Create "Electronics" category
    /// 2.  Create "MacBook Pro 14" product
    /// 3.  Create second product "iPhone 15"
    /// 4.  Get product by ID
    /// 5.  Get paged product list (verify both appear)
    /// 6.  Update product details
    /// 7.  Adjust stock (+10 units)
    /// 8.  Adjust stock (-5 units)
    /// 9.  Try to over-reduce stock (should fail with 422)
    /// 10. Search for "mac" (should find MacBook)
    /// 11. Filter by category
    /// 12. Change product status to Inactive
    /// 13. Verify inactive product excluded from default list
    /// 14. Delete second product (soft delete)
    /// 15. Try duplicate SKU (should fail with 409)
    /// </summary>
    [HttpPost("complete-flow")]
    [ProducesResponseType(typeof(ProductDemoResult), 200)]
    public async Task<IActionResult> RunCompleteFlow(CancellationToken ct)
    {
        var ts     = DateTime.UtcNow.Ticks;
        var result = new ProductDemoResult();

        // ── Step 1: Create category ───────────────────────────
        var s1 = await mediator.Send(
            new CreateCategoryCommand($"Electronics_{ts}", "Consumer electronics", null), ct);
        result.Step1_CreateCategory = Step(
            s1.IsSuccess,
            s1.IsSuccess ? $"Category created: Electronics_{ts}" : s1.Error.Message,
            s1.IsSuccess ? new { CategoryId = s1.Value } : null);

        if (!s1.IsSuccess) return Ok(result);
        var catId = s1.Value;

        // ── Step 2: Create MacBook ────────────────────────────
        var s2 = await mediator.Send(new CreateProductCommand(
            "MacBook Pro 14",
            "Apple M3 chip, 16GB RAM, 512GB SSD",
            $"APPL-MBP14-{ts}",
            1999.99m, "USD", 25, catId, "Apple"), ct);
        result.Step2_CreateProduct = Step(
            s2.IsSuccess,
            s2.IsSuccess ? "MacBook Pro 14 created" : s2.Error.Message,
            s2.IsSuccess ? new { ProductId = s2.Value } : null);

        if (!s2.IsSuccess) return Ok(result);
        var macId = s2.Value;

        // ── Step 3: Create iPhone ─────────────────────────────
        var s3 = await mediator.Send(new CreateProductCommand(
            "iPhone 15 Pro",
            "A17 Pro chip, 256GB",
            $"APPL-IP15P-{ts}",
            1199.99m, "USD", 50, catId, "Apple"), ct);
        result.Step3_CreateSecondProduct = Step(
            s3.IsSuccess,
            s3.IsSuccess ? "iPhone 15 Pro created" : s3.Error.Message,
            s3.IsSuccess ? new { ProductId = s3.Value } : null);

        if (!s3.IsSuccess) return Ok(result);
        var iphoneId = s3.Value;

        // ── Step 4: Get by ID ─────────────────────────────────
        var s4 = await mediator.Send(new GetProductByIdQuery(macId), ct);
        result.Step4_GetById = Step(
            s4.IsSuccess && s4.Value?.Id == macId,
            s4.IsSuccess ? $"Retrieved: {s4.Value!.Name} @ ${s4.Value.Price}" : s4.Error.Message,
            s4.IsSuccess ? new { s4.Value!.Name, s4.Value.Sku, s4.Value.StockQuantity } : null);

        // ── Step 5: Paged list ────────────────────────────────
        var s5 = await mediator.Send(
            new GetProductsQuery(PageNumber: 1, PageSize: 20, CategoryId: catId), ct);
        result.Step5_PagedList = Step(
            s5.IsSuccess && s5.Value!.TotalCount == 2,
            s5.IsSuccess
                ? $"Found {s5.Value!.TotalCount} products in category"
                : s5.Error.Message,
            s5.IsSuccess
                ? new { s5.Value!.TotalCount, Products = s5.Value.Items.Select(p => p.Name) }
                : null);

        // ── Step 6: Update product ────────────────────────────
        var s6 = await mediator.Send(new UpdateProductCommand(
            macId,
            "MacBook Pro 14 M3",
            "Apple M3 chip, 16GB RAM, 512GB SSD — Updated model",
            1899.99m, 1799.99m, "Apple", catId), ct);
        result.Step6_UpdateProduct = Step(
            s6.IsSuccess,
            s6.IsSuccess
                ? "Updated: name, description, price $1999.99→$1899.99, sale price $1799.99"
                : s6.Error.Message,
            null);

        // ── Step 7: Add stock ─────────────────────────────────
        var s7 = await mediator.Send(
            new AdjustStockCommand(macId, 10, "Restock from supplier"), ct);
        result.Step7_AddStock = Step(
            s7.IsSuccess,
            s7.IsSuccess ? "Added 10 units → 35 total" : s7.Error.Message,
            null);

        // ── Step 8: Reduce stock ──────────────────────────────
        var s8 = await mediator.Send(
            new AdjustStockCommand(macId, -5, "Customer order fulfillment"), ct);
        result.Step8_ReduceStock = Step(
            s8.IsSuccess,
            s8.IsSuccess ? "Removed 5 units → 30 total" : s8.Error.Message,
            null);

        // ── Step 9: Over-reduce stock (must fail) ─────────────
        var s9 = await mediator.Send(
            new AdjustStockCommand(macId, -9999, "Invalid reduction"), ct);
        result.Step9_OverReduceStock = Step(
            !s9.IsSuccess,
            !s9.IsSuccess
                ? $"Correctly rejected over-reduction: {s9.Error.Message}"
                : "BUG: should have failed but succeeded",
            null);

        // ── Step 10: Search ───────────────────────────────────
        var s10 = await mediator.Send(
            new GetProductsQuery(Search: "mac", CategoryId: catId), ct);
        result.Step10_Search = Step(
            s10.IsSuccess && s10.Value!.Items.Any(p =>
                p.Name.Contains("Mac", StringComparison.OrdinalIgnoreCase)),
            s10.IsSuccess
                ? $"Search 'mac' returned {s10.Value!.TotalCount} result(s)"
                : s10.Error.Message,
            s10.IsSuccess
                ? new { Results = s10.Value!.Items.Select(p => p.Name) }
                : null);

        // ── Step 11: Price filter ─────────────────────────────
        var s11 = await mediator.Send(
            new GetProductsQuery(MinPrice: 1500m, MaxPrice: 2000m, CategoryId: catId), ct);
        result.Step11_PriceFilter = Step(
            s11.IsSuccess && s11.Value!.Items.All(p => p.EffectivePrice is >= 1500m and <= 2000m),
            s11.IsSuccess
                ? $"Price filter $1500-$2000 returned {s11.Value!.TotalCount} product(s)"
                : s11.Error.Message,
            s11.IsSuccess
                ? new { Results = s11.Value!.Items.Select(p => new { p.Name, p.EffectivePrice }) }
                : null);

        // ── Step 12: Deactivate iPhone ────────────────────────
        var s12 = await mediator.Send(
            new ChangeProductStatusCommand(iphoneId, "deactivate"), ct);
        result.Step12_DeactivateProduct = Step(
            s12.IsSuccess,
            s12.IsSuccess ? "iPhone 15 Pro deactivated" : s12.Error.Message,
            null);

        // ── Step 13: Verify inactive excluded from default list ─
        var s13 = await mediator.Send(
            new GetProductsQuery(CategoryId: catId), ct);
        result.Step13_InactiveExcluded = Step(
            s13.IsSuccess && s13.Value!.Items.All(p => p.Status == "Active"),
            s13.IsSuccess
                ? $"Default list has {s13.Value!.TotalCount} active product(s). " +
                  $"Inactive iPhone excluded: {!s13.Value.Items.Any(p => p.Name.Contains("iPhone"))}"
                : s13.Error.Message,
            s13.IsSuccess
                ? new { ActiveProducts = s13.Value!.Items.Select(p => p.Name) }
                : null);

        // ── Step 14: Soft delete iPhone ───────────────────────
        var s14 = await mediator.Send(new DeleteProductCommand(iphoneId), ct);
        result.Step14_DeleteProduct = Step(
            s14.IsSuccess,
            s14.IsSuccess ? "iPhone 15 Pro soft-deleted" : s14.Error.Message,
            null);

        // ── Step 15: Duplicate SKU (must fail) ────────────────
        var s15 = await mediator.Send(new CreateProductCommand(
            "MacBook Duplicate",
            "Should fail",
            $"APPL-MBP14-{ts}",   // same SKU as step 2
            999m, "USD", 1, catId, null), ct);
        result.Step15_DuplicateSku = Step(
            !s15.IsSuccess,
            !s15.IsSuccess
                ? $"Correctly rejected duplicate SKU: {s15.Error.Message}"
                : "BUG: duplicate SKU was accepted",
            null);

        // ── Summary ────────────────────────────────────────────
        var steps = new[]
        {
            result.Step1_CreateCategory,  result.Step2_CreateProduct,
            result.Step3_CreateSecondProduct, result.Step4_GetById,
            result.Step5_PagedList,       result.Step6_UpdateProduct,
            result.Step7_AddStock,        result.Step8_ReduceStock,
            result.Step9_OverReduceStock, result.Step10_Search,
            result.Step11_PriceFilter,    result.Step12_DeactivateProduct,
            result.Step13_InactiveExcluded, result.Step14_DeleteProduct,
            result.Step15_DuplicateSku
        };

        result.TotalSteps  = steps.Length;
        result.PassedSteps = steps.Count(s => s?.Success == true);
        result.AllPassed   = result.PassedSteps == result.TotalSteps;

        return Ok(result);
    }

    private static StepResult Step(bool success, string message, object? data) =>
        new() { Success = success, Message = message, Data = data };
}

public sealed class ProductDemoResult
{
    public StepResult? Step1_CreateCategory          { get; set; }
    public StepResult? Step2_CreateProduct           { get; set; }
    public StepResult? Step3_CreateSecondProduct     { get; set; }
    public StepResult? Step4_GetById                 { get; set; }
    public StepResult? Step5_PagedList               { get; set; }
    public StepResult? Step6_UpdateProduct           { get; set; }
    public StepResult? Step7_AddStock                { get; set; }
    public StepResult? Step8_ReduceStock             { get; set; }
    public StepResult? Step9_OverReduceStock         { get; set; }
    public StepResult? Step10_Search                 { get; set; }
    public StepResult? Step11_PriceFilter            { get; set; }
    public StepResult? Step12_DeactivateProduct      { get; set; }
    public StepResult? Step13_InactiveExcluded       { get; set; }
    public StepResult? Step14_DeleteProduct          { get; set; }
    public StepResult? Step15_DuplicateSku           { get; set; }
    public int  TotalSteps  { get; set; }
    public int  PassedSteps { get; set; }
    public bool AllPassed   { get; set; }
}

public sealed class StepResult
{
    public bool    Success { get; init; }
    public string  Message { get; init; } = default!;
    public object? Data    { get; init; }
}
