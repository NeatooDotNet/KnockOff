namespace Prototype.Library;

/// <summary>
/// Zero-size sentinel type used as TArgs for zero-parameter methods.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = default;
}
