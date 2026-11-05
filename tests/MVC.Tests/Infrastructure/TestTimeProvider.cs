namespace MVC.Tests.Infrastructure;

/// <summary>
/// A settable clock. Local time equals UTC so tests do not depend on the
/// container's time zone; screening times are compared as local wall-clock values.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void SetUtcNow(DateTimeOffset now) => _now = now;
}
