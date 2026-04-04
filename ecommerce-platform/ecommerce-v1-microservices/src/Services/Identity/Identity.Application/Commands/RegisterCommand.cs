using Common.Domain.Primitives;
using FluentValidation;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Commands;

public sealed record RegisterCommand(
    string Email, string Password,
    string FirstName, string LastName,
    string PhoneNumber,
    string Role = UserRoles.Customer) : IRequest<Result<AuthResponseDto>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty()
            .Matches(@"^\+?[1-9]\d{6,14}$").WithMessage("Use international format, e.g. +14155552671");
        RuleFor(x => x.Role).Must(r => UserRoles.All.Contains(r)).WithMessage("Invalid role.");
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWorkIdentity unitOfWork)
    : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(cmd.Email, ct))
            return Result.Failure<AuthResponseDto>(
                Error.Conflict("User", $"Email '{cmd.Email}' is already registered."));

        var user = ApplicationUser.Create(
            cmd.Email, cmd.FirstName, cmd.LastName,
            cmd.PhoneNumber, passwordHasher.Hash(cmd.Password), cmd.Role);

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        var tokens = tokenService.GenerateTokens(user);
        user.SetRefreshToken(tokens.RefreshToken, DateTime.UtcNow.AddDays(30));
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(tokens);
    }
}
