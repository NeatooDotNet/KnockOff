namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Base entity interface.
/// Used for inheritance benchmarks.
/// </summary>
public interface IBaseEntity
{
    int Id { get; }
}

/// <summary>
/// Extended entity interface with timestamps.
/// Used to measure inherited member access overhead.
/// </summary>
public interface ITimestampedEntity : IBaseEntity
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
