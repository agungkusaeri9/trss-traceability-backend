using Mapster;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class PartService : BaseService<Part, PartDto>, IPartService
{
    private readonly IPartRepository _partRepository;
    public PartService(IPartRepository repository) : base(repository)
    {
        _partRepository = repository;
    }
    
    public async Task<PagedResult<PartDto>> GetPartsAsync(
        int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var (parts, totalCount) = await _partRepository.GetPagedAsync(
            page,
            pageSize,
            predicate: u => (string.IsNullOrEmpty(searchTerm)
                            || u.Name.Contains(searchTerm)
                            || u.Number.Contains(searchTerm))
                            && (!isActive.HasValue || u.IsActive == isActive.Value),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);
        return new PagedResult<PartDto>
        {
            Items = parts.Adapt<IEnumerable<PartDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PartDto> GetPartByIdAsync(
     int id,
     CancellationToken cancellationToken = default)
    {
        var part = await _partRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Part), id);

        return part.Adapt<PartDto>();
    }

    public async Task<PartDto> CreatePartAsync(CreatePartRequestDto request, CancellationToken cancellationToken = default)
    {
       bool checkByNumber = await CheckByNumberAsync(request.Number, cancellationToken);
       if(checkByNumber) throw new AppException("Number is already registered.", 409);
       
       var part = request.Adapt<Part>();
       await _partRepository.AddAsync(part, cancellationToken);
       await _partRepository.SaveChangesAsync(cancellationToken);
       return part.Adapt<PartDto>();
    }
    // public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    // {
    //     var exists = await _userRepository.ExistsAsync(u => u.Username == request.Username.ToLower(), cancellationToken);
    //     if (exists) throw new AppException("Username is already registered.", 409);
    //
    //     var user = _mapper.Map<User>(request);
    //     user.Username = request.Username.ToLower();
    //     user.PasswordHash = _passwordHasher.Hash(request.Password);
    //
    //     await _userRepository.AddAsync(user, cancellationToken);
    //     await _userRepository.SaveChangesAsync(cancellationToken);
    //
    //     return user.Adapt<UserDto>();
    // }
    
    public async Task<PartDto> UpdatePartAsync(int id, UpdatePartRequestDto request, CancellationToken cancellationToken = default)
    {
        var part = await _partRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Part), id);

        if (!string.IsNullOrWhiteSpace(request.Number) && request.Number != part.Number)
        {
            var exists = await _partRepository.ExistsAsync(
                p => p.Number == request.Number && p.Id != id, cancellationToken);
            if (exists) throw new AppException("Number is already in use.", 409);
            part.Number = request.Number;
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) part.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Description)) part.Description = request.Description;
        if (request.IsActive.HasValue) part.IsActive = request.IsActive.Value;

        _partRepository.Update(part);
        await _partRepository.SaveChangesAsync(cancellationToken);

        return part.Adapt<PartDto>();
    }

    public async Task ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var part = await _partRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Part), id);

        part.IsActive = isActive;
        _partRepository.Update(part);
        await _partRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePartAsync(int id, CancellationToken cancellationToken = default)
    {

        try
        {
            var entity = await _partRepository.GetDetailByIdAsync(id, cancellationToken);
            if (entity == null) throw new NotFoundException(nameof(Part), id);
            await _partRepository.RemoveAsync(entity, cancellationToken);
        }catch(Exception)
        {
            throw new AppException("Data Part tidak dapat dihapus karena masih digunakan pada transaksi lain.");
        }
       
    }

    private Task<bool> CheckByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return _partRepository.ExistsAsync(p => p.Number == number, cancellationToken);
    }

}