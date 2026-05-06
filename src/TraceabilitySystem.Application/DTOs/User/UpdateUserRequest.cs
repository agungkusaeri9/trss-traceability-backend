namespace TraceabilitySystem.Application.DTOs.User;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
