namespace Eventify.Shared.Helpers;

/// <summary>
/// Helper utilities for working with dates and times in Eventify Pro.
///
/// Golden rule: All dates in the database are stored as UTC.
/// Conversion to local time happens only in the presentation layer (View).
/// </summary>
public static class DateTimeHelper
{
    private static readonly CultureInfo ArabicEgypt = new("ar-EG");
    private static DateTime GetLastDayOfWeekOfMonth(int year, int month, DayOfWeek dayOfWeek, int hour, int minute)
    {
        var date = new DateTime(year, month, DateTime.DaysInMonth(year, month), hour, minute, 0, DateTimeKind.Utc);
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(-1);
        }
        return date;
    }

    private static TimeSpan GetEgyptOffset(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : utcDateTime.ToUniversalTime();

        int year = utc.Year;

        // DST starts: last Friday of April at 00:00:00 local (which is Thursday before it at 22:00:00 UTC)
        var lastFridayOfApril = GetLastDayOfWeekOfMonth(year, 4, DayOfWeek.Friday, 0, 0);
        var dstStartUtc = lastFridayOfApril.AddHours(-2);

        // DST ends: last Thursday of October at 23:59:59 local (which is Thursday at 20:59:59 UTC)
        var lastThursdayOfOctober = GetLastDayOfWeekOfMonth(year, 10, DayOfWeek.Thursday, 23, 59);
        var dstEndUtc = lastThursdayOfOctober.AddHours(-3);

        if (utc >= dstStartUtc && utc <= dstEndUtc)
        {
            return TimeSpan.FromHours(3); // Egypt DST offset (UTC+3)
        }

        return TimeSpan.FromHours(2); // Egypt Standard Time offset (UTC+2)
    }

    /// <summary>
    /// Converts a UTC DateTime (or Unspecified kind loaded from DB) to Egypt local time.
    /// </summary>
    public static DateTime ToEgyptTime(this DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
            : utcDateTime.ToUniversalTime();

        var offset = GetEgyptOffset(utc);
        return utc.Add(offset);
    }

    /// <summary>
    /// Nullable-safe version of ToEgyptTime().
    /// </summary>
    public static DateTime? ToEgyptTime(this DateTime? utcDateTime)
        => utcDateTime.HasValue ? utcDateTime.Value.ToEgyptTime() : null;

    /// <summary>
    /// Converts a DateTimeOffset to Egypt local time DateTime.
    /// </summary>
    public static DateTime ToEgyptTime(this DateTimeOffset dateTimeOffset)
        => ToEgyptTime(dateTimeOffset.UtcDateTime);

    /// <summary>
    /// Nullable-safe version of ToEgyptTime() for DateTimeOffset.
    /// </summary>
    public static DateTime? ToEgyptTime(this DateTimeOffset? dateTimeOffset)
        => dateTimeOffset.HasValue ? dateTimeOffset.Value.ToEgyptTime() : null;

    /// <summary>
    /// Converts a local Egypt DateTime input to UTC.
    /// </summary>
    public static DateTime EgyptTimeToUtc(this DateTime egyptDateTime)
    {
        if (egyptDateTime.Kind == DateTimeKind.Utc)
            return egyptDateTime;

        int year = egyptDateTime.Year;

        var lastFridayOfApril = GetLastDayOfWeekOfMonth(year, 4, DayOfWeek.Friday, 0, 0);
        var dstStartLocal = new DateTime(year, 4, lastFridayOfApril.Day, 0, 0, 0, DateTimeKind.Unspecified);

        var lastThursdayOfOctober = GetLastDayOfWeekOfMonth(year, 10, DayOfWeek.Thursday, 23, 59);
        var dstEndLocal = new DateTime(year, 10, lastThursdayOfOctober.Day, 23, 59, 59, DateTimeKind.Unspecified);

        var local = DateTime.SpecifyKind(egyptDateTime, DateTimeKind.Unspecified);

        TimeSpan offset;
        if (local >= dstStartLocal && local <= dstEndLocal)
        {
            offset = TimeSpan.FromHours(3);
        }
        else
        {
            offset = TimeSpan.FromHours(2);
        }

        return DateTime.SpecifyKind(local.Subtract(offset), DateTimeKind.Utc);
    }

    /// <summary>
    /// Nullable-safe version of EgyptTimeToUtc().
    /// </summary>
    public static DateTime? EgyptTimeToUtc(this DateTime? egyptDateTime)
        => egyptDateTime.HasValue ? egyptDateTime.Value.EgyptTimeToUtc() : null;

    /// <summary>
    /// Ensures that a DateTime is represented in UTC.
    /// If Kind=Local it is converted. If Kind=Unspecified it is treated as UTC.
    /// </summary>
    public static DateTime ToUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => throw new ArgumentOutOfRangeException(nameof(dateTime.Kind))
        };
    }

    /// <summary>
    /// Nullable-safe version of ToUtc().
    /// </summary>
    public static DateTime? ToUtc(this DateTime? dateTime)
        => dateTime?.ToUtc();

    /// <summary>
    /// Converts a DateTime input from the user (assumed to be local time) to UTC.
    /// If Kind=Utc it is returned as is. Otherwise, it is converted to UTC assuming it is local time.
    /// </summary>
    public static DateTime UserInputToUtc(this DateTime dateTime)
    {
        return dateTime.EgyptTimeToUtc();
    }

    /// <summary>
    /// Nullable-safe version of UserInputToUtc().
    /// </summary>
    public static DateTime? UserInputToUtc(this DateTime? dateTime)
        => dateTime?.EgyptTimeToUtc();

    /// <summary>
    /// Checks whether the given date is in the future compared to UtcNow.
    /// Used in EventCreateDtoValidator for StartDate validation.
    /// </summary>
    public static bool IsInFuture(this DateTime dateTime, int bufferMinutes = 5)
        => dateTime.ToUtc() > DateTime.UtcNow.AddMinutes(bufferMinutes);

    /// <summary>
    /// Nullable-safe version of IsInFuture().
    /// </summary>
    public static bool IsInFuture(this DateTime? dateTime, int bufferMinutes = 5)
        => dateTime.HasValue && dateTime.Value.IsInFuture(bufferMinutes);

    /// <summary>
    /// Calculates the number of hours from now (UtcNow) until the given date.
    /// Returns a negative value if the date is in the past.
    ///
    /// Main usage: cancellation window validation (e.g., 24 hours before event).
    /// </summary>
    public static double HoursUntil(this DateTime dateTime)
        => (dateTime.ToUtc() - DateTime.UtcNow).TotalHours;

    /// <summary>
    /// Nullable-safe version of HoursUntil().
    /// </summary>
    public static double HoursUntil(this DateTime? dateTime)
        => dateTime.HasValue ? dateTime.Value.HoursUntil() : double.MinValue;

    /// <summary>
    /// Checks whether an event has ended (EndDate is in the past).
    /// Used in ReviewService to allow submitting reviews.
    /// </summary>
    public static bool IsOver(this DateTime endDate)
        => endDate.ToUtc() < DateTime.UtcNow;

    /// <summary>
    /// Checks whether a ticket sale window is currently open.
    /// </summary>
    public static bool IsSaleWindowOpen(DateTime? saleStart, DateTime? saleEnd)
    {
        var now = DateTime.UtcNow;

        if (saleStart.HasValue && now < saleStart.Value.ToUtc())
            return false;

        if (saleEnd.HasValue && now > saleEnd.Value.ToUtc())
            return false;

        return true;
    }

    /// <summary>
    /// Formats DateTime for UI display.
    /// Example: "Saturday 3 May 2026 — 09:30 PM"
    /// Should only be used in the Web/UI layer.
    /// </summary>
    public static string ToArabicDisplayString(this DateTime dateTime)
        => dateTime.ToEgyptTime().ToString("dddd d MMMM yyyy — hh:mm tt", ArabicEgypt);
}