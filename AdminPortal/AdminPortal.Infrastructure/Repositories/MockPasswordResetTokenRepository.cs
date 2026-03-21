using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash) =>
        Task.FromResult(MockUserStore.ResetTokens.FirstOrDefault(t => t.Token == tokenHash));

    public Task<PasswordResetToken> AddAsync(PasswordResetToken token)
    {
        MockUserStore.ResetTokens.Add(token);
        return Task.FromResult(token);
    }

    public Task InvalidateAllForUserAsync(Guid userId)
    {
        var tokens = MockUserStore.ResetTokens.Where(t => t.UserId == userId && !t.IsUsed).ToList();
        foreach (var t in tokens)
            t.IsUsed = true;
        return Task.CompletedTask;
    }

    public Task MarkUsedAsync(Guid tokenId)
    {
        var token = MockUserStore.ResetTokens.FirstOrDefault(t => t.Id == tokenId);
        if (token is not null) token.IsUsed = true;
        return Task.CompletedTask;
    }
}
