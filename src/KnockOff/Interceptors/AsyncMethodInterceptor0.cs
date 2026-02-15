#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords
#pragma warning disable CA1030 // Use events where appropriate

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for non-void methods with 0 parameters.
/// Handles Task&lt;TReturn&gt; and ValueTask&lt;TReturn&gt; interface methods.
/// </summary>
/// <typeparam name="TReturn">The inner return type (e.g., int for Task&lt;int&gt;).</typeparam>
public sealed class AsyncMethodInterceptor0<TReturn>
{
    private readonly string _memberName;

    // Callback (stored as async form)
    private Func<Task<TReturn>>? _call;
    private MethodCallBuilder0? _callTracking;

    // Return value
    private TReturn _returnValue = default!;
    private bool _hasReturnValue;
    private MethodCallBuilder0? _returnValueTracking;

    // Sequence
    private List<(Func<Task<TReturn>> Callback, MethodCallBuilder0 Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;

    // When chain
    private List<WhenMatcherBase>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;

    // Verification
    private bool _isVerifiable;
    private Called? _verifiableTimes;

    // Unconfigured tracking
    private int _unconfiguredCallCount;

    // Fallback delegates (stored as async form)
    private Func<Task<TReturn>>? _fallback;
    private Func<Task<TReturn>>? _sourceFallback;

    public AsyncMethodInterceptor0(string memberName)
    {
        _memberName = memberName;
    }

    /// <summary>Count of calls not handled by any configured behavior.</summary>
    public int UnconfiguredCallCount => _unconfiguredCallCount;

    /// <summary>Total call count across all configured behaviors and unconfigured calls.</summary>
    public int TotalCallCount
    {
        get
        {
            var sum = _unconfiguredCallCount
                + (_callTracking?._callCount ?? 0)
                + (_returnValueTracking?._callCount ?? 0);
            if (_sequence != null)
                foreach (var s in _sequence)
                    sum += s.Tracking._callCount;
            if (_whenChain != null)
                foreach (var m in _whenChain)
                    sum += m.CallCount;
            return sum;
        }
    }

    /// <summary>Whether this interceptor has been configured.</summary>
    public bool IsConfigured => _hasReturnValue || _call != null || (_sequence?.Count ?? 0) > 0 || (_whenChain?.Count ?? 0) > 0;

    // ========================================================================
    // Invoke
    // ========================================================================

    /// <summary>Invokes the configured behavior. Called by generated interface implementation. Returns Task&lt;TReturn&gt;.</summary>
    public async Task<TReturn> Invoke(bool strict)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches())
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                return await matcher.CallReturn().ConfigureAwait(false);
            }
            else if (matcher.IsTerminal)
            {
                _whenChainHead++;
            }
        }

        // Sequence
        if (_sequence != null && _sequenceIndex < _sequence.Count)
        {
            var (callback, tracking) = _sequence[_sequenceIndex];
            tracking.RecordCall();
            _sequenceIndex++;
            return await callback().ConfigureAwait(false);
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall();
            return _returnValue;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall();
            return await _call().ConfigureAwait(false);
        }

        // Nothing handled - unconfigured path
        _unconfiguredCallCount++;

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall();
                return await callback().ConfigureAwait(false);
            }
            return default!;
        }

        // Fallback (stub override)
        if (_fallback != null) return await _fallback().ConfigureAwait(false);

        // Source fallback
        if (_sourceFallback != null) return await _sourceFallback().ConfigureAwait(false);

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
        return default!;
    }

    // ========================================================================
    // Return / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures async callback that repeats indefinitely.</summary>
    public MethodCallBuilder0 Return(Func<Task<TReturn>> asyncCallback)
    {
        var builder = new MethodCallBuilder0(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = asyncCallback;
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures simplified sync callback that repeats indefinitely. Auto-wraps in Task.FromResult.</summary>
    public MethodCallBuilder0 Return(Func<TReturn> callback)
    {
        return Return(() => Task.FromResult(callback()));
    }

    /// <summary>Configures return value that repeats indefinitely. Auto-wraps in Task.FromResult.</summary>
    public MethodCallBuilder0 Return(TReturn value)
    {
        var builder = new MethodCallBuilder0(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
        return builder;
    }

    /// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
    public MethodSequence0 Return(TReturn first, params TReturn[] rest)
    {
        var builder = Return(() => Task.FromResult(first));
        if (rest.Length == 0)
        {
            return builder.ThenReturn(first);
        }
        var seq = builder.ThenReturn(rest[0]);
        for (int i = 1; i < rest.Length; i++)
        {
            seq.ThenReturn(rest[i]);
        }
        return seq;
    }

    /// <summary>Configures parameter-specific matching with predicate.</summary>
    public WhenBuilder0 When(Func<bool> predicate)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder0(this, predicate);
    }

    /// <summary>Sets the fallback delegate (async form).</summary>
    public void SetFallback(Func<Task<TReturn>>? fallback) => _fallback = fallback;

    /// <summary>Sets the fallback delegate (sync form, auto-wraps).</summary>
    public void SetFallback(Func<TReturn>? fallback) =>
        _fallback = fallback != null ? () => Task.FromResult(fallback()) : null;

    /// <summary>Sets the source fallback delegate (async form).</summary>
    public void SetSourceFallback(Func<Task<TReturn>>? sourceFallback) => _sourceFallback = sourceFallback;

    /// <summary>Sets the source fallback delegate (sync form, auto-wraps).</summary>
    public void SetSourceFallback(Func<TReturn>? sourceFallback) =>
        _sourceFallback = sourceFallback != null ? () => Task.FromResult(sourceFallback()) : null;

    /// <summary>Verifies method was called at least once.</summary>
    public void Verify() => Verify(Called.AtLeastOnce);

    /// <summary>Verifies call count satisfies the Called constraint.</summary>
    public void Verify(Called times)
    {
        if (!times.Validate(TotalCallCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, TotalCallCount));
    }

    public void Verifiable() { _isVerifiable = true; _verifiableTimes = null; }
    public void Verifiable(Called times) { _isVerifiable = true; _verifiableTimes = times; }
    public bool IsVerifiable => _isVerifiable;

    public VerificationFailure? CheckVerification()
    {
        if (!_isVerifiable && !_whenVerifiable) return null;
        if (_isVerifiable)
        {
            var times = _verifiableTimes ?? Called.AtLeastOnce;
            if (!times.Validate(TotalCallCount))
                return new VerificationFailure(_memberName, times, TotalCallCount);
        }
        if (_whenVerifiable && _whenChain != null && _whenChain.Count > 0)
        {
            var head = _whenChainHead;
            var count = _whenChain.Count;
            if (head < count && !_whenChain[head].IsTerminal && _whenChain[head].CallCount == 0)
                return VerificationFailure.SequenceIncomplete($"{_memberName} When chain", count, head);
        }
        return null;
    }

    public VerificationFailure? CheckVerificationAll()
    {
        if (!IsConfigured) return null;
        if (!Called.AtLeastOnce.Validate(TotalCallCount))
            return new VerificationFailure(_memberName, Called.AtLeastOnce, TotalCallCount);
        if (_whenChain != null && _whenChain.Count > 0)
        {
            var head = _whenChainHead;
            var count = _whenChain.Count;
            if (head < count && !_whenChain[head].IsTerminal && _whenChain[head].CallCount == 0)
                return VerificationFailure.SequenceIncomplete($"{_memberName} When chain", count, head);
        }
        return null;
    }

    public void Reset()
    {
        _unconfiguredCallCount = 0;
        _callTracking?.Reset();
        _returnValueTracking?.Reset();
        if (_sequence != null)
            foreach (var (_, tracking) in _sequence) tracking.Reset();
        _sequenceIndex = 0;
        _whenChainHead = 0;
        if (_whenChain != null)
            foreach (var matcher in _whenChain) matcher.CallCount = 0;
    }

    // ========================================================================
    // Inner classes
    // ========================================================================

    private abstract class WhenMatcherBase
    {
        public abstract bool Matches();
        public abstract Task<TReturn> CallReturn();
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    private sealed class WhenMatcherValue : WhenMatcherBase
    {
        private readonly Func<bool> _predicate;
        private readonly TReturn _value;
        public WhenMatcherValue(Func<bool> predicate, TReturn value) { _predicate = predicate; _value = value; }
        public override bool Matches() => _predicate();
        public override Task<TReturn> CallReturn() => Task.FromResult(_value);
        public override bool IsTerminal => false;
    }

    private sealed class WhenMatcherCall : WhenMatcherBase
    {
        private readonly Func<Task<TReturn>> _callback;
        public WhenMatcherCall(Func<Task<TReturn>> callback) => _callback = callback;
        public override bool Matches() => true;
        public override Task<TReturn> CallReturn() => _callback();
        public override bool IsTerminal => true;
    }

    private sealed class WhenMatcherNone : WhenMatcherBase
    {
        public override bool Matches() => false;
        public override Task<TReturn> CallReturn() => Task.FromResult(default(TReturn)!);
        public override bool IsTerminal => true;
    }

    public sealed class MethodCallBuilder0 : IMethodReturnBuilder<Func<Task<TReturn>>>
    {
        private readonly AsyncMethodInterceptor0<TReturn> _interceptor;
        internal int _callCount;

        internal MethodCallBuilder0(AsyncMethodInterceptor0<TReturn> interceptor) => _interceptor = interceptor;

        internal void RecordCall() => _callCount++;

        public void Reset() => _callCount = 0;
        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("method", called, _callCount));
        }

        public MethodSequence0 ThenReturn(Func<Task<TReturn>> asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder0(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, nextBuilder));
            return new MethodSequence0(_interceptor);
        }

        public MethodSequence0 ThenReturn(Func<TReturn> callback) => ThenReturn(() => Task.FromResult(callback()));
        public MethodSequence0 ThenReturn(TReturn value) => ThenReturn(() => Task.FromResult(value));

        public MethodSequence0 ThenReturn(params TReturn[] values)
        {
            if (values.Length == 0) { ElevateToSequence(); return new MethodSequence0(_interceptor); }
            var seq = ThenReturn(values[0]);
            for (int i = 1; i < values.Length; i++) seq.ThenReturn(values[i]);
            return seq;
        }

        public MethodCallBuilder0 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public MethodCallBuilder0 Verifiable(Called times) { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = times; return this; }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Func<Task<TReturn>> Callback, MethodCallBuilder0 Tracking)>();
                if (_interceptor._call != null)
                    _interceptor._sequence.Add((_interceptor._call, this));
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add((() => Task.FromResult(capturedValue), this));
                    _interceptor._hasReturnValue = false;
                    _interceptor._returnValue = default!;
                    _interceptor._returnValueTracking = null;
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        IMethodReturnSequence<Func<Task<TReturn>>> IMethodReturnBuilder<Func<Task<TReturn>>>.ThenReturn(Func<Task<TReturn>> callback) => ThenReturn(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodReturnBuilder<Func<Task<TReturn>>> IMethodReturnBuilder<Func<Task<TReturn>>>.Verifiable() => Verifiable();
        IMethodReturnBuilder<Func<Task<TReturn>>> IMethodReturnBuilder<Func<Task<TReturn>>>.Verifiable(Called called) => Verifiable(called);
    }

    public sealed class MethodSequence0 : IMethodReturnSequence<Func<Task<TReturn>>>, IMethodReturnSequence, IMethodSequence
    {
        private readonly AsyncMethodInterceptor0<TReturn> _interceptor;
        internal MethodSequence0(AsyncMethodInterceptor0<TReturn> interceptor) => _interceptor = interceptor;

        public MethodSequence0 ThenReturn(Func<Task<TReturn>> asyncCallback)
        {
            var tracking = new MethodCallBuilder0(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, tracking));
            return this;
        }

        public MethodSequence0 ThenReturn(Func<TReturn> callback) => ThenReturn(() => Task.FromResult(callback()));
        public MethodSequence0 ThenReturn(TReturn value) => ThenReturn(() => Task.FromResult(value));
        public MethodSequence0 ThenReturn(params TReturn[] values) { foreach (var v in values) ThenReturn(v); return this; }

        public void Verify()
        {
            if (_interceptor._sequence == null) return;
            if (_interceptor._sequenceIndex < _interceptor._sequence.Count)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("method", _interceptor._sequence.Count, _interceptor._sequenceIndex));
        }

        public void Reset() => _interceptor.Reset();
        public MethodSequence0 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public void ThenDefault() => _interceptor._repeatLastValue = false;

        IMethodReturnSequence<Func<Task<TReturn>>> IMethodReturnSequence<Func<Task<TReturn>>>.ThenReturn(Func<Task<TReturn>> callback) => ThenReturn(callback);
        IMethodReturnSequence<Func<Task<TReturn>>> IMethodReturnSequence<Func<Task<TReturn>>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    public sealed class WhenBuilder0
    {
        private readonly AsyncMethodInterceptor0<TReturn> _interceptor;
        private readonly Func<bool> _predicate;
        internal WhenBuilder0(AsyncMethodInterceptor0<TReturn> interceptor, Func<bool> predicate) { _interceptor = interceptor; _predicate = predicate; }

        public WhenChain0 Return(TReturn value)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));
            return new WhenChain0(_interceptor);
        }
    }

    public sealed class WhenChain0
    {
        private readonly AsyncMethodInterceptor0<TReturn> _interceptor;
        internal WhenChain0(AsyncMethodInterceptor0<TReturn> interceptor) => _interceptor = interceptor;

        public WhenBuilder0 ThenWhen(Func<bool> predicate) => new WhenBuilder0(_interceptor, predicate);

        public WhenChain0 ThenCall(Func<Task<TReturn>> asyncCallback)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCall(asyncCallback));
            return this;
        }

        public WhenChain0 ThenCall(Func<TReturn> callback) => ThenCall(() => Task.FromResult(callback()));

        public WhenChain0 ThenNone()
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherNone());
            return this;
        }

        public void Verify()
        {
            if (_interceptor._whenChain == null || _interceptor._whenChain.Count == 0) return;
            var head = _interceptor._whenChainHead;
            var count = _interceptor._whenChain.Count;
            if (head < count && !_interceptor._whenChain[head].IsTerminal && _interceptor._whenChain[head].CallCount == 0)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("When chain", count, head));
        }

        public void Reset()
        {
            _interceptor._whenChainHead = 0;
            if (_interceptor._whenChain != null)
                foreach (var matcher in _interceptor._whenChain) matcher.CallCount = 0;
        }

        public WhenChain0 Verifiable() { _interceptor._whenVerifiable = true; return this; }
    }
}
