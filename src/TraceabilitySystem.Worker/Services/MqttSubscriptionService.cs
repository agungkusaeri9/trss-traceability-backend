using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Worker.BackgroundServices;

namespace TraceabilitySystem.Worker.Services
{
    public class MqttSubscriptionService
    {

        private readonly ILogger<MqttSubscriptionService> _logger;
        private readonly MqttSettings _mqttSettings;
        private readonly WorkerSettings _workerSettings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MqttClientAccessor _mqttClientAccessor;
        private readonly DatabaseService _databaseService;

        private IMqttClient? _mqttClient;
        private HubConnection? _hubConnection;
        private bool _isConnected;

        public MqttSubscriptionService(
            ILogger<MqttSubscriptionService> logger,
            IOptions<MqttSettings> mqttSettings,
            IOptions<WorkerSettings> workerSettings,
            IServiceScopeFactory scopeFactory,
            MqttClientAccessor mqttClientAccessor,
            DatabaseService databaseService)
        {
            _logger = logger;
            _mqttSettings = mqttSettings.Value;
            _workerSettings = workerSettings.Value;
            _scopeFactory = scopeFactory;
            _mqttClientAccessor = mqttClientAccessor;
            _databaseService = databaseService;
        }


        public async Task HandleClinchingShortSideResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][ClinchingShortSide] Payload: {Payload}", payload);

            await _databaseService.SaveProcessClinchingShortSideAsync(payload, request);

            if (request == null)
            {
                //await _databaseService.UpdateStatusAsync(
                //    logId,
                //    "FAILED",
                //    "Failed to deserialize payload.");

                _logger.LogWarning("[MQTT][ClinchingShortSide] Failed to deserialize payload");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

            try
            {
                request.ProcessCode = "CLINCHING_SHORT_SIDE";

                _logger.LogInformation("[MQTT][ClinchingShortSide] Calling CreateProcessLogByClinchingAsync...");

                var result = await processLogService.CreateProcessLogByClinchingAsync(
                    request,
                    cancellationToken: default);

                //await _databaseService.UpdateStatusAsync(
                //    logId,
                //    "SUCCESS");

                _logger.LogInformation(
                    "[MQTT][ClinchingShortSide] Process log created. Id={ProcessLogId}, SN={SerialNumber}",
                    result.Id,
                    result.SerialNumberCode);
            }
            catch (Exception ex)
            {
                //await _databaseService.UpdateStatusAsync(
                //    logId,
                //    "FAILED",
                //    ex.ToString());

                _logger.LogError(ex,
                    "[MQTT][ClinchingShortSide] Error: {Message}",
                    ex.Message);
            }
        }

        public async Task HandleClinchingLongSideResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][ClinchingLongSide] Payload: {Payload}", payload);

            await _databaseService.SaveProcessClinchingLongSideAsync(payload, request);


            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][ClinchingLongSide] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "CLINCHING_LONG_SIDE";

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][ClinchingLongSide] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][ClinchingLongSide] Error handling process result: {Message}", ex.Message);
            }
        }

        public async Task HandleHeLeakResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][HeLeak] Payload: {Payload}", payload);


            await _databaseService.SaveHeLeakAsync(payload, request);

            try
            {

                if (request == null)
                {
                    _logger.LogWarning("[MQTT][HeLeak] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "HE_LEAK";

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][HeLeak] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][HeLeak] Error handling process result: {Message}", ex.Message);
            }
        }

        public async Task HandleMFanAssyResultScanAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][MFanAssyScan] Payload: {Payload}", payload);

            await _databaseService.SaveMFanAssyScanAsync(payload, request);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][MFanAssyScan] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "M_FAN_ASSY";
                request.SerialNumber = request.SerialNumberClinching;

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogMFanAssyAsync(request, "create_with_issue_number");
                _logger.LogInformation("[MQTT][MFanAssyScan] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][MFanAssy] Error handling process result: {Message}", ex.Message);
            }
        }

        public async Task HandleMFanAssyResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][MFanAssy] Payload: {Payload}", payload);

            await _databaseService.SaveMFanAssyAsync(payload, request);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][MFanAssy] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "M_FAN_ASSY";
                request.SerialNumber = request.SerialNumberMFanAssy;

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogMFanAssyAsync(request, "create_without_issue_number");
                _logger.LogInformation("[MQTT][MFanAssy] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][MFanAssy] Error handling process result: {Message}", ex.Message);
            }
        }

        public async Task HandleMFanInspectionResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][MFanInspection] Payload: {Payload}", payload);

            await _databaseService.SaveMFanInspectionAsync(payload, request);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][MFanInspection] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "M_FAN_INSPECTION";
                request.SerialNumber = request.SerialNumberMFanAssy;

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][MFanInspection] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][MFanInspection] Error handling process result: {Message}", ex.Message);
            }
        }



        public async Task HandleEcmAssyResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][EcmAssy] Payload: {Payload}", payload);

            await _databaseService.SaveEcmAssynAsync(payload, request);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][EcmAssy] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "ECM_ASSY";

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][EcmAssy] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][EcmAssy] Error handling process result: {Message}", ex.Message);
            }
        }

        public async Task HandleFinalInspectionResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][FinalInspection] Payload: {Payload}", payload);

            await _databaseService.SaveFinalInspectionAsync(payload, request);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][FinalInspection] Failed to deserialize payload");
                    return;
                }

                request.ProcessCode = "FINAL_INSPECTION";

                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][FinalInspection] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][FinalInspection] Error handling process result: {Message}", ex.Message);
            }
        }
      
    }
}
