using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Domain.Entities;

namespace AdminPortal.Application.Interfaces;

public interface ICreditService
{
    Task<Result<CreditSummaryDto>> GetSummaryAsync(TransactionType? filterType = null, string? dateRange = null);
}
