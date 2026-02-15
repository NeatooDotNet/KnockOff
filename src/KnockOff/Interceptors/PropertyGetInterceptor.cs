#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace KnockOff.Interceptors;

/// <summary>
/// Concrete get-only property interceptor with delegate-based fallback.
/// Directly instantiable as a field: <c>public PropertyGetInterceptor&lt;string&gt; Name { get; } = new("Name");</c>
/// </summary>
/// <typeparam name="TValue">The property value type.</typeparam>
public class PropertyGetInterceptor<TValue> : PropertyGetInterceptorBase<TValue>
{
    private Func<TValue>? _fallback;
    private Func<TValue>? _sourceFallback;

    // Smart default factory (for NewInstance/ThrowException strategies)
    private readonly Func<TValue>? _defaultFactory;

    public PropertyGetInterceptor(string memberName) : base(memberName) { }

    /// <summary>Constructor with smart default factory for non-strict unconfigured calls.</summary>
    public PropertyGetInterceptor(string memberName, Func<TValue> defaultFactory) : base(memberName)
    {
        _defaultFactory = defaultFactory;
    }

    /// <summary>Sets the fallback delegate for stub overrides.</summary>
    public void SetFallback(Func<TValue>? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation.</summary>
    public void SetSourceFallback(Func<TValue>? sourceFallback) => _sourceFallback = sourceFallback;

    /// <summary>Handles unconfigured get with delegate-based fallback chain.</summary>
    protected override TValue InvokeGetUnconfigured(bool strict)
    {
        // Fallback (stub override)
        if (_fallback != null) return _fallback();

        // Source fallback
        if (_sourceFallback != null) return _sourceFallback();

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);

        // Smart default (NewInstance or ThrowException)
        if (_defaultFactory != null) return _defaultFactory();

        return default!;
    }
}
