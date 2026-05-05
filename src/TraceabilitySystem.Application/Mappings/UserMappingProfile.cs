using AutoMapper;
using TraceabilitySystem.Application.DTOs;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<CreateUserRequest, User>()
            .ForMember(d => d.PasswordHash, opt => opt.Ignore())
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())

            .ForMember(d => d.IsActive, opt => opt.Ignore())
            .ForMember(d => d.RefreshTokens, opt => opt.Ignore());
    }
}
