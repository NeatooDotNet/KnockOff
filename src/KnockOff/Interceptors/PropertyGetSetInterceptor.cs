// Suppressions justified: Generated interceptor classes inherit from this type
// and directly access public fields and nested types. Changing visibility would
// break all generated code across all 9 patterns.
#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace KnockOff.Interceptors;

/// <summary>
/// Concrete get+set property interceptor with delegate-based fallback.
/// Directly instantiable as a field: <c>public PropertyGetSetInterceptor&lt;string&gt; Name { get; } = new("Name");</c>
/// Supports round-trip storage: value set via setter can be read back via getter.
/// </summary>
/// <typeparam name="TValue">The property value type.</typeparam>
public class PropertyGetSetInterceptor<TValue> : PropertyGetSetInterceptorBase<TValue>
{
    private Func<TValue>? _getFallback;
    private Func<TValue>? _getSourceFallback;
    private Action<TValue>? _setFallback;
    private Action<TValue>? _setSourceFallback;

    // Smart default factory (for NewInstance/ThrowException strategies)
    private readonly Func<TValue>? _defaultFactory;

    public PropertyGetSetInterceptor(string memberName) : base(memberName) { }

    /// <summary>Constructor with smart default factory for non-strict unconfigured calls.</summary>
    public PropertyGetSetInterceptor(string memberName, Func<TValue> defaultFactory) : base(memberName)
    {
        _defaultFactory = defaultFactory;
    }

    /// <summary>Sets the get fallback delegate for stub overrides.</summary>
    public void SetGetFallback(Func<TValue>? fallback) => _getFallback = fallback;

    /// <summary>Sets the get source fallback delegate for source delegation.</summary>
    public void SetGetSourceFallback(Func<TValue>? sourceFallback) => _getSourceFallback = sourceFallback;

    /// <summary>Sets the set fallback delegate for stub overrides.</summary>
    public void SetSetFallback(Action<TValue>? fallback) => _setFallback = fallback;

    /// <summary>Sets the set source fallback delegate for source delegation.</summary>
    public void SetSetSourceFallback(Action<TValue>? sourceFallback) => _setSourceFallback = sourceFallback;

    /// <summary>Sets both get and set fallback delegates for stub overrides.</summary>
    public void SetFallback(Func<TValue>? getFallback, Action<TValue>? setFallback)
    {
        _getFallback = getFallback;
        _setFallback = setFallback;
    }

    /// <summary>Sets both get and set source fallback delegates for source delegation.</summary>
    public void SetSourceFallback(Func<TValue>? getSourceFallback, Action<TValue>? setSourceFallback)
    {
        _getSourceFallback = getSourceFallback;
        _setSourceFallback = setSourceFallback;
    }

    /// <summary>Handles unconfigured get after round-trip check fails.</summary>
    protected override TValue InvokeGetUnconfiguredFinal(bool strict)
    {
        // Fallback (stub override)
        if (_getFallback != null) return _getFallback();

        // Source fallback
        if (_getSourceFallback != null) return _getSourceFallback();

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);

        // Smart default (NewInstance or ThrowException)
        if (_defaultFactory != null) return _defaultFactory();

        return default!;
    }

    /// <summary>Handles unconfigured set with delegate-based fallback chain.</summary>
    protected override void InvokeSetUnconfigured(bool strict, TValue value)
    {
        // Fallback (stub override)
        if (_setFallback != null) { _setFallback(value); return; }

        // Source fallback
        if (_setSourceFallback != null) { _setSourceFallback(value); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
        // If not strict, base class stores round-trip value after this returns.
    }
}
