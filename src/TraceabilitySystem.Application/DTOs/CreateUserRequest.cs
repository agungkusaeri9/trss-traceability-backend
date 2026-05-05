using TraceabilitySystem.Domain.Enums;

namespace TraceabilitySystem.Application.DTOs;

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
}
