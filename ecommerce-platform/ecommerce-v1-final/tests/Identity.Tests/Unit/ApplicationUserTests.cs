using FluentAssertions;
using Identity.Domain.Entities;
using Xunit;

namespace Identity.Tests.Unit;

public sealed class ApplicationUserTests
{
    private static ApplicationUser CreateUser() =>
        ApplicationUser.Create("alice@test.com", "Alice", "Smith",
            "+14155552671", "hashedpw");

    [Fact]
    public void Create_ShouldSetPropertiesAndRaiseDomainEvent()
    {
        var user = CreateUser();
        user.Email.Should().Be("alice@test.com");
        user.FullName.Should().Be("Alice Smith");
        user.Role.Should().Be(UserRoles.Customer);
        user.IsActive.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
    }

    [Fact]
    public void RecordFailedLogin_FiveTimes_ShouldLockAccount()
    {
        var user = CreateUser();
        for (int i = 0; i < 5; i++) user.RecordFailedLogin();
        user.IsLockedOut.Should().BeTrue();
        user.LockoutEnd.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldResetFailedAttempts()
    {
        var user = CreateUser();
        user.RecordFailedLogin();
        user.RecordSuccessfulLogin();
        user.FailedLoginAttempts.Should().Be(0);
        user.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void SetRefreshToken_ShouldStoreTokenAndExpiry()
    {
        var user = CreateUser();
        var expiry = DateTime.UtcNow.AddDays(30);
        user.SetRefreshToken("tok", expiry);
        user.RefreshToken.Should().Be("tok");
        user.RefreshTokenExpiry.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RevokeRefreshToken_ShouldClearToken()
    {
        var user = CreateUser();
        user.SetRefreshToken("tok", DateTime.UtcNow.AddDays(1));
        user.RevokeRefreshToken();
        user.RefreshToken.Should().BeNull();
    }
}
