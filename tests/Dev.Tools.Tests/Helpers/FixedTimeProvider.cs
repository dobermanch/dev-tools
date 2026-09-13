namespace Dev.Tools.Tests.Helpers;

/// <summary>A clock stopped at <paramref name="now"/>, in UTC, so "local now" is the same on every machine.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
