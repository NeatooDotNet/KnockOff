// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class for demonstrating stub override property patterns
// with standalone class stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class for testing stub override property overrides with standalone class stubs.
/// Tests [KnockOffBase&lt;ConfigBase&gt;] pattern with virtual/abstract properties.
/// </summary>
public abstract class ConfigBase
{
    /// <summary>Abstract get-only property - must be stubbed or overridden.</summary>
    public abstract string ConfigName { get; }

    /// <summary>Virtual get/set property with default value.</summary>
    public virtual int MaxRetries { get; set; } = 3;

    /// <summary>Virtual get-only property.</summary>
    public virtual bool IsDebugMode => false;

    /// <summary>Abstract set-only property - must be stubbed or overridden.</summary>
#pragma warning disable CA1044 // Properties should not be write only - intentional for demo
    public abstract string LogPath { set; }
#pragma warning restore CA1044
}

/// <summary>
/// Generic abstract base class for testing stub override property overrides with
/// generic standalone class stubs.
/// Tests [KnockOffBase(typeof(CacheBase&lt;&gt;))] pattern.
/// </summary>
/// <typeparam name="T">The cached item type.</typeparam>
public abstract class CacheBase<T> where T : class
{
    /// <summary>Abstract get-only property returning generic type.</summary>
    public abstract T? DefaultValue { get; }

    /// <summary>Virtual get/set property with generic type.</summary>
    public virtual T? CurrentValue { get; set; }

    /// <summary>Virtual get-only property returning count.</summary>
    public virtual int CacheSize => 0;

    /// <summary>Abstract get-only property for name.</summary>
    public abstract string CacheName { get; }
}
