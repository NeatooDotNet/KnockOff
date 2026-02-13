using KnockOff;

namespace Prototype.Library.Interceptors;

/// <summary>
/// Base class for indexer interceptors with both get and set support.
/// Handles per-key builders, dual get/set sequences, dual When chains,
/// verification, and source delegation.
/// </summary>
/// <typeparam name="TKey">The indexer key type (single type or ValueTuple for multi-key).</typeparam>
/// <typeparam name="TValue">The indexer value type.</typeparam>
public abstract class IndexerGetSetInterceptorBase<TKey, TValue>
    where TKey : notnull
{
    // Per-key builders
    private readonly Dictionary<TKey, PerKeyBuilder> _perKeyBuilders = new();

    // Get fields
    protected Func<TKey, TValue>? _get;
    protected IndexerGetBuilderBase? _getTracking;
    protected List<(Func<TKey, TValue> Callback, IndexerGetBuilderBase Tracking)>? _getSequence;
    protected int _getSequenceIndex;
    protected bool _getRepeatLastValue = true;
    protected bool _isGetVerifiable;
    protected Called? _getVerifiableTimes;
    protected int _unconfiguredGetCount;
    protected TKey? _unconfiguredLastGetKey;

    // Set fields
    protected Action<TKey, TValue>? _set;
    protected IndexerSetBuilderBase? _setTracking;
    protected List<(Action<TKey, TValue> Callback, IndexerSetBuilderBase Tracking)>? _setSequence;
    protected int _setSequenceIndex;
    protected bool _setRepeatLastValue = true;
    protected bool _isSetVerifiable;
    protected Called? _setVerifiableTimes;
    protected int _unconfiguredSetCount;
    protected (TKey Key, TValue Value)? _unconfiguredLastSetEntry;

    // Get When chain
    protected List<IndexerGetWhenMatcherBase>? _whenGetChain;
    protected int _whenGetChainHead;
    protected bool _whenGetVerifiable;

    // Set When chain
    protected List<IndexerSetWhenMatcherBase>? _whenSetChain;
    protected int _whenSetChainHead;
    protected bool _whenSetVerifiable;

    // ========================================================================
    // Total counts
    // ========================================================================

    protected int TotalGetCount
    {
        get
        {
            var sum = _unconfiguredGetCount + (_getTracking?._callCount ?? 0);
            if (_getSequence != null)
                foreach (var s in _getSequence) sum += s.Tracking._callCount;
            foreach (var b in _perKeyBuilders.Values) sum += b._getCallCount;
            return sum;
        }
    }

    protected int TotalSetCount
    {
        get
        {
            var sum = _unconfiguredSetCount + (_setTracking?._callCount ?? 0);
            if (_setSequence != null)
                foreach (var s in _setSequence) sum += s.Tracking._callCount;
            foreach (var b in _perKeyBuilders.Values) sum += b._setCallCount;
            return sum;
        }
    }

    // ========================================================================
    // Per-key builder access
    // ========================================================================

    /// <summary>Gets or creates a per-key builder for the specified key.</summary>
    public PerKeyBuilder GetOrCreatePerKeyBuilder(TKey key)
    {
        return _perKeyBuilders.TryGetValue(key, out var existing) ? existing : (_perKeyBuilders[key] = new PerKeyBuilder());
    }

    // ========================================================================
    // Last access tracking
    // ========================================================================

    /// <summary>The key from the last getter access.</summary>
    public TKey? LastGetKey => _unconfiguredLastGetKey;

    /// <summary>The key-value pair from the last setter access.</summary>
    public (TKey Key, TValue Value)? LastSetEntry => _unconfiguredLastSetEntry;

    // ========================================================================
    // InvokeGet
    // ========================================================================

    /// <summary>Invokes the configured getter. Called by explicit interface implementation.</summary>
    public TValue InvokeGet(bool strict, TKey key)
    {
        _unconfiguredLastGetKey = key;

        // Per-key builder
        if (_perKeyBuilders.TryGetValue(key, out var perKeyBuilder) && perKeyBuilder.HasGetConfig)
        {
            perKeyBuilder._getCallCount++;
            return perKeyBuilder.InvokeGet();
        }

        // When chain
        if (_whenGetChain != null && _whenGetChainHead < _whenGetChain.Count)
        {
            var matcher = _whenGetChain[_whenGetChainHead];
            if (matcher.Matches(key))
            {
                matcher.CallCount++;
                if (_whenGetChainHead < _whenGetChain.Count - 1)
                    _whenGetChainHead++;
                return matcher.Call(key);
            }
            else if (matcher.IsTerminal)
            {
                _whenGetChainHead++;
            }
        }

        // Sequence
        if (_getSequence != null && _getSequenceIndex < _getSequence.Count)
        {
            var (callback, tracking) = _getSequence[_getSequenceIndex];
            tracking.RecordCall(key);
            _getSequenceIndex++;
            return callback(key);
        }

        // Sequence exhausted repeat
        if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted("Indexer (get)");
            if (_getRepeatLastValue && _getSequence.Count > 0)
            {
                var (callback, tracking) = _getSequence[_getSequence.Count - 1];
                tracking.RecordCall(key);
                return callback(key);
            }
        }

        // Callback
        if (_get != null && _getTracking != null)
        {
            _getTracking.RecordCall(key);
            return _get(key);
        }

        _unconfiguredGetCount++;

        return InvokeGetUnconfigured(strict, key);
    }

    /// <summary>Handles unconfigured get. Override for source delegation / strict mode.</summary>
    protected abstract TValue InvokeGetUnconfigured(bool strict, TKey key);

    // ========================================================================
    // InvokeSet
    // ========================================================================

    /// <summary>Invokes the configured setter. Called by explicit interface implementation.</summary>
    public void InvokeSet(bool strict, TKey key, TValue value)
    {
        _unconfiguredLastSetEntry = (key, value);

        // Per-key builder
        if (_perKeyBuilders.TryGetValue(key, out var perKeyBuilder) && perKeyBuilder.HasSetConfig)
        {
            perKeyBuilder._setCallCount++;
            perKeyBuilder.InvokeSet(value);
            return;
        }

        // When chain
        if (_whenSetChain != null && _whenSetChainHead < _whenSetChain.Count)
        {
            var matcher = _whenSetChain[_whenSetChainHead];
            if (matcher.Matches(key))
            {
                matcher.CallCount++;
                if (_whenSetChainHead < _whenSetChain.Count - 1)
                    _whenSetChainHead++;
                matcher.Call(key, value);
                return;
            }
            else if (matcher.IsTerminal)
            {
                _whenSetChainHead++;
            }
        }

        // Sequence
        if (_setSequence != null && _setSequenceIndex < _setSequence.Count)
        {
            var (callback, tracking) = _setSequence[_setSequenceIndex];
            tracking.RecordCall(key, value);
            _setSequenceIndex++;
            callback(key, value);
            return;
        }

        // Sequence exhausted repeat
        if (_setSequence != null && _setSequenceIndex >= _setSequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted("Indexer (set)");
            if (_setRepeatLastValue && _setSequence.Count > 0)
            {
                var (callback, tracking) = _setSequence[_setSequence.Count - 1];
                tracking.RecordCall(key, value);
                callback(key, value);
                return;
            }
            return;
        }

        // Callback
        if (_set != null && _setTracking != null)
        {
            _setTracking.RecordCall(key, value);
            _set(key, value);
            return;
        }

        _unconfiguredSetCount++;

        InvokeSetUnconfigured(strict, key, value);
    }

    /// <summary>Handles unconfigured set. Override for source delegation / strict mode.</summary>
    protected abstract void InvokeSetUnconfigured(bool strict, TKey key, TValue value);

    // ========================================================================
    // Reset
    // ========================================================================

    /// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>
    public virtual void Reset()
    {
        _unconfiguredGetCount = 0;
        _unconfiguredLastGetKey = default;
        _getTracking?.Reset();
        if (_getSequence != null)
            foreach (var (_, tracking) in _getSequence)
                tracking.Reset();
        _getSequenceIndex = 0;

        _unconfiguredSetCount = 0;
        _unconfiguredLastSetEntry = default;
        _setTracking?.Reset();
        if (_setSequence != null)
            foreach (var (_, tracking) in _setSequence)
                tracking.Reset();
        _setSequenceIndex = 0;

        _whenGetChainHead = 0;
        if (_whenGetChain != null) foreach (var m in _whenGetChain) m.CallCount = 0;
        _whenSetChainHead = 0;
        if (_whenSetChain != null) foreach (var m in _whenSetChain) m.CallCount = 0;

        foreach (var b in _perKeyBuilders.Values) b.Reset();
    }

    // ========================================================================
    // Verification
    // ========================================================================

    /// <summary>Verifies the indexer was accessed at least once.</summary>
    public void Verify() => Verify(Called.AtLeastOnce);

    /// <summary>Verifies total access count satisfies the Called constraint.</summary>
    public void Verify(Called times)
    {
        var totalCount = TotalGetCount + TotalSetCount;
        if (!times.Validate(totalCount))
            throw new VerificationException(new VerificationFailure("Indexer", times, totalCount));
    }

    /// <summary>Verifies the getter was accessed at least once.</summary>
    public void VerifyGet() => VerifyGet(Called.AtLeastOnce);

    /// <summary>Verifies getter access count satisfies the Called constraint.</summary>
    public void VerifyGet(Called times)
    {
        if (!times.Validate(TotalGetCount))
            throw new VerificationException(new VerificationFailure("Indexer (get)", times, TotalGetCount));
    }

    /// <summary>Verifies the setter was accessed at least once.</summary>
    public void VerifySet() => VerifySet(Called.AtLeastOnce);

    /// <summary>Verifies setter access count satisfies the Called constraint.</summary>
    public void VerifySet(Called times)
    {
        if (!times.Validate(TotalSetCount))
            throw new VerificationException(new VerificationFailure("Indexer (set)", times, TotalSetCount));
    }

    /// <summary>Marks this indexer for verification by Stub.Verify().</summary>
    public IndexerGetSetInterceptorBase<TKey, TValue> Verifiable()
    {
        _isGetVerifiable = true; _getVerifiableTimes = null;
        _isSetVerifiable = true; _setVerifiableTimes = null;
        return this;
    }

    /// <summary>Marks this indexer for verification by Stub.Verify() with Called constraint.</summary>
    public IndexerGetSetInterceptorBase<TKey, TValue> Verifiable(Called times)
    {
        _isGetVerifiable = true; _getVerifiableTimes = times;
        _isSetVerifiable = true; _setVerifiableTimes = times;
        return this;
    }

    /// <summary>Whether this indexer was marked with Verifiable().</summary>
    public bool IsVerifiable => _isGetVerifiable || _whenGetVerifiable || _isSetVerifiable || _whenSetVerifiable;

    /// <summary>Whether this indexer has been configured.</summary>
    public bool IsConfigured
    {
        get
        {
            foreach (var b in _perKeyBuilders.Values)
                if (b.HasGetConfig || b.HasSetConfig) return true;
            if (_get != null || (_getSequence?.Count ?? 0) > 0) return true;
            if (_set != null || (_setSequence?.Count ?? 0) > 0) return true;
            return false;
        }
    }

    /// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>
    public VerificationFailure? CheckVerification()
    {
        if (!IsVerifiable) return null;
        if (_isGetVerifiable || _isSetVerifiable)
        {
            var times = Called.AtLeastOnce;
            if (_getVerifiableTimes != null) times = _getVerifiableTimes.Value;
            if (_setVerifiableTimes != null) times = _setVerifiableTimes.Value;
            var totalCount = TotalGetCount + TotalSetCount;
            if (!times.Validate(totalCount))
                return new VerificationFailure("Indexer", times, totalCount);
        }
        if (_whenGetVerifiable && _whenGetChain != null && _whenGetChain.Count > 0)
        {
            var head = _whenGetChainHead;
            var count = _whenGetChain.Count;
            if (head < count && !_whenGetChain[head].IsTerminal && _whenGetChain[head].CallCount == 0)
                return VerificationFailure.SequenceIncomplete("indexer getter When chain", count, head);
        }
        if (_whenSetVerifiable && _whenSetChain != null && _whenSetChain.Count > 0)
        {
            var head = _whenSetChainHead;
            var count = _whenSetChain.Count;
            if (head < count && !_whenSetChain[head].IsTerminal && _whenSetChain[head].CallCount == 0)
                return VerificationFailure.SequenceIncomplete("indexer setter When chain", count, head);
        }
        return null;
    }

    /// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>
    public VerificationFailure? CheckVerificationAll()
    {
        if (!IsConfigured) return null;
        var totalCount = TotalGetCount + TotalSetCount;
        return totalCount >= 1 ? null : new VerificationFailure("Indexer", Called.AtLeastOnce, totalCount);
    }

    // ========================================================================
    // Inner class: PerKeyBuilder
    // ========================================================================

    /// <summary>Per-key builder for configuring behavior for a specific key.</summary>
    public sealed class PerKeyBuilder
    {
        private TValue _getValue = default!;
        private bool _hasValue;
        private Func<TValue>? _getCallback;
        internal List<TValue>? _getSequence;
        private int _getSequenceIndex;

        private Action<TValue>? _setCallback;

        internal int _getCallCount;
        internal int _setCallCount;

        /// <summary>Whether getter configuration has been set.</summary>
        public bool HasGetConfig => _hasValue || _getCallback != null || _getSequence != null;

        /// <summary>Whether setter configuration has been set.</summary>
        public bool HasSetConfig => _setCallback != null;

        /// <summary>Configures this key to return the specified value.</summary>
        public PerKeyBuilder Returns(TValue value)
        {
            _getValue = value;
            _hasValue = true;
            _getCallback = null;
            _getSequence = null;
            _getSequenceIndex = 0;
            return this;
        }

        /// <summary>Adds another value to the per-key return sequence.</summary>
        public PerKeySequence ThenReturns(TValue value)
        {
            if (_getSequence == null)
            {
                _getSequence = new List<TValue>();
                if (_hasValue) _getSequence.Add(_getValue!);
            }
            _getSequence.Add(value);
            _hasValue = false;
            _getCallback = null;
            return new PerKeySequence(this);
        }

        /// <summary>Configures this key to use the specified callback for getter.</summary>
        public PerKeyBuilder Get(Func<TValue> callback)
        {
            _getCallback = callback;
            _hasValue = false;
            _getValue = default!;
            _getSequence = null;
            _getSequenceIndex = 0;
            return this;
        }

        /// <summary>Configures this key to use the specified callback for setter.</summary>
        public PerKeyBuilder Set(Action<TValue> callback)
        {
            _setCallback = callback;
            return this;
        }

        /// <summary>Invokes the configured getter for this key.</summary>
        public TValue InvokeGet()
        {
            if (_getSequence != null)
            {
                if (_getSequenceIndex < _getSequence.Count)
                    return _getSequence[_getSequenceIndex++];
                return _getSequence[_getSequence.Count - 1];
            }
            if (_getCallback != null) return _getCallback();
            return _getValue!;
        }

        /// <summary>Invokes the configured setter for this key.</summary>
        public void InvokeSet(TValue value)
        {
            _setCallback?.Invoke(value);
        }

        /// <summary>Verifies this key's getter was called at least once.</summary>
        public void VerifyGet() => VerifyGet(Called.AtLeastOnce);

        /// <summary>Verifies this key's getter call count satisfies the Called constraint.</summary>
        public void VerifyGet(Called times)
        {
            if (!times.Validate(_getCallCount))
                throw new VerificationException(new VerificationFailure("indexer getter (per-key)", times, _getCallCount));
        }

        /// <summary>Verifies this key's setter was called at least once.</summary>
        public void VerifySet() => VerifySet(Called.AtLeastOnce);

        /// <summary>Verifies this key's setter call count satisfies the Called constraint.</summary>
        public void VerifySet(Called times)
        {
            if (!times.Validate(_setCallCount))
                throw new VerificationException(new VerificationFailure("indexer setter (per-key)", times, _setCallCount));
        }

        /// <summary>Resets call counts for this per-key builder.</summary>
        public void Reset()
        {
            _getCallCount = 0;
            _getSequenceIndex = 0;
            _setCallCount = 0;
        }
    }

    // ========================================================================
    // Inner class: PerKeySequence
    // ========================================================================

    /// <summary>Per-key sequence for chaining multiple return values.</summary>
    public sealed class PerKeySequence
    {
        private readonly PerKeyBuilder _builder;

        internal PerKeySequence(PerKeyBuilder builder) => _builder = builder;

        /// <summary>Adds another value to the per-key return sequence.</summary>
        public PerKeySequence ThenReturns(TValue value)
        {
            _builder._getSequence!.Add(value);
            return this;
        }
    }

    // ========================================================================
    // Inner class: IndexerGetBuilderBase
    // ========================================================================

    /// <summary>Builder for getter callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public class IndexerGetBuilderBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;
        private TKey _lastKey = default!;
        public int _callCount;

        public IndexerGetBuilderBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor) => _interceptor = interceptor;

        /// <summary>Last key passed to this getter callback.</summary>
        public TKey LastKey => _lastKey;

        /// <summary>Records a call to this callback.</summary>
        public void RecordCall(TKey key) { _callCount++; _lastKey = key; }

        /// <summary>Resets tracking state.</summary>
        public void Reset() { _callCount = 0; _lastKey = default!; }

        /// <summary>Verifies callback was invoked at least once.</summary>
        public void Verify() => Verify(Called.AtLeastOnce);

        /// <summary>Verifies call count satisfies the Called constraint.</summary>
        public void Verify(Called times)
        {
            if (!times.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("indexer getter", times, _callCount));
        }

        /// <summary>Elevates to sequence mode and adds another getter callback.</summary>
        protected IndexerGetSequenceBase ThenGetBase(Func<TKey, TValue> callback)
        {
            if (_interceptor._getSequence == null)
            {
                _interceptor._getSequence = new List<(Func<TKey, TValue> Callback, IndexerGetBuilderBase Tracking)>();
                _interceptor._getSequence.Add((_interceptor._get!, this));
                _interceptor._get = null;
                _interceptor._getTracking = null;
                _interceptor._getSequenceIndex = 0;
            }
            var nextBuilder = new IndexerGetBuilderBase(_interceptor);
            _interceptor._getSequence.Add((callback, nextBuilder));
            return new IndexerGetSequenceBase(_interceptor);
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public void VerifiableBase()
        {
            _interceptor._isGetVerifiable = true;
            _interceptor._getVerifiableTimes = null;
        }
    }

    // ========================================================================
    // Inner class: IndexerGetSequenceBase
    // ========================================================================

    /// <summary>Sequence implementation for ThenGet chaining.</summary>
    public class IndexerGetSequenceBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;

        public IndexerGetSequenceBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor) => _interceptor = interceptor;

        /// <summary>Adds another getter callback to the sequence.</summary>
        protected IndexerGetSequenceBase ThenGetBase(Func<TKey, TValue> callback)
        {
            var tracking = new IndexerGetBuilderBase(_interceptor);
            _interceptor._getSequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Verifies the entire sequence was executed.</summary>
        public void Verify()
        {
            if (_interceptor._getSequence == null) return;
            var sequenceLength = _interceptor._getSequence.Count;
            var completedCount = _interceptor._getSequenceIndex;
            if (completedCount < sequenceLength)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("indexer getter", sequenceLength, completedCount));
        }

        /// <summary>Resets all tracking in the sequence.</summary>
        public void Reset() => _interceptor.Reset();

        /// <summary>Marks this sequence for verification by Stub.Verify().</summary>
        public void VerifiableBase()
        {
            _interceptor._isGetVerifiable = true;
            _interceptor._getVerifiableTimes = null;
        }

        /// <summary>Terminates sequence with default(T) after exhaustion instead of repeating last value.</summary>
        public void ThenDefault()
        {
            _interceptor._getRepeatLastValue = false;
        }
    }

    // ========================================================================
    // Inner class: IndexerSetBuilderBase
    // ========================================================================

    /// <summary>Builder for setter callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public class IndexerSetBuilderBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;
        private (TKey Key, TValue Value)? _lastEntry;
        public int _callCount;

        public IndexerSetBuilderBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor) => _interceptor = interceptor;

        /// <summary>Last key and value passed to this setter callback.</summary>
        public (TKey Key, TValue Value)? LastEntry => _lastEntry;

        /// <summary>Records a call to this callback.</summary>
        public void RecordCall(TKey key, TValue value) { _callCount++; _lastEntry = (key, value); }

        /// <summary>Resets tracking state.</summary>
        public void Reset() { _callCount = 0; _lastEntry = null; }

        /// <summary>Verifies callback was invoked at least once.</summary>
        public void Verify() => Verify(Called.AtLeastOnce);

        /// <summary>Verifies call count satisfies the Called constraint.</summary>
        public void Verify(Called times)
        {
            if (!times.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("indexer setter", times, _callCount));
        }

        /// <summary>Elevates to sequence mode and adds another setter callback.</summary>
        protected IndexerSetSequenceBase ThenSetBase(Action<TKey, TValue> callback)
        {
            if (_interceptor._setSequence == null)
            {
                _interceptor._setSequence = new List<(Action<TKey, TValue> Callback, IndexerSetBuilderBase Tracking)>();
                _interceptor._setSequence.Add((_interceptor._set!, this));
                _interceptor._set = null;
                _interceptor._setTracking = null;
                _interceptor._setSequenceIndex = 0;
            }
            var nextBuilder = new IndexerSetBuilderBase(_interceptor);
            _interceptor._setSequence.Add((callback, nextBuilder));
            return new IndexerSetSequenceBase(_interceptor);
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public void VerifiableBase()
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = null;
        }
    }

    // ========================================================================
    // Inner class: IndexerSetSequenceBase
    // ========================================================================

    /// <summary>Sequence implementation for ThenSet chaining.</summary>
    public class IndexerSetSequenceBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;

        public IndexerSetSequenceBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor) => _interceptor = interceptor;

        /// <summary>Adds another setter callback to the sequence.</summary>
        protected IndexerSetSequenceBase ThenSetBase(Action<TKey, TValue> callback)
        {
            var tracking = new IndexerSetBuilderBase(_interceptor);
            _interceptor._setSequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Verifies the entire sequence was executed.</summary>
        public void Verify()
        {
            if (_interceptor._setSequence == null) return;
            var sequenceLength = _interceptor._setSequence.Count;
            var completedCount = _interceptor._setSequenceIndex;
            if (completedCount < sequenceLength)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("indexer setter", sequenceLength, completedCount));
        }

        /// <summary>Resets all tracking in the sequence.</summary>
        public void Reset() => _interceptor.Reset();

        /// <summary>Marks this sequence for verification by Stub.Verify().</summary>
        public void VerifiableBase()
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = null;
        }

        /// <summary>Terminates sequence after exhaustion instead of repeating last callback.</summary>
        public void ThenDefault()
        {
            _interceptor._setRepeatLastValue = false;
        }
    }

    // ========================================================================
    // Inner class: IndexerGetWhenMatcherBase
    // ========================================================================

    /// <summary>Abstract base for indexer getter When chain matchers.</summary>
    public abstract class IndexerGetWhenMatcherBase
    {
        public abstract bool Matches(TKey key);
        public abstract TValue Call(TKey key);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Getter matcher that uses a predicate and returns a stored value.</summary>
    public class IndexerGetWhenMatcherValue : IndexerGetWhenMatcherBase
    {
        private readonly Func<TKey, bool> _predicate;
        private readonly TValue _value;

        public IndexerGetWhenMatcherValue(Func<TKey, bool> predicate, TValue value)
        {
            _predicate = predicate;
            _value = value;
        }

        public override bool Matches(TKey key) => _predicate(key);
        public override TValue Call(TKey key) => _value;
        public override bool IsTerminal => false;
    }

    /// <summary>Getter matcher that uses a predicate and invokes a callback.</summary>
    public class IndexerGetWhenMatcherCallback : IndexerGetWhenMatcherBase
    {
        private readonly Func<TKey, bool> _predicate;
        private readonly Func<TKey, TValue> _callback;

        public IndexerGetWhenMatcherCallback(Func<TKey, bool> predicate, Func<TKey, TValue> callback)
        {
            _predicate = predicate;
            _callback = callback;
        }

        public override bool Matches(TKey key) => _predicate(key);
        public override TValue Call(TKey key) => _callback(key);
        public override bool IsTerminal => false;
    }

    /// <summary>Getter matcher that never matches. Terminal.</summary>
    public class IndexerGetWhenMatcherNone : IndexerGetWhenMatcherBase
    {
        public override bool Matches(TKey key) => false;
        public override TValue Call(TKey key) => default!;
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: IndexerSetWhenMatcherBase
    // ========================================================================

    /// <summary>Abstract base for indexer setter When chain matchers.</summary>
    public abstract class IndexerSetWhenMatcherBase
    {
        public abstract bool Matches(TKey key);
        public abstract void Call(TKey key, TValue value);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Setter matcher that uses a predicate and invokes a callback.</summary>
    public class IndexerSetWhenMatcherCallback : IndexerSetWhenMatcherBase
    {
        private readonly Func<TKey, bool> _predicate;
        private readonly Action<TKey, TValue> _callback;

        public IndexerSetWhenMatcherCallback(Func<TKey, bool> predicate, Action<TKey, TValue> callback)
        {
            _predicate = predicate;
            _callback = callback;
        }

        public override bool Matches(TKey key) => _predicate(key);
        public override void Call(TKey key, TValue value) => _callback(key, value);
        public override bool IsTerminal => false;
    }

    /// <summary>Setter matcher that never matches. Terminal.</summary>
    public class IndexerSetWhenMatcherNone : IndexerSetWhenMatcherBase
    {
        public override bool Matches(TKey key) => false;
        public override void Call(TKey key, TValue value) { }
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: IndexerWhenBuilderBase
    // ========================================================================

    /// <summary>Builder for indexer When matchers. Routes Returns()/Get() to getter chain and Set() to setter chain.</summary>
    public class IndexerWhenBuilderBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;
        protected readonly Func<TKey, bool> _predicate;

        public IndexerWhenBuilderBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor, Func<TKey, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
        }

        /// <summary>Configures the return value when predicate matches (getter chain).</summary>
        public IndexerGetWhenChainBase ReturnsBase(TValue value)
        {
            _interceptor._whenGetChain ??= new List<IndexerGetWhenMatcherBase>();
            _interceptor._whenGetChain.Add(new IndexerGetWhenMatcherValue(_predicate, value));
            return new IndexerGetWhenChainBase(_interceptor);
        }

        /// <summary>Configures a callback when predicate matches (getter chain).</summary>
        public IndexerGetWhenChainBase GetBase(Func<TKey, TValue> callback)
        {
            _interceptor._whenGetChain ??= new List<IndexerGetWhenMatcherBase>();
            _interceptor._whenGetChain.Add(new IndexerGetWhenMatcherCallback(_predicate, callback));
            return new IndexerGetWhenChainBase(_interceptor);
        }

        /// <summary>Configures a callback when predicate matches (setter chain).</summary>
        public IndexerSetWhenChainBase SetBase(Action<TKey, TValue> callback)
        {
            _interceptor._whenSetChain ??= new List<IndexerSetWhenMatcherBase>();
            _interceptor._whenSetChain.Add(new IndexerSetWhenMatcherCallback(_predicate, callback));
            return new IndexerSetWhenChainBase(_interceptor);
        }
    }

    // ========================================================================
    // Inner class: IndexerGetWhenChainBase
    // ========================================================================

    /// <summary>Getter When chain with ThenWhen, ThenNone, Verify, Reset.</summary>
    public class IndexerGetWhenChainBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;

        public IndexerGetWhenChainBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor) => _interceptor = interceptor;

        /// <summary>Adds another predicate matcher to the getter When chain.</summary>
        public IndexerWhenBuilderBase ThenWhenBase(Func<TKey, bool> predicate)
        {
            return new IndexerWhenBuilderBase(_interceptor, predicate);
        }

        /// <summary>Closes getter chain with no matcher.</summary>
        public void ThenNone()
        {
            _interceptor._whenGetChain ??= new List<IndexerGetWhenMatcherBase>();
            _interceptor._whenGetChain.Add(new IndexerGetWhenMatcherNone());
        }

        /// <summary>Verifies the getter When chain was fully consumed.</summary>
        public void Verify()
        {
            if (_interceptor._whenGetChain == null || _interceptor._whenGetChain.Count == 0) return;
            var head = _interceptor._whenGetChainHead;
            var count = _interceptor._whenGetChain.Count;
            if (head < count && !_interceptor._whenGetChain[head].IsTerminal && _interceptor._whenGetChain[head].CallCount == 0)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("indexer getter When chain", count, head));
        }

        /// <summary>Resets getter When chain HEAD and all matcher call counts.</summary>
        public void Reset()
        {
            _interceptor._whenGetChainHead = 0;
            if (_interceptor._whenGetChain != null)
                foreach (var matcher in _interceptor._whenGetChain)
                    matcher.CallCount = 0;
        }

        /// <summary>Marks this getter When chain for verification by Stub.Verify().</summary>
        public IndexerGetWhenChainBase VerifiableBase()
        {
            _interceptor._whenGetVerifiable = true;
            return this;
        }
    }

    // ========================================================================
    // Inner class: IndexerSetWhenChainBase
    // ========================================================================

    /// <summary>Setter When chain with ThenWhen, ThenNone, Verify, Reset.</summary>
    public class IndexerSetWhenChainBase
    {
        protected readonly IndexerGetSetInterceptorBase<TKey, TValue> _interceptor;

        public IndexerSetWhenChainBase(IndexerGetSetInterceptorBase<TKey, TValue> interceptor) => _interceptor = interceptor;

        /// <summary>Adds another predicate matcher to the setter When chain.</summary>
        public IndexerWhenBuilderBase ThenWhenBase(Func<TKey, bool> predicate)
        {
            return new IndexerWhenBuilderBase(_interceptor, predicate);
        }

        /// <summary>Closes setter chain with no matcher.</summary>
        public void ThenNone()
        {
            _interceptor._whenSetChain ??= new List<IndexerSetWhenMatcherBase>();
            _interceptor._whenSetChain.Add(new IndexerSetWhenMatcherNone());
        }

        /// <summary>Verifies the setter When chain was fully consumed.</summary>
        public void Verify()
        {
            if (_interceptor._whenSetChain == null || _interceptor._whenSetChain.Count == 0) return;
            var head = _interceptor._whenSetChainHead;
            var count = _interceptor._whenSetChain.Count;
            if (head < count && !_interceptor._whenSetChain[head].IsTerminal && _interceptor._whenSetChain[head].CallCount == 0)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("indexer setter When chain", count, head));
        }

        /// <summary>Resets setter When chain HEAD and all matcher call counts.</summary>
        public void Reset()
        {
            _interceptor._whenSetChainHead = 0;
            if (_interceptor._whenSetChain != null)
                foreach (var matcher in _interceptor._whenSetChain)
                    matcher.CallCount = 0;
        }

        /// <summary>Marks this setter When chain for verification by Stub.Verify().</summary>
        public IndexerSetWhenChainBase VerifiableBase()
        {
            _interceptor._whenSetVerifiable = true;
            return this;
        }
    }
}
