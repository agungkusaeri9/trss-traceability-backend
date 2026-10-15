using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Worker.BackgroundServices;
using TraceabilitySystem.Worker.Validator;

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
        private readonly IProcessValidator _validator;
        private readonly IMqttPublisher _mqttPublisher;

        public MqttSubscriptionService(
            ILogger<MqttSubscriptionService> logger,
            IOptions<MqttSettings> mqttSettings,
            IOptions<WorkerSettings> workerSettings,
            IServiceScopeFactory scopeFactory,
            MqttClientAccessor mqttClientAccessor,
            DatabaseService databaseService,
            IProcessValidator validator,
            IMqttPublisher mqttPublisher
            )
        {
            _logger = logger;
            _mqttSettings = mqttSettings.Value;
            _workerSettings = workerSettings.Value;
            _scopeFactory = scopeFactory;
            _mqttClientAccessor = mqttClientAccessor;
            _databaseService = databaseService;
            _validator = validator;
            _mqttPublisher = mqttPublisher;
        }


        public async Task HandleClinchingShortSideResultScanAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][ClinchingShortSideScan] Payload: {Payload}", payload);
            request.ProcessCode = "CLINCHING_SHORT_SIDE";

            var validation = await _validator.ClinchingShortSideScanValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                   Environment.NewLine,
                   validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "clinching-short-side-scan",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][ClinchingShortSideScan][Validation] {Error}", error);
                // }

                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

            try
            {
                var result = await processLogService.CreateProcessLogByClinchingAsync(
                    request,
                    cancellationToken: default);

                _logger.LogInformation(
                    "[MQTT][ClinchingShortSideScan] Process log created. Id={ProcessLogId}, SN={SerialNumber}",
                    result.Id,
                    result.SerialNumberCode);

                var successPayload = new
                {
                    status = true,
                    process = "clinching-short-side-scan",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MQTT][ClinchingShortSideScan] Error: {Message}",
                    ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "clinching-short-side-scan",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }

        public async Task HandleClinchingShortSideResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][ClinchingShortSide] Payload: {Payload}", payload);
            request.ProcessCode = "CLINCHING_SHORT_SIDE";

            var validation = await _validator.ClinchingShortSideValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                  Environment.NewLine,
                  validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "clinching-short-side",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][ClinchingShortSide][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][ClinchingShortSide] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "clinching-short-side",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][ClinchingShortSide] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "clinching-short-side",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }




        public async Task HandleClinchingLongSideResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][ClinchingLongSide] Payload: {Payload}", payload);
            request.ProcessCode = "CLINCHING_LONG_SIDE";

            var validation = await _validator.ClinchingLongSideValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                  Environment.NewLine,
                  validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "clinching-long-side",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][ClinchingLongSide][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][ClinchingLongSide] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "clinching-long-side",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][ClinchingLongSide] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "clinching-long-side",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }

        public async Task HandleHeLeakResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][HeLeak] Payload: {Payload}", payload);
            request.ProcessCode = "HE_LEAK";

            var validation = await _validator.HeLeakValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                Environment.NewLine,
                validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "he-leak",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][HeLeak][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
                request.IsFInihed = true;
                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][HeLeak] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "he-leak",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][HeLeak] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "he-leak",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }

        public async Task HandleMFanAssyResultScanAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][MFanAssyScan] Payload: {Payload}", payload);
            request.ProcessCode = "M_FAN_ASSY";

            var validation = await _validator.MFanAssyScanValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                   Environment.NewLine,
                   validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "m-fan-assy-scan",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][MFanAssyScan][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

                var result = await processLogService.CreateProcessLogMFanAssyAsync(request, "create_with_issue_number");
                _logger.LogInformation("[MQTT][MFanAssyScan] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "m-fan-assy-scan",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][MFanAssy] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "m-fan-assy-scan",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }

        public async Task HandleMFanAssyResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][MFanAssy] Payload: {Payload}", payload);
            request.ProcessCode = "M_FAN_ASSY";
            request.SerialNumber = request.SerialNumberMFanAssy;

            var validation = await _validator.MFanAssyValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                  Environment.NewLine,
                  validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "m-fan-assy",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][MFanAssy][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
                request.IsFInihed = false;

                var result = await processLogService.CreateProcessLogMFanAssyAsync(request, "create_without_issue_number");
                _logger.LogInformation("[MQTT][MFanAssy] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "m-fan-assy",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][MFanAssy] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "m-fan-assy",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }


        public async Task HandleMFanInspectionResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][MFanInspection] Payload: {Payload}", payload);
            request.ProcessCode = "M_FAN_INSPECTION";
            request.SerialNumber = request.SerialNumberMFanAssy;

            var validation = await _validator.MFanInspectionValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                 Environment.NewLine,
                 validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "m-fan-inspection",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][MFanInspection][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
                request.IsFInihed = true;
                var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
                _logger.LogInformation("[MQTT][MFanInspection] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "m-fan-inspection",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][MFanInspection] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "m-fan-inspection",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }



        public async Task HandleEcmAssyResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][EcmAssy] Payload: {Payload}", payload);
            request.ProcessCode = "ECM_ASSY";

            var validation = await _validator.EcmAssyValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                 Environment.NewLine,
                 validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "ecm-assy",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][EcmAssy][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var traceabilityLogService = scope.ServiceProvider.GetRequiredService<ITraceabilityLogService>();
                await traceabilityLogService.CreateNewAsync(request);
                _logger.LogInformation("[MQTT][EcmAssy] Successfully created TraceabilityLog for {Code}", request.SerialNumberClinching);

                var successPayload = new
                {
                    status = true,
                    process = "ecm-assy",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][EcmAssy] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "ecm-assy",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }

        public async Task HandleFinalInspectionResultAsync(string messageId, string payload, CreateProcessLogRequestDto request)
        {
            // _logger.LogInformation("[MQTT][FinalInspection] Payload: {Payload}", payload);
            request.ProcessCode = "FINAL_INSPECTION";

            var validation = await _validator.FinalInspectionValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                   Environment.NewLine,
                   validation.Errors);

                var errorPayload = new
                {
                    status = false,
                    process = "final-inspection",
                    error = errorMessage
                };
                await _mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMessage);

                // foreach (var error in validation.Errors)
                // {
                //     _logger.LogWarning("[MQTT][FinalInspection][Validation] {Error}", error);
                // }
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                request.IsFInihed = true;

                var traceabilityLogService = scope.ServiceProvider.GetRequiredService<ITraceabilityLogService>();
                await traceabilityLogService.CreateNewAsync(request);
                _logger.LogInformation("[MQTT][FinalInspection] Successfully processed TraceabilityLog (isFinish: true) for SN: {SerialNumber}", request.SerialNumber);

                var successPayload = new
                {
                    status = true,
                    process = "final-inspection",
                    error = (string?)null
                };
                await _mqttPublisher.PublishAsync("data/process/validation", successPayload);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][FinalInspection] Error handling process result: {Message}", ex.Message);

                var payloadError = new
                {
                    status = false,
                    process = "final-inspection",
                    error = ex.Message
                };
                await _mqttPublisher.PublishAsync("data/process/validation", payloadError);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", ex.Message);
            }
        }

        public async Task HandleProcessSubscriptionAsync(string messageId, string payload)
        {
            _logger.LogInformation("[MQTT][ProcessSubscription] Processing payload: {Payload}", payload);

            ProcessIsPublisherMqttDto? request = null;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                request = JsonSerializer.Deserialize<ProcessIsPublisherMqttDto>(payload, options);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Invalid JSON payload: {ex.Message}";
                _logger.LogWarning("[MQTT][ProcessIsPublisher] {Error}", errorMsg);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMsg);
                await _mqttPublisher.PublishAsync("data/process/validation", new
                {
                    status = false,
                    process = "is-publisher",
                    error = errorMsg
                });
                return;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.ProcessCode))
            {
                var errorMsg = "process_code is required in payload.";
                _logger.LogWarning("[MQTT][ProcessIsPublisher] {Error}", errorMsg);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMsg);
                await _mqttPublisher.PublishAsync("data/process/validation", new
                {
                    status = false,
                    process = "is-publisher",
                    error = errorMsg
                });
                return;
            }

            if (!request.IsMqttPublisher.HasValue)
            {
                var errorMsg = "isMqttPublisher boolean value is required in payload.";
                _logger.LogWarning("[MQTT][ProcessIsPublisher] {Error}", errorMsg);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMsg);
                await _mqttPublisher.PublishAsync("data/process/validation", new
                {
                    status = false,
                    process = "is-publisher",
                    error = errorMsg
                });
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processRepository = scope.ServiceProvider.GetRequiredService<IProcessRepository>();

            var process = await processRepository.FirstOrDefaultAsync(
                p => p.Code == request.ProcessCode || p.Code.ToLower() == request.ProcessCode.ToLower());

            if (process == null)
            {
                var errorMsg = $"Process with code '{request.ProcessCode}' not found.";
                _logger.LogWarning("[MQTT][ProcessIsPublisher] {Error}", errorMsg);
                await _databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", errorMsg);
                await _mqttPublisher.PublishAsync("data/process/validation", new
                {
                    status = false,
                    process = "is-publisher",
                    error = errorMsg
                });
                return;
            }

            var previousState = process.IsMqttPublisher;
            process.IsMqttPublisher = request.IsMqttPublisher.Value;
            process.UpdatedAt = DateTime.UtcNow;

            processRepository.Update(process);
            await processRepository.SaveChangesAsync();

            _logger.LogInformation(
                "[MQTT][ProcessIsPublisher] Process '{Code}' IsMqttPublisher updated from {OldState} to {NewState}.",
                process.Code,
                previousState,
                process.IsMqttPublisher);

            await _databaseService.UpdateMqttMessageStatusAsync(messageId, "SUCCESS");
            await _mqttPublisher.PublishAsync("data/process/validation", new
            {
                status = true,
                process = "is-publisher",
                process_code = process.Code,
                isMqttPublisher = process.IsMqttPublisher,
                error = (string?)null
            });
        }
    }
}
