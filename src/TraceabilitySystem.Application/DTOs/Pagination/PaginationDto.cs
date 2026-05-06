namespace TraceabilitySystem.Application.DTOs.Pagination;

public class PaginationDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPage { get; set; }
}
