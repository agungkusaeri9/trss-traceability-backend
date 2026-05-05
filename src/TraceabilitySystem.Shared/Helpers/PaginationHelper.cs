using TraceabilitySystem.Shared.Constants;

namespace TraceabilitySystem.Shared.Helpers;

public static class PaginationHelper
{
    public static (int skip, int take) GetPaginationParams(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = AppConstants.DefaultPageSize;
        if (pageSize > AppConstants.MaxPageSize) pageSize = AppConstants.MaxPageSize;

        return ((page - 1) * pageSize, pageSize);
    }
}
