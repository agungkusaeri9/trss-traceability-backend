namespace TraceabilitySystem.Shared.Models;

public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public PaginationMetadata Pagination { get; set; } = null!;

    public static PagedApiResponse<T> Ok(PagedResult<T> pagedResult, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        Data = pagedResult.Items,
        Pagination = new PaginationMetadata
        {
            TotalCount = pagedResult.TotalCount,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage
        }
    };
}

public class PaginationMetadata
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
