namespace SportHub.Services;

// ======================================================
// SHARED BUSINESS CLOCK - MALAYSIA (UTC+08:00)
// Keeps date-sensitive booking rules consistent even when
// the web server is later hosted outside Malaysia.
// ======================================================
public static class MalaysiaClock
{
    private static readonly TimeZoneInfo TimeZone = ResolveMalaysiaTimeZone();

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

    public static DateOnly Today => DateOnly.FromDateTime(Now);

    // The booking window ends at Sunday of the current Malaysian week.
    public static DateOnly CurrentWeekEnd
    {
        get
        {
            var today = Today;
            var daysUntilSunday =
                ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;

            return today.AddDays(daysUntilSunday);
        }
    }

    public static bool IsInsideCurrentBookingWeek(DateOnly date) =>
        date >= Today && date <= CurrentWeekEnd;

    public static DateTime FromUtc(DateTime utcDateTime) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            TimeZone);

    private static TimeZoneInfo ResolveMalaysiaTimeZone()
    {
        // Windows and Linux use different time-zone identifiers.
        foreach (var identifier in new[] { "Singapore Standard Time", "Asia/Kuala_Lumpur" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(identifier);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next platform-specific identifier.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next identifier if system time-zone data is damaged.
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Malaysia Standard Time",
            TimeSpan.FromHours(8),
            "Malaysia Standard Time",
            "Malaysia Standard Time");
    }
}
