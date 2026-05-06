using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.User;
using TraceabilitySystem.Shared.Models;
using Xunit;

namespace TraceabilitySystem.IntegrationTests.Controllers;

[Collection("Integration Tests Collection")]
public class UsersControllerTests
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnSuccess_WithPaginatedUsers()
    {
        // Act
        var response = await _client.GetAsync("/api/users?page=1&limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<PagedApiResponse<UserDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Pagination);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Integration User Test",
            Username = "integration_user_new",
            Password = "Password123!",
            Role = "User",
            IsActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("User created successfully.", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("integration_user_new", apiResponse.Data.Username);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnConflict_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Duplicate User Test",
            Username = "integration_user_duplicate",
            Password = "Password123!",
            Role = "User",
            IsActive = true
        };

        // Create first
        await _client.PostAsJsonAsync("/api/users", request);

        // Act - Create second
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Username is already registered.", apiResponse.Message);
    }

    [Fact]
    public async Task GetUser_ShouldReturnUser_WhenExists()
    {
        // Arrange - Create a user first
        var request = new CreateUserRequest
        {
            Name = "Fetch User",
            Username = "fetch_user",
            Password = "Password123!",
            Role = "User"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", request);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();

        // Act
        var response = await _client.GetAsync($"/api/users/{createdUser!.Data!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("fetch_user", apiResponse.Data!.Username);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/users/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange - Create a user first
        var request = new CreateUserRequest
        {
            Name = "User To Update",
            Username = "user_to_update",
            Password = "Password123!",
            Role = "User"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/users", request);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();

        var updateRequest = new UpdateUserRequest
        {
            Name = "User Updated Name",
            Username = "user_updated_username",
            Role = "Admin",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{createdUser!.Data!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("user_updated_username", apiResponse.Data!.Username);
        Assert.Equal("User Updated Name", apiResponse.Data.Name);
        Assert.Equal("Admin", apiResponse.Data.Role);
        Assert.False(apiResponse.Data.IsActive);
    }
}
