using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Shared.Models;
using Xunit;

namespace TraceabilitySystem.IntegrationTests.Controllers;

[Collection("Integration Tests Collection")]
public class ParametersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ParametersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetParameters_ShouldReturnSuccess_WithPaginatedParameters()
    {
        // Act
        var response = await _client.GetAsync("/api/parameters?page=1&limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<PagedApiResponse<ParameterDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Pagination);
    }

    [Fact]
    public async Task CreateParameter_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-I-0001",
            Name = "Integration Test Parameter",
            Description = "Integration Parameter Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/parameters", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ParameterDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Parameter created successfully.", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("PRM-I-0001", apiResponse.Data.Code);
    }

    [Fact]
    public async Task CreateParameter_ShouldReturnConflict_WhenParameterCodeAlreadyExists()
    {
        // Arrange
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-I-DUPLICATE",
            Name = "Duplicate Parameter Test"
        };

        // Create first
        await _client.PostAsJsonAsync("/api/parameters", request);

        // Act - Create second
        var response = await _client.PostAsJsonAsync("/api/parameters", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Code is already registered.", apiResponse.Message);
    }

    [Fact]
    public async Task GetParameter_ShouldReturnParameter_WhenExists()
    {
        // Arrange - Create a parameter first
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-I-FETCH",
            Name = "Parameter To Fetch"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/parameters", request);
        var createdParameter = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ParameterDto>>();

        // Act
        var response = await _client.GetAsync($"/api/parameters/{createdParameter!.Data!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ParameterDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("PRM-I-FETCH", apiResponse.Data!.Code);
    }

    [Fact]
    public async Task GetParameter_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/parameters/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateParameter_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange - Create a parameter first
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-I-UPDATE",
            Name = "Parameter To Update"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/parameters", request);
        var createdParameter = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ParameterDto>>();

        var updateRequest = new UpdateParameterRequestDto
        {
            Code = "PRM-I-UPDATED",
            Name = "Parameter Updated Name",
            Description = "Updated description",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/parameters/{createdParameter!.Data!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ParameterDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("PRM-I-UPDATED", apiResponse.Data!.Code);
        Assert.Equal("Parameter Updated Name", apiResponse.Data.Name);
        Assert.False(apiResponse.Data.IsActive);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnSuccess_WhenStatusIsChanged()
    {
        // Arrange - Create a parameter first
        var request = new CreateParameterRequestDto
        {
            Code = "PRM-I-STATUS",
            Name = "Parameter To Change Status"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/parameters", request);
        var createdParameter = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ParameterDto>>();

        var statusRequest = new ChangePartStatusRequestDto
        {
            IsActive = false
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/parameters/{createdParameter!.Data!.Id}/change-status", statusRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Parameter deactivated successfully.", apiResponse.Message);
    }
}
