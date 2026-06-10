using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using Xunit;

namespace TraceabilitySystem.UnitTests.Services;

public class ParameterServiceTests
{
    private readonly Mock<IParameterRepository> _parameterRepositoryMock;
    private readonly ParameterService _parameterService;

    public ParameterServiceTests()
    {
        _parameterRepositoryMock = new Mock<IParameterRepository>();
        _parameterService = new ParameterService(_parameterRepositoryMock.Object);
    }

    [Fact]
    public async Task GetParametersAsync_ShouldReturnPagedParameters_WhenCalled()
    {
        // Arrange
        var parameters = new List<Parameter>
        {
            new() { Id = 1, Code = "PRM-001", Name = "Temperature", IsActive = true },
            new() { Id = 2, Code = "PRM-002", Name = "Pressure", IsActive = true }
        };

        _parameterRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Parameter, bool>>>(),
                It.IsAny<Func<IQueryable<Parameter>, IOrderedQueryable<Parameter>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((parameters, 2));

        // Act
        var result = await _parameterService.GetParametersAsync(1, 10, "PRM", true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("PRM-001", result.Items.First().Code);
    }

    [Fact]
    public async Task GetParameterByIdAsync_ShouldReturnParameter_WhenExists()
    {
        // Arrange
        var parameter = new Parameter { Id = 1, Code = "PRM-001", Name = "Temperature" };

        _parameterRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parameter);

        // Act
        var result = await _parameterService.GetParameterByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRM-001", result.Code);
    }

    [Fact]
    public async Task GetParameterByIdAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
    {
        // Arrange
        _parameterRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parameter?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _parameterService.GetParameterByIdAsync(99));
    }

    [Fact]
    public async Task CreateParameterAsync_ShouldCreateParameter_WhenCodeIsUnique()
    {
        // Arrange
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-003",
            Name = "Torque",
            Description = "A brand new parameter"
        };

        _parameterRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Parameter, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _parameterService.CreateParameterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRM-003", result.Code);
        _parameterRepositoryMock.Verify(r => r.AddAsync(It.Is<Parameter>(p => p.Code == "PRM-003"), It.IsAny<CancellationToken>()), Times.Once);
        _parameterRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateParameterAsync_ShouldThrowAppException_WhenCodeAlreadyExists()
    {
        // Arrange
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-001",
            Name = "Duplicate Parameter"
        };

        _parameterRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Parameter, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _parameterService.CreateParameterAsync(request));
        Assert.Equal("Code is already registered.", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateParameterAsync_ShouldUpdateParameter_WhenRequestIsValid()
    {
        // Arrange
        var parameter = new Parameter { Id = 1, Code = "PRM-001", Name = "Temperature", IsActive = true };
        var request = new UpdateParameterRequestDto
        {
            Code = "PRM-001-Updated",
            Name = "Temperature Updated",
            Description = "Updated description",
            IsActive = false
        };

        _parameterRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parameter);

        _parameterRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Parameter, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _parameterService.UpdateParameterAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRM-001-Updated", result.Code);
        Assert.Equal("Temperature Updated", result.Name);
        Assert.False(result.IsActive);
        _parameterRepositoryMock.Verify(r => r.Update(parameter), Times.Once);
        _parameterRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldUpdateParameterStatus()
    {
        // Arrange
        var parameter = new Parameter { Id = 1, Code = "PRM-001", Name = "Temperature", IsActive = true };

        _parameterRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parameter);

        // Act
        await _parameterService.ChangeStatusAsync(1, false);

        // Assert
        Assert.False(parameter.IsActive);
        _parameterRepositoryMock.Verify(r => r.Update(parameter), Times.Once);
        _parameterRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
