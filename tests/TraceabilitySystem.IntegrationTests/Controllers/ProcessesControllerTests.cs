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

    [Fact]
    public async Task AssignAndRemoveParameters_ShouldSucceed_WhenValid()
    {
        // 1. Create a parameter
        var paramRequest = new { Code = "PRM-TEST-M2M", Name = "M2M Test Param" };
        var paramResponse = await _client.PostAsJsonAsync("/api/parameters", paramRequest);
        Assert.Equal(HttpStatusCode.Created, paramResponse.StatusCode);
        var createdParam = await paramResponse.Content.ReadFromJsonAsync<ApiResponse<TraceabilitySystem.Application.DTOs.Parameter.ParameterDto>>();

        // 2. Create a process
        var procRequest = new CreateProcessRequestDto { Code = "PRC-TEST-M2M", Name = "M2M Test Process" };
        var procResponse = await _client.PostAsJsonAsync("/api/processes", procRequest);
        Assert.Equal(HttpStatusCode.Created, procResponse.StatusCode);
        var createdProc = await procResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();

        // 3. Assign Parameter to Process
        var assignRequest = new AdjustProcessParametersRequestDto
        {
            ParameterIds = new System.Collections.Generic.List<int> { createdParam!.Data!.Id }
        };
        var assignResponse = await _client.PostAsJsonAsync($"/api/processes/{createdProc!.Data!.Id}/parameters", assignRequest);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        // 4. Fetch Process and verify parameter exists
        var fetchResponse = await _client.GetAsync($"/api/processes/{createdProc!.Data!.Id}");
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);
        var fetchedProc = await fetchResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();
        Assert.Single(fetchedProc!.Data!.Parameters);
        Assert.Equal("PRM-TEST-M2M", fetchedProc.Data.Parameters[0].Code);

        // 5. Remove Parameter from Process
        var requestMessage = new HttpRequestMessage
        {
            Content = JsonContent.Create(assignRequest),
            Method = HttpMethod.Delete,
            RequestUri = new System.Uri(_client.BaseAddress ?? new System.Uri("http://localhost"), $"/api/processes/{createdProc.Data.Id}/parameters")
        };
        var removeResult = await _client.SendAsync(requestMessage);
        Assert.Equal(HttpStatusCode.OK, removeResult.StatusCode);

        // 6. Fetch Process and verify parameter is deleted
        var finalFetchResponse = await _client.GetAsync($"/api/processes/{createdProc.Data.Id}");
        var finalFetchedProc = await finalFetchResponse.Content.ReadFromJsonAsync<ApiResponse<ProcessDto>>();
        Assert.Empty(finalFetchedProc!.Data!.Parameters);
    }
}
