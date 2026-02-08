namespace KnockOff;

/// <summary>
/// Specifies the expected number of times a method should be called during verification.
/// Used with Verify() and Verifiable() methods to assert call counts.
/// </summary>
public readonly struct Called : IEquatable<Called>
{
    private readonly int _count;
    private readonly CalledKind _kind;

    private enum CalledKind
    {
        Exactly,
        AtLeast,
        AtMost,
        Never
    }

    private Called(int count, CalledKind kind)
    {
        _count = count;
        _kind = kind;
    }

    /// <summary>Exactly one call expected.</summary>
    public static Called Once => new(1, CalledKind.Exactly);

    /// <summary>Exactly two calls expected.</summary>
    public static Called Twice => new(2, CalledKind.Exactly);

    /// <summary>Exactly n calls expected.</summary>
    public static Called Exactly(int count) => new(count, CalledKind.Exactly);

    /// <summary>At least n calls expected.</summary>
    public static Called AtLeast(int count) => new(count, CalledKind.AtLeast);

    /// <summary>At least one call expected.</summary>
    public static Called AtLeastOnce => new(1, CalledKind.AtLeast);

    /// <summary>At most n calls expected.</summary>
    public static Called AtMost(int count) => new(count, CalledKind.AtMost);

    /// <summary>No calls expected.</summary>
    public static Called Never => new(0, CalledKind.Never);

    /// <summary>The count for this constraint.</summary>
    public int Count => _count;

    /// <summary>Validates if actual call count satisfies this constraint.</summary>
    /// <param name="actualCount">The actual number of calls.</param>
    /// <returns>True if the constraint is satisfied.</returns>
    public bool Validate(int actualCount) => _kind switch
    {
        CalledKind.Exactly => actualCount == _count,
        CalledKind.AtLeast => actualCount >= _count,
        CalledKind.AtMost => actualCount <= _count,
        CalledKind.Never => actualCount == 0,
        _ => false
    };

    /// <inheritdoc />
    public bool Equals(Called other) => _count == other._count && _kind == other._kind;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Called other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_count, _kind);

    /// <inheritdoc />
    public override string ToString() => _kind switch
    {
        CalledKind.Exactly => _count == 1 ? "Once" : _count == 2 ? "Twice" : $"Exactly({_count})",
        CalledKind.AtLeast => _count == 1 ? "AtLeastOnce" : $"AtLeast({_count})",
        CalledKind.AtMost => $"AtMost({_count})",
        CalledKind.Never => "Never",
        _ => "Unknown"
    };

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Called left, Called right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Called left, Called right) => !left.Equals(right);
}
