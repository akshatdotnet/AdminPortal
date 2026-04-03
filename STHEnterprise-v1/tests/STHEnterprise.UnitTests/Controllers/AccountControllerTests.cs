using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Api.Helpers;
using STHEnterprise.Application.DTOs.Account;
using STHEnterprise.Application.Interfaces;

public class AccountControllerTests : IDisposable
{
    private readonly Mock<IJwtTokenGenerator> _tokenGeneratorMock;
    private readonly AccountController _controller;
    private readonly Mock<IAuthService> _authServiceMock;


    public AccountControllerTests()
    {
        // Ensure clean state per test
        InMemoryUserStore.Users.Clear();

        _tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        _authServiceMock = new Mock<IAuthService>();          // ← add this
        _controller = new AccountController(
                                  _tokenGeneratorMock.Object,
                                  _authServiceMock.Object);      // ← pass second arg
    }

    public void Dispose()
    {
        InMemoryUserStore.Users.Clear();
    }

    // =====================================================
    // REGISTER
    // =====================================================

    [Fact]
    public void Register_ShouldReturnOk_WhenUserIsNew()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "test@test.com",
            Password = "Password@123"
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("User registered successfully", response.Message);
        Assert.True(InMemoryUserStore.Users.ContainsKey(request.Email));
    }

    [Fact]
    public void Register_ShouldReturnBadRequest_WhenUserAlreadyExists()
    {
        // Arrange
        InMemoryUserStore.Users["test@test.com"] = new AppUser
        {
            Email = "test@test.com",
            PasswordHash = "hashed"
        };

        var request = new RegisterRequest
        {
            FullName = "Duplicate User",
            Email = "test@test.com",
            Password = "Password@123"
        };

        // Act
        var result = _controller.Register(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(badRequest.Value);

        Assert.False(response.Success);
        Assert.Equal("User already exists", response.Message);
    }

    // =====================================================
    // LOGIN
    // =====================================================

    //[Fact]
    //public void Login_ShouldReturnToken_WhenCredentialsAreValid()
    //{
    //    // Arrange
    //    var password = "Admin@123";
    //    var user = new AppUser
    //    {
    //        Email = "admin@sth.com",
    //        FullName = "System Admin",
    //        Role = "Admin",
    //        PasswordHash = PasswordHasher.Hash(password)
    //    };

    //    InMemoryUserStore.Users[user.Email] = user;

    //    _tokenGeneratorMock
    //        .Setup(x => x.GenerateToken(It.IsAny<AppUser>()))
    //        .Returns("fake-jwt-token");

    //    var request = new LoginRequest
    //    {
    //        Email = user.Email,
    //        Password = password
    //    };

    //    // Act
    //    var result = _controller.Login(request);

    //    // Assert
    //    var okResult = Assert.IsType<OkObjectResult>(result);

    //    var response = okResult.Value as dynamic;
    //    Assert.NotNull(response);
    //    Assert.Equal("fake-jwt-token", (string)response.token);
    //}

    //[Fact]
    //public void Login_ShouldReturnToken_WhenCredentialsAreValid()
    //{
    //    // Arrange
    //    var request = new LoginRequest
    //    {
    //        Email = "admin@test.com",
    //        Password = "Admin@123"
    //    };

    //    // Act
    //    var result = _controller.Login(request);

    //    // Assert
    //    var okResult = Assert.IsType<OkObjectResult>(result);
    //    var response = Assert.IsType<LoginResponseDto>(okResult.Value);

    //    Assert.False(string.IsNullOrEmpty(response.Token));
    //    Assert.Equal("admin@test.com", response.User.Email);
    //}


    [Fact]
    public void Login_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "user@sth.com",
            PasswordHash = PasswordHasher.Hash("CorrectPwd"),
            Role = "User"
        };

        InMemoryUserStore.Users[user.Email] = user;

        var request = new LoginRequest
        {
            Email = user.Email,
            Password = "WrongPwd"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // =====================================================
    // FORGOT PASSWORD
    // =====================================================

    [Fact]
    public void ForgotPassword_ShouldReturnOk_EvenIfUserDoesNotExist()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "unknown@test.com"
        };

        // Act
        var result = _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);

        Assert.True(response.Success);
    }

    // =====================================================
    // RESET PASSWORD
    // =====================================================

    [Fact]
    public void ResetPassword_ShouldReturnBadRequest_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = "invalid-token",
            NewPassword = "NewPwd@123"
        };

        // Act
        var result = _controller.ResetPassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
