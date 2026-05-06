using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using Xunit;

namespace TraceabilitySystem.UnitTests.Services;

public class ProcessServiceTests
{
    private readonly Mock<IProcessRepository> _processRepositoryMock;
    private readonly ProcessService _processService;

    public ProcessServiceTests()
    {
        _processRepositoryMock = new Mock<IProcessRepository>();
        _processService = new ProcessService(_processRepositoryMock.Object);
    }

    [Fact]
    public async Task GetProcessesAsync_ShouldReturnPagedProcesses_WhenCalled()
    {
        // Arrange
        var processes = new List<Process>
        {
            new() { Id = 1, Code = "PRC-001", Name = "Process A", IsActive = true },
            new() { Id = 2, Code = "PRC-002", Name = "Process B", IsActive = true }
        };

        _processRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Process, bool>>>(),
                It.IsAny<Func<IQueryable<Process>, IOrderedQueryable<Process>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((processes, 2));

        // Act
        var result = await _processService.GetProcessesAsync(1, 10, "PRC", true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("PRC-001", result.Items.First().Code);
    }

    [Fact]
    public async Task GetProcessByIdAsync_ShouldReturnProcess_WhenExists()
    {
        // Arrange
        var process = new Process { Id = 1, Code = "PRC-001", Name = "Process A" };

        _processRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        // Act
        var result = await _processService.GetProcessByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRC-001", result.Code);
    }

    [Fact]
    public async Task GetProcessByIdAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
    {
        // Arrange
        _processRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Process)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _processService.GetProcessByIdAsync(99));
    }

    [Fact]
    public async Task CreateProcessAsync_ShouldCreateProcess_WhenCodeIsUnique()
    {
        // Arrange
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-003",
            Name = "New Process",
            Description = "A brand new process"
        };

        _processRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Process, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _processService.CreateProcessAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRC-003", result.Code);
        _processRepositoryMock.Verify(r => r.AddAsync(It.Is<Process>(p => p.Code == "PRC-003"), It.IsAny<CancellationToken>()), Times.Once);
        _processRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProcessAsync_ShouldThrowAppException_WhenCodeAlreadyExists()
    {
        // Arrange
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-001",
            Name = "Duplicate Process"
        };

        _processRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Process, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _processService.CreateProcessAsync(request));
        Assert.Equal("Code is already registered.", exception.Message);
        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateProcessAsync_ShouldUpdateProcess_WhenRequestIsValid()
    {
        // Arrange
        var process = new Process { Id = 1, Code = "PRC-001", Name = "Process A", IsActive = true };
        var request = new UpdateProcessRequestDto
        {
            Code = "PRC-001-Updated",
            Name = "Process A Updated",
            Description = "Updated description",
            IsActive = false
        };

        _processRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _processRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Process, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _processService.UpdateProcessAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRC-001-Updated", result.Code);
        Assert.Equal("Process A Updated", result.Name);
        Assert.False(result.IsActive);
        _processRepositoryMock.Verify(r => r.Update(process), Times.Once);
        _processRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldUpdateProcessStatus()
    {
        // Arrange
        var process = new Process { Id = 1, Code = "PRC-001", Name = "Process A", IsActive = true };

        _processRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        // Act
        await _processService.ChangeStatusAsync(1, false);

        // Assert
        Assert.False(process.IsActive);
        _processRepositoryMock.Verify(r => r.Update(process), Times.Once);
        _processRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
