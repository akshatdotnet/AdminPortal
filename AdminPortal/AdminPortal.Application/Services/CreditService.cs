using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class CreditService : ICreditService
{
    private readonly ICreditRepository _creditRepository;

    public CreditService(ICreditRepository creditRepository)
    {
        _creditRepository = creditRepository;
    }

    public async Task<Result<CreditSummaryDto>> GetSummaryAsync(
        TransactionType? filterType = null, string? dateRange = null)
    {
        var balance = await _creditRepository.GetCurrentBalanceAsync();

        IEnumerable<CreditTransaction> transactions;
        if (filterType.HasValue)
            transactions = await _creditRepository.GetByTypeAsync(filterType.Value);
        else if (!string.IsNullOrEmpty(dateRange))
        {
            var (from, to) = ParseDateRange(dateRange);
            transactions = await _creditRepository.GetByDateRangeAsync(from, to);
        }
        else
            transactions = await _creditRepository.GetAllAsync();

        var dto = new CreditSummaryDto
        {
            CurrentBalance = balance,
            Transactions = transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new CreditTransactionDto
                {
                    ReferenceId = t.ReferenceId,
                    Details = t.Details,
                    OrderId = t.OrderId,
                    Credits = t.Credits,
                    Type = t.Type.ToString(),
                    IsDebit = t.Type == TransactionType.Debit,
                    Balance = t.Balance,
                    CreatedAt = t.CreatedAt
                }).ToList()
        };

        return Result<CreditSummaryDto>.Success(dto);
    }

    private static (DateTime from, DateTime to) ParseDateRange(string range) => range switch
    {
        "7d"  => (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
        "30d" => (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
        "90d" => (DateTime.UtcNow.AddDays(-90), DateTime.UtcNow),
        _     => (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow)
    };
}
