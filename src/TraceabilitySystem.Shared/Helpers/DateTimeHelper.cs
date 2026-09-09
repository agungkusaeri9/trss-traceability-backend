namespace TraceabilitySystem.Shared.Helpers;

public static class DateTimeHelper
{
    /// <summary>
    /// Formats a DateTime to an ISO 8601 UTC string format (e.g. 2026-09-09T07:46:00Z)
    /// suitable for accurate timezone conversion on frontend clients.
    /// </summary>
    public static string FormatTimestamp(DateTime dateTime)
    {
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    /// <summary>
    /// Extension method to format DateTime to ISO 8601 UTC string.
    /// </summary>
    public static string ToIsoUtcString(this DateTime dateTime)
    {
        return FormatTimestamp(dateTime);
    }

    /// <summary>
    /// Extension method to format nullable DateTime to ISO 8601 UTC string, or null if dateTime is null.
    /// </summary>
    public static string? ToIsoUtcString(this DateTime? dateTime)
    {
        return dateTime.HasValue ? FormatTimestamp(dateTime.Value) : null;
    }
}
