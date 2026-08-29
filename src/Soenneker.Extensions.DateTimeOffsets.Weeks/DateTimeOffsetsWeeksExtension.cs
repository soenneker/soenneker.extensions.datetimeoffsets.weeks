using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using Soenneker.Enums.UnitOfTime;

namespace Soenneker.Extensions.DateTimeOffsets.Weeks;

/// <summary>
/// Provides extension methods for <see cref="DateTimeOffset"/> that operate on week boundaries,
/// including helpers that compute week starts/ends in a specified time zone while returning UTC instants.
/// </summary>
public static class DateTimeOffsetsWeeksExtension
{
    // Most DST gaps are 60 minutes, but some zones have had different transitions historically.
    // Keep it conservative while still avoiding excessive IsInvalidTime() calls.
    private const int _invalidStepMinutes = 1;

    /// <summary>
    /// Returns the start of the week containing <paramref name="dateTimeOffset"/>,
    /// using this library's <see cref="UnitOfTime.Week"/> definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value whose week boundary should be computed.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the first instant of the week containing <paramref name="dateTimeOffset"/>.
    /// </returns>
    /// <remarks>
    /// No time zone conversion is performed and the original offset is preserved.
    /// The exact definition of "week" (for example, ISO-8601 Monday start) is determined by
    /// the implementation of <c>ToStartOf(UnitOfTime.Week)</c>.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToStartOfWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToStartOf(UnitOfTime.Week);

    /// <summary>
    /// Returns the end of the week containing <paramref name="dateTimeOffset"/>,
    /// using this library's <see cref="UnitOfTime.Week"/> definition.
    /// </summary>
    /// <param name="dateTimeOffset">The value whose week boundary should be computed.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the last tick of the week containing <paramref name="dateTimeOffset"/>.
    /// </returns>
    /// <remarks>
    /// No time zone conversion is performed and the original offset is preserved.
    /// End-of-week is typically defined as one tick before the start of the next week as defined by this library.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToEndOfWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToEndOf(UnitOfTime.Week);

    /// <summary>
    /// Returns the start of the week immediately following the week containing <paramref name="dateTimeOffset"/>,
    /// using this library's <see cref="UnitOfTime.Week"/> definition.
    /// </summary>
    /// <param name="dateTimeOffset">The reference value.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the first instant of the next week.
    /// </returns>
    /// <remarks>
    /// No time zone conversion is performed. The result is computed by taking the start of the current week
    /// and adding seven calendar days.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToStartOfNextWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToStartOfWeek()
                      .AddDays(7);

    /// <summary>
    /// Returns the start of the week immediately preceding the week containing <paramref name="dateTimeOffset"/>,
    /// using this library's <see cref="UnitOfTime.Week"/> definition.
    /// </summary>
    /// <param name="dateTimeOffset">The reference value.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the first instant of the previous week.
    /// </returns>
    /// <remarks>
    /// No time zone conversion is performed. The result is computed by taking the start of the current week
    /// and subtracting seven calendar days.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToStartOfPreviousWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToStartOfWeek()
                      .AddDays(-7);

    /// <summary>
    /// Returns the end of the week immediately following the week containing <paramref name="dateTimeOffset"/>,
    /// using this library's <see cref="UnitOfTime.Week"/> definition.
    /// </summary>
    /// <param name="dateTimeOffset">The reference value.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the last tick of the next week.
    /// </returns>
    /// <remarks>
    /// No time zone conversion is performed. The result is computed by taking the end of the current week
    /// and adding seven calendar days.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToEndOfNextWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToEndOfWeek()
                      .AddDays(7);

    /// <summary>
    /// Returns the end of the week immediately preceding the week containing <paramref name="dateTimeOffset"/>,
    /// using this library's <see cref="UnitOfTime.Week"/> definition.
    /// </summary>
    /// <param name="dateTimeOffset">The reference value.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the last tick of the previous week.
    /// </returns>
    /// <remarks>
    /// No time zone conversion is performed. The result is computed by taking the end of the current week
    /// and subtracting seven calendar days.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToEndOfPreviousWeek(this DateTimeOffset dateTimeOffset) =>
        dateTimeOffset.ToEndOfWeek()
                      .AddDays(-7);

    /// <summary>
    /// Computes the start of the week in <paramref name="tz"/> that contains the instant <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time. The value is treated as a UTC instant; if it has a non-zero offset, it is normalized to UTC first.
    /// </param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">
    /// The first day of the week used for boundary calculations in <paramref name="tz"/> (default: Monday).
    /// </param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the earliest instant of the week (in <paramref name="tz"/>)
    /// that contains <paramref name="utcInstant"/>.
    /// </returns>
    /// <remarks>
    /// The computation is performed by:
    /// <list type="number">
    /// <item><description>Normalizing <paramref name="utcInstant"/> to UTC.</description></item>
    /// <item><description>Converting the instant to <paramref name="tz"/> to obtain the local date.</description></item>
    /// <item><description>Taking local midnight for that date and moving backward to <paramref name="firstDayOfWeek"/>.</description></item>
    /// <item><description>Mapping that local wall-clock time back to UTC, resolving DST transitions deterministically.</description></item>
    /// </list>
    /// DST resolution rules when converting the computed local wall time to UTC:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// If the local time is invalid (a DST "gap"), the time is advanced minute-by-minute until a valid time is reached.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// If the local time is ambiguous (a DST "fold"), the earlier UTC instant is selected (the larger UTC offset).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToStartOfTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTime startLocal = GetStartOfTzWeekLocal(NormalizeUtc(utcInstant), tz, firstDayOfWeek);
        return ResolveLocalWallTimeToUtc(startLocal, tz);
    }

    /// <summary>
    /// Computes the start of the next week in <paramref name="tz"/> relative to <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time treated as UTC (any non-zero offset is normalized to UTC first).
    /// </param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">
    /// The first day of the week used for boundary calculations in <paramref name="tz"/> (default: Monday).
    /// </param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the start of the next week in <paramref name="tz"/>.
    /// </returns>
    /// <remarks>
    /// This method advances the computed start-of-week in local wall-clock time by seven days and then converts back to UTC.
    /// This avoids DST drift that can occur when adding a fixed 168 hours in UTC.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToStartOfNextTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTime startLocal = GetStartOfTzWeekLocal(NormalizeUtc(utcInstant), tz, firstDayOfWeek);
        return ResolveLocalWallTimeToUtc(startLocal.AddDays(7), tz);
    }

    /// <summary>
    /// Computes the start of the previous week in <paramref name="tz"/> relative to <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time treated as UTC (any non-zero offset is normalized to UTC first).
    /// </param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">
    /// The first day of the week used for boundary calculations in <paramref name="tz"/> (default: Monday).
    /// </param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the start of the previous week in <paramref name="tz"/>.
    /// </returns>
    /// <remarks>
    /// This method moves the computed start-of-week in local wall-clock time backward by seven days and then converts back to UTC.
    /// This avoids DST drift that can occur when subtracting a fixed 168 hours in UTC.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToStartOfPreviousTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTime startLocal = GetStartOfTzWeekLocal(NormalizeUtc(utcInstant), tz, firstDayOfWeek);
        return ResolveLocalWallTimeToUtc(startLocal.AddDays(-7), tz);
    }

    /// <summary>
    /// Computes the end of the week in <paramref name="tz"/> that contains <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time treated as UTC (any non-zero offset is normalized to UTC first).
    /// </param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">
    /// The first day of the week used for boundary calculations in <paramref name="tz"/> (default: Monday).
    /// </param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the last tick of the week in <paramref name="tz"/> that contains <paramref name="utcInstant"/>.
    /// </returns>
    /// <remarks>
    /// The end of week is defined as one tick before the start of the next week in <paramref name="tz"/>.
    /// The "start of next week" boundary is computed in local wall-clock time (not by adding 168 hours in UTC),
    /// preventing DST-related drift.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToEndOfTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTime startLocal = GetStartOfTzWeekLocal(NormalizeUtc(utcInstant), tz, firstDayOfWeek);
        DateTimeOffset nextStartUtc = ResolveLocalWallTimeToUtc(startLocal.AddDays(7), tz);
        return nextStartUtc.AddTicks(-1);
    }

    /// <summary>
    /// Computes the end of the previous week in <paramref name="tz"/> relative to <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time treated as UTC (any non-zero offset is normalized to UTC first).
    /// </param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">
    /// The first day of the week used for boundary calculations in <paramref name="tz"/> (default: Monday).
    /// </param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the last tick of the previous week in <paramref name="tz"/>.
    /// </returns>
    /// <remarks>
    /// This is defined as one tick before the start of the current week in <paramref name="tz"/>.
    /// The boundary is computed using local wall-clock calendar math and then converted to UTC.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToEndOfPreviousTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTime startLocal = GetStartOfTzWeekLocal(NormalizeUtc(utcInstant), tz, firstDayOfWeek);
        DateTimeOffset startUtc = ResolveLocalWallTimeToUtc(startLocal, tz);
        return startUtc.AddTicks(-1);
    }

    /// <summary>
    /// Computes the end of the next week in <paramref name="tz"/> relative to <paramref name="utcInstant"/>,
    /// returning the result as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time treated as UTC (any non-zero offset is normalized to UTC first).
    /// </param>
    /// <param name="tz">The time zone whose local calendar rules determine week boundaries.</param>
    /// <param name="firstDayOfWeek">
    /// The first day of the week used for boundary calculations in <paramref name="tz"/> (default: Monday).
    /// </param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the last tick of the next week in <paramref name="tz"/>.
    /// </returns>
    /// <remarks>
    /// This is defined as one tick before the start of the week after next in <paramref name="tz"/>.
    /// The "start of week after next" boundary is computed in local wall-clock time (start + 14 days) and then converted to UTC,
    /// preventing DST-related drift.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToEndOfNextTzWeek(this DateTimeOffset utcInstant, TimeZoneInfo tz, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        DateTime startLocal = GetStartOfTzWeekLocal(NormalizeUtc(utcInstant), tz, firstDayOfWeek);
        DateTimeOffset startAfterNextUtc = ResolveLocalWallTimeToUtc(startLocal.AddDays(14), tz);
        return startAfterNextUtc.AddTicks(-1);
    }

    /// <summary>
    /// Converts <paramref name="utcInstant"/> to <paramref name="tz"/> and returns the ISO-8601 week-of-year
    /// for the resulting local date.
    /// </summary>
    /// <param name="utcInstant">
    /// An instant in time treated as UTC (any non-zero offset is normalized to UTC first).
    /// </param>
    /// <param name="tz">The time zone used to determine the local date.</param>
    /// <returns>The ISO-8601 week number (1-53) for the local date in <paramref name="tz"/>.</returns>
    /// <remarks>
    /// ISO-8601 week numbering uses Monday as the first day of the week and defines week 1 as the week containing January 4.
    /// This method uses <see cref="ISOWeek.GetWeekOfYear(DateTime)"/> on the converted local <see cref="DateTimeOffset.Date"/>.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToTzWeekNumber(this DateTimeOffset utcInstant, TimeZoneInfo tz)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(NormalizeUtc(utcInstant), tz);
        return ISOWeek.GetWeekOfYear(local.Date);
    }

    /// <summary>
    /// Returns the ISO-8601 week-of-year for the UTC date corresponding to <paramref name="utcInstant"/>.
    /// </summary>
    /// <param name="utcInstant">An instant in time treated as UTC (any non-zero offset is normalized to UTC first).</param>
    /// <returns>The ISO-8601 week number (1-53) for the UTC date.</returns>
    /// <remarks>
    /// ISO-8601 week numbering uses Monday as the first day of the week and defines week 1 as the week containing January 4.
    /// This method uses <see cref="ISOWeek.GetWeekOfYear(DateTime)"/> on <see cref="DateTimeOffset.UtcDateTime"/>.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToUtcWeekNumber(this DateTimeOffset utcInstant) =>
        ISOWeek.GetWeekOfYear(NormalizeUtc(utcInstant)
            .UtcDateTime);

    /// <summary>
    /// Normalizes a <see cref="DateTimeOffset"/> to a UTC instant.
    /// </summary>
    /// <param name="dto">The instant to express with a zero UTC offset.</param>
    /// <returns>
    /// A value representing the same instant in time with offset <c>+00:00</c>.
    /// </returns>
    /// <remarks>
    /// This avoids calling <see cref="DateTimeOffset.ToUniversalTime"/> when the value is already UTC.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTimeOffset NormalizeUtc(DateTimeOffset dto) =>
        dto.Offset == TimeSpan.Zero ? dto : dto.ToUniversalTime();

    /// <summary>
    /// Computes the local (wall-clock) start-of-week midnight for the week containing the specified UTC instant in <paramref name="tz"/>.
    /// </summary>
    /// <param name="utc">A UTC instant.</param>
    /// <param name="tz">The time zone whose local date is used for the week calculation.</param>
    /// <param name="firstDayOfWeek">The first day of the week in <paramref name="tz"/>.</param>
    /// <returns>
    /// A <see cref="DateTime"/> with <see cref="DateTimeKind.Unspecified"/> representing local midnight at the start of the week in <paramref name="tz"/>.
    /// </returns>
    /// <remarks>
    /// The returned value is a local wall-clock time (kind = <see cref="DateTimeKind.Unspecified"/>). It is not yet a UTC instant.
    /// Call <see cref="ResolveLocalWallTimeToUtc(DateTime, TimeZoneInfo)"/> to map it back to UTC while handling DST gaps/folds.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime GetStartOfTzWeekLocal(DateTimeOffset utc, TimeZoneInfo tz, DayOfWeek firstDayOfWeek)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(utc, tz);
        DateTime localMidnight = DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified);

        int diff = (7 + (localMidnight.DayOfWeek - firstDayOfWeek)) % 7;
        return localMidnight.AddDays(-diff);
    }

    /// <summary>
    /// Converts a local wall-clock time in <paramref name="tz"/> to the corresponding UTC instant,
    /// deterministically resolving DST gaps and folds.
    /// </summary>
    /// <param name="localWallTimeUnspecified">
    /// A local wall-clock <see cref="DateTime"/> in <paramref name="tz"/> with <see cref="DateTimeKind.Unspecified"/>.
    /// </param>
    /// <param name="tz">The time zone used to interpret <paramref name="localWallTimeUnspecified"/>.</param>
    /// <returns>
    /// A UTC <see cref="DateTimeOffset"/> representing the mapped instant in time.
    /// </returns>
    /// <remarks>
    /// Resolution rules:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// If <paramref name="localWallTimeUnspecified"/> is invalid (a DST "gap"), the time is advanced minute-by-minute until valid.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// If <paramref name="localWallTimeUnspecified"/> is ambiguous (a DST "fold"), the earlier UTC instant is chosen
    /// (which corresponds to the larger UTC offset).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTimeOffset ResolveLocalWallTimeToUtc(DateTime localWallTimeUnspecified, TimeZoneInfo tz)
    {
        if (localWallTimeUnspecified.Kind != DateTimeKind.Unspecified)
            localWallTimeUnspecified = DateTime.SpecifyKind(localWallTimeUnspecified, DateTimeKind.Unspecified);

        DateTime local = localWallTimeUnspecified;

        while (tz.IsInvalidTime(local))
            local = local.AddMinutes(_invalidStepMinutes);

        if (tz.IsAmbiguousTime(local))
        {
            TimeSpan[] offsets = tz.GetAmbiguousTimeOffsets(local);
            TimeSpan chosen = offsets[0] >= offsets[1] ? offsets[0] : offsets[1];

            DateTime utc = DateTime.SpecifyKind(local - chosen, DateTimeKind.Utc);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        DateTime utcResult = TimeZoneInfo.ConvertTimeToUtc(local, tz);
        return new DateTimeOffset(utcResult, TimeSpan.Zero);
    }
}