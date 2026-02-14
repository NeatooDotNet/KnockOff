#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists

using KnockOff;

namespace KnockOff.Interceptors;

/// <summary>
/// Base class for get-only property interceptors.
/// </summary>
/// <typeparam name="TValue">The property value type.</typeparam>
public abstract class PropertyGetInterceptorBase<TValue>
{
    protected readonly string _memberName;

    protected Func<TValue>? _get;
    protected PropertyGetBuilderBase? _getTracking;
    protected List<(Func<TValue> Callback, PropertyGetBuilderBase Tracking)>? _getSequence;
    protected int _getSequenceIndex;
    protected bool _getRepeatLastValue = true;
    protected bool _isGetVerifiable;
    protected Called? _getVerifiableTimes;
    protected int _unconfiguredGetCount;

    protected PropertyGetInterceptorBase(string memberName)
    {
        _memberName = memberName;
    }

    protected int TotalGetCount
    {
        get
        {
            var sum = _unconfiguredGetCount + (_getTracking?._callCount ?? 0);
            if (_getSequence != null)
                foreach (var s in _getSequence)
                    sum += s.Tracking._callCount;
            return sum;
        }
    }

    // --- Public Get configuration API ---

    /// <summary>Configures the getter to invoke the given callback.</summary>
    public IPropertyGetBuilder<TValue> Get(Func<TValue> callback)
    {
        _getSequence = null; _getSequenceIndex = 0;
        _isGetVerifiable = false; _getVerifiableTimes = null;
        _get = callback;
        var builder = new PropertyGetBuilderBase(this);
        _getTracking = builder;
        return builder;
    }

    /// <summary>Configures the getter to return the given value.</summary>
    public IPropertyGetBuilder<TValue> Get(TValue value) => Get(() => value);

    // --- Stub override helpers (structural -- all in base class) ---

    /// <summary>Records a getter access (tracking only). Used by stub override pattern.</summary>
    public void RecordGet() => _unconfiguredGetCount++;

    /// <summary>True if Get is configured (callback or sequence).</summary>
    public bool HasGet => _get != null || (_getSequence?.Count ?? 0) > 0;

    /// <summary>Invokes the configured getter callback. Used by stub override pattern.</summary>
    public TValue InvokeGetCallback()
    {
        if (_getSequence != null && _getSequenceIndex < _getSequence.Count)
        {
            var (callback, tracking) = _getSequence[_getSequenceIndex];
            tracking._callCount++;
            _getSequenceIndex++;
            return callback();
        }
        if (_get != null && _getTracking != null)
        {
            _getTracking._callCount++;
            return _get();
        }
        throw new InvalidOperationException("InvokeGetCallback called without callback configured");
    }

    /// <summary>Invokes the configured getter. Called by interface implementation.</summary>
    public TValue InvokeGet(bool strict)
    {
        if (_getSequence != null && _getSequenceIndex < _getSequence.Count)
        {
            var (callback, tracking) = _getSequence[_getSequenceIndex];
            tracking._callCount++;
            _getSequenceIndex++;
            return callback();
        }

        if (_get != null && _getTracking != null)
        {
            _getTracking._callCount++;
            return _get();
        }

        _unconfiguredGetCount++;

        if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted($"{_memberName} (get)");
            if (_getRepeatLastValue && _getSequence.Count > 0)
            {
                var (callback, tracking) = _getSequence[_getSequence.Count - 1];
                tracking._callCount++;
                return callback();
            }
            return GetSequenceExhaustedDefaultValue();
        }

        return InvokeGetUnconfigured(strict);
    }

    /// <summary>Handles unconfigured get. Override for source delegation / strict mode.</summary>
    protected abstract TValue InvokeGetUnconfigured(bool strict);

    /// <summary>Returns the default value when a get sequence is exhausted and _repeatLastValue is false.
    /// Overridden by PropertyGetSetInterceptorBase to return round-trip stored value.</summary>
    protected virtual TValue GetSequenceExhaustedDefaultValue() => default!;

    /// <summary>Resets tracking state.</summary>
    public virtual void Reset()
    {
        _unconfiguredGetCount = 0;
        _getTracking?.Reset();
        if (_getSequence != null)
        {
            foreach (var (_, tracking) in _getSequence)
                tracking.Reset();
        }
        _getSequenceIndex = 0;
    }

    public void Verify() => Verify(Called.AtLeastOnce);

    public void Verify(Called times)
    {
        var totalCount = TotalGetCount;
        if (!times.Validate(totalCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, totalCount));
    }

    public void VerifyGet() => VerifyGet(Called.AtLeastOnce);

    public void VerifyGet(Called times)
    {
        if (!times.Validate(TotalGetCount))
            throw new VerificationException(new VerificationFailure($"{_memberName} (get)", times, TotalGetCount));
    }

    public bool IsVerifiable => _isGetVerifiable;
    public bool IsConfigured => _get != null || (_getSequence?.Count ?? 0) > 0;

    public VerificationFailure? CheckVerification()
    {
        if (!_isGetVerifiable) return null;
        var times = _getVerifiableTimes ?? Called.AtLeastOnce;
        if (!times.Validate(TotalGetCount))
            return new VerificationFailure($"{_memberName} (get)", times, TotalGetCount);
        return null;
    }

    public VerificationFailure? CheckVerificationAll()
    {
        if (!IsConfigured) return null;
        var totalCount = TotalGetCount;
        return totalCount >= 1 ? null : new VerificationFailure(_memberName, Called.AtLeastOnce, totalCount);
    }

    // ========================================================================
    // Inner class: PropertyGetBuilderBase
    // ========================================================================

    public class PropertyGetBuilderBase : IPropertyGetBuilder<TValue>, IPropertyGetTracking
    {
        protected readonly PropertyGetInterceptorBase<TValue> _interceptor;

        public PropertyGetBuilderBase(PropertyGetInterceptorBase<TValue> interceptor)
        {
            _interceptor = interceptor;
        }

        public int _callCount;

        public void Reset() => _callCount = 0;

        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("property getter", called, _callCount));
        }

        public IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback)
        {
            if (_interceptor._getSequence == null)
            {
                _interceptor._getSequence = new List<(Func<TValue> Callback, PropertyGetBuilderBase Tracking)>();
                _interceptor._getSequence.Add((_interceptor._get!, this));
                _interceptor._get = null;
                _interceptor._getTracking = null;
                _interceptor._getSequenceIndex = 0;
            }
            var nextBuilder = CreateNextBuilder();
            _interceptor._getSequence.Add((callback, nextBuilder));
            return new PropertyGetSequenceBase(_interceptor);
        }

        public IPropertyGetSequence<TValue> ThenGet(TValue value) => ThenGet(() => value);

        public IPropertyGetSequence<TValue> ThenGet(params TValue[] values)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            if (values.Length == 0) { ThenGet(() => default!); return new PropertyGetSequenceBase(_interceptor); }
            var seq = ThenGet(values[0]);
            for (int i = 1; i < values.Length; i++) seq = seq.ThenGet(values[i]);
#pragma warning restore CA1062
            return seq;
        }

        protected virtual PropertyGetBuilderBase CreateNextBuilder() => new PropertyGetBuilderBase(_interceptor);

        public PropertyGetBuilderBase Verifiable()
        {
            _interceptor._isGetVerifiable = true;
            _interceptor._getVerifiableTimes = null;
            return this;
        }

        public PropertyGetBuilderBase Verifiable(Called called)
        {
            _interceptor._isGetVerifiable = true;
            _interceptor._getVerifiableTimes = called;
            return this;
        }

        IPropertyGetBuilder<TValue> IPropertyGetBuilder<TValue>.Verifiable() => (IPropertyGetBuilder<TValue>)Verifiable();
        IPropertyGetTracking IPropertyGetTracking.Verifiable() => (IPropertyGetTracking)Verifiable();
        IPropertyGetTracking IPropertyGetTracking.Verifiable(Called called) => (IPropertyGetTracking)Verifiable(called);
    }

    // ========================================================================
    // Inner class: PropertyGetSequenceBase
    // ========================================================================

    public class PropertyGetSequenceBase : IPropertyGetSequence<TValue>
    {
        protected readonly PropertyGetInterceptorBase<TValue> _interceptor;

        public PropertyGetSequenceBase(PropertyGetInterceptorBase<TValue> interceptor)
        {
            _interceptor = interceptor;
        }

        public IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback)
        {
            var tracking = new PropertyGetBuilderBase(_interceptor);
            _interceptor._getSequence!.Add((callback, tracking));
            return this;
        }

        public IPropertyGetSequence<TValue> ThenGet(TValue value) => ThenGet(() => value);

        public IPropertyGetSequence<TValue> ThenGet(params TValue[] values)
        {
#pragma warning disable CA1062 // Validate arguments of public methods
            foreach (var v in values) ThenGet(v);
#pragma warning restore CA1062
            return this;
        }

        public void Verify()
        {
            if (_interceptor._getSequence == null) return;
            var sequenceLength = _interceptor._getSequence.Count;
            var completedCount = _interceptor._getSequenceIndex;
            if (completedCount < sequenceLength)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("property getter", sequenceLength, completedCount));
        }

        public void Reset() => _interceptor.Reset();

        public IPropertyGetSequence<TValue> Verifiable()
        {
            _interceptor._isGetVerifiable = true;
            _interceptor._getVerifiableTimes = null;
            return this;
        }

        public void ThenDefault()
        {
            _interceptor._getRepeatLastValue = false;
        }
    }

    // --- Verifiable fluent support ---
    public PropertyGetInterceptorBase<TValue> SetVerifiable()
    {
        _isGetVerifiable = true;
        _getVerifiableTimes = null;
        return this;
    }

    public PropertyGetInterceptorBase<TValue> SetVerifiable(Called times)
    {
        _isGetVerifiable = true;
        _getVerifiableTimes = times;
        return this;
    }
}
