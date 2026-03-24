// Suppressions justified: Generated interceptor classes inherit from this type
// and directly access public fields and nested types. Changing visibility would
// break all generated code across all 9 patterns.
#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace KnockOff.Interceptors;

/// <summary>
/// Concrete indexer interceptor with delegate-based fallback.
/// Directly instantiable as a field: <c>public IndexerGetSetInterceptor&lt;string, int&gt; Indexer { get; } = new();</c>
/// Provides top-level Get(), Set(), When(), and per-key access via indexer.
/// </summary>
/// <typeparam name="TKey">The indexer key type.</typeparam>
/// <typeparam name="TValue">The indexer value type.</typeparam>
public class IndexerGetSetInterceptor<TKey, TValue> : IndexerGetSetInterceptorBase<TKey, TValue>
    where TKey : notnull
{
    private Func<TKey, TValue>? _getFallback;
    private Func<TKey, TValue>? _getSourceFallback;
    private Action<TKey, TValue>? _setFallback;
    private Action<TKey, TValue>? _setSourceFallback;

    // Smart default factory (for NewInstance/ThrowException strategies)
    private readonly Func<TValue>? _defaultFactory;

    public IndexerGetSetInterceptor(string memberName) : base()
    {
        _memberName = memberName;
    }

    /// <summary>Constructor with smart default factory for non-strict unconfigured calls.</summary>
    public IndexerGetSetInterceptor(string memberName, Func<TValue> defaultFactory) : base()
    {
        _memberName = memberName;
        _defaultFactory = defaultFactory;
    }

    // Store member name since IndexerGetSetInterceptorBase doesn't have a constructor that takes it
    private readonly string _memberName;

    // ========================================================================
    // Per-key indexer access
    // ========================================================================

    /// <summary>Gets or creates a per-key builder for the specified key. Enables stub.Indexer["key"].Returns(value) syntax.</summary>
    public PerKeyBuilder this[TKey key] => GetOrCreatePerKeyBuilder(key);

    // ========================================================================
    // Top-level Get/Set/When
    // ========================================================================

    /// <summary>Configures a callback for getter invocations. Returns builder for ThenGet chaining.</summary>
    public IndexerGetBuilderBase Get(Func<TKey, TValue> callback)
    {
        _getSequence = null; _getSequenceIndex = 0;
        _isGetVerifiable = false; _getVerifiableTimes = null;
        _get = callback;
        var builder = new IndexerGetBuilderBase(this);
        _getTracking = builder;
        return builder;
    }

    /// <summary>Configures a callback for setter invocations. Returns builder for ThenSet chaining.</summary>
    public IndexerSetBuilderBase Set(Action<TKey, TValue> callback)
    {
        _setSequence = null; _setSequenceIndex = 0;
        _isSetVerifiable = false; _setVerifiableTimes = null;
        _set = callback;
        var builder = new IndexerSetBuilderBase(this);
        _setTracking = builder;
        return builder;
    }

    /// <summary>Configures parameter-specific matching with exact key. Returns When builder for Returns/Get/Set.</summary>
    public IndexerWhenBuilderBase When(TKey key)
    {
        return new IndexerWhenBuilderBase(this, k => object.Equals(k, key));
    }

    /// <summary>Configures parameter-specific matching with predicate. Returns When builder for Returns/Get/Set.</summary>
    public IndexerWhenBuilderBase When(Func<TKey, bool> predicate)
    {
        return new IndexerWhenBuilderBase(this, predicate);
    }

    // ========================================================================
    // Fallback/Source delegation
    // ========================================================================

    /// <summary>Sets the get fallback delegate for stub overrides.</summary>
    public void SetGetFallback(Func<TKey, TValue>? fallback) => _getFallback = fallback;

    /// <summary>Sets the get source fallback delegate for source delegation.</summary>
    public void SetGetSourceFallback(Func<TKey, TValue>? sourceFallback) => _getSourceFallback = sourceFallback;

    /// <summary>Sets the set fallback delegate for stub overrides.</summary>
    public void SetSetFallback(Action<TKey, TValue>? fallback) => _setFallback = fallback;

    /// <summary>Sets the set source fallback delegate for source delegation.</summary>
    public void SetSetSourceFallback(Action<TKey, TValue>? sourceFallback) => _setSourceFallback = sourceFallback;

    /// <summary>Sets both get and set fallback delegates for stub overrides.</summary>
    public void SetFallback(Func<TKey, TValue>? getFallback, Action<TKey, TValue>? setFallback)
    {
        _getFallback = getFallback;
        _setFallback = setFallback;
    }

    /// <summary>Sets both get and set source fallback delegates for source delegation.</summary>
    public void SetSourceFallback(Func<TKey, TValue>? getSourceFallback, Action<TKey, TValue>? setSourceFallback)
    {
        _getSourceFallback = getSourceFallback;
        _setSourceFallback = setSourceFallback;
    }

    /// <summary>Handles unconfigured get with delegate-based fallback chain.</summary>
    protected override TValue InvokeGetUnconfigured(bool strict, TKey key)
    {
        // Fallback (stub override)
        if (_getFallback != null) return _getFallback(key);

        // Source fallback
        if (_getSourceFallback != null) return _getSourceFallback(key);

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);

        // Smart default (NewInstance or ThrowException)
        if (_defaultFactory != null) return _defaultFactory();

        return default!;
    }

    /// <summary>Handles unconfigured set with delegate-based fallback chain.</summary>
    protected override void InvokeSetUnconfigured(bool strict, TKey key, TValue value)
    {
        // Fallback (stub override)
        if (_setFallback != null) { _setFallback(key, value); return; }

        // Source fallback
        if (_setSourceFallback != null) { _setSourceFallback(key, value); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
    }
}
