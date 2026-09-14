namespace TraceabilitySystem.Shared.Helpers;

public static class DateTimeHelper
{
    private static TimeZoneInfo? _cachedJakartaTimeZone;

    public static TimeZoneInfo JakartaTimeZone
    {
        get
        {
            if (_cachedJakartaTimeZone != null) return _cachedJakartaTimeZone;
            try
            {
                _cachedJakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                try
                {
                    _cachedJakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
                }
                catch
                {
                    _cachedJakartaTimeZone = TimeZoneInfo.CreateCustomTimeZone("WIB", TimeSpan.FromHours(7), "WIB", "WIB");
                }
            }
            return _cachedJakartaTimeZone;
        }
    }

    /// <summary>
    /// Returns current date and time in Jakarta (WIB / UTC+7).
    /// </summary>
    public static DateTime GetJakartaNow() => DateTime.UtcNow.AddHours(7);

    /// <summary>
    /// Returns current DateOnly in Jakarta (WIB / UTC+7).
    /// </summary>
    public static DateOnly GetJakartaToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

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
