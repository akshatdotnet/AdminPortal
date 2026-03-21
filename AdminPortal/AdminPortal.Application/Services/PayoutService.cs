using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class PayoutService : IPayoutService
{
    private readonly IPayoutRepository _payoutRepository;

    public PayoutService(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result<PayoutSummaryDto>> GetPayoutSummaryAsync()
    {
        var balance = await _payoutRepository.GetAvailableBalanceAsync();
        var payouts = await _payoutRepository.GetAllAsync();
        var pending = payouts.Where(p => p.Status == PayoutStatus.Pending).Sum(p => p.Amount);
        var total = payouts.Where(p => p.Status == PayoutStatus.Completed).Sum(p => p.Amount);

        var dto = new PayoutSummaryDto
        {
            AvailableBalance = FormatCurrency(balance),
            TotalEarned = FormatCurrency(total),
            PendingAmount = FormatCurrency(pending),
            RecentPayouts = payouts.OrderByDescending(p => p.RequestedAt).Take(10)
                .Select(p => new PayoutDto
                {
                    Id = p.Id,
                    Amount = FormatCurrency(p.Amount),
                    BankAccount = p.BankAccount,
                    Status = p.Status.ToString(),
                    StatusClass = p.Status switch
                    {
                        PayoutStatus.Completed => "success",
                        PayoutStatus.Processing => "warning",
                        PayoutStatus.Pending => "secondary",
                        PayoutStatus.Failed => "danger",
                        _ => "secondary"
                    },
                    RequestedAt = p.RequestedAt,
                    ProcessedAt = p.ProcessedAt,
                    TransactionRef = p.TransactionRef
                }).ToList()
        };

        return Result<PayoutSummaryDto>.Success(dto);
    }

    public async Task<Result<PayoutDto>> RequestPayoutAsync(decimal amount)
    {
        var balance = await _payoutRepository.GetAvailableBalanceAsync();
        if (amount > balance)
            return Result<PayoutDto>.Failure("Insufficient balance.");

        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            BankAccount = "****1234",
            Status = PayoutStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            TransactionRef = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        var created = await _payoutRepository.AddAsync(payout);
        return Result<PayoutDto>.Success(new PayoutDto
        {
            Id = created.Id,
            Amount = FormatCurrency(created.Amount),
            Status = created.Status.ToString(),
            StatusClass = "secondary",
            RequestedAt = created.RequestedAt
        });
    }

    private static string FormatCurrency(decimal amount) =>
        $"\u20B9{amount:N2}";
}
