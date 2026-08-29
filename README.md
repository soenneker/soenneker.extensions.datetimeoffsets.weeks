[![](https://img.shields.io/nuget/v/soenneker.extensions.datetimeoffsets.weeks.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.datetimeoffsets.weeks/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.datetimeoffsets.weeks/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.datetimeoffsets.weeks/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.datetimeoffsets.weeks.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.datetimeoffsets.weeks/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.datetimeoffsets.weeks/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.datetimeoffsets.weeks/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.DateTimeOffsets.Weeks
A collection of helpful DateTimeOffset week extension methods.

## Installation

```bash
dotnet add package Soenneker.Extensions.DateTimeOffsets.Weeks
```

## Quick start

```csharp
using Soenneker.Extensions.DateTimeOffsets.Weeks;

DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
var result = dateTimeOffset.ToStartOfWeek();
```

## Common operations

- `ToStartOfWeek()` - Returns the start of the week containing `dateTimeOffset`, using this library's `UnitOfTime.Week` definition. No time zone conversion is performed and the original offset is preserved.
- `ToEndOfWeek()` - Returns the end of the week containing `dateTimeOffset`, using this library's `UnitOfTime.Week` definition. No time zone conversion is performed and the original offset is preserved.
- `ToStartOfNextWeek()` - Returns the start of the week immediately following the week containing `dateTimeOffset`, using this library's `UnitOfTime.Week` definition. No time zone conversion is performed.
- `ToStartOfPreviousWeek()` - Returns the start of the week immediately preceding the week containing `dateTimeOffset`, using this library's `UnitOfTime.Week` definition. No time zone conversion is performed.
- `ToEndOfNextWeek()` - Returns the end of the week immediately following the week containing `dateTimeOffset`, using this library's `UnitOfTime.Week` definition. No time zone conversion is performed.
- `ToEndOfPreviousWeek()` - Returns the end of the week immediately preceding the week containing `dateTimeOffset`, using this library's `UnitOfTime.Week` definition. No time zone conversion is performed.
- `ToStartOfTzWeek()` - Computes the start of the week in `tz` that contains the instant `utcInstant`, returning the result as a UTC `DateTimeOffset`. The computation is performed by: Normalizing `utcInstant` to UTC.
- `ToStartOfNextTzWeek()` - Computes the start of the next week in `tz` relative to `utcInstant`, returning the result as a UTC `DateTimeOffset`. This method advances the computed start-of-week in local wall-clock time by seven days and then converts back to UTC.
- `ToStartOfPreviousTzWeek()` - Computes the start of the previous week in `tz` relative to `utcInstant`, returning the result as a UTC `DateTimeOffset`. This method moves the computed start-of-week in local wall-clock time backward by seven days and then converts back to UTC.
- `ToEndOfTzWeek()` - Computes the end of the week in `tz` that contains `utcInstant`, returning the result as a UTC `DateTimeOffset`.
- `ToEndOfPreviousTzWeek()` - Computes the end of the previous week in `tz` relative to `utcInstant`, returning the result as a UTC `DateTimeOffset`.
- `ToEndOfNextTzWeek()` - Computes the end of the next week in `tz` relative to `utcInstant`, returning the result as a UTC `DateTimeOffset`.

The package also includes 2 additional operations for more specialized cases.
