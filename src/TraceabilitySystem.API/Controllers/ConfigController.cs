using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;


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
    private readonly ISerialNumberService _serialNumberService;
    private readonly IPrintService _printService;

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
       AppDbContext context,
       ISerialNumberService serialNumberService,
       IPrintService printService
       )
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
       _serialNumberService = serialNumberService;
       _printService = printService;
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
                   new { Code = "CORE_ASM_VALUE", Name = "Core Asm", Type = "string" },
                   new { Code = "UPPER_TANK_ASM_VALUE", Name = "Upper Tank Asm Result", Type = "string" },
                   new { Code = "LOWER_TANK_ASM_VALUE", Name = "Lower Tank Asm Result", Type = "string" },
                   new { Code = "O_RING_SET_RESULT", Name = "O-Ring Set", Type = "boolean" },
                   new { Code = "NG_BOX_SENSOR_SHORT_SIDE_VALUE", Name = "NG Box (red) Sensor Clinching Short Side", Type = "string"}
               }
           },
           new
           {
               ProcCode = "CLINCHING_LONG_SIDE",
               ProcName = "Clincing long side",
               ProcDesc = "Process for clinching the long side of radiator.",
               Params = new[]
               {
                   new { Code = "END_PLATE_WIDTH_1_RESULT", Name = "End Plate Width 1 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_2_RESULT", Name = "End Plate Width 2 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_3_RESULT", Name = "End Plate Width 3 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_4_RESULT", Name = "End Plate Width 4 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_5_RESULT", Name = "End Plate Width 5 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_6_RESULT", Name = "End Plate Width 6 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_7_RESULT", Name = "End Plate Width 7 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_8_RESULT", Name = "End Plate Width 8 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_9_RESULT", Name = "End Plate Width 9 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_10_RESULT", Name = "End Plate Width 10 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_11_RESULT", Name = "End Plate Width 11 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_12_RESULT", Name = "End Plate Width 12 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_13_RESULT", Name = "End Plate Width 13 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_14_RESULT", Name = "End Plate Width 14 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_15_RESULT", Name = "End Plate Width 15 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_16_RESULT", Name = "End Plate Width 16 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_17_RESULT", Name = "End Plate Width 17 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_18_RESULT", Name = "End Plate Width 18 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_19_RESULT", Name = "End Plate Width 19 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_20_RESULT", Name = "End Plate Width 20 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_21_RESULT", Name = "End Plate Width 21 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_22_RESULT", Name = "End Plate Width 22 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_23_RESULT", Name = "End Plate Width 23 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_24_RESULT", Name = "End Plate Width 24 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_25_RESULT", Name = "End Plate Width 25 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_26_RESULT", Name = "End Plate Width 26 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_27_RESULT", Name = "End Plate Width 27 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_28_RESULT", Name = "End Plate Width 28 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_29_RESULT", Name = "End Plate Width 29 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_30_RESULT", Name = "End Plate Width 30 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_31_RESULT", Name = "End Plate Width 31 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_32_RESULT", Name = "End Plate Width 32 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_33_RESULT", Name = "End Plate Width 33 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_34_RESULT", Name = "End Plate Width 34 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_35_RESULT", Name = "End Plate Width 35 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_36_RESULT", Name = "End Plate Width 36 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_37_RESULT", Name = "End Plate Width 37 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_38_RESULT", Name = "End Plate Width 38 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_39_RESULT", Name = "End Plate Width 39 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_40_RESULT", Name = "End Plate Width 40 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_41_RESULT", Name = "End Plate Width 41 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_42_RESULT", Name = "End Plate Width 42 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_43_RESULT", Name = "End Plate Width 43 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_44_RESULT", Name = "End Plate Width 44 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_45_RESULT", Name = "End Plate Width 45 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_46_RESULT", Name = "End Plate Width 46 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_47_RESULT", Name = "End Plate Width 47 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_48_RESULT", Name = "End Plate Width 48 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_49_RESULT", Name = "End Plate Width 49 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_50_RESULT", Name = "End Plate Width 50 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_51_RESULT", Name = "End Plate Width 51 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_52_RESULT", Name = "End Plate Width 52 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_53_RESULT", Name = "End Plate Width 53 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_54_RESULT", Name = "End Plate Width 54 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_55_RESULT", Name = "End Plate Width 55 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_56_RESULT", Name = "End Plate Width 56 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_57_RESULT", Name = "End Plate Width 57 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_58_RESULT", Name = "End Plate Width 58 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_59_RESULT", Name = "End Plate Width 59 Result", Type = "boolean" },
                   new { Code = "END_PLATE_WIDTH_60_RESULT", Name = "End Plate Width 60 Result", Type = "boolean" },
                   
                   new { Code = "CLINCHING_HEIGHT_1_VALUE", Name = "Clinching Height 1 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_2_VALUE", Name = "Clinching Height 2 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_3_VALUE", Name = "Clinching Height 3 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_4_VALUE", Name = "Clinching Height 4 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_5_VALUE", Name = "Clinching Height 5 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_6_VALUE", Name = "Clinching Height 6 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_7_VALUE", Name = "Clinching Height 7 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_8_VALUE", Name = "Clinching Height 8 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_9_VALUE", Name = "Clinching Height 9 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_10_VALUE", Name = "Clinching Height 10 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_11_VALUE", Name = "Clinching Height 11 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_12_VALUE", Name = "Clinching Height 12 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_13_VALUE", Name = "Clinching Height 13 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_14_VALUE", Name = "Clinching Height 14 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_15_VALUE", Name = "Clinching Height 15 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_16_VALUE", Name = "Clinching Height 16 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_17_VALUE", Name = "Clinching Height 17 Result", Type = "number" },
                   new { Code = "CLINCHING_HEIGHT_18_VALUE", Name = "Clinching Height 18 Result", Type = "number" },
                   new { Code = "NG_BOX_SENSOR_LONG_SIDE_VALUE", Name = "NG Box (red) Sensor Clinching Long Side", Type = "string" },
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
                   new { Code = "LEAK_RESULT", Name = "Leak Result", Type = "boolean" },
                   new { Code = "LEAK_LAST_LEAKAGE_VALUE", Name = "Leak Last Leakage Value", Type = "number" }
               }
           },
           new
           {
               ProcCode = "M_FAN_ASSY",
               ProcName = "M Fan Assy",
               ProcDesc = "Main fan assembly process.",
               Params = new[]
               {
                   new { Code = "LOT_FAN_ASM_RESULT", Name = "Lot Fan Asm Result", Type = "string" },
                   new { Code = "LOT_MOTOR_ASM_RESULT", Name = "Lot Motor Asm Result", Type = "string" },
                   new { Code = "LOT_GUIDE_ASM_RESULT", Name = "Lot Guide Asm Result", Type = "string" },
                   new { Code = "BOLT_TIGHTEN_VALUE", Name = "Bolt tighten result", Type = "string" },
                   new { Code = "BOLT_TIGHTEN_QTY_VALUE", Name = "Bold Tighten Value", Type = "string" },
                   new { Code = "NUT_TIGHTEN_VALUE", Name = "Nut Tighten Result", Type = "boolean" }
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
                   new { Code = "M_FAN_INSPECTION_ROTATION_SPEED_MAX_VALUE", Name = "M Fan Inspection Rotation Speed Max Value", Type = "number" },
                   new { Code = "M_FAN_INSPECTION_ROTATION_SPEED_MIN_VALUE", Name = "M Fan Inspection Rotation Speed Min Value", Type = "number" },
                   new { Code = "M_FAN_INSPECTION_AMPERE_MAX_VALUE", Name = "M Fan Inspection Amperage Max Value", Type = "number" },
                   new { Code = "M_FAN_INSPECTION_AMPERE_MIN_VALUE", Name = "M Fan Inspection Amperage Min Value", Type = "number" },
                   new { Code = "M_FAN_INSPECTION_WIND_DIRECTION_VALUE", Name = "M Fan Inspection Wind Direction Value", Type = "string" },
                    new { Code = "NG_BOX_SENSOR_M_FAN_INSPECTION_VALUE", Name = "NG Box (red) Sensor M Fan Inspection Value", Type = "string"}
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
                   new { Code = "ECM_ASSY_BOLT_TIGHTEN_QTY_VALUE", Name = "ECM Assy Bolt Tighten Qty Value", Type = "number" },
                   new { Code = "NG_BOX_SENSOR_ECM_ASSY_VALUE", Name = "NG Box (red) Sensor ECM Assy Value", Type = "string"}
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
                   new { Code = "CHECK_POINT_1", Name = "Check Point 1", Type = "boolean" },
                   new { Code = "CHECK_POINT_2", Name = "Check Point 2", Type = "boolean" },
                   new { Code = "CHECK_POINT_3", Name = "Check Point 3", Type = "boolean" },
                   new { Code = "CHECK_POINT_4", Name = "Check Point 4", Type = "boolean" },
                   new { Code = "CHECK_POINT_5", Name = "Check Point 5", Type = "boolean" },
                   new { Code = "CHECK_POINT_6", Name = "Check Point 6", Type = "boolean" },
                   new { Code = "CHECK_POINT_7", Name = "Check Point 7", Type = "boolean" },
                   new { Code = "CHECK_POINT_8", Name = "Check Point 8", Type = "boolean" },
                   new { Code = "CHECK_POINT_9", Name = "Check Point 9", Type = "boolean" },
                   new { Code = "CHECK_POINT_10", Name = "Check Point 10", Type = "boolean" },
                   new { Code = "CHECK_POINT_11", Name = "Check Point 11", Type = "boolean" },
                   new { Code = "CHECK_POINT_12", Name = "Check Point 12", Type = "boolean" },
                   new { Code = "CHECK_POINT_13", Name = "Check Point 13", Type = "boolean" },
                   new { Code = "CHECK_POINT_14", Name = "Check Point 14", Type = "boolean" },
                   new { Code = "CHECK_POINT_15", Name = "Check Point 15", Type = "boolean" },
                   new { Code = "CHECK_POINT_16", Name = "Check Point 16", Type = "boolean" },
                   new { Code = "CHECK_POINT_17", Name = "Check Point 17", Type = "boolean" },
                   new { Code = "CHECK_POINT_18", Name = "Check Point 18", Type = "boolean" },
                   new { Code = "CHECK_POINT_19", Name = "Check Point 19", Type = "boolean" },
                   new { Code = "CHECK_POINT_20", Name = "Check Point 20", Type = "boolean" },
                   new { Code = "NG_BOX_SENSOR_FINAL_INSPECTION_VALUE", Name = "NG Box (red) Sensor Final Inspection Value", Type = "string"}
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

    ///// <summary>Seed exactly 1 dummy process log with full details (all processes, many values) for TRSS.</summary>
    //[HttpPost("seed-process-logs")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedProcessLogs(CancellationToken cancellationToken)
    //{
    //    // 1. Delete all existing process logs first to keep it clean
    //    var logs = await _processLogRepository.GetAllAsync(cancellationToken);
    //    _processLogRepository.RemoveRange(logs);
    //    await _processLogRepository.SaveChangesAsync(cancellationToken);

    //    // 2. Get all processes and parameters for reference
    //    var processes = await _context.Processes
    //        .Include(p => p.ProcessParameters)
    //        .ThenInclude(pp => pp.Parameter)
    //        .ToListAsync(cancellationToken);

    //    if (!processes.Any())
    //    {
    //        return ResponseFormatter.Error("No processes found. Please run seed-trss-master-data first.");
    //    }

    //    // 3. Get or create a serial number
    //    var serialNumber = await _context.SerialNumbers.FirstOrDefaultAsync(cancellationToken);
    //    if (serialNumber == null)
    //    {
    //        serialNumber = new SerialNumber
    //        {
    //            SerialNumberCode = $"SN-{DateTime.UtcNow:yyyyMMdd}-0001",
    //            Type = "CLINCHING",
    //            CreatedAt = DateTime.UtcNow
    //        };
    //        await _context.SerialNumbers.AddAsync(serialNumber, cancellationToken);
    //        await _context.SaveChangesAsync(cancellationToken);
    //    }

    //    var random = new Random();

    //    // 4. Create exactly ONE comprehensive process log
    //    var processLog = new ProcessLog
    //    {
    //        SerialNumberId = serialNumber.Id,
    //        IsActive = true,
    //        CreatedAt = DateTime.UtcNow
    //    };

    //    await _processLogRepository.AddAsync(processLog, cancellationToken);

    //    // 4. Fill with details for EVERY process
    //    foreach (var process in processes)
    //    {
    //        // For each parameter in that process
    //        foreach (var procParam in process.ProcessParameters)
    //        {
    //            var param = procParam.Parameter;
    //            if (param == null) continue;

    //            // Create 10-15 values per parameter as requested
    //            int valueCount = random.Next(10, 16);
    //            for (int v = 0; v < valueCount; v++)
    //            {
    //                var detail = new ProcessLogDetail
    //                {
    //                    ProcessLog = processLog,
    //                    ProcessId = process.Id,
    //                    ParameterId = param.Id,
    //                    CreatedAt = processLog.CreatedAt.AddMinutes(random.Next(1, 120))
    //                };

    //                if (param.DataType.ToLower() == "boolean")
    //                {
    //                    detail.ValueBoolean = random.Next(0, 10) > 1; // 90% chance of true/OK
    //                }
    //                else if (param.DataType.ToLower() == "number")
    //                {
    //                    // Generate logical random numbers based on parameter names
    //                    if (param.Code.Contains("TEMP")) detail.ValueNumber = (decimal)(170 + random.NextDouble() * 20);
    //                    else if (param.Code.Contains("VOLTAGE")) detail.ValueNumber = (decimal)(215 + random.NextDouble() * 10);
    //                    else if (param.Code.Contains("TORQUE")) detail.ValueNumber = (decimal)(10 + random.NextDouble() * 5);
    //                    else detail.ValueNumber = (decimal)(random.NextDouble() * 100);
    //                }
    //                else
    //                {
    //                    detail.ValueText = "Data-" + random.Next(1000, 9999);
    //                }

    //                processLog.Details.Add(detail);
    //            }
    //        }
    //    }

    //    await _processLogRepository.SaveChangesAsync(cancellationToken);

    //    return ResponseFormatter.Success(message: $"Single comprehensive process log for serial number [{serialNumber.SerialNumberCode}] with all 7 processes and 10-15 values per parameter seeded successfully.");
    //}

    ///// <summary>Seed a dummy admin user.</summary>
    //[HttpPost("seed-admin")]
    //[ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedAdmin(CancellationToken cancellationToken)
    //{
    //    // Cek apakah user admin sudah ada (mengakses repository auth/user)
    //    var exists = await _userRepository.ExistsAsync(u => u.Username == "admin", cancellationToken);

    //    if (exists)
    //    {
    //        return ResponseFormatter.Success(message: "Admin user already exists.");
    //    }

    //    // Jika belum ada, buat user menggunakan auth service
    //    var request = new RegisterRequest
    //    {
    //        Name = "admin",
    //        Username = "admin",
    //        Role = "admin",
    //        Password = "password",
    //        ConfirmPassword = "password"
    //    };

    //    var result = await _authService.RegisterAsync(request, cancellationToken);
    //    return ResponseFormatter.Success(result, "Admin dummy user created successfully.");
    //}

    ///// <summary>Seed 100 dummy users.</summary>
    //[HttpPost("seed-users")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedUsers(CancellationToken cancellationToken)
    //{
    //    int createdCount = 0;

    //    for (int i = 1; i <= 100; i++)
    //    {
    //        var username = $"user{i}";
    //        var exists = await _userRepository.ExistsAsync(u => u.Username == username, cancellationToken);

    //        if (!exists)
    //        {
    //            var request = new RegisterRequest
    //            {
    //                Name = $"Dummy User {i}",
    //                Username = username,
    //                Role = "user",
    //                Password = "password",
    //                ConfirmPassword = "password"
    //            };

    //            await _authService.RegisterAsync(request, cancellationToken);
    //            createdCount++;
    //        }
    //    }

    //    return ResponseFormatter.Success(message: $"{createdCount} dummy users seeded successfully.");
    //}

    ///// <summary>Seed 100 dummy parts.</summary>
    //[HttpPost("seed-parts")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedParts(CancellationToken cancellationToken)
    //{
    //    int createdCount = 0;

    //    for (int i = 1; i <= 100; i++)
    //    {
    //        var number = $"PN-{i:D4}";

    //        try
    //        {
    //            var request = new TraceabilitySystem.Application.DTOs.Part.CreatePartRequestDto
    //            {
    //                Number = number,
    //                Name = $"Sample Part {i}",
    //                Description = $"This is an auto-generated sample description for part {i}."
    //            };

    //            await _partService.CreatePartAsync(request, cancellationToken);
    //            createdCount++;
    //        }
    //        catch (TraceabilitySystem.Shared.Exceptions.AppException)
    //        {
    //            // Number is already registered, skip
    //            continue;
    //        }
    //    }

    //    return ResponseFormatter.Success(message: $"{createdCount} dummy parts seeded successfully.");
    //}

    ///// <summary>Seed 10 dummy processes.</summary>
    //[HttpPost("seed-processes")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedProcesses(CancellationToken cancellationToken)
    //{
    //    int createdCount = 0;
    //    var processNames = new[] { "Blanking", "Piercing", "Bending", "Welding", "Painting", "Assembly", "Quality Control", "Packaging", "Shipping", "Stamping" };

    //    for (int i = 0; i < 10; i++)
    //    {
    //        var code = $"PRC-{(i + 1):D3}";
    //        var exists = await _processRepository.ExistsAsync(p => p.Code == code, cancellationToken);

    //        if (!exists)
    //        {
    //            var process = new Process
    //            {
    //                Code = code,
    //                Name = processNames[i],
    //                Description = $"Auto-generated process for {processNames[i]}.",
    //                IsActive = true,
    //                CreatedAt = DateTime.UtcNow
    //            };

    //            await _processRepository.AddAsync(process, cancellationToken);
    //            createdCount++;
    //        }
    //    }

    //    if (createdCount > 0)
    //    {
    //        await _processRepository.SaveChangesAsync(cancellationToken);
    //    }

    //    return ResponseFormatter.Success(message: $"{createdCount} dummy processes seeded successfully.");
    //}

    ///// <summary>Seed 50 dummy parameters for radiator manufacturing.</summary>
    //[HttpPost("seed-parameters")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedParameters(CancellationToken cancellationToken)
    //{
    //    int createdCount = 0;
    //    var dummyParams = new[]
    //    {
    //        new { Code = "PRM-001", Name = "Welding Temperature", Desc = "Tube Mill welding temperature for core tubes.", Type = "number" },
    //        new { Code = "PRM-002", Name = "Oxygen Level", Desc = "Brazing furnace oxygen level to prevent oxidation.", Type = "number" },
    //        new { Code = "PRM-003", Name = "Furnace Temperature Zone 1", Desc = "Brazing furnace heating zone 1 temperature.", Type = "number" },
    //        new { Code = "PRM-004", Name = "Furnace Temperature Zone 2", Desc = "Brazing furnace heating zone 2 temperature.", Type = "number" },
    //        new { Code = "PRM-005", Name = "Brazing Belt Speed", Desc = "Speed of core transport through brazing furnace.", Type = "number" },
    //        new { Code = "PRM-006", Name = "Crimping Force", Desc = "Clinching/crimping force for tank-to-core assembly.", Type = "number" },
    //        new { Code = "PRM-007", Name = "Crimping Depth", Desc = "Clinching/crimping depth measurement.", Type = "number" },
    //        new { Code = "PRM-008", Name = "Test Air Pressure", Desc = "Radiator air pressure used in leakage test.", Type = "number" },
    //        new { Code = "PRM-009", Name = "Leak Rate", Desc = "Radiator leak rate measured in leakage test.", Type = "number" },
    //        new { Code = "PRM-010", Name = "Pressing Force", Desc = "Stamping press machine force for header plates.", Type = "number" },
    //        new { Code = "PRM-011", Name = "Fin Height", Desc = "Height of the generated radiator fin.", Type = "number" },
    //        new { Code = "PRM-012", Name = "Fin Pitch", Desc = "Fin pitch distance on fin mill.", Type = "number" },
    //        new { Code = "PRM-013", Name = "Fin Width", Desc = "Measured width of the radiator fin.", Type = "number" },
    //        new { Code = "PRM-014", Name = "Fin Forming Lubricant Flow", Desc = "Flow rate of fin forming lubricant oil.", Type = "number" },
    //        new { Code = "PRM-015", Name = "Fin Cutter Speed", Desc = "Cutter rotary speed on fin mill.", Type = "number" },
    //        new { Code = "PRM-016", Name = "Tube Thickness", Desc = "Wall thickness of radiator tubes.", Type = "number" },
    //        new { Code = "PRM-017", Name = "Tube Width", Desc = "Width of formed tube mill profile.", Type = "number" },
    //        new { Code = "PRM-018", Name = "Welding Current", Desc = "High frequency induction welding current.", Type = "number" },
    //        new { Code = "PRM-019", Name = "Welding Voltage", Desc = "High frequency induction welding voltage.", Type = "number" },
    //        new { Code = "PRM-020", Name = "Sizing Roller Pressure", Desc = "Pressure applied by tube sizing rollers.", Type = "number" },
    //        new { Code = "PRM-021", Name = "Pre-heating Zone Temp", Desc = "Furnace pre-heating chamber temperature.", Type = "number" },
    //        new { Code = "PRM-022", Name = "Dew Point Level", Desc = "Dew point measurement inside brazing chamber.", Type = "number" },
    //        new { Code = "PRM-023", Name = "Nitrogen Gas Flow Rate", Desc = "Nitrogen gas flow rate inside brazing furnace.", Type = "number" },
    //        new { Code = "PRM-024", Name = "Clinching Speed", Desc = "Clinching/crimping tool feed speed.", Type = "number" },
    //        new { Code = "PRM-025", Name = "Gasket Compression Rate", Desc = "Compression percentage of tank sealing gasket.", Type = "number" },
    //        new { Code = "PRM-026", Name = "Chamber Temperature", Desc = "Testing chamber ambient temperature.", Type = "number" },
    //        new { Code = "PRM-027", Name = "Test Cycle Time", Desc = "Total time taken for leakage test cycle.", Type = "number" },
    //        new { Code = "PRM-028", Name = "Core Assembly Press Force", Desc = "Clamping force of core assembly fixture.", Type = "number" },
    //        new { Code = "PRM-029", Name = "Core Height Deviation", Desc = "Deviation from nominal radiator core height.", Type = "number" },
    //        new { Code = "PRM-030", Name = "Core Width Deviation", Desc = "Deviation from nominal radiator core width.", Type = "number" },
    //        new { Code = "PRM-031", Name = "Sheet Metal Thickness", Desc = "Thickness of stamping sheet metal coils.", Type = "number" },
    //        new { Code = "PRM-032", Name = "Die Cushion Pressure", Desc = "Press cushion pressure on stamping machine.", Type = "number" },
    //        new { Code = "PRM-033", Name = "Lubricant Viscosity", Desc = "Viscosity of drawing oil for stamping dies.", Type = "number" },
    //        new { Code = "PRM-034", Name = "Press Stroke Speed", Desc = "Stamping press machine stroke rate.", Type = "number" },
    //        new { Code = "PRM-035", Name = "Header Plate Pitch", Desc = "Pitch of tube slots on header plate.", Type = "number" },
    //        new { Code = "PRM-036", Name = "Side Plate Tension Force", Desc = "Tension force applied to side plates during assembly.", Type = "number" },
    //        new { Code = "PRM-037", Name = "Brazing Paste Volume", Desc = "Volume of brazing paste dispensed per core.", Type = "number" },
    //        new { Code = "PRM-038", Name = "Flux Spray Coverage", Desc = "Percentage of flux coverage on radiator core.", Type = "number" },
    //        new { Code = "PRM-039", Name = "Flux Concentration", Desc = "Concentration of active flux solution.", Type = "number" },
    //        new { Code = "PRM-040", Name = "Drying Oven Temp", Desc = "Temperature of drying oven post flux application.", Type = "number" },
    //        new { Code = "PRM-041", Name = "Solder Melt Temp", Desc = "Melting point of solder alloy.", Type = "number" },
    //        new { Code = "PRM-042", Name = "Solder Bath Level", Desc = "Level of molten solder in dip tank.", Type = "number" },
    //        new { Code = "PRM-043", Name = "Air Leak Test Stabilization Time", Desc = "Stabilization delay during air leak test.", Type = "number" },
    //        new { Code = "PRM-044", Name = "Water Bath Immersion Time", Desc = "Immersion duration for bubble leak test.", Type = "number" },
    //        new { Code = "PRM-045", Name = "Burst Pressure", Desc = "Maximum burst pressure tolerance limit.", Type = "number" },
    //        new { Code = "PRM-046", Name = "Fan Assembly Torque", Desc = "Torque applied on cooling fan bolts.", Type = "number" },
    //        new { Code = "PRM-047", Name = "Shroud Attachment Clip Force", Desc = "Clip lock force of fan shroud.", Type = "number" },
    //        new { Code = "PRM-048", Name = "Radiator Weight", Desc = "Total empty dry weight of the radiator.", Type = "number" },
    //        new { Code = "PRM-049", Name = "Paint Coating Thickness", Desc = "Dry film thickness of protective paint.", Type = "number" },
    //        new { Code = "PRM-050", Name = "Leakage Test Result", Desc = "Final result of the leakage test (OK/NG).", Type = "boolean" }
    //    };

    //    foreach (var dp in dummyParams)
    //    {
    //        var exists = await _parameterRepository.ExistsAsync(p => p.Code == dp.Code, cancellationToken);

    //        if (!exists)
    //        {
    //            var parameter = new Parameter
    //            {
    //                Code = dp.Code,
    //                Name = dp.Name,
    //                Description = dp.Desc,
    //                DataType = dp.Type,
    //                IsActive = true,
    //                CreatedAt = DateTime.UtcNow
    //            };

    //            await _parameterRepository.AddAsync(parameter, cancellationToken);
    //            createdCount++;
    //        }
    //    }

    //    if (createdCount > 0)
    //    {
    //        await _parameterRepository.SaveChangesAsync(cancellationToken);
    //    }

    //    return ResponseFormatter.Success(message: $"{createdCount} radiator manufacturing dummy parameters seeded successfully.");
    //}

    ///// <summary>Seed processes and parameters cleanly, linking them together.</summary>
    //[HttpPost("seed-process-parameters")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedProcessParameters(CancellationToken cancellationToken)
    //{
    //    // 1. Delete all existing processes & parameters
    //    var existingProcesses = await _processRepository.GetAllAsync(cancellationToken);
    //    _processRepository.RemoveRange(existingProcesses);

    //    var existingParameters = await _parameterRepository.GetAllAsync(cancellationToken);
    //    _parameterRepository.RemoveRange(existingParameters);

    //    await _processRepository.SaveChangesAsync(cancellationToken);

    //    // 2. Define the unified radiator manufacturing seed data
    //    var seedData = new[]
    //    {
    //        new
    //        {
    //            ProcCode = "PRC-001",
    //            ProcName = "Stamping Press",
    //            ProcDesc = "Precision stamping machine forming radiator header plates and side plates.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-010", Name = "Pressing Force", Desc = "Stamping press machine force for header plates.", Type = "number" },
    //                new { Code = "PRM-031", Name = "Sheet Metal Thickness", Desc = "Thickness of stamping sheet metal coils.", Type = "number" },
    //                new { Code = "PRM-032", Name = "Die Cushion Pressure", Desc = "Press cushion pressure on stamping machine.", Type = "number" },
    //                new { Code = "PRM-034", Name = "Press Stroke Speed", Desc = "Stamping press machine stroke rate.", Type = "number" }
    //            }
    //        },
    //        new
    //        {
    //            ProcCode = "PRC-002",
    //            ProcName = "Fin Mill",
    //            ProcDesc = "High-speed fin forming mill creating radiator corrugated fins.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-011", Name = "Fin Height", Desc = "Height of the generated radiator fin.", Type = "number" },
    //                new { Code = "PRM-012", Name = "Fin Pitch", Desc = "Fin pitch distance on fin mill.", Type = "number" },
    //                new { Code = "PRM-013", Name = "Fin Width", Desc = "Measured width of the radiator fin.", Type = "number" },
    //                new { Code = "PRM-015", Name = "Fin Cutter Speed", Desc = "Cutter rotary speed on fin mill.", Type = "number" }
    //            }
    //        },
    //        new
    //        {
    //            ProcCode = "PRC-003",
    //            ProcName = "Tube Mill",
    //            ProcDesc = "High-frequency induction tube mill forming and welding flat tubes.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-001", Name = "Welding Temperature", Desc = "Tube Mill welding temperature for core tubes.", Type = "number" },
    //                new { Code = "PRM-016", Name = "Tube Thickness", Desc = "Wall thickness of radiator tubes.", Type = "number" },
    //                new { Code = "PRM-017", Name = "Tube Width", Desc = "Width of formed tube mill profile.", Type = "number" },
    //                new { Code = "PRM-018", Name = "Welding Current", Desc = "High frequency induction welding current.", Type = "number" },
    //                new { Code = "PRM-019", Name = "Welding Voltage", Desc = "High frequency induction welding voltage.", Type = "number" }
    //            }
    //        },
    //        new
    //        {
    //            ProcCode = "PRC-004",
    //            ProcName = "Core Assembly",
    //            ProcDesc = "Semiautomatic assembly of tubes, fins, header plates, and side plates.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-028", Name = "Core Assembly Press Force", Desc = "Clamping force of core assembly fixture.", Type = "number" },
    //                new { Code = "PRM-029", Name = "Core Height Deviation", Desc = "Deviation from nominal radiator core height.", Type = "number" },
    //                new { Code = "PRM-030", Name = "Core Width Deviation", Desc = "Deviation from nominal radiator core width.", Type = "number" }
    //            }
    //        },
    //        new
    //        {
    //            ProcCode = "PRC-005",
    //            ProcName = "Brazing Furnace",
    //            ProcDesc = "Controlled Atmosphere Brazing (CAB) furnace melting clad layers to join parts.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-002", Name = "Oxygen Level", Desc = "Brazing furnace oxygen level to prevent oxidation.", Type = "number" },
    //                new { Code = "PRM-003", Name = "Furnace Temperature Zone 1", Desc = "Brazing furnace heating zone 1 temperature.", Type = "number" },
    //                new { Code = "PRM-004", Name = "Furnace Temperature Zone 2", Desc = "Brazing furnace heating zone 2 temperature.", Type = "number" },
    //                new { Code = "PRM-005", Name = "Brazing Belt Speed", Desc = "Speed of core transport through brazing furnace.", Type = "number" },
    //                new { Code = "PRM-023", Name = "Nitrogen Gas Flow Rate", Desc = "Nitrogen gas flow rate inside brazing furnace.", Type = "number" }
    //            }
    //        },
    //        new
    //        {
    //            ProcCode = "PRC-006",
    //            ProcName = "Tank Assembly",
    //            ProcDesc = "Clinching plastic tanks onto aluminum cores with rubber sealing gaskets.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-006", Name = "Crimping Force", Desc = "Clinching/crimping force for tank-to-core assembly.", Type = "number" },
    //                new { Code = "PRM-007", Name = "Crimping Depth", Desc = "Clinching/crimping depth measurement.", Type = "number" },
    //                new { Code = "PRM-024", Name = "Clinching Speed", Desc = "Clinching/crimping tool feed speed.", Type = "number" },
    //                new { Code = "PRM-025", Name = "Gasket Compression Rate", Desc = "Compression percentage of tank sealing gasket.", Type = "number" }
    //            }
    //        },
    //        new
    //        {
    //            ProcCode = "PRC-007",
    //            ProcName = "Leakage Testing",
    //            ProcDesc = "High-sensitivity dry air leak testing and differential pressure decay test.",
    //            Params = new[]
    //            {
    //                new { Code = "PRM-008", Name = "Test Air Pressure", Desc = "Radiator air pressure used in leakage test.", Type = "number" },
    //                new { Code = "PRM-009", Name = "Leak Rate", Desc = "Radiator leak rate measured in leakage test.", Type = "number" },
    //                new { Code = "PRM-026", Name = "Chamber Temperature", Desc = "Testing chamber ambient temperature.", Type = "number" },
    //                new { Code = "PRM-027", Name = "Test Cycle Time", Desc = "Total time taken for leakage test cycle.", Type = "number" },
    //                new { Code = "PRM-050", Name = "Leakage Test Result", Desc = "Final result of the leakage test (OK/NG).", Type = "boolean" }
    //            }
    //        }
    //    };

    //    var parameterCache = new Dictionary<string, Parameter>();

    //    foreach (var data in seedData)
    //    {
    //        var process = new Process
    //        {
    //            Code = data.ProcCode,
    //            Name = data.ProcName,
    //            Description = data.ProcDesc,
    //            IsActive = true,
    //            CreatedAt = DateTime.UtcNow
    //        };

    //        await _processRepository.AddAsync(process, cancellationToken);

    //        foreach (var dp in data.Params)
    //        {
    //            if (!parameterCache.TryGetValue(dp.Code, out var parameter))
    //            {
    //                parameter = new Parameter
    //                {
    //                    Code = dp.Code,
    //                    Name = dp.Name,
    //                    Description = dp.Desc,
    //                    DataType = dp.Type,
    //                    IsActive = true,
    //                    CreatedAt = DateTime.UtcNow
    //                };
    //                await _parameterRepository.AddAsync(parameter, cancellationToken);
    //                parameterCache[dp.Code] = parameter;
    //            }

    //            process.ProcessParameters.Add(new ProcessParameter
    //            {
    //                Process = process,
    //                Parameter = parameter
    //            });
    //        }
    //    }

    //    await _processRepository.SaveChangesAsync(cancellationToken);

    //    return ResponseFormatter.Success(message: "Unified processes and parameters successfully cleaned and seeded together.");
    //}

    ///// <summary>Seed StockIn records with associated Issue arrays.</summary>
    //[HttpPost("seed-stockins")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedStockIns(CancellationToken cancellationToken)
    //{
    //    // 1. Delete all existing StockIn records
    //    var existingStockIns = await _stockInRepository.GetAllAsync(cancellationToken);
    //    _stockInRepository.RemoveRange(existingStockIns);
    //    await _stockInRepository.SaveChangesAsync(cancellationToken);

    //    // 2. Ensure we have at least one Part to associate with StockIn
    //    var parts = await _partRepository.GetAllAsync(cancellationToken);
    //    var part = parts.FirstOrDefault();
    //    if (part == null)
    //    {
    //        part = new Part
    //        {
    //            Number = "PN-SEED-01",
    //            Name = "Default Radiator Bracket Assembly",
    //            Description = "Auto-generated default part for stock-in seeding.",
    //            IsActive = true,
    //            CreatedAt = DateTime.UtcNow
    //        };
    //        await _partRepository.AddAsync(part, cancellationToken);
    //        await _partRepository.SaveChangesAsync(cancellationToken);
    //    }

    //    // 3. Seed 30 StockIn records, each with exactly 1 Issue
    //    // Re-fetch parts so the newly created default part is included
    //    var partList = (await _partRepository.GetAllAsync(cancellationToken)).ToList();

    //    var supplyQtys = new[] { 50, 100, 150, 200, 250, 300, 500, 750, 1000, 1200 };
    //    var random = new Random(42);

    //    for (int i = 1; i <= 30; i++)
    //    {
    //        var partForThisEntry = partList.Count > 1
    //            ? partList[(i - 1) % partList.Count]
    //            : part;

    //        var supplyQty = supplyQtys[random.Next(supplyQtys.Length)];
    //        var receiptQty = supplyQty - random.Next(0, 5);
    //        var daysAgo = random.Next(1, 60);

    //        var stockIn = new StockIn
    //        {
    //            Code = $"STI-{i:D5}",
    //            PartId = partForThisEntry.Id,
    //            SupplyQty = supplyQty,
    //            SupplyDate = DateTime.UtcNow.AddDays(-daysAgo),
    //            ReceiptQty = receiptQty,
    //            ReceiptDate = DateTime.UtcNow.AddDays(-daysAgo).AddHours(random.Next(1, 8)),
    //            CreatedAt = DateTime.UtcNow
    //        };

    //        await _stockInRepository.AddAsync(stockIn, cancellationToken);

    //        // Exactly 1 issue per stock-in
    //        stockIn.Issues.Add(new Issue
    //        {
    //            Number = $"ISS-{i:D5}",
    //            CreatedAt = DateTime.UtcNow
    //        });
    //    }

    //    await _stockInRepository.SaveChangesAsync(cancellationToken);

    //    return ResponseFormatter.Success(message: "Successfully seeded 30 dummy stock-in records, each with exactly 1 issue.");
    //}

    ///// <summary>Seed 10 dummy printers.</summary>
    //[HttpPost("seed-printers")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedPrinters(CancellationToken cancellationToken)
    //{
    //    int createdCount = 0;

    //    var dummyPrinters = new[]
    //    {
    //        new { Name = "Printer-Line-01", IpAddress = "192.168.1.101", Port = 9100, Desc = "Label printer for Stamping Press line 1." },
    //        new { Name = "Printer-Line-02", IpAddress = "192.168.1.102", Port = 9100, Desc = "Label printer for Fin Mill line 2." },
    //        new { Name = "Printer-Line-03", IpAddress = "192.168.1.103", Port = 9100, Desc = "Label printer for Tube Mill line 3." },
    //        new { Name = "Printer-Line-04", IpAddress = "192.168.1.104", Port = 9100, Desc = "Label printer for Core Assembly station." },
    //        new { Name = "Printer-Line-05", IpAddress = "192.168.1.105", Port = 9100, Desc = "Label printer for Brazing Furnace exit." },
    //        new { Name = "Printer-Line-06", IpAddress = "192.168.1.106", Port = 9100, Desc = "Label printer for Tank Assembly station." },
    //        new { Name = "Printer-Line-07", IpAddress = "192.168.1.107", Port = 9100, Desc = "Label printer for Leakage Testing area." },
    //        new { Name = "Printer-Line-08", IpAddress = "192.168.1.108", Port = 9100, Desc = "Label printer for Final Inspection gate." },
    //        new { Name = "Printer-Line-09", IpAddress = "192.168.1.109", Port = 9100, Desc = "Label printer for Packaging station." },
    //        new { Name = "Printer-Line-10", IpAddress = "192.168.1.110", Port = 9100, Desc = "Label printer for Shipping dock." },
    //    };

    //    foreach (var dp in dummyPrinters)
    //    {
    //        var exists = await _printerRepository.ExistsAsync(p => p.IpAddress == dp.IpAddress, cancellationToken);

    //        if (!exists)
    //        {
    //            var printer = new Printer
    //            {
    //                Name = dp.Name,
    //                IpAddress = dp.IpAddress,
    //                Port = dp.Port,
    //                Description = dp.Desc,
    //                IsActive = true,
    //                CreatedAt = DateTime.UtcNow
    //            };

    //            await _printerRepository.AddAsync(printer, cancellationToken);
    //            createdCount++;
    //        }
    //    }

    //    if (createdCount > 0)
    //    {
    //        await _printerRepository.SaveChangesAsync(cancellationToken);
    //    }

    //    return ResponseFormatter.Success(message: $"{createdCount} dummy printers seeded successfully.");
    //}

    ///// <summary>Seed initial AppConfigs for printers.</summary>
    //[HttpPost("seed-appconfigs")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedAppConfigs(CancellationToken cancellationToken)
    //{
    //    int createdCount = 0;

    //    // Get some printers to use as values
    //    var printers = (await _printerRepository.GetAllAsync(cancellationToken)).ToList();

    //    var stockInPrinter = printers.FirstOrDefault(p => p.Name.Contains("Line-10"))?.Name ?? "Printer-Line-10";
    //    var line1Printer = printers.FirstOrDefault(p => p.Name.Contains("Line-01"))?.Name ?? "Printer-Line-01";
    //    var line2Printer = printers.FirstOrDefault(p => p.Name.Contains("Line-02"))?.Name ?? "Printer-Line-02";

    //    var dummyConfigs = new[]
    //    {
    //        new { Key = "PRINTER_NAME_STOCK_IN", Value = stockInPrinter, Desc = "Printer name for Stock In process." },
    //        new { Key = "PRINTER_NAME_LINE_1", Value = line1Printer, Desc = "Printer name for Line 1 production." },
    //        new { Key = "PRINTER_NAME_LINE_2", Value = line2Printer, Desc = "Printer name for Line 2 production." },
    //    };

    //    foreach (var dc in dummyConfigs)
    //    {
    //        var config = await _appConfigRepository.GetByKeyAsync(dc.Key, cancellationToken);

    //        if (config == null)
    //        {
    //            config = new AppConfig
    //            {
    //                Key = dc.Key,
    //                Value = dc.Value,
    //                Description = dc.Desc,
    //                CreatedAt = DateTime.UtcNow
    //            };

    //            await _appConfigRepository.AddAsync(config, cancellationToken);
    //            createdCount++;
    //        }
    //        else
    //        {
    //            // Update value if already exists to match requirement
    //            config.Value = dc.Value;
    //            _appConfigRepository.Update(config);
    //            createdCount++;
    //        }
    //    }

    //    if (createdCount > 0)
    //    {
    //        await _appConfigRepository.SaveChangesAsync(cancellationToken);
    //    }

    //    return ResponseFormatter.Success(message: $"{createdCount} app configs seeded/updated successfully.");
    //}

    ///// <summary>Clear all data from the database (Dangerous! Resets IDs).</summary>
    //[HttpDelete("clear-all-data")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> ClearAllData(CancellationToken cancellationToken)
    //{
    //    // 1. Disable Foreign Key Checks
    //    await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;", cancellationToken);

    //    try
    //    {
    //        // 2. Delete data from Tables (DELETE works with FOREIGN_KEY_CHECKS=0 while TRUNCATE sometimes doesn't)
    //        var tables = new[]
    //        {
    //            "process_log_details",
    //            "process_logs",
    //            "issues",
    //            "stock_ins",
    //            "process_parameters",
    //            "parameters",
    //            "processes",
    //            "parts",
    //            "printers",
    //            "app_configs",
    //            "refresh_tokens",
    //            "users"
    //        };

    //        foreach (var table in tables)
    //        {
    //            // Delete all rows
    //            await _context.Database.ExecuteSqlRawAsync($"DELETE FROM `{table}`;", cancellationToken);
    //            // Reset Auto-increment
    //            await _context.Database.ExecuteSqlRawAsync($"ALTER TABLE `{table}` AUTO_INCREMENT = 1;", cancellationToken);
    //        }
    //    }
    //    finally
    //    {
    //        // 3. Re-enable Foreign Key Checks
    //        await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", cancellationToken);
    //    }

    //    return ResponseFormatter.Success(message: "All database tables cleared and IDs reset successfully.");
    //}

    ///// <summary>Run all seed methods in logical order after clearing everything.</summary>
    //[HttpPost("seed-all")]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //public async Task<IActionResult> SeedAll(CancellationToken cancellationToken)
    //{
    //    // First, nuke everything to avoid FK conflicts
    //    await ClearAllData(cancellationToken);

    //    // Then seed everything back
    //    await SeedAdmin(cancellationToken);
    //    await SeedUsers(cancellationToken);
    //    await SeedParts(cancellationToken);
    //    await SeedPrinters(cancellationToken);
    //    await SeedAppConfigs(cancellationToken);
    //    await SeedProcessParameters(cancellationToken);
    //    await SeedStockIns(cancellationToken);
    //    // await SeedProcessLogs(cancellationToken);

    //    return ResponseFormatter.Success(message: "All dummy data seeded successfully after full reset.");
    //}

    ///// <summary>Buat serial numbers sekaligus dari daftar issue_number.</summary>
    //[HttpPost("serial-numbers/create")]
    //[ProducesResponseType(typeof(ApiResponse<IEnumerable<SerialNumberDto>>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    //public async Task<IActionResult> CreateSerialNumbers(
    //    [FromBody] CreateSerialNumbersFromIssuesRequestDto request,
    //    CancellationToken cancellationToken)
    //{
    //    var result = await _serialNumberService.CreateFromIssuesAsync(request, cancellationToken);
    //    return ResponseFormatter.Success(data: result, message: "Serial numbers berhasil dibuat.");
    //}


    //[HttpPost("print-stock-in")]
    //[ProducesResponseType(typeof(ApiResponse<IEnumerable<SerialNumberDto>>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    //public async Task<IActionResult> PrintStockIn(
    //    CancellationToken cancellationToken)
    //{
    //    string issueNumber = "ISS-00001"; // Contoh issue number
    //    await _printService.PrintStockInAsync(issueNumber, cancellationToken);
    //    return Ok(new
    //    {
    //        StatusCode = 200,
    //        Success = true,
    //        Message = "Stock In berhasil dicetak."
    //    });
    //}

    /// <summary>Seed process logs dengan alur: buat serial numbers dari issues, kemudian buat process logs dari proses pertama sampai selesai.</summary>
    // [HttpPost("process-log-seeder")]
    // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    // public async Task<IActionResult> ProcessLogSeeder(CancellationToken cancellationToken)
    // {

    //     // ============================================================================
    //     // STEP 1: Buat Serial Numbers dari Issue Numbers (ISS-00001 sampai ISS-00005)
    //     // ============================================================================
    //     var issueNumbers = new[] { "ISS-00001", "ISS-00002", "ISS-00003"}.ToList();

    //     var serialNumber = await _serialNumberService.CreateFromIssuesAsync(new CreateSerialNumbersFromIssuesRequestDto { IssueNumbers = issueNumbers }, cancellationToken);

    //     // ============================================================================
    //     // STEP 2: Buat Process Logs untuk Setiap Proses (dari proses pertama sampai selesai)
    //     // ============================================================================

    //     //1 process clinching sort side

    //     var newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "CLINCHING_SORT_SIDE",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "CORE_ASM_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "UPPER_TANK_ASM_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "LOWER_TANK_ASM_RESULT",
    //                 ValueBoolean = true,
    //             },
    //         }
    //     };
    //     var processLog = await _processLogService.CreateAsync(newProcessLogDto, cancellationToken);

    //     //2 process clinching long side

    //     newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "CLINCHING_LONG_SIDE",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "CLINCHING_HEIGHT_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "CLINCHING_HEIGHT_VALUE",
    //                 ValueNumber = 100.0,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "END_PLATE_WIDTH_VALUE",
    //                 ValueNumber = 50.0,
    //             },
    //         }
    //     };
    //     processLog = await _processLogService.CreateAsync(newProcessLogDto, cancellationToken);

    //     //3. process he leak

    //     newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "HE_LEAK",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "CAP_TYPE_POSITION_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "LEAK_TEST_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "LEAK_VALUE",
    //                 ValueNumber = 100.0,
    //             },
    //         }
    //     };
    //     processLog = await _processLogService.CreateAsync(newProcessLogDto, cancellationToken);

    //     //4 m fan assy (create child process log dengan serial number tersebut)
    //     newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "M_FAN_ASSY",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "FAN_ASM_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "MOTOR_ASM_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "FUN_GUIDE_ASM_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "BOLT_TIGHTEN_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "BOLT_TIGHTEN_VALUE",
    //                 ValueNumber = 100.0,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "NUT_TIGHTEN_RESULT",
    //                 ValueBoolean = true,
    //             },
    //         }
    //     };
    //     processLog = await _processLogService.CreateAsync(newProcessLogDto, cancellationToken);

    //     //5 m fan inspection

    //     newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "M_FAN_INSPECTION",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "M_FAN_TEST_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "M_FAN_INSPECTION_ROTATION_SPEED_VALUE",
    //                 //value;max;min
    //                 ValueString = "80;70;90",
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "M_FAN_INSPECTION_AMPERE_VALUE",
    //                 //value;max;min
    //                 ValueString = "80;70;90",
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "M_FAN_INSPECTION_WIND_DIRECTION_VALUE",
    //                 ValueNumber = 50,
    //             },
    //         }
    //     };
    //     processLog = await _processLogService.CreateAsync(newProcessLogDto, cancellationToken);

    //     //6 ecm assy
    //     newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "ECM_ASSY",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "RAD_CORE_ASM_NAME_LABEL_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "MOTOR_FAN_ASSY_LABEL_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "ECM_ASSY_BOLT_TIGHTEN_VALUE",
    //                 ValueNumber = 100.0,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "ECM_ASSY_BOLT_TIGHTEN_RESULT",
    //                 ValueBoolean = true,
    //             },
    //         }
    //     };

    //     // 7 ecm inspection
    //     newProcessLogDto = new AddProcessLogPerProcessRequestDto(){
    //         SerialNumberCode = serialNumber.SerialNumberCode,
    //         ProcessCode = "FINAL_INSPECTION",
    //         IsOk = true,
    //         Parameters = new List<ProcessLogParameterDto>()
    //         {
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "FINAL_INSPECTION_RAD_CORE_ASM_NAME_LABEL_RESULT",
    //                 ValueBoolean = true,
    //             },
    //             new ProcessLogParameterDto()
    //             {
    //                 ParameterCode = "ALL_CHECK_POINT_RESULT",
    //                 ValueBoolean = true,
    //             },

    //         }
    //     };
    //     processLog = await _processLogService.CreateAsync(newProcessLogDto, cancellationToken);

    //     return ResponseFormatter.Success(message: "Process log seeder executed. (Implementasi sebenarnya perlu menyesuaikan dengan struktur database yang ada)");
    // }
}
