using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using Soenneker.Enums.UnitOfTime;

namespace Soenneker.Extensions.DateTimeOffsets.Weeks;

/// <summary>
/// Provides extension methods for <see cref="DateTimeOffset"/> that operate on week boundaries,
/// including helpers that compute week starts/ends in a specified time zone while returning UTC instants.
/// </summary>
public static class DateTimeOffsetsWeeksExtension
{
    /// <summary>
    /// Returns the start of the week containing <paramref name="dateTimeOffset"/> using the extension library's week definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value to adjust.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the start of the week containing <paramref name="dateTimeOffset"/>.
    /// </returns>
    /// <remarks>
    /// This delegates to <c>ToStartOf(UnitOfTime.Week)</c>. Ensure your <c>ToStartOf</c>/<c>Trim</c> implementation for
    /// <see cref="UnitOfTime.Week"/> matches the desired rule (e.g., ISO week starting Monday).
    /// </remarks>
    [Pure]
    public static DateTimeOffset ToStartOfWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToStartOf(UnitOfTime.Week);

    /// <summary>
    /// Returns the end of the week containing <paramref name="dateTimeOffset"/> using the extension library's week definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value to adjust.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the last tick of the week containing <paramref name="dateTimeOffset"/>.
    /// </returns>
    /// <remarks>
    /// This delegates to <c>ToEndOf(UnitOfTime.Week)</c>, which is typically defined as one tick before the start of the next week.
    /// </remarks>
    [Pure]
    public static DateTimeOffset ToEndOfWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToEndOf(UnitOfTime.Week);

    /// <summary>
    /// Returns the start of the next week relative to <paramref name="dateTimeOffset"/> using the extension library's week definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value to adjust.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the start of the following week.</returns>
    /// <remarks>No time zone conversion is performed.</remarks>
    [Pure]
    public static DateTimeOffset ToStartOfNextWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToStartOfWeek()
                      .AddDays(7);

    /// <summary>
    /// Returns the start of the previous week relative to <paramref name="dateTimeOffset"/> using the extension library's week definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value to adjust.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the start of the preceding week.</returns>
    /// <remarks>No time zone conversion is performed.</remarks>
    [Pure]
    public static DateTimeOffset ToStartOfPreviousWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToStartOfWeek()
                      .AddDays(-7);

    /// <summary>
    /// Returns the end of the next week relative to <paramref name="dateTimeOffset"/> using the extension library's week definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value to adjust.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the last tick of the following week.</returns>
    /// <remarks>
    /// This is computed as the end of the current week plus 7 days.
    /// </remarks>
    [Pure]
    public static DateTimeOffset ToEndOfNextWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToEndOfWeek()
                      .AddDays(7);

    /// <summary>
    /// Returns the end of the previous week relative to <paramref name="dateTimeOffset"/> using the extension library's week definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value to adjust.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representing the last tick of the preceding week.</returns>
    /// <remarks>
    /// This is computed as the end of the current week minus 7 days.
    /// </remarks>
    [Pure]
    public static DateTimeOffset ToEndOfPreviousWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToEndOfWeek()
                      .AddDays(-7);

    /// <summary>
    /// Computes the start of the week in <paramref name="tz"/> that contains the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time. It is treated as a UTC instant (any offset is normalized to UTC).</param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">The first day of the week in the target time zone (default: Monday).</param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the start of the target time zone's week containing <paramref name="utcInstant"/>.
    /// </returns>
    /// <remarks>
    /// The computation is performed by converting the UTC instant to the target time zone, taking local midnight,
    /// moving back to the configured <paramref name="firstDayOfWeek"/>, then mapping that local wall-clock time back to UTC.
    /// If the computed local time falls into a DST "gap" (invalid time), it advances minute-by-minute until valid.
    /// If it falls into a DST "fold" (ambiguous time), it selects the earlier UTC instant (the larger offset).
    /// </remarks>
    [Pure]
    public static DateTimeOffset ToStartOfTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        // Normalize to a UTC instant
        DateTimeOffset utc = utcInstant.ToUniversalTime();

        // Convert to timezone for local calendar math
        DateTimeOffset local = TimeZoneInfo.ConvertTime(utc, tz);

        // Local midnight for that date (kind must be Unspecified for TZ conversion APIs)
        DateTime localMidnight = DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified);

        int diff = (7 + (localMidnight.DayOfWeek - firstDayOfWeek)) % 7;
        DateTime startLocal = localMidnight.AddDays(-diff);

        // Resolve DST gap/fold when mapping local wall time back to UTC
        if (tz.IsInvalidTime(startLocal))
        {
            do
            {
                startLocal = startLocal.AddMinutes(1);
            }
            while (tz.IsInvalidTime(startLocal));
        }

        if (tz.IsAmbiguousTime(startLocal))
        {
            TimeSpan[] offsets = tz.GetAmbiguousTimeOffsets(startLocal);
            // Earlier UTC instant => UTC = local - offset => choose the larger offset.
            TimeSpan chosen = offsets[0] >= offsets[1] ? offsets[0] : offsets[1];

            DateTime utcAmbiguous = DateTime.SpecifyKind(startLocal - chosen, DateTimeKind.Utc);
            return new DateTimeOffset(utcAmbiguous, TimeSpan.Zero);
        }

        DateTime startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        return new DateTimeOffset(startUtc, TimeSpan.Zero);
    }

    /// <summary>
    /// Computes the start of the next week in <paramref name="tz"/> relative to the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">The first day of the week in the target time zone (default: Monday).</param>
    /// <returns>A UTC <see cref="DateTimeOffset"/> representing the start of the next week in <paramref name="tz"/>.</returns>
    [Pure]
    public static DateTimeOffset ToStartOfNextTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) =>
        utcInstant.ToStartOfTzWeek(tz, firstDayOfWeek)
                  .AddDays(7);

    /// <summary>
    /// Computes the start of the previous week in <paramref name="tz"/> relative to the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">The first day of the week in the target time zone (default: Monday).</param>
    /// <returns>A UTC <see cref="DateTimeOffset"/> representing the start of the previous week in <paramref name="tz"/>.</returns>
    [Pure]
    public static DateTimeOffset ToStartOfPreviousTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) =>
        utcInstant.ToStartOfTzWeek(tz, firstDayOfWeek)
                  .AddDays(-7);

    /// <summary>
    /// Computes the end of the week in <paramref name="tz"/> that contains the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">The first day of the week in the target time zone (default: Monday).</param>
    /// <returns>A UTC <see cref="DateTimeOffset"/> representing the last tick of the week in <paramref name="tz"/>.</returns>
    /// <remarks>
    /// This is computed as one tick before the start of the next week in <paramref name="tz"/>.
    /// </remarks>
    [Pure]
    public static DateTimeOffset ToEndOfTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTimeOffset start = utcInstant.ToStartOfTzWeek(tz, firstDayOfWeek);
        DateTimeOffset startNext = start.AddDays(7);
        return startNext.AddTicks(-1);
    }

    /// <summary>
    /// Computes the end of the previous week in <paramref name="tz"/> relative to the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">The first day of the week in the target time zone (default: Monday).</param>
    /// <returns>A UTC <see cref="DateTimeOffset"/> representing the last tick of the previous week in <paramref name="tz"/>.</returns>
    [Pure]
    public static DateTimeOffset ToEndOfPreviousTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) =>
        utcInstant.ToEndOfTzWeek(tz, firstDayOfWeek)
                  .AddDays(-7);

    /// <summary>
    /// Computes the end of the next week in <paramref name="tz"/> relative to the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">The first day of the week in the target time zone (default: Monday).</param>
    /// <returns>A UTC <see cref="DateTimeOffset"/> representing the last tick of the next week in <paramref name="tz"/>.</returns>
    [Pure]
    public static DateTimeOffset ToEndOfNextTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) =>
        utcInstant.ToEndOfTzWeek(tz, firstDayOfWeek)
                  .AddDays(7);

    /// <summary>
    /// Converts the instant <paramref name="utcInstant"/> to <paramref name="tz"/> and returns the ISO-8601 week number for the resulting local date.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <param name="tz">The time zone used to determine the local date.</param>
    /// <returns>The ISO-8601 week number in the target time zone.</returns>
    [Pure]
    public static int ToTzWeekNumber(this DateTimeOffset utcInstant, TimeZoneInfo tz) =>
        ISOWeek.GetWeekOfYear(TimeZoneInfo.ConvertTime(utcInstant.ToUniversalTime(), tz)
                                          .DateTime);

    /// <summary>
    /// Returns the ISO-8601 week number for the UTC date corresponding to <paramref name="utcInstant"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time, treated as UTC.</param>
    /// <returns>The ISO-8601 week number for the UTC date.</returns>
    [Pure]
    public static int ToUtcWeekNumber(this DateTimeOffset utcInstant) =>
        ISOWeek.GetWeekOfYear(utcInstant.UtcDateTime);
}