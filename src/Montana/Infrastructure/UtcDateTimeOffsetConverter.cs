using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Montana.Infrastructure;

/// <summary>
/// Persists <see cref="DateTimeOffset"/> as UTC.
/// </summary>
/// <remarks>
/// SQLite stores date/time values as TEXT, so ordering and comparisons are
/// string comparisons. The default mapping of <see cref="DateTimeOffset"/>
/// includes the offset, which means rows captured in different offsets sort by
/// wall-clock time rather than by instant. Normalizing on write keeps ordering
/// and comparisons correct, and keeps <c>DateTimeKind</c> unambiguous on read.
/// </remarks>
public sealed class UtcDateTimeOffsetConverter 
    : ValueConverter<DateTimeOffset, DateTime>
{
    public UtcDateTimeOffsetConverter()
        : base(
            value => 
                value.UtcDateTime,
            value => 
                new DateTimeOffset(
                    DateTime.SpecifyKind(value, DateTimeKind.Utc), 
                    TimeSpan.Zero))
    {
    }
}
