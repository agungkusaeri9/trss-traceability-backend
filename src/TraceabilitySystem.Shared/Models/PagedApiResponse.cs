using System.Text.Json.Serialization;

namespace TraceabilitySystem.Shared.Models;

public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public static PagedApiResponse<T> Ok(PagedResult<T> pagedResult, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        Data = pagedResult.Items,
        Pagination = new PaginationMetadata
        {
            Total = pagedResult.TotalCount,
            Page = pagedResult.Page,
            Limit = pagedResult.PageSize,
            TotalPage = pagedResult.TotalPages
        }
    };

    [JsonPropertyOrder(100)]
    public PaginationMetadata Pagination { get; set; } = null!;
}

public class PaginationMetadata
{
    [JsonPropertyOrder(1)]
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyOrder(2)]
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyOrder(3)]
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyOrder(4)]
    [JsonPropertyName("totalPage")]
    public int TotalPage { get; set; }
}
