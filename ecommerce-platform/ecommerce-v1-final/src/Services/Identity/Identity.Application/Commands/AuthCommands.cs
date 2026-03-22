using Common.Application.Exceptions;
using Common.Domain.Primitives;
using FluentValidation;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Commands;

// ══════════════════════════════════════════════════════════════
// REGISTER COMMAND
// ══════════════════════════════════════════════════════════════
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Role = UserRoles.Customer) : IRequest<Result<AuthResponseDto>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{1,14}$");
        RuleFor(x => x.Role).Must(r => UserRoles.All.Contains(r)).WithMessage("Invalid role.");
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWorkIdentity unitOfWork) :
    IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        // Guard: email must be unique
        if (await userRepository.ExistsByEmailAsync(cmd.Email, ct))
            return Result.Failure<AuthResponseDto>(
                Error.Conflict("User", $"Email '{cmd.Email}' is already registered."));

        var passwordHash = passwordHasher.Hash(cmd.Password);

        var user = ApplicationUser.Create(
            cmd.Email, cmd.FirstName, cmd.LastName,
            cmd.PhoneNumber, passwordHash, cmd.Role);

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        var tokens = tokenService.GenerateTokens(user);

        user.SetRefreshToken(tokens.RefreshToken, DateTime.UtcNow.AddDays(30));
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(tokens);
    }
}

// ══════════════════════════════════════════════════════════════
// LOGIN COMMAND
// ══════════════════════════════════════════════════════════════
public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthResponseDto>>;

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
    IUnitOfWorkIdentity unitOfWork) :
    IRequestHandler<LoginCommand, Result<AuthResponseDto>>
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

// ══════════════════════════════════════════════════════════════
// REFRESH TOKEN COMMAND
// ══════════════════════════════════════════════════════════════
public sealed record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : IRequest<Result<AuthResponseDto>>;

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
