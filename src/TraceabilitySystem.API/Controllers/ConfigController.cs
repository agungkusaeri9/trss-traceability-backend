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
    private readonly IPartService _partService;

    public ConfigController(IAuthService authService, IUserRepository userRepository, IPartService partService)
    {
        _authService = authService;
        _userRepository = userRepository;
        _partService = partService;
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
            Role = "admin",
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
                    Role = "user",
                    Password = "password",
                    ConfirmPassword = "password"
                };
                
                await _authService.RegisterAsync(request, cancellationToken);
                createdCount++;
            }
        }

        return ResponseFormatter.Success(message: $"{createdCount} dummy users seeded successfully.");
    }

    /// <summary>Seed 100 dummy parts.</summary>
    [HttpPost("seed-parts")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedParts(CancellationToken cancellationToken)
    {
        int createdCount = 0;
        
        for (int i = 1; i <= 100; i++)
        {
            var number = $"PN-{i:D4}";
            
            try
            {
                var request = new TraceabilitySystem.Application.DTOs.Part.CreatePartRequestDto
                {
                    Number = number,
                    Name = $"Sample Part {i}",
                    Description = $"This is an auto-generated sample description for part {i}."
                };
                
                await _partService.CreatePartAsync(request, cancellationToken);
                createdCount++;
            }
            catch (TraceabilitySystem.Shared.Exceptions.AppException)
            {
                // Number is already registered, skip
                continue;
            }
        }

        return ResponseFormatter.Success(message: $"{createdCount} dummy parts seeded successfully.");
    }
}
