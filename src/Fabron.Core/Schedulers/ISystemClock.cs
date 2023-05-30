namespace Fabron.Schedulers;

/// <summary>
/// Abstracts the system clock to facilitate testing.
/// </summary>
internal interface ISystemClock
{
    /// <summary>
    /// Retrieves the current system time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

internal class SystemClock : ISystemClock
{
    /// <summary>
    /// Retrieves the current system time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
