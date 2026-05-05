using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[Authorize]
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
    public async Task<ActionResult<PagedApiResponse<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, search, cancellationToken);
        return Ok(PagedApiResponse<UserDto>.Ok(result));
    }

    /// <summary>Get a single user by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(
        int id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    /// <summary>Create a new user. (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetUser),
            new { id = result.Id },
            ApiResponse<UserDto>.Ok(result, "User created successfully."));
    }

    /// <summary>Update an existing user. (Admin only)</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(result, "User updated successfully."));
    }

    /// <summary>Soft-delete a user. (Admin only)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("User deleted successfully."));
    }
}
