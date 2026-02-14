namespace KnockOff;

/// <summary>
/// Zero-size sentinel type used as TArgs for zero-parameter methods.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value;

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
