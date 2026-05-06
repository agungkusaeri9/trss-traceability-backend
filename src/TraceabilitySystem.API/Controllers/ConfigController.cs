using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IPartService _partService;
    private readonly IProcessRepository _processRepository;
    private readonly IParameterRepository _parameterRepository;

    public ConfigController(
        IAuthService authService,
        IUserRepository userRepository,
        IPartService partService,
        IProcessRepository processRepository,
        IParameterRepository parameterRepository)
    {
        _authService = authService;
        _userRepository = userRepository;
        _partService = partService;
        _processRepository = processRepository;
        _parameterRepository = parameterRepository;
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

    /// <summary>Seed 10 dummy processes.</summary>
    [HttpPost("seed-processes")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedProcesses(CancellationToken cancellationToken)
    {
        int createdCount = 0;
        var processNames = new[] { "Blanking", "Piercing", "Bending", "Welding", "Painting", "Assembly", "Quality Control", "Packaging", "Shipping", "Stamping" };
        
        for (int i = 0; i < 10; i++)
        {
            var code = $"PRC-{(i + 1):D3}";
            var exists = await _processRepository.ExistsAsync(p => p.Code == code, cancellationToken);
            
            if (!exists)
            {
                var process = new Process
                {
                    Code = code,
                    Name = processNames[i],
                    Description = $"Auto-generated process for {processNames[i]}.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _processRepository.AddAsync(process, cancellationToken);
                createdCount++;
            }
        }

        if (createdCount > 0)
        {
            await _processRepository.SaveChangesAsync(cancellationToken);
        }

        return ResponseFormatter.Success(message: $"{createdCount} dummy processes seeded successfully.");
    }

    /// <summary>Seed 50 dummy parameters for radiator manufacturing.</summary>
    [HttpPost("seed-parameters")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedParameters(CancellationToken cancellationToken)
    {
        int createdCount = 0;
        var dummyParams = new[]
        {
            new { Code = "PRM-001", Name = "Welding Temperature", Desc = "Tube Mill welding temperature for core tubes." },
            new { Code = "PRM-002", Name = "Oxygen Level", Desc = "Brazing furnace oxygen level to prevent oxidation." },
            new { Code = "PRM-003", Name = "Furnace Temperature Zone 1", Desc = "Brazing furnace heating zone 1 temperature." },
            new { Code = "PRM-004", Name = "Furnace Temperature Zone 2", Desc = "Brazing furnace heating zone 2 temperature." },
            new { Code = "PRM-005", Name = "Brazing Belt Speed", Desc = "Speed of core transport through brazing furnace." },
            new { Code = "PRM-006", Name = "Crimping Force", Desc = "Clinching/crimping force for tank-to-core assembly." },
            new { Code = "PRM-007", Name = "Crimping Depth", Desc = "Clinching/crimping depth measurement." },
            new { Code = "PRM-008", Name = "Test Air Pressure", Desc = "Radiator air pressure used in leakage test." },
            new { Code = "PRM-009", Name = "Leak Rate", Desc = "Radiator leak rate measured in leakage test." },
            new { Code = "PRM-010", Name = "Pressing Force", Desc = "Stamping press machine force for header plates." },
            new { Code = "PRM-011", Name = "Fin Height", Desc = "Height of the generated radiator fin." },
            new { Code = "PRM-012", Name = "Fin Pitch", Desc = "Fin pitch distance on fin mill." },
            new { Code = "PRM-013", Name = "Fin Width", Desc = "Measured width of the radiator fin." },
            new { Code = "PRM-014", Name = "Fin Forming Lubricant Flow", Desc = "Flow rate of fin forming lubricant oil." },
            new { Code = "PRM-015", Name = "Fin Cutter Speed", Desc = "Cutter rotary speed on fin mill." },
            new { Code = "PRM-016", Name = "Tube Thickness", Desc = "Wall thickness of radiator tubes." },
            new { Code = "PRM-017", Name = "Tube Width", Desc = "Width of formed tube mill profile." },
            new { Code = "PRM-018", Name = "Welding Current", Desc = "High frequency induction welding current." },
            new { Code = "PRM-019", Name = "Welding Voltage", Desc = "High frequency induction welding voltage." },
            new { Code = "PRM-020", Name = "Sizing Roller Pressure", Desc = "Pressure applied by tube sizing rollers." },
            new { Code = "PRM-021", Name = "Pre-heating Zone Temp", Desc = "Furnace pre-heating chamber temperature." },
            new { Code = "PRM-022", Name = "Dew Point Level", Desc = "Dew point measurement inside brazing chamber." },
            new { Code = "PRM-023", Name = "Nitrogen Gas Flow Rate", Desc = "Nitrogen gas flow rate inside brazing furnace." },
            new { Code = "PRM-024", Name = "Clinching Speed", Desc = "Clinching/crimping tool feed speed." },
            new { Code = "PRM-025", Name = "Gasket Compression Rate", Desc = "Compression percentage of tank sealing gasket." },
            new { Code = "PRM-026", Name = "Chamber Temperature", Desc = "Testing chamber ambient temperature." },
            new { Code = "PRM-027", Name = "Test Cycle Time", Desc = "Total time taken for leakage test cycle." },
            new { Code = "PRM-028", Name = "Core Assembly Press Force", Desc = "Clamping force of core assembly fixture." },
            new { Code = "PRM-029", Name = "Core Height Deviation", Desc = "Deviation from nominal radiator core height." },
            new { Code = "PRM-030", Name = "Core Width Deviation", Desc = "Deviation from nominal radiator core width." },
            new { Code = "PRM-031", Name = "Sheet Metal Thickness", Desc = "Thickness of stamping sheet metal coils." },
            new { Code = "PRM-032", Name = "Die Cushion Pressure", Desc = "Press cushion pressure on stamping machine." },
            new { Code = "PRM-033", Name = "Lubricant Viscosity", Desc = "Viscosity of drawing oil for stamping dies." },
            new { Code = "PRM-034", Name = "Press Stroke Speed", Desc = "Stamping press machine stroke rate." },
            new { Code = "PRM-035", Name = "Header Plate Pitch", Desc = "Pitch of tube slots on header plate." },
            new { Code = "PRM-036", Name = "Side Plate Tension Force", Desc = "Tension force applied to side plates during assembly." },
            new { Code = "PRM-037", Name = "Brazing Paste Volume", Desc = "Volume of brazing paste dispensed per core." },
            new { Code = "PRM-038", Name = "Flux Spray Coverage", Desc = "Percentage of flux coverage on radiator core." },
            new { Code = "PRM-039", Name = "Flux Concentration", Desc = "Concentration of active flux solution." },
            new { Code = "PRM-040", Name = "Drying Oven Temp", Desc = "Temperature of drying oven post flux application." },
            new { Code = "PRM-041", Name = "Solder Melt Temp", Desc = "Melting point of solder alloy." },
            new { Code = "PRM-042", Name = "Solder Bath Level", Desc = "Level of molten solder in dip tank." },
            new { Code = "PRM-043", Name = "Air Leak Test Stabilization Time", Desc = "Stabilization delay during air leak test." },
            new { Code = "PRM-044", Name = "Water Bath Immersion Time", Desc = "Immersion duration for bubble leak test." },
            new { Code = "PRM-045", Name = "Burst Pressure", Desc = "Maximum burst pressure tolerance limit." },
            new { Code = "PRM-046", Name = "Fan Assembly Torque", Desc = "Torque applied on cooling fan bolts." },
            new { Code = "PRM-047", Name = "Shroud Attachment Clip Force", Desc = "Clip lock force of fan shroud." },
            new { Code = "PRM-048", Name = "Radiator Weight", Desc = "Total empty dry weight of the radiator." },
            new { Code = "PRM-049", Name = "Paint Coating Thickness", Desc = "Dry film thickness of protective paint." },
            new { Code = "PRM-050", Name = "Final Inspection Visual Score", Desc = "Visual quality score from automated vision system." }
        };
        
        foreach (var dp in dummyParams)
        {
            var exists = await _parameterRepository.ExistsAsync(p => p.Code == dp.Code, cancellationToken);
            
            if (!exists)
            {
                var parameter = new Parameter
                {
                    Code = dp.Code,
                    Name = dp.Name,
                    Description = dp.Desc,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _parameterRepository.AddAsync(parameter, cancellationToken);
                createdCount++;
            }
        }

        if (createdCount > 0)
        {
            await _parameterRepository.SaveChangesAsync(cancellationToken);
        }

        return ResponseFormatter.Success(message: $"{createdCount} radiator manufacturing dummy parameters seeded successfully.");
    }

    /// <summary>Seed processes and parameters cleanly, linking them together.</summary>
    [HttpPost("seed-process-parameters")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedProcessParameters(CancellationToken cancellationToken)
    {
        // 1. Delete all existing processes & parameters
        var existingProcesses = await _processRepository.GetAllAsync(cancellationToken);
        _processRepository.RemoveRange(existingProcesses);

        var existingParameters = await _parameterRepository.GetAllAsync(cancellationToken);
        _parameterRepository.RemoveRange(existingParameters);

        await _processRepository.SaveChangesAsync(cancellationToken);

        // 2. Define the unified radiator manufacturing seed data
        var seedData = new[]
        {
            new
            {
                ProcCode = "PRC-001",
                ProcName = "Stamping Press",
                ProcDesc = "Precision stamping machine forming radiator header plates and side plates.",
                Params = new[]
                {
                    new { Code = "PRM-010", Name = "Pressing Force", Desc = "Stamping press machine force for header plates." },
                    new { Code = "PRM-031", Name = "Sheet Metal Thickness", Desc = "Thickness of stamping sheet metal coils." },
                    new { Code = "PRM-032", Name = "Die Cushion Pressure", Desc = "Press cushion pressure on stamping machine." },
                    new { Code = "PRM-034", Name = "Press Stroke Speed", Desc = "Stamping press machine stroke rate." }
                }
            },
            new
            {
                ProcCode = "PRC-002",
                ProcName = "Fin Mill",
                ProcDesc = "High-speed fin forming mill creating radiator corrugated fins.",
                Params = new[]
                {
                    new { Code = "PRM-011", Name = "Fin Height", Desc = "Height of the generated radiator fin." },
                    new { Code = "PRM-012", Name = "Fin Pitch", Desc = "Fin pitch distance on fin mill." },
                    new { Code = "PRM-013", Name = "Fin Width", Desc = "Measured width of the radiator fin." },
                    new { Code = "PRM-015", Name = "Fin Cutter Speed", Desc = "Cutter rotary speed on fin mill." }
                }
            },
            new
            {
                ProcCode = "PRC-003",
                ProcName = "Tube Mill",
                ProcDesc = "High-frequency induction tube mill forming and welding flat tubes.",
                Params = new[]
                {
                    new { Code = "PRM-001", Name = "Welding Temperature", Desc = "Tube Mill welding temperature for core tubes." },
                    new { Code = "PRM-016", Name = "Tube Thickness", Desc = "Wall thickness of radiator tubes." },
                    new { Code = "PRM-017", Name = "Tube Width", Desc = "Width of formed tube mill profile." },
                    new { Code = "PRM-018", Name = "Welding Current", Desc = "High frequency induction welding current." },
                    new { Code = "PRM-019", Name = "Welding Voltage", Desc = "High frequency induction welding voltage." }
                }
            },
            new
            {
                ProcCode = "PRC-004",
                ProcName = "Core Assembly",
                ProcDesc = "Semiautomatic assembly of tubes, fins, header plates, and side plates.",
                Params = new[]
                {
                    new { Code = "PRM-028", Name = "Core Assembly Press Force", Desc = "Clamping force of core assembly fixture." },
                    new { Code = "PRM-029", Name = "Core Height Deviation", Desc = "Deviation from nominal radiator core height." },
                    new { Code = "PRM-030", Name = "Core Width Deviation", Desc = "Deviation from nominal radiator core width." }
                }
            },
            new
            {
                ProcCode = "PRC-005",
                ProcName = "Brazing Furnace",
                ProcDesc = "Controlled Atmosphere Brazing (CAB) furnace melting clad layers to join parts.",
                Params = new[]
                {
                    new { Code = "PRM-002", Name = "Oxygen Level", Desc = "Brazing furnace oxygen level to prevent oxidation." },
                    new { Code = "PRM-003", Name = "Furnace Temperature Zone 1", Desc = "Brazing furnace heating zone 1 temperature." },
                    new { Code = "PRM-004", Name = "Furnace Temperature Zone 2", Desc = "Brazing furnace heating zone 2 temperature." },
                    new { Code = "PRM-005", Name = "Brazing Belt Speed", Desc = "Speed of core transport through brazing furnace." },
                    new { Code = "PRM-023", Name = "Nitrogen Gas Flow Rate", Desc = "Nitrogen gas flow rate inside brazing furnace." }
                }
            },
            new
            {
                ProcCode = "PRC-006",
                ProcName = "Tank Assembly",
                ProcDesc = "Clinching plastic tanks onto aluminum cores with rubber sealing gaskets.",
                Params = new[]
                {
                    new { Code = "PRM-006", Name = "Crimping Force", Desc = "Clinching/crimping force for tank-to-core assembly." },
                    new { Code = "PRM-007", Name = "Crimping Depth", Desc = "Clinching/crimping depth measurement." },
                    new { Code = "PRM-024", Name = "Clinching Speed", Desc = "Clinching/crimping tool feed speed." },
                    new { Code = "PRM-025", Name = "Gasket Compression Rate", Desc = "Compression percentage of tank sealing gasket." }
                }
            },
            new
            {
                ProcCode = "PRC-007",
                ProcName = "Leakage Testing",
                ProcDesc = "High-sensitivity dry air leak testing and differential pressure decay test.",
                Params = new[]
                {
                    new { Code = "PRM-008", Name = "Test Air Pressure", Desc = "Radiator air pressure used in leakage test." },
                    new { Code = "PRM-009", Name = "Leak Rate", Desc = "Radiator leak rate measured in leakage test." },
                    new { Code = "PRM-026", Name = "Chamber Temperature", Desc = "Testing chamber ambient temperature." },
                    new { Code = "PRM-027", Name = "Test Cycle Time", Desc = "Total time taken for leakage test cycle." }
                }
            }
        };

        var parameterCache = new Dictionary<string, Parameter>();

        foreach (var data in seedData)
        {
            var process = new Process
            {
                Code = data.ProcCode,
                Name = data.ProcName,
                Description = data.ProcDesc,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _processRepository.AddAsync(process, cancellationToken);

            foreach (var dp in data.Params)
            {
                if (!parameterCache.TryGetValue(dp.Code, out var parameter))
                {
                    parameter = new Parameter
                    {
                        Code = dp.Code,
                        Name = dp.Name,
                        Description = dp.Desc,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _parameterRepository.AddAsync(parameter, cancellationToken);
                    parameterCache[dp.Code] = parameter;
                }

                process.ProcessParameters.Add(new ProcessParameter
                {
                    Process = process,
                    Parameter = parameter
                });
            }
        }

        await _processRepository.SaveChangesAsync(cancellationToken);

        return ResponseFormatter.Success(message: "Unified processes and parameters successfully cleaned and seeded together.");
    }
}
