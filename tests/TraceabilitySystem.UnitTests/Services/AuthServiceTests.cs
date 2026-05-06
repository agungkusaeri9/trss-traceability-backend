using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using Xunit;

namespace TraceabilitySystem.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _jwtServiceMock.Object,
            _passwordHasherMock.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenUsernameIsUnique()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Username = "testuser",
            Role = "user",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash(request.Password))
            .Returns("hashed_password");

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "testuser" && u.Name == "Test User"), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowAppException_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Username = "existinguser",
            Role = "user",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _authService.RegisterAsync(request));
        Assert.Equal("Username is already registered.", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashed_password",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.Verify(request.Password, user.PasswordHash))
            .Returns(true);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(user))
            .Returns("access_token");

        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "password123"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
        Assert.Equal("Invalid username or password.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashed_password",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.Verify(request.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
        Assert.Equal("Invalid username or password.", exception.Message);
    }
}
