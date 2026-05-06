using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Shared.Models;
using Xunit;

namespace TraceabilitySystem.IntegrationTests.Controllers;

[Collection("Integration Tests Collection")]
public class ProcessesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProcessesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProcesses_ShouldReturnSuccess_WithPaginatedProcesses()
    {
        // Act
        var response = await _client.GetAsync("/api/processes?page=1&limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<PagedApiResponse<ProcessDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Pagination);
    }

    [Fact]
    public async Task CreateProcess_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-I-0001",
            Name = "Integration Test Process",
            Description = "Integration Process Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/processes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Process created successfully.", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("PRC-I-0001", apiResponse.Data.Code);
    }

    [Fact]
    public async Task CreateProcess_ShouldReturnConflict_WhenProcessCodeAlreadyExists()
    {
        // Arrange
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-I-DUPLICATE",
            Name = "Duplicate Process Test"
        };

        // Create first
        await _client.PostAsJsonAsync("/api/processes", request);

        // Act - Create second
        var response = await _client.PostAsJsonAsync("/api/processes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Code is already registered.", apiResponse.Message);
    }

    [Fact]
    public async Task GetProcess_ShouldReturnProcess_WhenExists()
    {
        // Arrange - Create a process first
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-I-FETCH",
            Name = "Process To Fetch"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/processes", request);
        var createdProcess = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();

        // Act
        var response = await _client.GetAsync($"/api/processes/{createdProcess!.Data!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("PRC-I-FETCH", apiResponse.Data!.Code);
    }

    [Fact]
    public async Task GetProcess_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/processes/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange - Create a process first
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-I-UPDATE",
            Name = "Process To Update"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/processes", request);
        var createdProcess = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();

        var updateRequest = new UpdateProcessRequestDto
        {
            Code = "PRC-I-UPDATED",
            Name = "Process Updated Name",
            Description = "Updated description",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/processes/{createdProcess!.Data!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("PRC-I-UPDATED", apiResponse.Data!.Code);
        Assert.Equal("Process Updated Name", apiResponse.Data.Name);
        Assert.False(apiResponse.Data.IsActive);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnSuccess_WhenStatusIsChanged()
    {
        // Arrange - Create a process first
        var request = new CreateProcessRequestDto
        {
            Code = "PRC-I-STATUS",
            Name = "Process To Change Status"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/processes", request);
        var createdProcess = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();

        var statusRequest = new ChangePartStatusRequestDto
        {
            IsActive = false
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/processes/{createdProcess!.Data!.Id}/change-status", statusRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Process deactivated successfully.", apiResponse.Message);
    }
}
