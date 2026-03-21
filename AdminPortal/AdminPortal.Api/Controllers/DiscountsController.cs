using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DiscountsController : ControllerBase
{
    private readonly MockDataStore _store;
    private int _nextId => _store.Discounts.Any() ? _store.Discounts.Max(d => d.Id) + 1 : 1;

    public DiscountsController(MockDataStore store) => _store = store;

    /// <summary>Get all discounts.</summary>
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<DiscountDto>>> GetAll([FromQuery] bool? isActive)
    {
        var query = _store.Discounts.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        return Ok(new ApiResponse<IEnumerable<DiscountDto>> { Data = query.Select(Map) });
    }

    /// <summary>Get a discount by ID.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<DiscountDto>> GetById(int id)
    {
        var d = _store.Discounts.FirstOrDefault(x => x.Id == id);
        if (d is null) return NotFound(new ApiResponse<DiscountDto> { Success = false, Message = "Discount not found." });
        return Ok(new ApiResponse<DiscountDto> { Data = Map(d) });
    }

    /// <summary>Create a new discount code.</summary>
    [HttpPost]
    public ActionResult<ApiResponse<DiscountDto>> Create([FromBody] CreateDiscountRequest request)
    {
        if (_store.Discounts.Any(d => d.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new ApiResponse<DiscountDto> { Success = false, Message = "Discount code already exists." });

        var discount = new Discount
        {
            Id = _nextId,
            Code = request.Code.ToUpper(),
            Description = request.Description,
            Percentage = request.Percentage,
            ExpiryDate = request.ExpiryDate,
            UsageLimit = request.UsageLimit,
            IsActive = true,
            UsageCount = 0
        };

        _store.Discounts.Add(discount);
        return CreatedAtAction(nameof(GetById), new { id = discount.Id },
            new ApiResponse<DiscountDto> { Data = Map(discount), Message = "Discount created." });
    }

    /// <summary>Toggle active/inactive.</summary>
    [HttpPatch("{id:int}/toggle")]
    public ActionResult<ApiResponse<DiscountDto>> Toggle(int id)
    {
        var d = _store.Discounts.FirstOrDefault(x => x.Id == id);
        if (d is null) return NotFound(new ApiResponse<DiscountDto> { Success = false, Message = "Discount not found." });

        d.IsActive = !d.IsActive;
        return Ok(new ApiResponse<DiscountDto> { Data = Map(d), Message = $"Discount is now {(d.IsActive ? "active" : "inactive")}." });
    }

    /// <summary>Delete a discount.</summary>
    [HttpDelete("{id:int}")]
    public ActionResult<ApiResponse<object>> Delete(int id)
    {
        var d = _store.Discounts.FirstOrDefault(x => x.Id == id);
        if (d is null) return NotFound(new ApiResponse<object> { Success = false, Message = "Discount not found." });

        _store.Discounts.Remove(d);
        return Ok(new ApiResponse<object> { Message = "Discount deleted." });
    }

    private static DiscountDto Map(Discount d) => new()
    {
        Id = d.Id, Code = d.Code, Description = d.Description,
        Percentage = d.Percentage, ExpiryDate = d.ExpiryDate,
        IsActive = d.IsActive, UsageCount = d.UsageCount, UsageLimit = d.UsageLimit
    };
}

public record CreateDiscountRequest(
    string Code,
    string Description,
    decimal Percentage,
    DateTime ExpiryDate,
    int UsageLimit);
