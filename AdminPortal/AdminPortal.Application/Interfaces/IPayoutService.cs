using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IPayoutService
{
    Task<Result<PayoutSummaryDto>> GetPayoutSummaryAsync();
    Task<Result<PayoutDto>> RequestPayoutAsync(decimal amount);
}
