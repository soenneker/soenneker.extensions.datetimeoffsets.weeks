[![](https://img.shields.io/nuget/v/soenneker.extensions.datetimeoffsets.weeks.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.datetimeoffsets.weeks/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.datetimeoffsets.weeks/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.datetimeoffsets.weeks/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.datetimeoffsets.weeks.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.datetimeoffsets.weeks/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.datetimeoffsets.weeks/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.datetimeoffsets.weeks/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.DateTimeOffsets.Weeks

Week-boundary and ISO week-number extensions for `DateTimeOffset`.

## Installation

```bash
dotnet add package Soenneker.Extensions.DateTimeOffsets.Weeks
```

## Offset-preserving boundaries

The non-time-zone methods use Monday as the first day of the week and preserve the input offset.

```csharp
using Soenneker.Extensions.DateTimeOffsets.Weeks;

var value = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.FromHours(-5));

DateTimeOffset start = value.ToStartOfWeek();
// Monday, 2026-03-02 00:00:00 -05:00

DateTimeOffset end = value.ToEndOfWeek();
// Sunday, 2026-03-08 23:59:59.9999999 -05:00
```

Previous and next boundaries are available through `ToStartOfPreviousWeek()`, `ToEndOfPreviousWeek()`, `ToStartOfNextWeek()`, and `ToEndOfNextWeek()`.

## Time-zone boundaries

The `Tz` methods first determine the input instant's local date in the supplied time zone, then return the corresponding boundary as a UTC `DateTimeOffset`. Monday is the default, but any `DayOfWeek` can be selected.

```csharp
TimeZoneInfo eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
var instant = new DateTimeOffset(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);

DateTimeOffset mondayStartUtc = instant.ToStartOfTzWeek(eastern);
DateTimeOffset sundayStartUtc = instant.ToStartOfTzWeek(eastern, DayOfWeek.Sunday);
DateTimeOffset nextMondayUtc = instant.ToStartOfNextTzWeek(eastern);
```

The same pattern is available for `ToStartOfPreviousTzWeek()`, `ToEndOfTzWeek()`, `ToEndOfPreviousTzWeek()`, and `ToEndOfNextTzWeek()`.

Time-zone boundaries use local calendar days, not fixed 168-hour durations, so daylight-saving changes do not shift local midnight. End methods are inclusive and return one tick before the following week begins. A skipped local midnight resolves to the first valid local time; an ambiguous midnight resolves to its earlier UTC occurrence.

## ISO week numbers

```csharp
int utcWeek = instant.ToUtcWeekNumber();
int easternWeek = instant.ToTzWeekNumber(eastern);
```

Both methods use ISO 8601 week numbering: weeks begin Monday, and week 1 is the week containing January 4. `ToTzWeekNumber()` uses the date in the supplied time zone, so it can differ from the UTC week number near day and year boundaries.
