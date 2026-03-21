using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AudienceController : ControllerBase
{
    private readonly MockDataStore _store;

    public AudienceController(MockDataStore store) => _store = store;

    /// <summary>Get all customers, with optional tag filter.</summary>
    [HttpGet]
    public ActionResult<PagedResponse<AudienceDto>> GetAll(
        [FromQuery] string? tag,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _store.Audience.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || a.Phone.Contains(search));

        var total = query.Count();
        var items = query
            .OrderByDescending(a => a.TotalSpent)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Map)
            .ToList();

        return Ok(new PagedResponse<AudienceDto>
        {
            Data = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    /// <summary>Get a customer by ID.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<AudienceDto>> GetById(int id)
    {
        var a = _store.Audience.FirstOrDefault(x => x.Id == id);
        if (a is null) return NotFound(new ApiResponse<AudienceDto> { Success = false, Message = "Customer not found." });
        return Ok(new ApiResponse<AudienceDto> { Data = Map(a) });
    }

    /// <summary>Get available tags.</summary>
    [HttpGet("tags")]
    public ActionResult<ApiResponse<IEnumerable<string>>> GetTags()
    {
        var tags = _store.Audience.Select(a => a.Tag).Distinct().OrderBy(t => t);
        return Ok(new ApiResponse<IEnumerable<string>> { Data = tags });
    }

    private static AudienceDto Map(Audience a) => new()
    {
        Id = a.Id, Name = a.Name, Phone = a.Phone, Email = a.Email,
        TotalOrders = a.TotalOrders, TotalSpent = a.TotalSpent,
        LastOrderDate = a.LastOrderDate, Tag = a.Tag
    };
}
