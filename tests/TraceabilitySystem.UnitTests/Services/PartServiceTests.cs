using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using Xunit;

namespace TraceabilitySystem.UnitTests.Services;

public class PartServiceTests
{
    private readonly Mock<IPartRepository> _partRepositoryMock;
    private readonly PartService _partService;

    public PartServiceTests()
    {
        _partRepositoryMock = new Mock<IPartRepository>();
        _partService = new PartService(_partRepositoryMock.Object);
    }

    [Fact]
    public async Task GetPartsAsync_ShouldReturnPagedParts_WhenCalled()
    {
        // Arrange
        var parts = new List<Part>
        {
            new() { Id = 1, Number = "PN-0001", Name = "Part A", IsActive = true },
            new() { Id = 2, Number = "PN-0002", Name = "Part B", IsActive = true }
        };

        _partRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Part, bool>>>(),
                It.IsAny<Func<IQueryable<Part>, IOrderedQueryable<Part>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((parts, 2));

        // Act
        var result = await _partService.GetPartsAsync(1, 10, "PN", true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("PN-0001", result.Items.First().Number);
    }

    [Fact]
    public async Task GetPartByIdAsync_ShouldReturnPart_WhenExists()
    {
        // Arrange
        var part = new Part { Id = 1, Number = "PN-0001", Name = "Part A" };

        _partRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(part);

        // Act
        var result = await _partService.GetPartByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PN-0001", result.Number);
    }

    [Fact]
    public async Task GetPartByIdAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
    {
        // Arrange
        _partRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Part)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _partService.GetPartByIdAsync(99));
    }

    [Fact]
    public async Task CreatePartAsync_ShouldCreatePart_WhenNumberIsUnique()
    {
        // Arrange
        var request = new CreatePartRequestDto
        {
            Number = "PN-9999",
            Name = "New Part",
            Description = "A brand new part"
        };

        _partRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Part, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _partService.CreatePartAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PN-9999", result.Number);
        _partRepositoryMock.Verify(r => r.AddAsync(It.Is<Part>(p => p.Number == "PN-9999"), It.IsAny<CancellationToken>()), Times.Once);
        _partRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePartAsync_ShouldThrowAppException_WhenNumberAlreadyExists()
    {
        // Arrange
        var request = new CreatePartRequestDto
        {
            Number = "PN-0001",
            Name = "Duplicate Part"
        };

        _partRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Part, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _partService.CreatePartAsync(request));
        Assert.Equal("Number is already registered.", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task UpdatePartAsync_ShouldUpdatePart_WhenRequestIsValid()
    {
        // Arrange
        var part = new Part { Id = 1, Number = "PN-0001", Name = "Part A", IsActive = true };
        var request = new UpdatePartRequestDto
        {
            Number = "PN-0001-Updated",
            Name = "Part A Updated",
            Description = "Updated description",
            IsActive = false
        };

        _partRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(part);

        _partRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Part, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _partService.UpdatePartAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PN-0001-Updated", result.Number);
        Assert.Equal("Part A Updated", result.Name);
        Assert.False(result.IsActive);
        _partRepositoryMock.Verify(r => r.Update(part), Times.Once);
        _partRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldUpdatePartStatus()
    {
        // Arrange
        var part = new Part { Id = 1, Number = "PN-0001", Name = "Part A", IsActive = true };

        _partRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(part);

        // Act
        await _partService.ChangeStatusAsync(1, false);

        // Assert
        Assert.False(part.IsActive);
        _partRepositoryMock.Verify(r => r.Update(part), Times.Once);
        _partRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
