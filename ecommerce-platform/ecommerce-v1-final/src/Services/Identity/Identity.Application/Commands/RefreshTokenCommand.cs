using Common.Domain.Primitives;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands;

public sealed record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<Result<AuthResponseDto>>;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IUnitOfWorkIdentity unitOfWork) :
    IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var userId = tokenService.GetUserIdFromExpiredToken(cmd.AccessToken);
        if (userId is null)
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid access token."));

        var user = await userRepository.GetByIdAsync(userId.Value, ct);
        if (user is null || user.RefreshToken != cmd.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid or expired refresh token."));

        var tokens = tokenService.GenerateTokens(user);
        user.SetRefreshToken(tokens.RefreshToken, DateTime.UtcNow.AddDays(30));
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(tokens);
    }
}

public sealed record RevokeTokenCommand(Guid UserId) : IRequest;

public sealed class RevokeTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWorkIdentity unitOfWork) : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(cmd.UserId, ct);
        user?.RevokeRefreshToken();
        await unitOfWork.SaveChangesAsync(ct);
    }
}
