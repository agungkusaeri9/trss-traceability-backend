using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Worker.Models;

namespace TraceabilitySystem.Worker.Validator
{
    public class ProcessValidator : IProcessValidator
    {
        private readonly IProcessRepository _processRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProcessCategoryRepository _processCategoryRepository;
        private readonly ISerialNumberRepository _serialNumberRepository;
        private readonly IProcessLogRepository _processLogRepository;

        public ProcessValidator(
            IProcessRepository processRepository,
            IIssueRepository issueRepository,
            IUserRepository userRepository,
            IProcessCategoryRepository processCategoryRepository,
            ISerialNumberRepository serialNumberRepository,
            IProcessLogRepository processLogRepository)
        {
            _processRepository = processRepository;
            _issueRepository = issueRepository;
            _userRepository = userRepository;
            _processCategoryRepository = processCategoryRepository;
            _serialNumberRepository = serialNumberRepository;
            _processLogRepository = processLogRepository;
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

        public async Task<ValidationResult> ClinchingShortSideScanValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
            {
                return validation;
            }

            var issueNumbers = GetIssueNumbers(request.Data!);

            // Validation 1: Issue numbers must not be empty
            if (issueNumbers == null || issueNumbers.Count == 0)
            {
                validation.Errors.Add("Issue numbers must not be empty.");
                return validation;
            }

            // Validation 2: Scanned issue numbers must be unique
            if (issueNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != issueNumbers.Count)
            {
                validation.Errors.Add("Each scanned issue number must be unique.");
                return validation;
            }

            // Validation 3: Issue numbers must exist in database and have an associated Part
            var issuesInDb = (await _issueRepository.GetByNumbersWithPartAsync(issueNumbers)).ToList();
            foreach (var issueNumber in issueNumbers)
            {
                var issue = issuesInDb.FirstOrDefault(i => string.Equals(i.Number, issueNumber, StringComparison.OrdinalIgnoreCase));
                if (issue == null)
                {
                    validation.Errors.Add($"Issue number '{issueNumber}' was not found in the database.");
                    return validation;
                }

                var part = issue.StockIn?.Part;
                if (part == null)
                {
                    validation.Errors.Add($"Part for issue number '{issueNumber}' was not found.");
                    return validation;
                }
            }

            // Fetch required parts for the clinching ProcessCategory
            var clinchingCategory = await _processCategoryRepository.GetByClinchingWithPartsAsync();
            if (clinchingCategory == null)
            {
                validation.Errors.Add("ProcessCategory 'clinching' configuration not found.");
                return validation;
            }

            var allowedPartIds = clinchingCategory.ProcessCategoryParts
                .Select(pcp => pcp.PartId)
                .ToHashSet();

            var allowedPartNames = clinchingCategory.ProcessCategoryParts
                .Where(pcp => pcp.Part != null)
                .Select(pcp => pcp.Part!.Name)
                .ToList();

            // Validation 4: Each scanned part must be registered in the clinching ProcessCategory
            foreach (var issueNumber in issueNumbers)
            {
                var issue = issuesInDb.First(i => string.Equals(i.Number, issueNumber, StringComparison.OrdinalIgnoreCase));
                var part = issue.StockIn!.Part!;

                if (!allowedPartIds.Contains(part.Id))
                {
                    validation.Errors.Add(
                        $"Issue number '{issueNumber}' (Part: '{part.Name}') is not registered in the clinching category. " +
                        $"Allowed parts: {string.Join(", ", allowedPartNames.Select(n => $"'{n}'"))}.");
                    return validation;
                }
            }

            // Validation 5: All clinching parts must be present (completeness check)
            var scannedPartIds = issuesInDb
                .Select(i => i.StockIn?.Part?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var missingParts = clinchingCategory.ProcessCategoryParts
                .Where(pcp => pcp.Part != null && !scannedPartIds.Contains(pcp.PartId))
                .Select(pcp => pcp.Part!.Name)
                .ToList();

            if (missingParts.Any())
            {
                var missingList = string.Join(", ", missingParts.Select(n => $"'{n}'"));
                validation.Errors.Add($"Clinching is incomplete. Missing parts: {missingList}.");
                return validation;
            }

            // Validation 6: Remaining stock for each issue number must be greater than 0
            foreach (var issueNumber in issueNumbers)
            {
                var finalStock = await _issueRepository.GetFinalStockByIssueNumberAsync(issueNumber);
                if (!finalStock.HasValue || finalStock.Value <= 0)
                {
                    validation.Errors.Add($"Issue number '{issueNumber}' has insufficient remaining stock (stock: {finalStock ?? 0}).");
                    return validation;
                }
            }

            return validation;
        }

        public async Task<ValidationResult> ClinchingShortSideValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            // Validation 0: MFan serial number must have an active ProcessLog (IsFinished=false, Status=true)
            if (string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                validation.Errors.Add("Serial number is required.");
                return validation;
            }

            if (!request.SerialNumber.StartsWith("CC", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' is not valid. It must start with 'CC'.");
                return validation;
            }

            var log = await _processLogRepository.GetLogBySerialNumberAsync(request.SerialNumber);
            if (log == null)
            {
                validation.Errors.Add($"No process log found for serial number '{request.SerialNumber}'.");
                return validation;
            }

            if (log.IsFinished)
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' is already finished (IsFinished=true). Cannot proceed with Clinching Short Side process.");
                return validation;
            }

            if (!log.Status)
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' has a failed status (Status=false). Cannot proceed with Clinching Short Side process.");
                return validation;
            }

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


        public async Task<ValidationResult> ClinchingLongSideValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
            {
                return validation;
            }

            if (string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                validation.Errors.Add("Serial number is required.");
                return validation;
            }

            if (!request.SerialNumber.StartsWith("CC", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' is not a valid clinching serial number. It must start with 'CC'.");
                return validation;
            }

            var snExists = await _serialNumberRepository.CheckByCodeAsync(request.SerialNumber);
            if (!snExists)
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' not found.");
                return validation;
            }

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

            // Validation 0: SerialNumber must be a clinching code (starts with 'CC')
            if (string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                validation.Errors.Add("Serial number is required.");
                return validation;
            }

            if (!request.SerialNumber.StartsWith("CC", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' is not a valid clinching serial number. It must start with 'CC'.");
                return validation;
            }

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

            // Validation 1: Issue numbers must not be empty
            if (issueNumbers == null || issueNumbers.Count == 0)
            {
                validation.Errors.Add("Issue numbers must not be empty.");
                return validation;
            }

            // Validation 2: Scanned issue numbers must be unique
            if (issueNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != issueNumbers.Count)
            {
                validation.Errors.Add("Each scanned issue number must be unique.");
                return validation;
            }

            // Validation 3: Issue numbers must exist in database and have an associated Part
            var issuesInDb = (await _issueRepository.GetByNumbersWithPartAsync(issueNumbers)).ToList();
            foreach (var issueNumber in issueNumbers)
            {
                var issue = issuesInDb.FirstOrDefault(i => string.Equals(i.Number, issueNumber, StringComparison.OrdinalIgnoreCase));
                if (issue == null)
                {
                    validation.Errors.Add($"Issue number '{issueNumber}' was not found in the database.");
                    return validation;
                }

                var part = issue.StockIn?.Part;
                if (part == null)
                {
                    validation.Errors.Add($"Part for issue number '{issueNumber}' was not found.");
                    return validation;
                }
            }

            // Fetch required parts for the mfan ProcessCategory
            var mfanCategory = await _processCategoryRepository.GetByMfanWithPartsAsync();
            if (mfanCategory == null)
            {
                validation.Errors.Add("ProcessCategory 'mfan' configuration not found.");
                return validation;
            }

            var allowedPartIds = mfanCategory.ProcessCategoryParts
                .Select(pcp => pcp.PartId)
                .ToHashSet();

            var allowedPartNames = mfanCategory.ProcessCategoryParts
                .Where(pcp => pcp.Part != null)
                .Select(pcp => pcp.Part!.Name)
                .ToList();

            // Validation 4: Each scanned part must be registered in the mfan ProcessCategory
            foreach (var issueNumber in issueNumbers)
            {
                var issue = issuesInDb.First(i => string.Equals(i.Number, issueNumber, StringComparison.OrdinalIgnoreCase));
                var part = issue.StockIn!.Part!;

                if (!allowedPartIds.Contains(part.Id))
                {
                    validation.Errors.Add(
                        $"Issue number '{issueNumber}' (Part: '{part.Name}') is not registered in the mfan category. " +
                        $"Allowed parts: {string.Join(", ", allowedPartNames.Select(n => $"'{n}'"))}.");
                    return validation;
                }
            }

            // Validation 5: All mfan parts must be present (completeness check)
            var scannedPartIds = issuesInDb
                .Select(i => i.StockIn?.Part?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var missingParts = mfanCategory.ProcessCategoryParts
                .Where(pcp => pcp.Part != null && !scannedPartIds.Contains(pcp.PartId))
                .Select(pcp => pcp.Part!.Name)
                .ToList();

            if (missingParts.Any())
            {
                var missingList = string.Join(", ", missingParts.Select(n => $"'{n}'"));
                validation.Errors.Add($"M-Fan is incomplete. Missing parts: {missingList}.");
                return validation;
            }

            // Validation 6: Remaining stock for each issue number must be greater than 0
            foreach (var issueNumber in issueNumbers)
            {
                var finalStock = await _issueRepository.GetFinalStockByIssueNumberAsync(issueNumber);
                if (!finalStock.HasValue || finalStock.Value <= 0)
                {
                    validation.Errors.Add($"Issue number '{issueNumber}' has insufficient remaining stock (stock: {finalStock ?? 0}).");
                    return validation;
                }
            }

            return validation;
        }

        public async Task<ValidationResult> MFanAssyValidator(CreateProcessLogRequestDto request)
        {
            var validation = await ValidateCommonAsync(request);

            if (!validation.IsValid)
                return validation;

            // Validation 0: MFan serial number must have an active ProcessLog (IsFinished=false, Status=true)
            if (string.IsNullOrWhiteSpace(request.SerialNumberMFanAssy))
            {
                validation.Errors.Add("MFan serial number is required.");
                return validation;
            }

            if (!request.SerialNumberMFanAssy.StartsWith("MF", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"MFan serial number '{request.SerialNumberMFanAssy}' is not valid. It must start with 'MF'.");
                return validation;
            }

            var mfanLog = await _processLogRepository.GetLogBySerialNumberAsync(request.SerialNumberMFanAssy);
            if (mfanLog == null)
            {
                validation.Errors.Add($"No process log found for MFan serial number '{request.SerialNumberMFanAssy}'.");
                return validation;
            }

            if (mfanLog.IsFinished)
            {
                validation.Errors.Add($"MFan serial number '{request.SerialNumberMFanAssy}' is already finished (IsFinished=true). Cannot proceed with M-Fan Assy process.");
                return validation;
            }

            if (!mfanLog.Status)
            {
                validation.Errors.Add($"MFan serial number '{request.SerialNumberMFanAssy}' has a failed status (Status=false). Cannot proceed with M-Fan Assy process.");
                return validation;
            }

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

            // Validation 0: MFan serial number must have an active ProcessLog (IsFinished=false, Status=true)
            if (string.IsNullOrWhiteSpace(request.SerialNumberMFanAssy))
            {
                validation.Errors.Add("MFan serial number is required.");
                return validation;
            }

            if (!request.SerialNumberMFanAssy.StartsWith("MF", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"MFan serial number '{request.SerialNumberMFanAssy}' is not valid. It must start with 'MF'.");
                return validation;
            }

            var mfanLog = await _processLogRepository.GetLogBySerialNumberAsync(request.SerialNumberMFanAssy);
            if (mfanLog == null)
            {
                validation.Errors.Add($"No process log found for MFan serial number '{request.SerialNumberMFanAssy}'.");
                return validation;
            }

            if (mfanLog.IsFinished)
            {
                validation.Errors.Add($"MFan serial number '{request.SerialNumberMFanAssy}' is already finished (IsFinished=true). Cannot proceed with M-Fan Inspection.");
                return validation;
            }

            if (!mfanLog.Status)
            {
                validation.Errors.Add($"MFan serial number '{request.SerialNumberMFanAssy}' has a failed status (Status=false). Cannot proceed with M-Fan Inspection.");
                return validation;
            }

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

            var clinchingSn = !string.IsNullOrWhiteSpace(request.SerialNumberClinching)
                ? request.SerialNumberClinching
                : request.SerialNumber;

            var mfanSn = !string.IsNullOrWhiteSpace(request.SerialNumberMFanAssy)
                ? request.SerialNumberMFanAssy
                : null;

            // Validation 0: Clinching serial number must have an active ProcessLog (IsFinished=false, Status=true)
            if (string.IsNullOrWhiteSpace(clinchingSn))
            {
                validation.Errors.Add("Serial number clinching is required.");
                return validation;
            }

            if (!clinchingSn.StartsWith("CC", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Serial number '{clinchingSn}' is not a valid clinching serial number. It must start with 'CC'.");
                return validation;
            }

            var clinchingLog = await _processLogRepository.GetLogBySerialNumberAsync(clinchingSn);
            if (clinchingLog == null)
            {
                validation.Errors.Add($"No process log found for clinching serial number '{clinchingSn}'.");
                return validation;
            }

            if (clinchingLog.IsFinished)
            {
                validation.Errors.Add($"Clinching serial number '{clinchingSn}' is already finished (IsFinished=true). Cannot proceed with ECM Assy.");
                return validation;
            }

            if (!clinchingLog.Status)
            {
                validation.Errors.Add($"Clinching serial number '{clinchingSn}' has a failed status (Status=false). Cannot proceed with ECM Assy.");
                return validation;
            }

            // Validation 1: M-Fan serial number must be provided and have an active ProcessLog
            if (string.IsNullOrWhiteSpace(mfanSn))
            {
                validation.Errors.Add("Serial number M-Fan is required.");
                return validation;
            }

            if (!mfanSn.StartsWith("MF", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Serial number '{mfanSn}' is not a valid M-Fan serial number. It must start with 'MF'.");
                return validation;
            }

            var mfanLog = await _processLogRepository.GetLogBySerialNumberAsync(mfanSn);
            if (mfanLog == null)
            {
                validation.Errors.Add($"No process log found for M-Fan serial number '{mfanSn}'.");
                return validation;
            }

            if (mfanLog.IsFinished)
            {
                validation.Errors.Add($"M-Fan serial number '{mfanSn}' is already finished (IsFinished=true). Cannot proceed with ECM Assy.");
                return validation;
            }

            if (!mfanLog.Status)
            {
                validation.Errors.Add($"M-Fan serial number '{mfanSn}' has a failed status (Status=false). Cannot proceed with ECM Assy.");
                return validation;
            }

            // Validation 2: Ensure M-Fan is not already paired with another Clinching serial
            var childSnEntity = await _serialNumberRepository.GetWithRelatedBySerialNumberAsync(mfanSn);
            if (childSnEntity?.ChildRelations != null && childSnEntity.ChildRelations.Any(r => r.ParentSerialNumber?.SerialNumberCode != clinchingSn))
            {
                validation.Errors.Add($"M-Fan serial number '{mfanSn}' is already assembled with another unit.");
                return validation;
            }

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

            // Validation 0: Clinching serial number must have an active ProcessLog (IsFinished=false, Status=true)
            if (string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                validation.Errors.Add("Serial number (clinching) is required.");
                return validation;
            }

            if (!request.SerialNumber.StartsWith("CC", StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' is not a valid clinching serial number. It must start with 'CC'.");
                return validation;
            }

            var clinchingLog = await _processLogRepository.GetLogBySerialNumberAsync(request.SerialNumber);
            if (clinchingLog == null)
            {
                validation.Errors.Add($"No process log found for serial number '{request.SerialNumber}'.");
                return validation;
            }

            if (clinchingLog.IsFinished)
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' is already finished (IsFinished=true). Cannot proceed with Final Inspection.");
                return validation;
            }

            if (!clinchingLog.Status)
            {
                validation.Errors.Add($"Serial number '{request.SerialNumber}' has a failed status (Status=false). Cannot proceed with Final Inspection.");
                return validation;
            }

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
