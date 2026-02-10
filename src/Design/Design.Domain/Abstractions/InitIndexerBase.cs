// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class with init-only indexer
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class with a get/init indexer.
///
/// This class demonstrates:
/// - Init-only indexer accessors on abstract classes
/// - Generator must emit 'init' instead of 'set' in class overrides
/// - Without this fix, CS8853: "must match by init-only of overridden member"
/// </summary>
public abstract class InitIndexerBase
{
    /// <summary>
    /// Abstract indexer with get and init accessors.
    /// </summary>
    public abstract int this[string key] { get; init; }
}

/// <summary>
/// Virtual base class with a get/init indexer.
///
/// This class demonstrates:
/// - Init-only indexer accessors on virtual (non-abstract) classes
/// - Generator must emit 'init' instead of 'set' in class overrides
/// </summary>
public class VirtualInitIndexerBase
{
    private readonly Dictionary<string, int> _data = new();

    public VirtualInitIndexerBase() { }

    /// <summary>
    /// Virtual indexer with get and init accessors.
    /// </summary>
    public virtual int this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0;
        init => _data[key] = value;
    }
}
