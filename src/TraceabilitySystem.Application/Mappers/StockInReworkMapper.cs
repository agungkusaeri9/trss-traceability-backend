using Mapster;
using TraceabilitySystem.Application.DTOs;
using TraceabilitySystem.Application.DTOs.StockInRework;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Application.Mappers;

public class StockInReworkMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<StockInRework, StockInReworkDto>()
            .Map(dest => dest.SerialNumberCode, src => src.SerialNumber!.SerialNumberCode);
    }
}