namespace KnockOff;

/// <summary>
/// Specifies how many times a callback should be used (sequencing)
/// or how many times it should have been called (verification).
/// </summary>
public readonly struct Times : IEquatable<Times>
{
    private readonly int _count;
    private readonly TimesKind _kind;

    private enum TimesKind
    {
        Exactly,
        Forever,
        AtLeast,
        AtMost,
        Never
    }

    private Times(int count, TimesKind kind)
    {
        _count = count;
        _kind = kind;
    }

    /// <summary>Use once, then advance to next callback.</summary>
    public static Times Once => new(1, TimesKind.Exactly);

    /// <summary>Use twice, then advance to next callback.</summary>
    public static Times Twice => new(2, TimesKind.Exactly);

    /// <summary>Use exactly n times, then advance to next callback.</summary>
    public static Times Exactly(int count) => new(count, TimesKind.Exactly);

    /// <summary>Repeat indefinitely (never advance).</summary>
    public static Times Forever => new(0, TimesKind.Forever);

    /// <summary>Verification: must be called at least n times.</summary>
    public static Times AtLeast(int count) => new(count, TimesKind.AtLeast);

    /// <summary>Verification: must be called at most n times.</summary>
    public static Times AtMost(int count) => new(count, TimesKind.AtMost);

    /// <summary>Verification: must never be called.</summary>
    public static Times Never => new(0, TimesKind.Never);

    /// <summary>The count for Exactly/Once/Twice.</summary>
    public int Count => _count;

    /// <summary>True if this represents Forever (indefinite repeat).</summary>
    public bool IsForever => _kind == TimesKind.Forever;

    /// <summary>True if this is for verification (AtLeast/AtMost/Never).</summary>
    public bool IsVerification => _kind is TimesKind.AtLeast or TimesKind.AtMost or TimesKind.Never;

    /// <summary>Verify if actual call count satisfies this constraint.</summary>
    public bool Verify(int actualCount) => _kind switch
    {
        TimesKind.Exactly => actualCount == _count,
        TimesKind.Forever => true,
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

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Times left, Times right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Times left, Times right) => !left.Equals(right);
}
