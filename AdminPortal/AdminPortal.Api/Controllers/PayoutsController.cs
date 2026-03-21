using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PayoutsController : ControllerBase
{
    private readonly MockDataStore _store;
    private int _nextId => _store.Payouts.Any() ? _store.Payouts.Max(p => p.Id) + 1 : 1;

    public PayoutsController(MockDataStore store) => _store = store;

    /// <summary>Get payout overview: balance + history.</summary>
    [HttpGet]
    public ActionResult<ApiResponse<PayoutsOverviewDto>> GetOverview()
    {
        var overview = new PayoutsOverviewDto
        {
            AvailableBalance = _store.AvailableBalance,
            TotalPaidOut = _store.Payouts.Where(p => p.Status == PayoutStatus.Processed).Sum(p => p.Amount),
            PendingAmount = _store.Payouts.Where(p => p.Status == PayoutStatus.Pending).Sum(p => p.Amount),
            History = _store.Payouts.OrderByDescending(p => p.RequestedAt).Select(Map).ToList()
        };
        return Ok(new ApiResponse<PayoutsOverviewDto> { Data = overview });
    }

    /// <summary>Get a single payout record.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<PayoutDto>> GetById(int id)
    {
        var p = _store.Payouts.FirstOrDefault(x => x.Id == id);
        if (p is null) return NotFound(new ApiResponse<PayoutDto> { Success = false, Message = "Payout not found." });
        return Ok(new ApiResponse<PayoutDto> { Data = Map(p) });
    }

    /// <summary>Request a new payout.</summary>
    [HttpPost("request")]
    public ActionResult<ApiResponse<PayoutDto>> Request([FromBody] RequestPayoutRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new ApiResponse<PayoutDto> { Success = false, Message = "Amount must be positive." });

        if (request.Amount > _store.AvailableBalance)
            return BadRequest(new ApiResponse<PayoutDto> { Success = false, Message = "Insufficient balance." });

        var payout = new Payout
        {
            Id = _nextId,
            Amount = request.Amount,
            Status = PayoutStatus.Pending,
            RequestedAt = DateTime.Now,
            BankAccount = "XXXX1234",
            Reference = $"PAY-{DateTime.Now:yyyyMMddHHmm}"
        };

        _store.Payouts.Add(payout);
        return CreatedAtAction(nameof(GetById), new { id = payout.Id },
            new ApiResponse<PayoutDto> { Data = Map(payout), Message = "Payout request submitted." });
    }

    private static PayoutDto Map(Payout p) => new()
    {
        Id = p.Id, Amount = p.Amount, Status = p.Status.ToString(),
        RequestedAt = p.RequestedAt, ProcessedAt = p.ProcessedAt,
        BankAccount = p.BankAccount, Reference = p.Reference
    };
}

public record RequestPayoutRequest(decimal Amount);
