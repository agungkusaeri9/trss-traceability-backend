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

        public MqttSubscriptionService(
            ILogger<MqttSubscriptionService> logger,
            IOptions<MqttSettings> mqttSettings,
            IOptions<WorkerSettings> workerSettings,
            IServiceScopeFactory scopeFactory,
            MqttClientAccessor mqttClientAccessor,
            DatabaseService databaseService,
            IProcessValidator validator
            
            )
        {
            _logger = logger;
            _mqttSettings = mqttSettings.Value;
            _workerSettings = workerSettings.Value;
            _scopeFactory = scopeFactory;
            _mqttClientAccessor = mqttClientAccessor;
            _databaseService = databaseService;
            _validator = validator;
        }


        public async Task HandleClinchingShortSideResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][ClinchingShortSide] Payload: {Payload}", payload);
            request.ProcessCode = "CLINCHING_SHORT_SIDE";

            var validation = await _validator.ClinchingShortSideValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                   Environment.NewLine,
                   validation.Errors);

                await _databaseService.SaveProcessClinchingShortSideAsync(
                    errorMessage,
                    payload,
                    request);

                foreach (var error in validation.Errors)
                {

                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }

                return;
            }


            await _databaseService.SaveProcessClinchingShortSideAsync(null, payload, request);

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

            try
            {
         
                var result = await processLogService.CreateProcessLogByClinchingAsync(
                    request,
                    cancellationToken: default);

                _logger.LogInformation(
                    "[MQTT][ClinchingShortSide] Process log created. Id={ProcessLogId}, SN={SerialNumber}",
                    result.Id,
                    result.SerialNumberCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MQTT][ClinchingShortSide] Error: {Message}",
                    ex.Message);
            }
        }

        public async Task HandleClinchingLongSideResultAsync(string payload, CreateProcessLogRequestDto request)
        {
            _logger.LogInformation("[MQTT][ClinchingLongSide] Payload: {Payload}", payload);
            request.ProcessCode = "CLINCHING_LONG_SIDE";
            

            var validation = await _validator.ClinchingLongSideValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                  Environment.NewLine,
                  validation.Errors);

                await _databaseService.SaveProcessClinchingLongSideAsync(errorMessage, payload, request);
                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }

            await _databaseService.SaveProcessClinchingLongSideAsync(null,payload, request);

            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][ClinchingLongSide] Failed to deserialize payload");
                    return;
                }

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

            request.ProcessCode = "HE_LEAK";
          

            var validation = await _validator.HeLeakValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                Environment.NewLine,
                validation.Errors);

                await _databaseService.SaveHeLeakAsync(errorMessage, payload, request);
                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }
            await _databaseService.SaveHeLeakAsync(null, payload, request);
            try
            {

                if (request == null)
                {
                    _logger.LogWarning("[MQTT][HeLeak] Failed to deserialize payload");
                    return;
                }

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

            request.ProcessCode = "M_FAN_ASSY";
            request.SerialNumber = request.SerialNumberClinching;

            var validation = await _validator.MFanAssyScanValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
               Environment.NewLine,
               validation.Errors);

                await _databaseService.SaveMFanAssyScanAsync(errorMessage,payload, request);

                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }
            await _databaseService.SaveMFanAssyScanAsync(null,payload, request);
            try
            {
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
            request.ProcessCode = "M_FAN_ASSY";
            request.SerialNumber = request.SerialNumberMFanAssy;
          
            var validation = await _validator.MFanAssyValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                  Environment.NewLine,
                  validation.Errors);
                await _databaseService.SaveMFanAssyAsync(errorMessage,payload, request);
                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }
            await _databaseService.SaveMFanAssyAsync(null, payload, request);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("[MQTT][MFanAssy] Failed to deserialize payload");
                    return;
                }

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
            request.ProcessCode = "M_FAN_INSPECTION";
            request.SerialNumber = request.SerialNumberMFanAssy;

            var validation = await _validator.MFanInspectionValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                 Environment.NewLine,
                 validation.Errors);
                await _databaseService.SaveMFanInspectionAsync(errorMessage, payload, request);
                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }
            await _databaseService.SaveMFanInspectionAsync(null,payload, request);

            try
            {

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
            request.ProcessCode = "ECM_ASSY";
          

            var validation = await _validator.EcmAssyValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
                 Environment.NewLine,
                 validation.Errors);
                await _databaseService.SaveEcmAssynAsync(errorMessage, payload, request);
                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }

            await _databaseService.SaveEcmAssynAsync(null,payload, request);

            try
            {
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
            request.ProcessCode = "FINAL_INSPECTION";

            var validation = await _validator.FinalInspectionValidator(request);
            if (!validation.IsValid)
            {
                var errorMessage = string.Join(
               Environment.NewLine,
               validation.Errors);
                await _databaseService.SaveFinalInspectionAsync(errorMessage, payload, request);

                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "[MQTT][ClinchingShortSide][Validation] {Error}",
                        error);
                }
                return;
            }
            await _databaseService.SaveFinalInspectionAsync(null, payload, request);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
                request.IsFInihed = true;
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
