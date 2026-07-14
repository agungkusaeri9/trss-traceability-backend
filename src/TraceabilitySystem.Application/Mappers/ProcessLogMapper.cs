using Mapster;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Application.Mappers;

public class ProcessLogMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProcessLogDetail, ProcessLogFullValueDetailDto>()
            .Map(dest => dest.ProcessCode, src => src.Process.Code)
            .Map(dest => dest.ProcessName, src => src.Process.Name)
            .Map(dest => dest.ParameterCode, src => src.Parameter.Code)
            .Map(dest => dest.ParameterName, src => src.Parameter.Name)
            .Map(dest => dest.Value, src =>
                src.ValueText ??
                (src.ValueNumber.HasValue
                    ? src.ValueNumber.Value.ToString()
                    : src.ValueBoolean.HasValue
                        ? src.ValueBoolean.Value.ToString()
                        : null));

        config.NewConfig<ProcessLog, ProcessLogFullValueDto>()
            .Map(dest => dest.SerialNumberCode, src => src.SerialNumber.SerialNumberCode)
            //.Map(dest => dest.Details, src => src.Details)
            .Ignore(dest => dest.Clinching)
            .Ignore(dest => dest.MFan);

        config.NewConfig<ProcessLog, ProcessLogFullValueParentDto>()
            .Map(dest => dest.SerialNumberCode, src => src.SerialNumber.SerialNumberCode)
            .Map(dest => dest.Details, src => src.Details);

        config.NewConfig<ProcessLog, ProcessLogFullValueChildDto>()
            .Map(dest => dest.SerialNumberCode, src => src.SerialNumber.SerialNumberCode)
            .Map(dest => dest.Details, src => src.Details);
    }
}