using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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
    private readonly IStockInRepository _stockInRepository;
    private readonly IPartRepository _partRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly IAppConfigRepository _appConfigRepository;
    private readonly IProcessLogRepository _processLogRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AppDbContext _context;

    public ConfigController(
        IAuthService authService,
        IUserRepository userRepository,
        IPartService partService,
        IProcessRepository processRepository,
        IParameterRepository parameterRepository,
        IStockInRepository stockInRepository,
        IPartRepository partRepository,
        IPrinterRepository printerRepository,
        IAppConfigRepository appConfigRepository,
        IProcessLogRepository processLogRepository,
        IIssueRepository issueRepository,
        IRefreshTokenRepository refreshTokenRepository,
        AppDbContext context)
    {
        _authService = authService;
        _userRepository = userRepository;
        _partService = partService;
        _processRepository = processRepository;
        _parameterRepository = parameterRepository;
        _stockInRepository = stockInRepository;
        _partRepository = partRepository;
        _printerRepository = printerRepository;
        _appConfigRepository = appConfigRepository;
        _processLogRepository = processLogRepository;
        _issueRepository = issueRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _context = context;
    }

    /// <summary>Reset all master data (Process, Parameter, Process Log) and their relations.</summary>
    [HttpPost("reset-master-data")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetMasterData(CancellationToken cancellationToken)
    {
        // 1. Delete all existing process logs & details
        var logs = await _processLogRepository.GetAllAsync(cancellationToken);
        _processLogRepository.RemoveRange(logs);

        // 2. Delete all existing process parameters (join table)
        var processParams = await _context.ProcessParameters.ToListAsync(cancellationToken);
        _context.ProcessParameters.RemoveRange(processParams);

        // 3. Delete all existing processes
        var existingProcesses = await _processRepository.GetAllAsync(cancellationToken);
        _processRepository.RemoveRange(existingProcesses);

        // 4. Delete all existing parameters
        var existingParameters = await _parameterRepository.GetAllAsync(cancellationToken);
        _parameterRepository.RemoveRange(existingParameters);

        await _processRepository.SaveChangesAsync(cancellationToken);

        return ResponseFormatter.Success(message: "All master data (Process, Parameter, Process Log) and their relations have been successfully reset.");
    }

    /// <summary>Seed specific processes and parameters for TRSS Traceability System.</summary>
    [HttpPost("seed-trss-master-data")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedTrssMasterData(CancellationToken cancellationToken)
    {
        // 1. Reset existing data first
        await ResetMasterData(cancellationToken);

        // 2. Define the TRSS Traceability seed data
        var seedData = new[]
        {
            new
            {
                ProcCode = "CLINCHING_SHORT_SIDE",
                ProcName = "CLINCHING SHORT SIDE",
                ProcDesc = "Process for clinching the short side of radiator.",
                Params = new[]
                {
                    new { Code = "CORE_ASM_RESULT", Name = "Core Asm", Type = "boolean" },
                    new { Code = "UPPER_TANK_ASM_RESULT", Name = "Upper Tank Asm Result", Type = "boolean" },
                    new { Code = "LOWER_TANK_ASM_RESULT", Name = "Lower Tank Asm Result", Type = "boolean" }
                }
            },
            new
            {
                ProcCode = "CLINCHING_LONG_SIDE",
                ProcName = "Clincing long side",
                ProcDesc = "Process for clinching the long side of radiator.",
                Params = new[]
                {
                    new { Code = "CLINCHING_HEIGHT_RESULT", Name = "Clinching Height Result", Type = "boolean" },
                    new { Code = "CLINCHING_HEIGHT_VALUE", Name = "Clinching Height Value", Type = "number" },
                    new { Code = "END_PLATE_WIDTH_VALUE", Name = "End Plate Width Value", Type = "number" }
                }
            },
            new
            {
                ProcCode = "HE_LEAK",
                ProcName = "He Leak",
                ProcDesc = "Helium leak testing process.",
                Params = new[]
                {
                    new { Code = "CAP_TYPE_POSITION_RESULT", Name = "Cap Type & Position", Type = "boolean" },
                    new { Code = "LEAK_TEST_RESULT", Name = "Leak Test Result", Type = "boolean" },
                    new { Code = "LEAK_VALUE", Name = "Leak Value", Type = "number" }
                }
            },
            new
            {
                ProcCode = "M_FAN_ASSY",
                ProcName = "M Fan Assy",
                ProcDesc = "Main fan assembly process.",
                Params = new[]
                {
                    new { Code = "FAN_ASM_RESULT", Name = "Fan Asm Result", Type = "boolean" },
                    new { Code = "MOTOR_ASM_RESULT", Name = "Motor Asm Result", Type = "boolean" },
                    new { Code = "FUN_GUIDE_ASM_RESULT", Name = "Fun Guide Asm Result", Type = "boolean" },
                    new { Code = "BOLT_TIGHTEN_RESULT", Name = "Bolt tighten result", Type = "boolean" },
                    new { Code = "BOLT_TIGHTEN_VALUE", Name = "Bold Tighten Value", Type = "number" },
                    new { Code = "NUT_TIGHTEN_RESULT", Name = "Nut Tighten Result", Type = "boolean" }
                }
            },
            new
            {
                ProcCode = "M_FAN_INSPECTION",
                ProcName = "M Fan Characteristics Inspection",
                ProcDesc = "Inspection of main fan operational characteristics.",
                Params = new[]
                {
                    new { Code = "M_FAN_TEST_RESULT", Name = "M Fan Test Result", Type = "boolean" },
                    new { Code = "M_FAN_INSPECTION_ROTATION_SPEED_VALUE", Name = "M Fan Inspection Rotation Speed Value", Type = "number" },
                    new { Code = "M_FAN_INSPECTION_AMPERE_VALUE", Name = "M Fan Inspection Amperage Value", Type = "number" },
                    new { Code = "M_FAN_INSPECTION_WIND_DIRECTION_VALUE", Name = "M Fan Inspection Wind Direction Value", Type = "number" }
                }
            },
            new
            {
                ProcCode = "ECM_ASSY",
                ProcName = "Ecm Assy",
                ProcDesc = "Electronic Control Module assembly process.",
                Params = new[]
                {
                    new { Code = "RAD_CORE_ASM_NAME_LABEL_RESULT", Name = "Rad Core Asm Name Label Result", Type = "boolean" },
                    new { Code = "MOTOR_FAN_ASSY_LABEL_RESULT", Name = "Motor Fan Assy Label Result", Type = "boolean" },
                    new { Code = "ECM_ASSY_BOLT_TIGHTEN_VALUE", Name = "ECM Assy Bolt Tighten Result", Type = "number" },
                    new { Code = "ECM_ASSY_BOLT_TIGHTEN_RESULT", Name = "ECM Assy Nut Tighten Result", Type = "boolean" }
                }
            },
            new
            {
                ProcCode = "FINAL_INSPECTION",
                ProcName = "Final Inspection",
                ProcDesc = "Final quality gate and inspection.",
                Params = new[]
                {
                    new { Code = "FINAL_INSPECTION_RAD_CORE_ASM_NAME_LABEL_RESULT", Name = "Final Inspection Rad Core Asm Name Label Result", Type = "boolean" },
                    new { Code = "ALL_CHECK_POINT_RESULT", Name = "All Check Point Result", Type = "boolean" }
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
                        DataType = dp.Type,
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

        return ResponseFormatter.Success(message: "TRSS master data (Processes and Parameters) successfully seeded.");
    }

    /// <summary>Seed exactly 1 dummy process log with full details (all processes, many values) for TRSS.</summary>
    [HttpPost("seed-process-logs")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedProcessLogs(CancellationToken cancellationToken)
    {
        // 1. Delete all existing process logs first to keep it clean
        var logs = await _processLogRepository.GetAllAsync(cancellationToken);
        _processLogRepository.RemoveRange(logs);
        await _processLogRepository.SaveChangesAsync(cancellationToken);

        // 2. Get all processes and parameters for reference
        var processes = await _context.Processes
            .Include(p => p.ProcessParameters)
            .ThenInclude(pp => pp.Parameter)
            .ToListAsync(cancellationToken);

        if (!processes.Any())
        {
            return ResponseFormatter.Error("No processes found. Please run seed-trss-master-data first.");
        }

        var random = new Random();

        // 3. Create exactly ONE comprehensive process log
        var issueNo = $"ISS-{DateTime.UtcNow:yyyyMMdd}-0001";

        var processLog = new ProcessLog
        {
            IssueNo = issueNo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _processLogRepository.AddAsync(processLog, cancellationToken);

        // 4. Fill with details for EVERY process
        foreach (var process in processes)
        {
            // For each parameter in that process
            foreach (var procParam in process.ProcessParameters)
            {
                var param = procParam.Parameter;
                if (param == null) continue;

                // Create 10-15 values per parameter as requested
                int valueCount = random.Next(10, 16);
                for (int v = 0; v < valueCount; v++)
                {
                    var detail = new ProcessLogDetail
                    {
                        ProcessLog = processLog,
                        ProcessId = process.Id,
                        ParameterId = param.Id,
                        CreatedAt = processLog.CreatedAt.AddMinutes(random.Next(1, 120))
                    };

                    if (param.DataType.ToLower() == "boolean")
                    {
                        detail.ValueBoolean = random.Next(0, 10) > 1; // 90% chance of true/OK
                    }
                    else if (param.DataType.ToLower() == "number")
                    {
                        // Generate logical random numbers based on parameter names
                        if (param.Code.Contains("TEMP")) detail.ValueNumber = (decimal)(170 + random.NextDouble() * 20);
                        else if (param.Code.Contains("VOLTAGE")) detail.ValueNumber = (decimal)(215 + random.NextDouble() * 10);
                        else if (param.Code.Contains("TORQUE")) detail.ValueNumber = (decimal)(10 + random.NextDouble() * 5);
                        else detail.ValueNumber = (decimal)(random.NextDouble() * 100);
                    }
                    else
                    {
                        detail.ValueText = "Data-" + random.Next(1000, 9999);
                    }

                    processLog.Details.Add(detail);
                }
            }
        }

        await _processLogRepository.SaveChangesAsync(cancellationToken);

        return ResponseFormatter.Success(message: $"Single comprehensive process log [{issueNo}] with all 7 processes and 10-15 values per parameter seeded successfully.");
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
            new { Code = "PRM-001", Name = "Welding Temperature", Desc = "Tube Mill welding temperature for core tubes.", Type = "number" },
            new { Code = "PRM-002", Name = "Oxygen Level", Desc = "Brazing furnace oxygen level to prevent oxidation.", Type = "number" },
            new { Code = "PRM-003", Name = "Furnace Temperature Zone 1", Desc = "Brazing furnace heating zone 1 temperature.", Type = "number" },
            new { Code = "PRM-004", Name = "Furnace Temperature Zone 2", Desc = "Brazing furnace heating zone 2 temperature.", Type = "number" },
            new { Code = "PRM-005", Name = "Brazing Belt Speed", Desc = "Speed of core transport through brazing furnace.", Type = "number" },
            new { Code = "PRM-006", Name = "Crimping Force", Desc = "Clinching/crimping force for tank-to-core assembly.", Type = "number" },
            new { Code = "PRM-007", Name = "Crimping Depth", Desc = "Clinching/crimping depth measurement.", Type = "number" },
            new { Code = "PRM-008", Name = "Test Air Pressure", Desc = "Radiator air pressure used in leakage test.", Type = "number" },
            new { Code = "PRM-009", Name = "Leak Rate", Desc = "Radiator leak rate measured in leakage test.", Type = "number" },
            new { Code = "PRM-010", Name = "Pressing Force", Desc = "Stamping press machine force for header plates.", Type = "number" },
            new { Code = "PRM-011", Name = "Fin Height", Desc = "Height of the generated radiator fin.", Type = "number" },
            new { Code = "PRM-012", Name = "Fin Pitch", Desc = "Fin pitch distance on fin mill.", Type = "number" },
            new { Code = "PRM-013", Name = "Fin Width", Desc = "Measured width of the radiator fin.", Type = "number" },
            new { Code = "PRM-014", Name = "Fin Forming Lubricant Flow", Desc = "Flow rate of fin forming lubricant oil.", Type = "number" },
            new { Code = "PRM-015", Name = "Fin Cutter Speed", Desc = "Cutter rotary speed on fin mill.", Type = "number" },
            new { Code = "PRM-016", Name = "Tube Thickness", Desc = "Wall thickness of radiator tubes.", Type = "number" },
            new { Code = "PRM-017", Name = "Tube Width", Desc = "Width of formed tube mill profile.", Type = "number" },
            new { Code = "PRM-018", Name = "Welding Current", Desc = "High frequency induction welding current.", Type = "number" },
            new { Code = "PRM-019", Name = "Welding Voltage", Desc = "High frequency induction welding voltage.", Type = "number" },
            new { Code = "PRM-020", Name = "Sizing Roller Pressure", Desc = "Pressure applied by tube sizing rollers.", Type = "number" },
            new { Code = "PRM-021", Name = "Pre-heating Zone Temp", Desc = "Furnace pre-heating chamber temperature.", Type = "number" },
            new { Code = "PRM-022", Name = "Dew Point Level", Desc = "Dew point measurement inside brazing chamber.", Type = "number" },
            new { Code = "PRM-023", Name = "Nitrogen Gas Flow Rate", Desc = "Nitrogen gas flow rate inside brazing furnace.", Type = "number" },
            new { Code = "PRM-024", Name = "Clinching Speed", Desc = "Clinching/crimping tool feed speed.", Type = "number" },
            new { Code = "PRM-025", Name = "Gasket Compression Rate", Desc = "Compression percentage of tank sealing gasket.", Type = "number" },
            new { Code = "PRM-026", Name = "Chamber Temperature", Desc = "Testing chamber ambient temperature.", Type = "number" },
            new { Code = "PRM-027", Name = "Test Cycle Time", Desc = "Total time taken for leakage test cycle.", Type = "number" },
            new { Code = "PRM-028", Name = "Core Assembly Press Force", Desc = "Clamping force of core assembly fixture.", Type = "number" },
            new { Code = "PRM-029", Name = "Core Height Deviation", Desc = "Deviation from nominal radiator core height.", Type = "number" },
            new { Code = "PRM-030", Name = "Core Width Deviation", Desc = "Deviation from nominal radiator core width.", Type = "number" },
            new { Code = "PRM-031", Name = "Sheet Metal Thickness", Desc = "Thickness of stamping sheet metal coils.", Type = "number" },
            new { Code = "PRM-032", Name = "Die Cushion Pressure", Desc = "Press cushion pressure on stamping machine.", Type = "number" },
            new { Code = "PRM-033", Name = "Lubricant Viscosity", Desc = "Viscosity of drawing oil for stamping dies.", Type = "number" },
            new { Code = "PRM-034", Name = "Press Stroke Speed", Desc = "Stamping press machine stroke rate.", Type = "number" },
            new { Code = "PRM-035", Name = "Header Plate Pitch", Desc = "Pitch of tube slots on header plate.", Type = "number" },
            new { Code = "PRM-036", Name = "Side Plate Tension Force", Desc = "Tension force applied to side plates during assembly.", Type = "number" },
            new { Code = "PRM-037", Name = "Brazing Paste Volume", Desc = "Volume of brazing paste dispensed per core.", Type = "number" },
            new { Code = "PRM-038", Name = "Flux Spray Coverage", Desc = "Percentage of flux coverage on radiator core.", Type = "number" },
            new { Code = "PRM-039", Name = "Flux Concentration", Desc = "Concentration of active flux solution.", Type = "number" },
            new { Code = "PRM-040", Name = "Drying Oven Temp", Desc = "Temperature of drying oven post flux application.", Type = "number" },
            new { Code = "PRM-041", Name = "Solder Melt Temp", Desc = "Melting point of solder alloy.", Type = "number" },
            new { Code = "PRM-042", Name = "Solder Bath Level", Desc = "Level of molten solder in dip tank.", Type = "number" },
            new { Code = "PRM-043", Name = "Air Leak Test Stabilization Time", Desc = "Stabilization delay during air leak test.", Type = "number" },
            new { Code = "PRM-044", Name = "Water Bath Immersion Time", Desc = "Immersion duration for bubble leak test.", Type = "number" },
            new { Code = "PRM-045", Name = "Burst Pressure", Desc = "Maximum burst pressure tolerance limit.", Type = "number" },
            new { Code = "PRM-046", Name = "Fan Assembly Torque", Desc = "Torque applied on cooling fan bolts.", Type = "number" },
            new { Code = "PRM-047", Name = "Shroud Attachment Clip Force", Desc = "Clip lock force of fan shroud.", Type = "number" },
            new { Code = "PRM-048", Name = "Radiator Weight", Desc = "Total empty dry weight of the radiator.", Type = "number" },
            new { Code = "PRM-049", Name = "Paint Coating Thickness", Desc = "Dry film thickness of protective paint.", Type = "number" },
            new { Code = "PRM-050", Name = "Leakage Test Result", Desc = "Final result of the leakage test (OK/NG).", Type = "boolean" }
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
                    DataType = dp.Type,
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
                    new { Code = "PRM-010", Name = "Pressing Force", Desc = "Stamping press machine force for header plates.", Type = "number" },
                    new { Code = "PRM-031", Name = "Sheet Metal Thickness", Desc = "Thickness of stamping sheet metal coils.", Type = "number" },
                    new { Code = "PRM-032", Name = "Die Cushion Pressure", Desc = "Press cushion pressure on stamping machine.", Type = "number" },
                    new { Code = "PRM-034", Name = "Press Stroke Speed", Desc = "Stamping press machine stroke rate.", Type = "number" }
                }
            },
            new
            {
                ProcCode = "PRC-002",
                ProcName = "Fin Mill",
                ProcDesc = "High-speed fin forming mill creating radiator corrugated fins.",
                Params = new[]
                {
                    new { Code = "PRM-011", Name = "Fin Height", Desc = "Height of the generated radiator fin.", Type = "number" },
                    new { Code = "PRM-012", Name = "Fin Pitch", Desc = "Fin pitch distance on fin mill.", Type = "number" },
                    new { Code = "PRM-013", Name = "Fin Width", Desc = "Measured width of the radiator fin.", Type = "number" },
                    new { Code = "PRM-015", Name = "Fin Cutter Speed", Desc = "Cutter rotary speed on fin mill.", Type = "number" }
                }
            },
            new
            {
                ProcCode = "PRC-003",
                ProcName = "Tube Mill",
                ProcDesc = "High-frequency induction tube mill forming and welding flat tubes.",
                Params = new[]
                {
                    new { Code = "PRM-001", Name = "Welding Temperature", Desc = "Tube Mill welding temperature for core tubes.", Type = "number" },
                    new { Code = "PRM-016", Name = "Tube Thickness", Desc = "Wall thickness of radiator tubes.", Type = "number" },
                    new { Code = "PRM-017", Name = "Tube Width", Desc = "Width of formed tube mill profile.", Type = "number" },
                    new { Code = "PRM-018", Name = "Welding Current", Desc = "High frequency induction welding current.", Type = "number" },
                    new { Code = "PRM-019", Name = "Welding Voltage", Desc = "High frequency induction welding voltage.", Type = "number" }
                }
            },
            new
            {
                ProcCode = "PRC-004",
                ProcName = "Core Assembly",
                ProcDesc = "Semiautomatic assembly of tubes, fins, header plates, and side plates.",
                Params = new[]
                {
                    new { Code = "PRM-028", Name = "Core Assembly Press Force", Desc = "Clamping force of core assembly fixture.", Type = "number" },
                    new { Code = "PRM-029", Name = "Core Height Deviation", Desc = "Deviation from nominal radiator core height.", Type = "number" },
                    new { Code = "PRM-030", Name = "Core Width Deviation", Desc = "Deviation from nominal radiator core width.", Type = "number" }
                }
            },
            new
            {
                ProcCode = "PRC-005",
                ProcName = "Brazing Furnace",
                ProcDesc = "Controlled Atmosphere Brazing (CAB) furnace melting clad layers to join parts.",
                Params = new[]
                {
                    new { Code = "PRM-002", Name = "Oxygen Level", Desc = "Brazing furnace oxygen level to prevent oxidation.", Type = "number" },
                    new { Code = "PRM-003", Name = "Furnace Temperature Zone 1", Desc = "Brazing furnace heating zone 1 temperature.", Type = "number" },
                    new { Code = "PRM-004", Name = "Furnace Temperature Zone 2", Desc = "Brazing furnace heating zone 2 temperature.", Type = "number" },
                    new { Code = "PRM-005", Name = "Brazing Belt Speed", Desc = "Speed of core transport through brazing furnace.", Type = "number" },
                    new { Code = "PRM-023", Name = "Nitrogen Gas Flow Rate", Desc = "Nitrogen gas flow rate inside brazing furnace.", Type = "number" }
                }
            },
            new
            {
                ProcCode = "PRC-006",
                ProcName = "Tank Assembly",
                ProcDesc = "Clinching plastic tanks onto aluminum cores with rubber sealing gaskets.",
                Params = new[]
                {
                    new { Code = "PRM-006", Name = "Crimping Force", Desc = "Clinching/crimping force for tank-to-core assembly.", Type = "number" },
                    new { Code = "PRM-007", Name = "Crimping Depth", Desc = "Clinching/crimping depth measurement.", Type = "number" },
                    new { Code = "PRM-024", Name = "Clinching Speed", Desc = "Clinching/crimping tool feed speed.", Type = "number" },
                    new { Code = "PRM-025", Name = "Gasket Compression Rate", Desc = "Compression percentage of tank sealing gasket.", Type = "number" }
                }
            },
            new
            {
                ProcCode = "PRC-007",
                ProcName = "Leakage Testing",
                ProcDesc = "High-sensitivity dry air leak testing and differential pressure decay test.",
                Params = new[]
                {
                    new { Code = "PRM-008", Name = "Test Air Pressure", Desc = "Radiator air pressure used in leakage test.", Type = "number" },
                    new { Code = "PRM-009", Name = "Leak Rate", Desc = "Radiator leak rate measured in leakage test.", Type = "number" },
                    new { Code = "PRM-026", Name = "Chamber Temperature", Desc = "Testing chamber ambient temperature.", Type = "number" },
                    new { Code = "PRM-027", Name = "Test Cycle Time", Desc = "Total time taken for leakage test cycle.", Type = "number" },
                    new { Code = "PRM-050", Name = "Leakage Test Result", Desc = "Final result of the leakage test (OK/NG).", Type = "boolean" }
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
                        DataType = dp.Type,
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

    /// <summary>Seed StockIn records with associated Issue arrays.</summary>
    [HttpPost("seed-stockins")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedStockIns(CancellationToken cancellationToken)
    {
        // 1. Delete all existing StockIn records
        var existingStockIns = await _stockInRepository.GetAllAsync(cancellationToken);
        _stockInRepository.RemoveRange(existingStockIns);
        await _stockInRepository.SaveChangesAsync(cancellationToken);

        // 2. Ensure we have at least one Part to associate with StockIn
        var parts = await _partRepository.GetAllAsync(cancellationToken);
        var part = parts.FirstOrDefault();
        if (part == null)
        {
            part = new Part
            {
                Number = "PN-SEED-01",
                Name = "Default Radiator Bracket Assembly",
                Description = "Auto-generated default part for stock-in seeding.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _partRepository.AddAsync(part, cancellationToken);
            await _partRepository.SaveChangesAsync(cancellationToken);
        }

        // 3. Seed 30 StockIn records, each with exactly 1 Issue
        // Re-fetch parts so the newly created default part is included
        var partList = (await _partRepository.GetAllAsync(cancellationToken)).ToList();

        var supplyQtys = new[] { 50, 100, 150, 200, 250, 300, 500, 750, 1000, 1200 };
        var random = new Random(42);

        for (int i = 1; i <= 30; i++)
        {
            var partForThisEntry = partList.Count > 1
                ? partList[(i - 1) % partList.Count]
                : part;

            var supplyQty = supplyQtys[random.Next(supplyQtys.Length)];
            var receiptQty = supplyQty - random.Next(0, 5);
            var daysAgo = random.Next(1, 60);

            var stockIn = new StockIn
            {
                Code = $"STI-{i:D5}",
                PartId = partForThisEntry.Id,
                SupplyQty = supplyQty,
                SupplyDate = DateTime.UtcNow.AddDays(-daysAgo),
                ReceiptQty = receiptQty,
                ReceiptDate = DateTime.UtcNow.AddDays(-daysAgo).AddHours(random.Next(1, 8)),
                CreatedAt = DateTime.UtcNow
            };

            await _stockInRepository.AddAsync(stockIn, cancellationToken);

            // Exactly 1 issue per stock-in
            stockIn.Issues.Add(new Issue
            {
                Number = $"ISS-{i:D5}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _stockInRepository.SaveChangesAsync(cancellationToken);

        return ResponseFormatter.Success(message: "Successfully seeded 30 dummy stock-in records, each with exactly 1 issue.");
    }

    /// <summary>Seed 10 dummy printers.</summary>
    [HttpPost("seed-printers")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedPrinters(CancellationToken cancellationToken)
    {
        int createdCount = 0;

        var dummyPrinters = new[]
        {
            new { Name = "Printer-Line-01", IpAddress = "192.168.1.101", Port = 9100, Desc = "Label printer for Stamping Press line 1." },
            new { Name = "Printer-Line-02", IpAddress = "192.168.1.102", Port = 9100, Desc = "Label printer for Fin Mill line 2." },
            new { Name = "Printer-Line-03", IpAddress = "192.168.1.103", Port = 9100, Desc = "Label printer for Tube Mill line 3." },
            new { Name = "Printer-Line-04", IpAddress = "192.168.1.104", Port = 9100, Desc = "Label printer for Core Assembly station." },
            new { Name = "Printer-Line-05", IpAddress = "192.168.1.105", Port = 9100, Desc = "Label printer for Brazing Furnace exit." },
            new { Name = "Printer-Line-06", IpAddress = "192.168.1.106", Port = 9100, Desc = "Label printer for Tank Assembly station." },
            new { Name = "Printer-Line-07", IpAddress = "192.168.1.107", Port = 9100, Desc = "Label printer for Leakage Testing area." },
            new { Name = "Printer-Line-08", IpAddress = "192.168.1.108", Port = 9100, Desc = "Label printer for Final Inspection gate." },
            new { Name = "Printer-Line-09", IpAddress = "192.168.1.109", Port = 9100, Desc = "Label printer for Packaging station." },
            new { Name = "Printer-Line-10", IpAddress = "192.168.1.110", Port = 9100, Desc = "Label printer for Shipping dock." },
        };

        foreach (var dp in dummyPrinters)
        {
            var exists = await _printerRepository.ExistsAsync(p => p.IpAddress == dp.IpAddress, cancellationToken);

            if (!exists)
            {
                var printer = new Printer
                {
                    Name = dp.Name,
                    IpAddress = dp.IpAddress,
                    Port = dp.Port,
                    Description = dp.Desc,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _printerRepository.AddAsync(printer, cancellationToken);
                createdCount++;
            }
        }

        if (createdCount > 0)
        {
            await _printerRepository.SaveChangesAsync(cancellationToken);
        }

        return ResponseFormatter.Success(message: $"{createdCount} dummy printers seeded successfully.");
    }

    /// <summary>Seed initial AppConfigs for printers.</summary>
    [HttpPost("seed-appconfigs")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedAppConfigs(CancellationToken cancellationToken)
    {
        int createdCount = 0;

        // Get some printers to use as values
        var printers = (await _printerRepository.GetAllAsync(cancellationToken)).ToList();

        var stockInPrinter = printers.FirstOrDefault(p => p.Name.Contains("Line-10"))?.Name ?? "Printer-Line-10";
        var line1Printer = printers.FirstOrDefault(p => p.Name.Contains("Line-01"))?.Name ?? "Printer-Line-01";
        var line2Printer = printers.FirstOrDefault(p => p.Name.Contains("Line-02"))?.Name ?? "Printer-Line-02";

        var dummyConfigs = new[]
        {
            new { Key = "PRINTER_NAME_STOCK_IN", Value = stockInPrinter, Desc = "Printer name for Stock In process." },
            new { Key = "PRINTER_NAME_LINE_1", Value = line1Printer, Desc = "Printer name for Line 1 production." },
            new { Key = "PRINTER_NAME_LINE_2", Value = line2Printer, Desc = "Printer name for Line 2 production." },
        };

        foreach (var dc in dummyConfigs)
        {
            var config = await _appConfigRepository.GetByKeyAsync(dc.Key, cancellationToken);

            if (config == null)
            {
                config = new AppConfig
                {
                    Key = dc.Key,
                    Value = dc.Value,
                    Description = dc.Desc,
                    CreatedAt = DateTime.UtcNow
                };

                await _appConfigRepository.AddAsync(config, cancellationToken);
                createdCount++;
            }
            else
            {
                // Update value if already exists to match requirement
                config.Value = dc.Value;
                _appConfigRepository.Update(config);
                createdCount++;
            }
        }

        if (createdCount > 0)
        {
            await _appConfigRepository.SaveChangesAsync(cancellationToken);
        }

        return ResponseFormatter.Success(message: $"{createdCount} app configs seeded/updated successfully.");
    }

    /// <summary>Clear all data from the database (Dangerous! Resets IDs).</summary>
    [HttpDelete("clear-all-data")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearAllData(CancellationToken cancellationToken)
    {
        // 1. Disable Foreign Key Checks
        await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;", cancellationToken);

        try
        {
            // 2. Delete data from Tables (DELETE works with FOREIGN_KEY_CHECKS=0 while TRUNCATE sometimes doesn't)
            var tables = new[]
            {
                "process_log_details",
                "process_logs",
                "issues",
                "stock_ins",
                "process_parameters",
                "parameters",
                "processes",
                "parts",
                "printers",
                "app_configs",
                "refresh_tokens",
                "users"
            };

            foreach (var table in tables)
            {
                // Delete all rows
                await _context.Database.ExecuteSqlRawAsync($"DELETE FROM `{table}`;", cancellationToken);
                // Reset Auto-increment
                await _context.Database.ExecuteSqlRawAsync($"ALTER TABLE `{table}` AUTO_INCREMENT = 1;", cancellationToken);
            }
        }
        finally
        {
            // 3. Re-enable Foreign Key Checks
            await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", cancellationToken);
        }

        return ResponseFormatter.Success(message: "All database tables cleared and IDs reset successfully.");
    }

    /// <summary>Run all seed methods in logical order after clearing everything.</summary>
    [HttpPost("seed-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedAll(CancellationToken cancellationToken)
    {
        // First, nuke everything to avoid FK conflicts
        await ClearAllData(cancellationToken);

        // Then seed everything back
        await SeedAdmin(cancellationToken);
        await SeedUsers(cancellationToken);
        await SeedParts(cancellationToken);
        await SeedPrinters(cancellationToken);
        await SeedAppConfigs(cancellationToken);
        await SeedProcessParameters(cancellationToken);
        await SeedStockIns(cancellationToken);
        await SeedProcessLogs(cancellationToken);

        return ResponseFormatter.Success(message: "All dummy data seeded successfully after full reset.");
    }
}
