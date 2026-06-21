// Suppressions justified: Generated interceptor classes inherit from this type
// and directly access public fields, nested types, and generic lists. Changing
// visibility or structure would break all generated code across all 9 patterns.
#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists

using KnockOff;

namespace KnockOff.Interceptors;

/// <summary>
/// Base class for set-only property interceptors.
/// Mirrors PropertyGetInterceptorBase but for properties with only a setter.
/// </summary>
/// <typeparam name="TValue">The property value type.</typeparam>
public abstract class PropertySetInterceptorBase<TValue>
{
    protected readonly string _memberName;

    protected Action<TValue>? _set;
    protected PropertySetBuilderBase? _setTracking;
    protected List<(Action<TValue> Callback, PropertySetBuilderBase Tracking)>? _setSequence;
    protected int _setSequenceIndex;
    protected bool _setRepeatLastValue = true;
    protected bool _isSetVerifiable;
    protected Called? _setVerifiableTimes;
    protected int _unconfiguredSetCount;
    protected TValue? _unconfiguredLastSetValue;

    protected PropertySetInterceptorBase(string memberName)
    {
        _memberName = memberName;
    }

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
    public IPropertySetBuilder<TValue> Set(Action<TValue> callback)
    {
        _setSequence = null; _setSequenceIndex = 0;
        _isSetVerifiable = false; _setVerifiableTimes = null;
        _set = callback;
        var builder = new PropertySetBuilderBase(this);
        _setTracking = builder;
        return builder;
    }

    // --- Stub override helpers ---

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

        InvokeSetUnconfigured(strict, value);
    }

    /// <summary>Handles unconfigured set. Override for source delegation / strict mode.</summary>
    protected abstract void InvokeSetUnconfigured(bool strict, TValue value);

    /// <summary>Resets tracking state.</summary>
    public virtual void Reset()
    {
        _unconfiguredSetCount = 0;
        _unconfiguredLastSetValue = default;
        _setTracking?.Reset();
        if (_setSequence != null)
        {
            foreach (var (_, tracking) in _setSequence)
                tracking.Reset();
        }
        _setSequenceIndex = 0;
    }

    public void Verify() => Verify(Called.AtLeastOnce);

    public void Verify(Called times)
    {
        var totalCount = TotalSetCount;
        if (!times.Validate(totalCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, totalCount));
    }

    public void VerifySet() => VerifySet(Called.AtLeastOnce);

    public void VerifySet(Called times)
    {
        if (!times.Validate(TotalSetCount))
            throw new VerificationException(new VerificationFailure($"{_memberName} (set)", times, TotalSetCount));
    }

    public bool IsVerifiable => _isSetVerifiable;
    public bool IsConfigured => _set != null || (_setSequence?.Count ?? 0) > 0;

    public VerificationFailure? CheckVerification()
    {
        if (!_isSetVerifiable) return null;
        var times = _setVerifiableTimes ?? Called.AtLeastOnce;
        if (!times.Validate(TotalSetCount))
            return new VerificationFailure($"{_memberName} (set)", times, TotalSetCount);
        return null;
    }

    public VerificationFailure? CheckVerificationAll()
    {
        if (!IsConfigured) return null;
        var totalCount = TotalSetCount;
        return totalCount >= 1 ? null : new VerificationFailure(_memberName, Called.AtLeastOnce, totalCount);
    }

    // ========================================================================
    // Inner class: PropertySetBuilderBase
    // ========================================================================

    public class PropertySetBuilderBase : IPropertySetBuilder<TValue>, IPropertySetTracking<TValue>
    {
        protected readonly PropertySetInterceptorBase<TValue> _interceptor;

        public PropertySetBuilderBase(PropertySetInterceptorBase<TValue> interceptor)
        {
            _interceptor = interceptor;
        }

        public int _callCount;
        private TValue _lastValue = default!;
        public TValue LastValue => _lastValue;

        public void RecordCall(TValue value) { _callCount++; _lastValue = value; }

        public void Reset() { _callCount = 0; _lastValue = default!; }

        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("property setter", called, _callCount));
        }

        public IPropertySetSequence<TValue> ThenSet(Action<TValue> callback)
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

        public PropertySetBuilderBase Verifiable()
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = null;
            return this;
        }

        public PropertySetBuilderBase Verifiable(Called called)
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = called;
            return this;
        }

        IPropertySetBuilder<TValue> IPropertySetBuilder<TValue>.Verifiable() => (IPropertySetBuilder<TValue>)Verifiable();
        IPropertySetTracking<TValue> IPropertySetTracking<TValue>.Verifiable() => (IPropertySetTracking<TValue>)Verifiable();
        IPropertySetTracking<TValue> IPropertySetTracking<TValue>.Verifiable(Called called) => (IPropertySetTracking<TValue>)Verifiable(called);
    }

    // ========================================================================
    // Inner class: PropertySetSequenceBase
    // ========================================================================

    public class PropertySetSequenceBase : IPropertySetSequence<TValue>
    {
        protected readonly PropertySetInterceptorBase<TValue> _interceptor;

        public PropertySetSequenceBase(PropertySetInterceptorBase<TValue> interceptor)
        {
            _interceptor = interceptor;
        }

        public IPropertySetSequence<TValue> ThenSet(Action<TValue> callback)
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

        public IPropertySetSequence<TValue> Verifiable()
        {
            _interceptor._isSetVerifiable = true;
            _interceptor._setVerifiableTimes = null;
            return this;
        }

        public void ThenDefault()
        {
            _interceptor._setRepeatLastValue = false;
        }
    }

    // --- Verifiable fluent support ---
    public PropertySetInterceptorBase<TValue> SetVerifiable()
    {
        _isSetVerifiable = true;
        _setVerifiableTimes = null;
        return this;
    }

    public PropertySetInterceptorBase<TValue> SetVerifiable(Called times)
    {
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
}
