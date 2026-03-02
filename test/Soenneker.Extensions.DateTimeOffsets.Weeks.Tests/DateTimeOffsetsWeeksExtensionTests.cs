using System;
using AwesomeAssertions;
using Soenneker.Tests.Unit;
using Xunit;

namespace Soenneker.Extensions.DateTimeOffsets.Weeks.Tests;

public sealed class DateTimeOffsetsWeeksExtensionTests : UnitTest
{
    // --- ToStartOfWeek / ToEndOfWeek (offset-preserving, delegates to ToStartOf/ToEndOf Week) ---

    [Fact]
    public void ToStartOfWeek_on_Monday_midnight_returns_same()
    {
        var monday = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.FromHours(-5));
        DateTimeOffset start = monday.ToStartOfWeek();
        start.Should().Be(monday);
        start.Offset.Should().Be(monday.Offset);
    }

    [Fact]
    public void ToStartOfWeek_on_Sunday_returns_previous_Monday()
    {
        var sunday = new DateTimeOffset(2026, 3, 8, 23, 59, 59, TimeSpan.Zero);
        DateTimeOffset start = sunday.ToStartOfWeek();
        start.Should().Be(new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToEndOfWeek_is_one_tick_before_next_week_start()
    {
        var wednesday = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset endOfWeek = wednesday.ToEndOfWeek();
        DateTimeOffset startOfNextWeek = wednesday.ToStartOfNextWeek();
        endOfWeek.Should().Be(startOfNextWeek.AddTicks(-1));
    }

    [Fact]
    public void ToEndOfWeek_and_ToStartOfNextWeek_are_contiguous()
    {
        var any = new DateTimeOffset(2026, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        DateTimeOffset end = any.ToEndOfWeek();
        DateTimeOffset nextStart = any.ToStartOfNextWeek();
        end.AddTicks(1).Should().Be(nextStart);
    }

    [Fact]
    public void ToStartOfPreviousWeek_and_ToStartOfNextWeek_are_14_days_apart()
    {
        var d = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset prev = d.ToStartOfPreviousWeek();
        DateTimeOffset next = d.ToStartOfNextWeek();
        (next - prev).TotalDays.Should().Be(14);
    }

    [Fact]
    public void ToEndOfPreviousWeek_is_one_tick_before_ToStartOfWeek()
    {
        var d = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset endPrev = d.ToEndOfPreviousWeek();
        DateTimeOffset startCur = d.ToStartOfWeek();
        endPrev.AddTicks(1).Should().Be(startCur);
    }

    [Fact]
    public void Non_UTC_offset_is_preserved_on_ToStartOfWeek()
    {
        // 2026-03-04 10:00 +05:00 = Wednesday; week start Monday same offset = 2026-03-02 00:00 +05:00 = 2026-03-01 19:00 UTC
        var withOffset = new DateTimeOffset(2026, 3, 4, 10, 0, 0, TimeSpan.FromHours(5));
        DateTimeOffset start = withOffset.ToStartOfWeek();
        start.Offset.Should().Be(TimeSpan.FromHours(5));
        start.UtcDateTime.Date.Should().Be(new DateTime(2026, 3, 1));
    }

    // --- MinValue / edge dates ---

    [Fact]
    public void ToStartOfWeek_at_DateTimeOffset_MinValue()
    {
        var min = DateTimeOffset.MinValue;
        DateTimeOffset start = min.ToStartOfWeek();
        start.Should().BeBefore(min.AddTicks(1)).And.BeOnOrBefore(min);
        start.Offset.Should().Be(min.Offset);
    }

    [Fact]
    public void ToEndOfWeek_at_DateTimeOffset_MaxValue_does_not_overflow()
    {
        // MaxValue is UTC; end of week is last tick before next week - should still be representable
        var nearMax = new DateTimeOffset(9999, 12, 25, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = nearMax.ToEndOfWeek();
        DateTimeOffset nextStart = nearMax.ToStartOfNextWeek();
        end.Should().Be(nextStart.AddTicks(-1));
    }

    // --- Time zone week boundaries (UTC result) ---

    [Fact]
    public void ToStartOfTzWeek_normalizes_non_UTC_input_to_UTC()
    {
        var utcInstant = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        var withOffset = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.FromHours(1));
        TimeZoneInfo tz = TimeZoneInfo.Utc;
        DateTimeOffset startUtc = utcInstant.ToStartOfTzWeek(tz);
        DateTimeOffset startFromOffset = withOffset.ToStartOfTzWeek(tz);
        startUtc.Should().Be(startFromOffset);
        startUtc.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToStartOfTzWeek_with_Utc_zone_matches_ToStartOfWeek_UTC()
    {
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset byWeek = d.ToStartOfWeek();
        DateTimeOffset byTz = d.ToStartOfTzWeek(TimeZoneInfo.Utc);
        byWeek.Should().Be(byTz);
    }

    [Fact]
    public void ToStartOfTzWeek_firstDayOfWeek_Sunday_gives_different_boundary_than_Monday()
    {
        // 2026-03-04 12:00 UTC is Wednesday; Monday week start vs Sunday week start differ
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        TimeZoneInfo tz = TimeZoneInfo.Utc;
        DateTimeOffset startMonday = d.ToStartOfTzWeek(tz, DayOfWeek.Monday);
        DateTimeOffset startSunday = d.ToStartOfTzWeek(tz, DayOfWeek.Sunday);
        startMonday.Should().Be(new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero));
        startSunday.Should().Be(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToEndOfTzWeek_is_one_tick_before_ToStartOfNextTzWeek()
    {
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        TimeZoneInfo tz = TimeZoneInfo.Utc;
        DateTimeOffset end = d.ToEndOfTzWeek(tz);
        DateTimeOffset nextStart = d.ToStartOfNextTzWeek(tz);
        end.AddTicks(1).Should().Be(nextStart);
        end.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToStartOfPreviousTzWeek_and_ToStartOfNextTzWeek_are_14_days_apart_in_local_sense()
    {
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        TimeZoneInfo tz = TimeZoneInfo.Utc;
        DateTimeOffset prev = d.ToStartOfPreviousTzWeek(tz);
        DateTimeOffset next = d.ToStartOfNextTzWeek(tz);
        (next - prev).TotalDays.Should().Be(14);
    }

    [Fact]
    public void ToEndOfNextTzWeek_is_one_tick_before_start_of_week_after_next()
    {
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        TimeZoneInfo tz = TimeZoneInfo.Utc;
        DateTimeOffset endNext = d.ToEndOfNextTzWeek(tz);
        // A moment in the week after next has the same week start (UTC: 14 days later)
        var inWeekAfterNext = d.AddDays(15);
        DateTimeOffset startWeekAfterNext = inWeekAfterNext.ToStartOfTzWeek(tz);
        endNext.AddTicks(1).Should().Be(startWeekAfterNext);
    }

    // --- DST: Eastern Time spring forward (gap) ---
    // 2026 US Eastern: DST starts March 8 2:00 AM -> 3:00 AM. So 2:30 AM local doesn't exist.
    // Week containing March 8: Monday March 2 00:00 local. That midnight is valid.
    // Next week Monday March 9 00:00 local - also valid. So we need a time that when converted
    // to local falls in the gap. E.g. UTC 2026-03-08 07:30 = Eastern 3:30 AM (valid).
    // 2026-03-08 06:00 UTC = 1:00 AM Eastern (valid). 2026-03-08 07:00 UTC = 2:00 AM Eastern (valid).
    // 2026-03-08 06:30 UTC = 1:30 AM Eastern. So week start for that week in Eastern is
    // Monday March 2 00:00 Eastern = 2026-03-02 05:00 UTC. That's valid.
    // If we ask for "start of week" for a local time that is in the gap, the implementation
    // advances minute-by-minute until valid. So we test that we get a valid result and that
    // start of week in Eastern for an instant in that week is Monday 00:00 Eastern.

    [Fact]
    public void ToStartOfTzWeek_Eastern_week_containing_DST_spring_forward()
    {
        TimeZoneInfo eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        // Wednesday March 11 2026 15:00 UTC = 11:00 AM Eastern (after DST)
        var d = new DateTimeOffset(2026, 3, 11, 15, 0, 0, TimeSpan.Zero);
        DateTimeOffset start = d.ToStartOfTzWeek(eastern, DayOfWeek.Monday);
        start.Offset.Should().Be(TimeSpan.Zero);
        // That week started Monday March 9 00:00 Eastern = March 9 05:00 UTC (DST)
        // March 9 00:00 Eastern is EDT (DST already started), so 04:00 UTC
        DateTimeOffset expected = new DateTimeOffset(2026, 3, 9, 4, 0, 0, TimeSpan.Zero);
        start.Should().Be(expected);
    }

    [Fact]
    public void ToEndOfTzWeek_Eastern_before_DST_spring_forward()
    {
        TimeZoneInfo eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        // Week of March 2-8 2026. End of week = last tick before March 9 00:00 Eastern.
        // March 9 00:00 Eastern (DST) = 05:00 UTC. So end = 05:00 UTC - 1 tick.
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = d.ToEndOfTzWeek(eastern, DayOfWeek.Monday);
        DateTimeOffset nextStart = d.ToStartOfNextTzWeek(eastern, DayOfWeek.Monday);
        end.AddTicks(1).Should().Be(nextStart);
        end.Offset.Should().Be(TimeSpan.Zero);
    }

    // --- Week numbers (ISO) ---

    [Fact]
    public void ToUtcWeekNumber_week_containing_Jan_4_is_week_1()
    {
        var jan4 = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);
        jan4.ToUtcWeekNumber().Should().Be(1);
    }

    [Fact]
    public void ToUtcWeekNumber_Jan_1_2026_is_week_1()
    {
        var jan1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        jan1.ToUtcWeekNumber().Should().Be(1);
    }

    [Fact]
    public void ToTzWeekNumber_same_as_ToUtcWeekNumber_when_zone_is_UTC()
    {
        var d = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);
        int utcWeek = d.ToUtcWeekNumber();
        int tzWeek = d.ToTzWeekNumber(TimeZoneInfo.Utc);
        tzWeek.Should().Be(utcWeek);
    }

    [Fact]
    public void ToTzWeekNumber_at_year_boundary_can_differ_from_UTC()
    {
        // 2026-01-04 22:00 UTC = Sunday Jan 4 (week 1 in UTC); in Tokyo = Monday Jan 5 07:00 -> week 2
        TimeZoneInfo tokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        var utc = new DateTimeOffset(2026, 1, 4, 22, 0, 0, TimeSpan.Zero);
        int utcWeek = utc.ToUtcWeekNumber();
        int tzWeek = utc.ToTzWeekNumber(tokyo);
        utcWeek.Should().Be(1);
        tzWeek.Should().Be(2);
    }

    [Fact]
    public void ToUtcWeekNumber_normalizes_offset_before_computing()
    {
        var utc = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);
        var plusOne = new DateTimeOffset(2026, 1, 3, 23, 0, 0, TimeSpan.FromHours(1));
        utc.ToUtcWeekNumber().Should().Be(1);
        plusOne.ToUtcWeekNumber().Should().Be(1);
    }

    // --- Weird: default DateTimeOffset (zero date) ---

    [Fact]
    public void ToStartOfWeek_on_default_DateTimeOffset()
    {
        var d = default(DateTimeOffset);
        DateTimeOffset start = d.ToStartOfWeek();
        start.Should().BeOnOrBefore(d.AddDays(7));
        start.Offset.Should().Be(d.Offset);
    }

    // --- Round-trip: start then end of same week ---

    [Fact]
    public void ToStartOfWeek_then_ToEndOfWeek_same_week()
    {
        var d = new DateTimeOffset(2026, 3, 5, 14, 22, 11, TimeSpan.FromHours(-3));
        DateTimeOffset start = d.ToStartOfWeek();
        DateTimeOffset end = d.ToEndOfWeek();
        start.Should().BeBefore(end);
        start.Should().BeOnOrBefore(d);
        end.Should().BeOnOrAfter(d);
        (end - start).Should().Be(TimeSpan.FromDays(7).Add(TimeSpan.FromTicks(-1)));
    }
}
