#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords
#pragma warning disable CA1030 // Use events where appropriate

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for non-void methods with 4 parameters.
/// Handles Task&lt;TReturn&gt; and ValueTask&lt;TReturn&gt; interface methods.
/// </summary>
/// <typeparam name="T1">The type of parameter 1.</typeparam>
/// <typeparam name="T2">The type of parameter 2.</typeparam>
/// <typeparam name="T3">The type of parameter 3.</typeparam>
/// <typeparam name="T4">The type of parameter 4.</typeparam>
/// <typeparam name="TReturn">The inner return type (e.g., int for Task&lt;int&gt;).</typeparam>
public sealed class AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn>
{
    private readonly string _memberName;

    private Func<T1, T2, T3, T4, Task<TReturn>>? _call;
    private MethodCallBuilder4? _callTracking;

    private TReturn _returnValue = default!;
    private bool _hasReturnValue;
    private MethodCallBuilder4? _returnValueTracking;

    private List<(Func<T1, T2, T3, T4, Task<TReturn>> Callback, MethodCallBuilder4 Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;

    private List<WhenMatcherBase>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;

    private bool _isVerifiable;
    private Called? _verifiableTimes;

    private int _unconfiguredCallCount;
        private (T1, T2, T3, T4)? _unconfiguredLastArgs;

    private Func<T1, T2, T3, T4, Task<TReturn>>? _fallback;
    private Func<T1, T2, T3, T4, Task<TReturn>>? _sourceFallback;

    // Smart default factory (for NewInstance/ThrowException strategies)
    private readonly Func<TReturn>? _defaultFactory;

    public AsyncMethodInterceptor4(string memberName)
    {
        _memberName = memberName;
    }

    /// <summary>Constructor with smart default factory for non-strict unconfigured calls.</summary>
    public AsyncMethodInterceptor4(string memberName, Func<TReturn> defaultFactory)
    {
        _memberName = memberName;
        _defaultFactory = defaultFactory;
    }

    public int UnconfiguredCallCount => _unconfiguredCallCount;

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

    public bool IsConfigured => _hasReturnValue || _call != null || (_sequence?.Count ?? 0) > 0 || (_whenChain?.Count ?? 0) > 0;

    /// <summary>Last arguments from the most recently called registration.</summary>
    public (T1, T2, T3, T4)? LastArgs
    {
        get
        {
            if ((_returnValueTracking?._callCount ?? 0) > 0)
                return _returnValueTracking!.LastArgs;
            if ((_callTracking?._callCount ?? 0) > 0)
                return _callTracking!.LastArgs;
            if (_sequence != null)
                for (int i = _sequence.Count - 1; i >= 0; i--)
                    if (_sequence[i].Tracking._callCount > 0)
                        return _sequence[i].Tracking.LastArgs;
            return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default;
        }
    }

    // ========================================================================
    // Invoke
    // ========================================================================

    public async Task<TReturn> Invoke(bool strict, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(arg1, arg2, arg3, arg4))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                return await matcher.CallReturn(arg1, arg2, arg3, arg4).ConfigureAwait(false);
            }
            else if (matcher.IsTerminal)
            {
                _whenChainHead++;
            }
        }

        if (_sequence != null && _sequenceIndex < _sequence.Count)
        {
            var (callback, tracking) = _sequence[_sequenceIndex];
            tracking.RecordCall(arg1, arg2, arg3, arg4);
            _sequenceIndex++;
            return await callback(arg1, arg2, arg3, arg4).ConfigureAwait(false);
        }

        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall(arg1, arg2, arg3, arg4);
            return _returnValue;
        }

        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg1, arg2, arg3, arg4);
            return await _call(arg1, arg2, arg3, arg4).ConfigureAwait(false);
        }

        _unconfiguredCallCount++;
        _unconfiguredLastArgs = (arg1, arg2, arg3, arg4);

        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(arg1, arg2, arg3, arg4);
                return await callback(arg1, arg2, arg3, arg4).ConfigureAwait(false);
            }
            return default!;
        }

        if (_fallback != null) return await _fallback(arg1, arg2, arg3, arg4).ConfigureAwait(false);
        if (_sourceFallback != null) return await _sourceFallback(arg1, arg2, arg3, arg4).ConfigureAwait(false);
        if (strict) throw StubException.NotConfigured("", _memberName);

        // Smart default (NewInstance or ThrowException)
        if (_defaultFactory != null) return _defaultFactory();
        return default!;
    }

    // ========================================================================
    // Return / When / Verify / Reset
    // ========================================================================

    public MethodCallBuilder4 Return(Func<T1, T2, T3, T4, Task<TReturn>> asyncCallback)
    {
        var builder = new MethodCallBuilder4(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = asyncCallback;
        _callTracking = builder;
        return builder;
    }

    public MethodCallBuilder4 Return(Func<T1, T2, T3, T4, TReturn> callback)
    {
        return Return((T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.FromResult(callback(arg1, arg2, arg3, arg4)));
    }

    public MethodCallBuilder4 Return(TReturn value)
    {
        var builder = new MethodCallBuilder4(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
        return builder;
    }

    public MethodSequence4 Return(TReturn first, params TReturn[] rest)
    {
        var builder = Return((T1 _, T2 _, T3 _, T4 _) => Task.FromResult(first));
        if (rest.Length == 0) return builder.ThenReturn(first);
        var seq = builder.ThenReturn(rest[0]);
        for (int i = 1; i < rest.Length; i++) seq.ThenReturn(rest[i]);
        return seq;
    }

    /// <summary>Configures parameter-specific matching with exact values.</summary>
    public WhenBuilder4 When(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder4(this, (a1, a2, a3, a4) => object.Equals(a1, arg1) && object.Equals(a2, arg2) && object.Equals(a3, arg3) && object.Equals(a4, arg4));
    }

    public WhenBuilder4 When(Func<T1, T2, T3, T4, bool> predicate)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder4(this, predicate);
    }

    public void SetFallback(Func<T1, T2, T3, T4, Task<TReturn>>? fallback) => _fallback = fallback;
    public void SetFallback(Func<T1, T2, T3, T4, TReturn>? fallback) =>
        _fallback = fallback != null ? (T1 a1, T2 a2, T3 a3, T4 a4) => Task.FromResult(fallback(a1, a2, a3, a4)) : null;
    public void SetSourceFallback(Func<T1, T2, T3, T4, Task<TReturn>>? sourceFallback) => _sourceFallback = sourceFallback;
    public void SetSourceFallback(Func<T1, T2, T3, T4, TReturn>? sourceFallback) =>
        _sourceFallback = sourceFallback != null ? (T1 a1, T2 a2, T3 a3, T4 a4) => Task.FromResult(sourceFallback(a1, a2, a3, a4)) : null;

    public void Verify() => Verify(Called.AtLeastOnce);
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
        _unconfiguredLastArgs = default;
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
        public abstract bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public abstract Task<TReturn> CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    private sealed class WhenMatcherValue : WhenMatcherBase
    {
        private readonly Func<T1, T2, T3, T4, bool> _predicate;
        private readonly TReturn _value;
        public WhenMatcherValue(Func<T1, T2, T3, T4, bool> predicate, TReturn value) { _predicate = predicate; _value = value; }
        public override bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _predicate(arg1, arg2, arg3, arg4);
        public override Task<TReturn> CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.FromResult(_value);
        public override bool IsTerminal => false;
    }

    private sealed class WhenMatcherCall : WhenMatcherBase
    {
        private readonly Func<T1, T2, T3, T4, Task<TReturn>> _callback;
        public WhenMatcherCall(Func<T1, T2, T3, T4, Task<TReturn>> callback) => _callback = callback;
        public override bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => true;
        public override Task<TReturn> CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => _callback(arg1, arg2, arg3, arg4);
        public override bool IsTerminal => true;
    }

    private sealed class WhenMatcherNone : WhenMatcherBase
    {
        public override bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => false;
        public override Task<TReturn> CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.FromResult(default(TReturn)!);
        public override bool IsTerminal => true;
    }

    public sealed class MethodCallBuilder4 : IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, Task<TReturn>>, (T1, T2, T3, T4)>
    {
        private readonly AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> _interceptor;
        internal int _callCount;
        private (T1, T2, T3, T4) _lastArgs;

        internal MethodCallBuilder4(AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> interceptor) => _interceptor = interceptor;

        /// <summary>Last arguments passed to this callback.</summary>
        public (T1, T2, T3, T4) LastArgs => _lastArgs;

        internal void RecordCall(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            _callCount++;
            _lastArgs = (arg1, arg2, arg3, arg4);
        }

        public void Reset()
        {
            _callCount = 0;
            _lastArgs = default;
        }

        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("method", called, _callCount));
        }

        public MethodSequence4 ThenReturn(Func<T1, T2, T3, T4, Task<TReturn>> asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder4(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, nextBuilder));
            return new MethodSequence4(_interceptor);
        }

        public MethodSequence4 ThenReturn(Func<T1, T2, T3, T4, TReturn> callback) => ThenReturn((T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.FromResult(callback(arg1, arg2, arg3, arg4)));
        public MethodSequence4 ThenReturn(TReturn value) => ThenReturn((_, _, _, _) => Task.FromResult(value));

        public MethodSequence4 ThenReturn(params TReturn[] values)
        {
            if (values.Length == 0) { ElevateToSequence(); return new MethodSequence4(_interceptor); }
            var seq = ThenReturn(values[0]);
            for (int i = 1; i < values.Length; i++) seq.ThenReturn(values[i]);
            return seq;
        }

        public MethodCallBuilder4 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public MethodCallBuilder4 Verifiable(Called times) { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = times; return this; }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Func<T1, T2, T3, T4, Task<TReturn>> Callback, MethodCallBuilder4 Tracking)>();
                if (_interceptor._call != null)
                    _interceptor._sequence.Add((_interceptor._call, this));
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add(((_, _, _, _) => Task.FromResult(capturedValue), this));
                    _interceptor._hasReturnValue = false;
                    _interceptor._returnValue = default!;
                    _interceptor._returnValueTracking = null;
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        IMethodReturnSequence<Func<T1, T2, T3, T4, Task<TReturn>>> IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, Task<TReturn>>, (T1, T2, T3, T4)>.ThenReturn(Func<T1, T2, T3, T4, Task<TReturn>> callback) => ThenReturn(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTrackingArgs<(T1, T2, T3, T4)> IMethodTrackingArgs<(T1, T2, T3, T4)>.Verifiable() => Verifiable();
        IMethodTrackingArgs<(T1, T2, T3, T4)> IMethodTrackingArgs<(T1, T2, T3, T4)>.Verifiable(Called called) => Verifiable(called);
        IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, Task<TReturn>>, (T1, T2, T3, T4)> IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, Task<TReturn>>, (T1, T2, T3, T4)>.Verifiable() => Verifiable();
        IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, Task<TReturn>>, (T1, T2, T3, T4)> IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, Task<TReturn>>, (T1, T2, T3, T4)>.Verifiable(Called called) => Verifiable(called);
    }

    public sealed class MethodSequence4 : IMethodReturnSequence<Func<T1, T2, T3, T4, Task<TReturn>>>, IMethodReturnSequence, IMethodSequence
    {
        private readonly AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> _interceptor;
        internal MethodSequence4(AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> interceptor) => _interceptor = interceptor;

        public MethodSequence4 ThenReturn(Func<T1, T2, T3, T4, Task<TReturn>> asyncCallback)
        {
            var tracking = new MethodCallBuilder4(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, tracking));
            return this;
        }

        public MethodSequence4 ThenReturn(Func<T1, T2, T3, T4, TReturn> callback) => ThenReturn((T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.FromResult(callback(arg1, arg2, arg3, arg4)));
        public MethodSequence4 ThenReturn(TReturn value) => ThenReturn((_, _, _, _) => Task.FromResult(value));
        public MethodSequence4 ThenReturn(params TReturn[] values) { foreach (var v in values) ThenReturn(v); return this; }

        public void Verify()
        {
            if (_interceptor._sequence == null) return;
            if (_interceptor._sequenceIndex < _interceptor._sequence.Count)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("method", _interceptor._sequence.Count, _interceptor._sequenceIndex));
        }

        public void Reset() => _interceptor.Reset();
        public MethodSequence4 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public void ThenDefault() => _interceptor._repeatLastValue = false;

        IMethodReturnSequence<Func<T1, T2, T3, T4, Task<TReturn>>> IMethodReturnSequence<Func<T1, T2, T3, T4, Task<TReturn>>>.ThenReturn(Func<T1, T2, T3, T4, Task<TReturn>> callback) => ThenReturn(callback);
        IMethodReturnSequence<Func<T1, T2, T3, T4, Task<TReturn>>> IMethodReturnSequence<Func<T1, T2, T3, T4, Task<TReturn>>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    public sealed class WhenBuilder4
    {
        private readonly AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> _interceptor;
        private readonly Func<T1, T2, T3, T4, bool> _predicate;
        internal WhenBuilder4(AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> interceptor, Func<T1, T2, T3, T4, bool> predicate) { _interceptor = interceptor; _predicate = predicate; }

        public WhenChain4 Return(TReturn value)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));
            return new WhenChain4(_interceptor);
        }
    }

    public sealed class WhenChain4
    {
        private readonly AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> _interceptor;
        internal WhenChain4(AsyncMethodInterceptor4<T1, T2, T3, T4, TReturn> interceptor) => _interceptor = interceptor;

        /// <summary>Adds another matcher with exact value matching.</summary>
        public WhenBuilder4 ThenWhen(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new WhenBuilder4(_interceptor, (a1, a2, a3, a4) => object.Equals(a1, arg1) && object.Equals(a2, arg2) && object.Equals(a3, arg3) && object.Equals(a4, arg4));
        }

        public WhenBuilder4 ThenWhen(Func<T1, T2, T3, T4, bool> predicate) => new WhenBuilder4(_interceptor, predicate);

        public WhenChain4 ThenCall(Func<T1, T2, T3, T4, Task<TReturn>> asyncCallback)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCall(asyncCallback));
            return this;
        }

        public WhenChain4 ThenCall(Func<T1, T2, T3, T4, TReturn> callback) => ThenCall((T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Task.FromResult(callback(arg1, arg2, arg3, arg4)));

        public WhenChain4 ThenNone()
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

        public WhenChain4 Verifiable() { _interceptor._whenVerifiable = true; return this; }
    }
}

