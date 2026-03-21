using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
    Task<PasswordResetToken> AddAsync(PasswordResetToken token);
    Task InvalidateAllForUserAsync(Guid userId);
    Task MarkUsedAsync(Guid tokenId);
}
