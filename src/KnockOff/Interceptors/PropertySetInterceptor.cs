#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace KnockOff.Interceptors;

/// <summary>
/// Concrete set-only property interceptor with delegate-based fallback.
/// Directly instantiable as a field: <c>public PropertySetInterceptor&lt;string&gt; Name { get; } = new("Name");</c>
/// </summary>
/// <typeparam name="TValue">The property value type.</typeparam>
public class PropertySetInterceptor<TValue> : PropertySetInterceptorBase<TValue>
{
    private Action<TValue>? _fallback;
    private Action<TValue>? _sourceFallback;

    public PropertySetInterceptor(string memberName) : base(memberName) { }

    /// <summary>Sets the fallback delegate for stub overrides.</summary>
    public void SetFallback(Action<TValue>? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation.</summary>
    public void SetSourceFallback(Action<TValue>? sourceFallback) => _sourceFallback = sourceFallback;

    /// <summary>Handles unconfigured set with delegate-based fallback chain.</summary>
    protected override void InvokeSetUnconfigured(bool strict, TValue value)
    {
        // Fallback (stub override)
        if (_fallback != null) { _fallback(value); return; }

        // Source fallback
        if (_sourceFallback != null) { _sourceFallback(value); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
    }
}
