using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TraceabilitySystem.Application.DTOs.User;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using Xunit;

namespace TraceabilitySystem.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object
        );
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedUsers_WhenCalled()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Name = "Alice", Username = "alice", IsActive = true },
            new() { Id = 2, Name = "Bob", Username = "bob", IsActive = true }
        };

        _userRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 2));

        // Act
        var result = await _userService.GetUsersAsync(1, 10, "search", true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("alice", result.Items.First().Username);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser_WhenUsernameIsUnique()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "New User",
            Username = "newuser",
            Password = "password123",
            Role = "User",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash(request.Password))
            .Returns("hashed_password");

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser", result.Username);
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "newuser"), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowAppException_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Existing User",
            Username = "existing",
            Password = "password123"
        };

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _userService.CreateUserAsync(request));
        Assert.Equal("Username is already registered.", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new User { Id = 1, Username = "alice", Name = "Alice" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("alice", result.Username);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(99));
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUser_WhenRequestIsValid()
    {
        // Arrange
        var user = new User { Id = 1, Username = "alice", Name = "Alice", Role = "User", IsActive = true };
        var request = new UpdateUserRequest
        {
            Name = "Alice Updated",
            Username = "aliceupdated",
            Role = "Admin",
            IsActive = false
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.UpdateUserAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aliceupdated", result.Username);
        Assert.Equal("Alice Updated", result.Name);
        Assert.Equal("Admin", result.Role);
        Assert.False(result.IsActive);
        _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldDeleteUser_WhenExists()
    {
        // Arrange
        var user = new User { Id = 1, Username = "alice" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _userService.DeleteUserAsync(1);

        // Assert
        _userRepositoryMock.Verify(r => r.Remove(user), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
