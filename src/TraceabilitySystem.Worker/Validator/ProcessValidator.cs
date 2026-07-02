using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Worker.Models;

namespace TraceabilitySystem.Worker.Validator
{
    public class ProcessValidator : IProcessValidator
    {
        private readonly IProcessRepository _processRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IUserRepository _userRepository;
        public ProcessValidator(
                IProcessRepository processRepository,
                IIssueRepository issueRepository,
                IUserRepository userRepository
            )
        {
            _processRepository = processRepository;
            _issueRepository = issueRepository;
            _userRepository = userRepository;
        }

        private async Task<ValidationResult> ValidateCommonAsync(CreateProcessLogRequestDto request)
        {
            var validation = new ValidationResult();

            if (request == null)
            {
                validation.Errors.Add("Payload is null.");
                return validation;
            }

            if (string.IsNullOrWhiteSpace(request.OperatorUsername))
                validation.Errors.Add("Operator username is required.");

            if (!string.IsNullOrWhiteSpace(request.OperatorUsername))
            {
                var user = await _userRepository.GetByUsernameAsync(request.OperatorUsername);

                if (user == null)
                    validation.Errors.Add("Username tidak ditemukan.");
            }

            if (request.Data == null)
                validation.Errors.Add("Data is required.");

            return validation;
        }

        private async Task ValidateIssueNumbersAsync(
     ValidationResult validation,
     IEnumerable<string> issueNumbers)
        {
            var result = await _issueRepository.CheckIssueNumbersAsync(issueNumbers);

            var invalidIssueNumbers = result
                .Where(x => !x.Value)
                .Select(x => x.Key)
                .ToList();

            if (!invalidIssueNumbers.Any())
                return;

            validation.Errors.AddRange(
                invalidIssueNumbers.Select(x =>
                    $"Issue Number '{x}' tidak ditemukan."));
        }

        private static List<string> GetIssueNumbers(Dictionary<string, object> data)
        {
            if (!data.TryGetValue("issue_numbers", out var value))
                return new();

            if (value is not JsonElement json || json.ValueKind != JsonValueKind.Array)
                return new();

            return json.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();
        }

        private static List<string> GetParameterCodes(Dictionary<string, object> data)
        {
            return data.Keys.ToList();
        }


        private async Task ValidateParametersAsync(
    ValidationResult validation,
    string processCode,
    IEnumerable<string> parameterCodes)
        {
            var result = await _processRepository.CheckParametersByProcessCodeAsync(
                processCode,
                parameterCodes);

            var invalidParameters = result
                .Where(x => !x.Value)
                .Select(x => x.Key)
                .ToList();

            if (!invalidParameters.Any())
                return;

            validation.Errors.AddRange(
                invalidParameters.Select(x =>
                    $"Parameter Code '{x}' tidak ditemukan."));
        }



        public async Task<ValidationResult> ClinchingShortSideValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;


            var issueNumbers = GetIssueNumbers(request.Data!);

            if (!issueNumbers.Any())
            {
                validation.Errors.Add("issue_numbers is required.");
                return validation;
            }

            await ValidateIssueNumbersAsync(validation, issueNumbers);

            return validation;
        }

        public async Task<ValidationResult> ClinchingLongSideValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var parameters = GetParameterCodes(request.Data!);

            if (!parameters.Any())
            {
                validation.Errors.Add("Parameter is required.");
                return validation;
            }

            await ValidateParametersAsync(
                validation,
                request.ProcessCode,
                parameters);

            return validation;
        }

        public async Task<ValidationResult> HeLeakValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var parameters = GetParameterCodes(request.Data!);

            if (!parameters.Any())
            {
                validation.Errors.Add("Parameter is required.");
                return validation;
            }

            await ValidateParametersAsync(
                validation,
                request.ProcessCode,
                parameters);

            return validation;
        }

        public async Task<ValidationResult> MFanAssyScanValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var issueNumbers = GetIssueNumbers(request.Data!);

            if (!issueNumbers.Any())
            {
                validation.Errors.Add("issue_numbers is required.");
                return validation;
            }

            await ValidateIssueNumbersAsync(validation, issueNumbers);

            return validation;
        }

        public async Task<ValidationResult> MFanAssyValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var parameters = GetParameterCodes(request.Data!);

            if (!parameters.Any())
            {
                validation.Errors.Add("Parameter is required.");
                return validation;
            }

            await ValidateParametersAsync(
                validation,
                request.ProcessCode,
                parameters);

            return validation;
        }

        public async Task<ValidationResult> MFanInspectionValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var parameters = GetParameterCodes(request.Data!);

            if (!parameters.Any())
            {
                validation.Errors.Add("Parameter is required.");
                return validation;
            }

            await ValidateParametersAsync(
                validation,
                request.ProcessCode,
                parameters);

            return validation;
        }

        public async Task<ValidationResult> EcmAssyValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var parameters = GetParameterCodes(request.Data!);

            if (!parameters.Any())
            {
                validation.Errors.Add("Parameter is required.");
                return validation;
            }

            await ValidateParametersAsync(
                validation,
                request.ProcessCode,
                parameters);

            return validation;
        }

        public async Task<ValidationResult> FinalInspectionValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            var parameters = GetParameterCodes(request.Data!);

            if (!parameters.Any())
            {
                validation.Errors.Add("Parameter is required.");
                return validation;
            }

            await ValidateParametersAsync(
                validation,
                request.ProcessCode,
                parameters);

            return validation;
        }
    }
}
