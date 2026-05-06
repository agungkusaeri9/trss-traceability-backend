using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Shared.Models;
using Xunit;

namespace TraceabilitySystem.IntegrationTests.Controllers;

[Collection("Integration Tests Collection")]
public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Integration Test User",
            Username = "integration_test_user",
            Role = "user",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Registration successful.", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.AccessToken);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Duplicate User",
            Username = "duplicate_user",
            Role = "user",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Register first time
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Register second time
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Username is already registered.", apiResponse.Message);
    }

    [Fact]
    public async Task Login_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        // Arrange - Register the user first
        var registerRequest = new RegisterRequest
        {
            Name = "Login User",
            Username = "login_user",
            Role = "user",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = "login_user",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Login successful.", apiResponse.Message);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.AccessToken);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent_user",
            Password = "WrongPassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Invalid username or password.", apiResponse.Message);
    }
}
