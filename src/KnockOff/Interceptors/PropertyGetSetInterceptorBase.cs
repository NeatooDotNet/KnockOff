#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists

using KnockOff;

namespace KnockOff.Interceptors;

/// <summary>
/// Base class for get+set property interceptors.
/// Extends PropertyGetInterceptorBase with set-side fields and round-trip storage.
/// When a value is set via the interface setter (unconfigured path), it can be read back
/// via the getter (also unconfigured path).
/// </summary>
/// <typeparam name="TValue">The property value type.</typeparam>
public abstract class PropertyGetSetInterceptorBase<TValue> : PropertyGetInterceptorBase<TValue>
{
    // Set-side fields (mirrors get-side pattern)
    protected Action<TValue>? _set;
    protected PropertySetBuilderBase? _setTracking;
    protected List<(Action<TValue> Callback, PropertySetBuilderBase Tracking)>? _setSequence;
    protected int _setSequenceIndex;
    protected bool _setRepeatLastValue = true;
    protected bool _isSetVerifiable;
    protected Called? _setVerifiableTimes;
    protected int _unconfiguredSetCount;
    protected TValue? _unconfiguredLastSetValue;

    // Round-trip storage: set via setter, read back via getter
    protected bool _valueSet;
    protected TValue _value = default!;

    protected PropertyGetSetInterceptorBase(string memberName) : base(memberName) { }

    protected int TotalSetCount
    {
        get
        {
            var sum = _unconfiguredSetCount + (_setTracking?._callCount ?? 0);
            if (_setSequence != null)
                foreach (var s in _setSequence)
                    sum += s.Tracking._callCount;
            return sum;
        }
    }

    // --- Public Set configuration API ---

    /// <summary>Configures the setter to invoke the given callback.</summary>
    public PropertySetBuilderBase Set(Action<TValue> callback)
    {
        _setSequence = null; _setSequenceIndex = 0;
        _isSetVerifiable = false; _setVerifiableTimes = null;
        _set = callback;
        var builder = new PropertySetBuilderBase(this);
        _setTracking = builder;
        return builder;
    }

    // --- Stub override helpers for set ---

    /// <summary>Records a setter access (tracking only). Used by stub override pattern.</summary>
    public void RecordSet(TValue value) { _unconfiguredSetCount++; _unconfiguredLastSetValue = value; }

    /// <summary>True if Set is configured (callback or sequence).</summary>
    public bool HasSet => _set != null || (_setSequence?.Count ?? 0) > 0;

    /// <summary>Invokes the configured setter callback. Used by stub override pattern.</summary>
    public void InvokeSetCallback(TValue value)
    {
        if (_setSequence != null && _setSequenceIndex < _setSequence.Count)
        {
            var (callback, tracking) = _setSequence[_setSequenceIndex];
            tracking.RecordCall(value);
            _setSequenceIndex++;
            callback(value);
            return;
        }
        if (_set != null && _setTracking != null)
        {
            _setTracking.RecordCall(value);
            _set(value);
            return;
        }
        throw new InvalidOperationException("InvokeSetCallback called without callback configured");
    }

    // --- InvokeSet with priority chain ---

    /// <summary>Invokes the configured setter. Called by interface implementation.</summary>
    public void InvokeSet(bool strict, TValue value)
    {
        // Sequence
        if (_setSequence != null && _setSequenceIndex < _setSequence.Count)
        {
            var (callback, tracking) = _setSequence[_setSequenceIndex];
            tracking.RecordCall(value);
            _setSequenceIndex++;
            callback(value);
            return;
        }

        // Callback
        if (_set != null && _setTracking != null)
        {
            _setTracking.RecordCall(value);
            _set(value);
            return;
        }

        // Unconfigured
        _unconfiguredSetCount++;
        _unconfiguredLastSetValue = value;

        // Sequence exhausted
        if (_setSequence != null && _setSequenceIndex >= _setSequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted($"{_memberName} (set)");
            if (_setRepeatLastValue && _setSequence.Count > 0)
            {
                var (callback, tracking) = _setSequence[_setSequence.Count - 1];
                tracking.RecordCall(value);
                callback(value);
                return;
            }
            return;
        }

        // Source delegation / strict check / round-trip store
        InvokeSetUnconfigured(strict, value);

        // Store in round-trip backing field
        _value = value;
        _valueSet = true;
    }

    /// <summary>Handles unconfigured set. Override for source delegation / strict mode.
    /// If the subclass handles the set (e.g., delegates to source), it should return without
    /// throwing so the round-trip store happens after this call.</summary>
    protected abstract void InvokeSetUnconfigured(bool strict, TValue value);

    // --- InvokeGetUnconfigured override: round-trip check before final ---

    /// <summary>Overrides the get-only base to check round-trip stored value before final unconfigured handling.</summary>
    protected sealed override TValue InvokeGetUnconfigured(bool strict)
    {
        if (_valueSet) return _value;
        return InvokeGetUnconfiguredFinal(strict);
    }

    /// <summary>Handles unconfigured get after round-trip check fails.
    /// Override for source delegation / strict mode / default return.</summary>
    protected abstract TValue InvokeGetUnconfiguredFinal(bool strict);

    /// <summary>Returns round-trip value when sequence is exhausted and _repeatLastValue is false.</summary>
    protected sealed override TValue GetSequenceExhaustedDefaultValue()
    {
        return _valueSet ? _value : default!;
    }

    // --- Verification (combined get+set) ---

    public new void Verify() => Verify(Called.AtLeastOnce);

    public new void Verify(Called times)
    {
        var totalCount = TotalGetCount + TotalSetCount;
        if (!times.Validate(totalCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, totalCount));
    }

    public void VerifySet() => VerifySet(Called.AtLeastOnce);

    public void VerifySet(Called times)
    {
        if (!times.Validate(TotalSetCount))
            throw new VerificationException(new VerificationFailure($"{_memberName} (set)", times, TotalSetCount));
    }

    public new bool IsVerifiable => _isGetVerifiable || _isSetVerifiable;

    public new bool IsConfigured
    {
        get
        {
            if (_get != null || (_getSequence?.Count ?? 0) > 0) return true;
            if (_set != null || (_setSequence?.Count ?? 0) > 0) return true;
            return false;
        }
    }

    public new VerificationFailure? CheckVerification()
    {
        if (!_isGetVerifiable && !_isSetVerifiable) return null;

        // Combined check
        if (_isGetVerifiable && _isSetVerifiable)
        {
            var times = _getVerifiableTimes ?? _setVerifiableTimes ?? Called.AtLeastOnce;
            var totalCount = TotalGetCount + TotalSetCount;
            if (!times.Validate(totalCount))
                return new VerificationFailure(_memberName, times, totalCount);
        }

        // Get-only check
        if (_isGetVerifiable && !_isSetVerifiable)
        {
            var times = _getVerifiableTimes ?? Called.AtLeastOnce;
            if (!times.Validate(TotalGetCount))
                return new VerificationFailure($"{_memberName} (get)", times, TotalGetCount);
        }

        // Set-only check
        if (_isSetVerifiable && !_isGetVerifiable)
        {
            var times = _setVerifiableTimes ?? Called.AtLeastOnce;
            if (!times.Validate(TotalSetCount))
                return new VerificationFailure($"{_memberName} (set)", times, TotalSetCount);
        }

        return null;
    }

    public new VerificationFailure? CheckVerificationAll()
    {
        if (!IsConfigured) return null;
        var totalCount = TotalGetCount + TotalSetCount;
        return totalCount >= 1 ? null : new VerificationFailure(_memberName, Called.AtLeastOnce, totalCount);
    }

    // --- Reset ---

    public override void Reset()
    {
        base.Reset();
        _unconfiguredSetCount = 0;
        _unconfiguredLastSetValue = default;
        _setTracking?.Reset();
        if (_setSequence != null)
        {
            foreach (var (_, tracking) in _setSequence)
                tracking.Reset();
        }
        _setSequenceIndex = 0;
        _valueSet = false;
    }

    // --- Verifiable fluent support ---

    public new PropertyGetSetInterceptorBase<TValue> SetVerifiable()
    {
        _isGetVerifiable = true;
        _getVerifiableTimes = null;
        _isSetVerifiable = true;
        _setVerifiableTimes = null;
        return this;
    }

    public new PropertyGetSetInterceptorBase<TValue> SetVerifiable(Called times)
    {
        _isGetVerifiable = true;
        _getVerifiableTimes = times;
        _isSetVerifiable = true;
        _setVerifiableTimes = times;
        return this;
    }

    /// <summary>The value from the last setter call (from most recently called registration).</summary>
    public TValue? LastSetValue
    {
        get
        {
            if ((_setTracking?._callCount ?? 0) > 0) return _setTracking!.LastValue;
            if (_setSequence != null)
                for (int i = _setSequence.Count - 1; i >= 0; i--)
                    if (_setSequence[i].Tracking._callCount > 0) return _setSequence[i].Tracking.LastValue;
            return _unconfiguredSetCount > 0 ? _unconfiguredLastSetValue : default;
        }
    }

    // ========================================================================
    // Inner class: PropertySetBuilderBase
    // ========================================================================

    public class PropertySetBuilderBase
    {
        protected readonly PropertyGetSetInterceptorBase<TValue> _interceptor;

        public PropertySetBuilderBase(PropertyGetSetInterceptorBase<TValue> interceptor)
        {
            _interceptor = interceptor;
        }

        public int _callCount;
        private TValue _lastValue = default!;
        public TValue LastValue => _lastValue;

        public void RecordCall(TValue value) { _callCount++; _lastValue = value; }

        public void Reset() { _callCount = 0; _lastValue = default!; }

        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called times)
        {
            if (!times.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("property setter", times, _callCount));
        }

        protected PropertySetSequenceBase ThenSetBase(Action<TValue> callback)
        {
            if (_interceptor._setSequence == null)
            {
                _interceptor._setSequence = new List<(Action<TValue> Callback, PropertySetBuilderBase Tracking)>();
                _interceptor._setSequence.Add((_interceptor._set!, this));
                _interceptor._set = null;
                _interceptor._setTracking = null;
                _interceptor._setSequenceIndex = 0;
            }
            var nextBuilder = CreateNextBuilder();
            _interceptor._setSequence.Add((callback, nextBuilder));
            return new PropertySetSequenceBase(_interceptor);
        }

        protected virtual PropertySetBuilderBase CreateNextBuilder() => new PropertySetBuilderBase(_interceptor);

        public void VerifiableBase()
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = null;
        }

        public void VerifiableBase(Called times)
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = times;
        }
    }

    // ========================================================================
    // Inner class: PropertySetSequenceBase
    // ========================================================================

    public class PropertySetSequenceBase
    {
        protected readonly PropertyGetSetInterceptorBase<TValue> _interceptor;

        public PropertySetSequenceBase(PropertyGetSetInterceptorBase<TValue> interceptor)
        {
            _interceptor = interceptor;
        }

        protected PropertySetSequenceBase ThenSetBase(Action<TValue> callback)
        {
            var tracking = new PropertySetBuilderBase(_interceptor);
            _interceptor._setSequence!.Add((callback, tracking));
            return this;
        }

        public void Verify()
        {
            if (_interceptor._setSequence == null) return;
            var sequenceLength = _interceptor._setSequence.Count;
            var completedCount = _interceptor._setSequenceIndex;
            if (completedCount < sequenceLength)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("property setter", sequenceLength, completedCount));
        }

        public void Reset() => _interceptor.Reset();

        public void VerifiableBase()
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = null;
        }

        public void ThenDefault()
        {
            _interceptor._setRepeatLastValue = false;
        }
    }
}
