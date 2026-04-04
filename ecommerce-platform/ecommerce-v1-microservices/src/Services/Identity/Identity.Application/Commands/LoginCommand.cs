using Common.Domain.Primitives;
using FluentValidation;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands;

public sealed record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthResponseDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWorkIdentity unitOfWork)
    : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(cmd.Email, ct);

        if (user is null)
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized("Invalid email or password."));

        if (user.IsLockedOut)
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized($"Account locked until {user.LockoutEnd:HH:mm UTC}."));

        if (!passwordHasher.Verify(cmd.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized("Invalid email or password."));
        }

        user.RecordSuccessfulLogin();
        var tokens = tokenService.GenerateTokens(user);
        user.SetRefreshToken(tokens.RefreshToken, DateTime.UtcNow.AddDays(30));
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(tokens);
    }
}
