using System;
using System.Collections.Generic;
using System.Text;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Worker.Models;

namespace TraceabilitySystem.Worker.Validator
{
    public interface IProcessValidator
    {
        Task<ValidationResult> ClinchingShortSideValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> ClinchingLongSideValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> HeLeakValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> MFanAssyScanValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> MFanAssyValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> MFanInspectionValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> EcmAssyValidator(CreateProcessLogRequestDto request);
        Task<ValidationResult> FinalInspectionValidator(CreateProcessLogRequestDto request);
    }
}
