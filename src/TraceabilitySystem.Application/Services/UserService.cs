using Mapster;
using TraceabilitySystem.Application.DTOs.User;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class UserService : BaseService<User, UserDto>, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher) : base(userRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(
        int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        // Using the enhanced generic GetPagedAsync from repository
        var (users, totalCount) = await _userRepository.GetPagedAsync(
            page,
            pageSize,
            searchTerm,
            isActive,
            cancellationToken: cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = users.Adapt<IEnumerable<UserDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // GetUserByIdAsync is now handled by BaseService.GetByIdAsync
    // But we implement it here if the signature in IUserService expects a specific name
    public Task<UserDto> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.ExistsAsync(u => u.Username == request.Username.ToLower(), cancellationToken);
        if (exists) throw new AppException("Username is already registered.", 409);

        var user = request.Adapt<User>();
        user.Username = request.Username.ToLower();
        user.PasswordHash = _passwordHasher.Hash(request.Password);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.Adapt<UserDto>();
    }

    public async Task<UserDto> UpdateUserAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(User), id);

        if (!string.IsNullOrWhiteSpace(request.Username)
            && request.Username.ToLower() != user.Username)
        {
            var exists = await _userRepository.ExistsAsync(
                u => u.Username == request.Username.ToLower() && u.Id != id,
                cancellationToken);

            if (exists)
                throw new AppException("Username is already in use.", 409);

            user.Username = request.Username.ToLower();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name;

        if (request.Role is not null)
            user.Role = request.Role.ToString();

        if (request.IsActive is not null)
            user.IsActive = request.IsActive.Value;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return user.Adapt<UserDto>();
    }
    
    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        // Hard delete since soft delete is removed
        _userRepository.Remove(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
