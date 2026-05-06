using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Registration successful.");
    }

    /// <summary>Login and obtain access + refresh tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        SetTokenCookie(result.RefreshToken);
        return ResponseFormatter.Success(result, "Login successful.");
    }

    /// <summary>Obtain a new access token using a valid refresh token.</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return ResponseFormatter.Error("Refresh token is missing from cookie.", StatusCodes.Status401Unauthorized);

        var result = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        SetTokenCookie(result.RefreshToken);
        return ResponseFormatter.Success(result, "Token refreshed successfully.");
    }

    /// <summary>Revoke a refresh token (logout).</summary>
    [Authorize]
    [HttpPost("revoke-token")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return ResponseFormatter.Error("Refresh token is missing from cookie.", StatusCodes.Status400BadRequest);

        await _authService.RevokeTokenAsync(refreshToken, cancellationToken);
        
        Response.Cookies.Delete("refreshToken");
        return ResponseFormatter.Success(message: "Token revoked successfully.");
    }

    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7), // Adjust expiration as needed
            SameSite = SameSiteMode.Strict,
            Secure = true // Set to true if using HTTPS
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
