namespace KnockOff;

/// <summary>
/// Specifies the expected number of times a method should be called during verification.
/// Used with Verify() and Verifiable() methods to assert call counts.
/// </summary>
public readonly struct Times : IEquatable<Times>
{
    private readonly int _count;
    private readonly TimesKind _kind;

    private enum TimesKind
    {
        Exactly,
        AtLeast,
        AtMost,
        Never
    }

    private Times(int count, TimesKind kind)
    {
        _count = count;
        _kind = kind;
    }

    /// <summary>Exactly one call expected.</summary>
    public static Times Once => new(1, TimesKind.Exactly);

    /// <summary>Exactly two calls expected.</summary>
    public static Times Twice => new(2, TimesKind.Exactly);

    /// <summary>Exactly n calls expected.</summary>
    public static Times Exactly(int count) => new(count, TimesKind.Exactly);

    /// <summary>At least n calls expected.</summary>
    public static Times AtLeast(int count) => new(count, TimesKind.AtLeast);

    /// <summary>At least one call expected.</summary>
    public static Times AtLeastOnce => new(1, TimesKind.AtLeast);

    /// <summary>At most n calls expected.</summary>
    public static Times AtMost(int count) => new(count, TimesKind.AtMost);

    /// <summary>No calls expected.</summary>
    public static Times Never => new(0, TimesKind.Never);

    /// <summary>The count for this constraint.</summary>
    public int Count => _count;

    /// <summary>Validates if actual call count satisfies this constraint.</summary>
    /// <param name="actualCount">The actual number of calls.</param>
    /// <returns>True if the constraint is satisfied.</returns>
    public bool Validate(int actualCount) => _kind switch
    {
        TimesKind.Exactly => actualCount == _count,
        TimesKind.AtLeast => actualCount >= _count,
        TimesKind.AtMost => actualCount <= _count,
        TimesKind.Never => actualCount == 0,
        _ => false
    };

    /// <inheritdoc />
    public bool Equals(Times other) => _count == other._count && _kind == other._kind;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Times other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_count, _kind);

    /// <inheritdoc />
    public override string ToString() => _kind switch
    {
        TimesKind.Exactly => _count == 1 ? "Once" : _count == 2 ? "Twice" : $"Exactly({_count})",
        TimesKind.AtLeast => _count == 1 ? "AtLeastOnce" : $"AtLeast({_count})",
        TimesKind.AtMost => $"AtMost({_count})",
        TimesKind.Never => "Never",
        _ => "Unknown"
    };

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Times left, Times right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Times left, Times right) => !left.Equals(right);
}
