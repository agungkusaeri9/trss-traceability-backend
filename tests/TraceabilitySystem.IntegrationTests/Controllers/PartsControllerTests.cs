using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Shared.Models;
using Xunit;

namespace TraceabilitySystem.IntegrationTests.Controllers;

[Collection("Integration Tests Collection")]
public class PartsControllerTests
{
    private readonly HttpClient _client;

    public PartsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetParts_ShouldReturnSuccess_WithPaginatedParts()
    {
        // Act
        var response = await _client.GetAsync("/api/parts?page=1&limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<PagedApiResponse<PartDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Pagination);
    }

    [Fact]
    public async Task CreatePart_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreatePartRequestDto
        {
            Number = "PN-I-0001",
            Name = "Integration Test Part",
            Description = "Integration Part Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/parts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PartDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Part created successfully.", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("PN-I-0001", apiResponse.Data.Number);
    }

    [Fact]
    public async Task CreatePart_ShouldReturnConflict_WhenPartNumberAlreadyExists()
    {
        // Arrange
        var request = new CreatePartRequestDto
        {
            Number = "PN-I-DUPLICATE",
            Name = "Duplicate Part Test"
        };

        // Create first
        await _client.PostAsJsonAsync("/api/parts", request);

        // Act - Create second
        var response = await _client.PostAsJsonAsync("/api/parts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Number is already registered.", apiResponse.Message);
    }

    [Fact]
    public async Task GetPart_ShouldReturnPart_WhenExists()
    {
        // Arrange - Create a part first
        var request = new CreatePartRequestDto
        {
            Number = "PN-I-FETCH",
            Name = "Part To Fetch"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/parts", request);
        var createdPart = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PartDto>>();

        // Act
        var response = await _client.GetAsync($"/api/parts/{createdPart!.Data!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PartDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("PN-I-FETCH", apiResponse.Data!.Number);
    }

    [Fact]
    public async Task GetPart_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/parts/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePart_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange - Create a part first
        var request = new CreatePartRequestDto
        {
            Number = "PN-I-UPDATE",
            Name = "Part To Update"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/parts", request);
        var createdPart = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PartDto>>();

        var updateRequest = new UpdatePartRequestDto
        {
            Number = "PN-I-UPDATED",
            Name = "Part Updated Name",
            Description = "Updated description",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/parts/{createdPart!.Data!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PartDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("PN-I-UPDATED", apiResponse.Data!.Number);
        Assert.Equal("Part Updated Name", apiResponse.Data.Name);
        Assert.False(apiResponse.Data.IsActive);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnSuccess_WhenStatusIsChanged()
    {
        // Arrange - Create a part first
        var request = new CreatePartRequestDto
        {
            Number = "PN-I-STATUS",
            Name = "Part To Change Status"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/parts", request);
        var createdPart = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PartDto>>();

        var statusRequest = new ChangePartStatusRequestDto
        {
            IsActive = false
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/parts/{createdPart!.Data!.Id}/change-status", statusRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Part deactivated successfully.", apiResponse.Message);
    }
}
