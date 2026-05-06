using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;

    public ConfigController(IAuthService authService, IUserRepository userRepository)
    {
        _authService = authService;
        _userRepository = userRepository;
    }

    /// <summary>Seed a dummy admin user.</summary>
    [HttpPost("seed-admin")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedAdmin(CancellationToken cancellationToken)
    {
        // Cek apakah user admin sudah ada (mengakses repository auth/user)
        var exists = await _userRepository.ExistsAsync(u => u.Username == "admin", cancellationToken);
        
        if (exists)
        {
            return ResponseFormatter.Success(message: "Admin user already exists.");
        }

        // Jika belum ada, buat user menggunakan auth service
        var request = new RegisterRequest
        {
            Name = "admin",
            Username = "admin",
            Password = "password",
            ConfirmPassword = "password"
        };

        var result = await _authService.RegisterAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Admin dummy user created successfully.");
    }

    /// <summary>Seed 100 dummy users.</summary>
    [HttpPost("seed-users")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedUsers(CancellationToken cancellationToken)
    {
        int createdCount = 0;
        
        for (int i = 1; i <= 100; i++)
        {
            var username = $"user{i}";
            var exists = await _userRepository.ExistsAsync(u => u.Username == username, cancellationToken);
            
            if (!exists)
            {
                var request = new RegisterRequest
                {
                    Name = $"Dummy User {i}",
                    Username = username,
                    Password = "password",
                    ConfirmPassword = "password"
                };
                
                await _authService.RegisterAsync(request, cancellationToken);
                createdCount++;
            }
        }

        return ResponseFormatter.Success(message: $"{createdCount} dummy users seeded successfully.");
    }
}
