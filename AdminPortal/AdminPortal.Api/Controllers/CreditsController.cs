using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CreditsController : ControllerBase
{
    private readonly MockDataStore _store;

    public CreditsController(MockDataStore store) => _store = store;

    /// <summary>Get credit overview: totals + history.</summary>
    [HttpGet]
    public ActionResult<ApiResponse<CreditsOverviewDto>> GetOverview()
    {
        var all = _store.Credits;
        var overview = new CreditsOverviewDto
        {
            TotalCredits = all.Sum(c => c.Amount),
            AvailableCredits = all.Where(c => !c.IsUsed).Sum(c => c.Amount),
            UsedCredits = all.Where(c => c.IsUsed).Sum(c => c.Amount),
            History = all.OrderByDescending(c => c.EarnedAt).Select(Map).ToList()
        };
        return Ok(new ApiResponse<CreditsOverviewDto> { Data = overview });
    }

    /// <summary>Get a single credit record.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<CreditDto>> GetById(int id)
    {
        var c = _store.Credits.FirstOrDefault(x => x.Id == id);
        if (c is null) return NotFound(new ApiResponse<CreditDto> { Success = false, Message = "Credit record not found." });
        return Ok(new ApiResponse<CreditDto> { Data = Map(c) });
    }

    private static CreditDto Map(Credit c) => new()
    {
        Id = c.Id, Amount = c.Amount, Source = c.Source,
        Description = c.Description, EarnedAt = c.EarnedAt, IsUsed = c.IsUsed
    };
}
