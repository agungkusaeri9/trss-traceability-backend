using TraceabilitySystem.Application.DTOs.User;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IUserService : IBaseService<User, UserDto>
{
    // Specialized methods for User
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<UserDto> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}
