using SecondHandShop.Application.Abstractions.Common;

namespace SecondHandShop.TestCommon.Time;

/// <summary>
/// Deterministic <see cref="IClock"/> for tests. Time only moves when
/// <see cref="Advance(TimeSpan)"/> or <see cref="Set(DateTime)"/> is called.
/// </summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("FakeClock requires a UTC DateTime.", nameof(utcNow));
        }

        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; private set; }

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);

    public void Set(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("FakeClock requires a UTC DateTime.", nameof(utcNow));
        }

        UtcNow = utcNow;
    }
}
