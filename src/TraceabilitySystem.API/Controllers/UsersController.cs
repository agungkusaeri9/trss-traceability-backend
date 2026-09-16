using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.User;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[Authorize(Roles = "superadmin,admin")]
[ApiController]
[Route("api/[controller]")]

public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Get paginated list of users with optional search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(pagination.Page, pagination.Limit, search, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    /// <summary>Get a single user by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(
        int id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "User retrived successfully.");
    }

    /// <summary>Create a new user. (superadmin,admin only)</summary>
    [HttpPost]
    // [Authorize(Roles = "superadmin,admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "User created successfully.", StatusCodes.Status201Created);
    }

    /// <summary>Update an existing user. (superadmin,admin only)</summary>
    [HttpPut("{id:int}")]
    // [Authorize(Roles = "superadmin,admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "User updated successfully.");
    }

    /// <summary>Delete a user. (superadmin,admin only)</summary>
    [HttpDelete("{id:int}")]
    // [Authorize(Roles = "superadmin,admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "User deleted successfully.");
    }
}
