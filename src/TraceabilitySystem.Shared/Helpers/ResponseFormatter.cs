using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;

namespace TraceabilitySystem.Shared.Helpers;

public static class ResponseFormatter
{
    /// <summary>
    /// Returns a success response (Success = true) with data.
    /// </summary>
    public static IActionResult Success<T>(T data, string message = "Success", int code = StatusCodes.Status200OK)
    {
        return new ObjectResult(new
        {
            success = true,
            message,
            data
        })
        {
            StatusCode = code
        };
    }

    /// <summary>
    /// Returns a success response with pagination metadata.
    /// </summary>
    public static IActionResult PagedSuccess<T>(TraceabilitySystem.Shared.Models.PagedResult<T> pagedResult, string message = "Success", int code = StatusCodes.Status200OK)
    {
        return new ObjectResult(new
        {
            success = true,
            message,
            data = pagedResult.Items,
            pagination = new
            {
                page = pagedResult.Page,
                limit = pagedResult.PageSize,
                total = pagedResult.TotalCount,
                total_page = pagedResult.TotalPages
            }
        })
        {
            StatusCode = code
        };
    }

    /// <summary>
    /// Returns a success response (Success = true) without data.
    /// </summary>
    public static IActionResult Success(string message = "Success", int code = StatusCodes.Status200OK)
    {
        return new ObjectResult(new
        {
            success = true,
            message
        })
        {
            StatusCode = code
        };
    }

    /// <summary>
    /// Returns an error response (Success = false).
    /// </summary>
    public static IActionResult Error(string message, int code = StatusCodes.Status400BadRequest)
    {
        return new ObjectResult(new
        {
            success = false,
            message
        })
        {
            StatusCode = code
        };
    }

    /// <summary>
    /// Returns a validation error response (Success = false, with errors array) usually 422.
    /// </summary>
    public static IActionResult ValidationError(string message, object errors, int code = StatusCodes.Status422UnprocessableEntity)
    {
        return new ObjectResult(new
        {
            success = false,
            message,
            errors
        })
        {
            StatusCode = code
        };
    }
}
