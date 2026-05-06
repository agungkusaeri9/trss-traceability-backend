using AutoMapper;
using TraceabilitySystem.Application.DTOs;
using TraceabilitySystem.Application.DTOs.Auth;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Constants;
using TraceabilitySystem.Shared.Exceptions;

namespace TraceabilitySystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.ExistsAsync(u => u.Username == request.Username.ToLower(), cancellationToken);
        if (exists) throw new AppException("Username is already registered.", 409);

        var user = new User
        {
            Name = request.Name,
            Username = request.Username.ToLower(),
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Username.ToLower(), cancellationToken)
            ?? throw new UnauthorizedException("Invalid username or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Your account has been deactivated.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid username or password.");

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedException("Refresh token has expired or been revoked.");

        // Rotate: revoke old, issue new
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepository.Update(token);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return await GenerateAuthResponseAsync(token.User, cancellationToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepository.Update(token);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(AppConstants.Jwt.RefreshTokenExpirationDays)
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(AppConstants.Jwt.AccessTokenExpirationMinutes),
            // User = _mapper.Map<UserDto>(user)
        };
    }
}
