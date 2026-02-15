#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords
#pragma warning disable CA1030 // Use events where appropriate

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for non-void methods with 2 parameters.
/// Handles Task&lt;TReturn&gt; and ValueTask&lt;TReturn&gt; interface methods.
/// Provides simplified Return(Func&lt;T1, T2, TReturn&gt;) that auto-wraps in Task.FromResult,
/// and full async Return(Func&lt;T1, T2, Task&lt;TReturn&gt;&gt;) for async chains.
/// </summary>
/// <typeparam name="T1">The type of the first parameter.</typeparam>
/// <typeparam name="T2">The type of the second parameter.</typeparam>
/// <typeparam name="TReturn">The inner return type (e.g., int for Task&lt;int&gt;).</typeparam>
public sealed class AsyncMethodInterceptor2<T1, T2, TReturn>
{
    private readonly string _memberName;

    // Callback (stored as async form)
    private Func<T1, T2, Task<TReturn>>? _call;
    private MethodCallBuilder2? _callTracking;

    // Return value
    private TReturn _returnValue = default!;
    private bool _hasReturnValue;
    private MethodCallBuilder2? _returnValueTracking;

    // Sequence
    private List<(Func<T1, T2, Task<TReturn>> Callback, MethodCallBuilder2 Tracking)>? _sequence;
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
    private (T1, T2)? _unconfiguredLastArgs;

    // Fallback delegates (stored as async form)
    private Func<T1, T2, Task<TReturn>>? _fallback;
    private Func<T1, T2, Task<TReturn>>? _sourceFallback;

    // Smart default factory (for NewInstance/ThrowException strategies)
    private readonly Func<TReturn>? _defaultFactory;

    public AsyncMethodInterceptor2(string memberName)
    {
        _memberName = memberName;
    }

    /// <summary>Constructor with smart default factory for non-strict unconfigured calls.</summary>
    public AsyncMethodInterceptor2(string memberName, Func<TReturn> defaultFactory)
    {
        _memberName = memberName;
        _defaultFactory = defaultFactory;
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

    /// <summary>Last arguments from the most recently called registration.</summary>
    public (T1, T2)? LastArgs
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

    /// <summary>Invokes the configured behavior. Called by generated interface implementation. Returns Task&lt;TReturn&gt;.</summary>
    public async Task<TReturn> Invoke(bool strict, T1 arg1, T2 arg2)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(arg1, arg2))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                return await matcher.CallReturn(arg1, arg2).ConfigureAwait(false);
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
            tracking.RecordCall(arg1, arg2);
            _sequenceIndex++;
            return await callback(arg1, arg2).ConfigureAwait(false);
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall(arg1, arg2);
            return _returnValue;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg1, arg2);
            return await _call(arg1, arg2).ConfigureAwait(false);
        }

        // Nothing handled - unconfigured path
        _unconfiguredCallCount++;
        _unconfiguredLastArgs = (arg1, arg2);

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(arg1, arg2);
                return await callback(arg1, arg2).ConfigureAwait(false);
            }
            return default!;
        }

        // Fallback (stub override)
        if (_fallback != null) return await _fallback(arg1, arg2).ConfigureAwait(false);

        // Source fallback
        if (_sourceFallback != null) return await _sourceFallback(arg1, arg2).ConfigureAwait(false);

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);

        // Smart default (NewInstance or ThrowException)
        if (_defaultFactory != null) return _defaultFactory();
        return default!;
    }

    // ========================================================================
    // Return / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures async callback that repeats indefinitely. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder2 Return(Func<T1, T2, Task<TReturn>> asyncCallback)
    {
        var builder = new MethodCallBuilder2(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = asyncCallback;
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures simplified sync callback that repeats indefinitely. Auto-wraps in Task.FromResult.</summary>
    public MethodCallBuilder2 Return(Func<T1, T2, TReturn> callback)
    {
        return Return((T1 arg1, T2 arg2) => Task.FromResult(callback(arg1, arg2)));
    }

    /// <summary>Configures return value that repeats indefinitely. Auto-wraps in Task.FromResult. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder2 Return(TReturn value)
    {
        var builder = new MethodCallBuilder2(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
        return builder;
    }

    /// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
    public MethodSequence2 Return(TReturn first, params TReturn[] rest)
    {
        var builder = Return((T1 _, T2 _) => Task.FromResult(first));
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

    /// <summary>Configures parameter-specific matching with exact values. Returns builder for Return().</summary>
    public WhenBuilder2 When(T1 arg1, T2 arg2)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder2(this, (a1, a2) => object.Equals(a1, arg1) && object.Equals(a2, arg2));
    }

    /// <summary>Configures parameter-specific matching with predicate. Returns builder for Return().</summary>
    public WhenBuilder2 When(Func<T1, T2, bool> predicate)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder2(this, predicate);
    }

    /// <summary>Sets the fallback delegate for stub overrides (async form).</summary>
    public void SetFallback(Func<T1, T2, Task<TReturn>>? fallback) => _fallback = fallback;

    /// <summary>Sets the fallback delegate for stub overrides (sync form, auto-wraps).</summary>
    public void SetFallback(Func<T1, T2, TReturn>? fallback) =>
        _fallback = fallback != null ? (T1 a1, T2 a2) => Task.FromResult(fallback(a1, a2)) : null;

    /// <summary>Sets the source fallback delegate for source delegation (async form).</summary>
    public void SetSourceFallback(Func<T1, T2, Task<TReturn>>? sourceFallback) => _sourceFallback = sourceFallback;

    /// <summary>Sets the source fallback delegate for source delegation (sync form, auto-wraps).</summary>
    public void SetSourceFallback(Func<T1, T2, TReturn>? sourceFallback) =>
        _sourceFallback = sourceFallback != null ? (T1 a1, T2 a2) => Task.FromResult(sourceFallback(a1, a2)) : null;

    /// <summary>Verifies method was called at least once.</summary>
    public void Verify() => Verify(Called.AtLeastOnce);

    /// <summary>Verifies call count satisfies the Called constraint.</summary>
    public void Verify(Called times)
    {
        if (!times.Validate(TotalCallCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, TotalCallCount));
    }

    /// <summary>Marks for verification by Stub.Verify().</summary>
    public void Verifiable()
    {
        _isVerifiable = true;
        _verifiableTimes = null;
    }

    /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
    public void Verifiable(Called times)
    {
        _isVerifiable = true;
        _verifiableTimes = times;
    }

    /// <summary>Whether this interceptor was marked with Verifiable().</summary>
    public bool IsVerifiable => _isVerifiable;

    /// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>
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

    /// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>
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

    /// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>
    public void Reset()
    {
        _unconfiguredCallCount = 0;
        _unconfiguredLastArgs = default;
        _callTracking?.Reset();
        _returnValueTracking?.Reset();
        if (_sequence != null)
        {
            foreach (var (_, tracking) in _sequence)
                tracking.Reset();
        }
        _sequenceIndex = 0;
        _whenChainHead = 0;
        if (_whenChain != null)
        {
            foreach (var matcher in _whenChain)
                matcher.CallCount = 0;
        }
    }

    // ========================================================================
    // Inner class: WhenMatcherBase (abstract)
    // ========================================================================

    private abstract class WhenMatcherBase
    {
        public abstract bool Matches(T1 arg1, T2 arg2);
        public abstract Task<TReturn> CallReturn(T1 arg1, T2 arg2);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and returns a stored value.</summary>
    private sealed class WhenMatcherValue : WhenMatcherBase
    {
        private readonly Func<T1, T2, bool> _predicate;
        private readonly TReturn _value;

        public WhenMatcherValue(Func<T1, T2, bool> predicate, TReturn value)
        {
            _predicate = predicate;
            _value = value;
        }

        public override bool Matches(T1 arg1, T2 arg2) => _predicate(arg1, arg2);
        public override Task<TReturn> CallReturn(T1 arg1, T2 arg2) => Task.FromResult(_value);
        public override bool IsTerminal => false;
    }

    /// <summary>Matcher that always matches and invokes an async callback. Terminal.</summary>
    private sealed class WhenMatcherCall : WhenMatcherBase
    {
        private readonly Func<T1, T2, Task<TReturn>> _callback;

        public WhenMatcherCall(Func<T1, T2, Task<TReturn>> callback) => _callback = callback;

        public override bool Matches(T1 arg1, T2 arg2) => true;
        public override Task<TReturn> CallReturn(T1 arg1, T2 arg2) => _callback(arg1, arg2);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class WhenMatcherNone : WhenMatcherBase
    {
        public override bool Matches(T1 arg1, T2 arg2) => false;
        public override Task<TReturn> CallReturn(T1 arg1, T2 arg2) => Task.FromResult(default(TReturn)!);
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder2
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder2 : IMethodReturnBuilderArgs<Func<T1, T2, Task<TReturn>>, (T1, T2)>
    {
        private readonly AsyncMethodInterceptor2<T1, T2, TReturn> _interceptor;
        internal int _callCount;
        private (T1, T2) _lastArgs;

        internal MethodCallBuilder2(AsyncMethodInterceptor2<T1, T2, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Last arguments passed to this callback.</summary>
        public (T1, T2) LastArgs => _lastArgs;

        internal void RecordCall(T1 arg1, T2 arg2)
        {
            _callCount++;
            _lastArgs = (arg1, arg2);
        }

        /// <summary>Resets tracking state.</summary>
        public void Reset()
        {
            _callCount = 0;
            _lastArgs = default;
        }

        /// <summary>Verifies callback was invoked at least once.</summary>
        public void Verify() => Verify(Called.AtLeastOnce);

        /// <summary>Verifies call count satisfies the Called constraint.</summary>
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("method", called, _callCount));
        }

        /// <summary>Elevates to sequence mode and adds another async callback. Returns sequence for further chaining.</summary>
        public MethodSequence2 ThenReturn(Func<T1, T2, Task<TReturn>> asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder2(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, nextBuilder));
            return new MethodSequence2(_interceptor);
        }

        /// <summary>Elevates to sequence mode and adds another sync callback. Returns sequence for further chaining.</summary>
        public MethodSequence2 ThenReturn(Func<T1, T2, TReturn> callback)
        {
            return ThenReturn((T1 arg1, T2 arg2) => Task.FromResult(callback(arg1, arg2)));
        }

        /// <summary>Elevates to sequence mode and adds a value. Returns sequence for further chaining.</summary>
        public MethodSequence2 ThenReturn(TReturn value)
        {
            return ThenReturn((T1 _, T2 _) => Task.FromResult(value));
        }

        /// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
        public MethodSequence2 ThenReturn(params TReturn[] values)
        {
            if (values.Length == 0)
            {
                ElevateToSequence();
                return new MethodSequence2(_interceptor);
            }
            var seq = ThenReturn(values[0]);
            for (int i = 1; i < values.Length; i++)
                seq.ThenReturn(values[i]);
            return seq;
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public MethodCallBuilder2 Verifiable()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
            return this;
        }

        /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
        public MethodCallBuilder2 Verifiable(Called times)
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = times;
            return this;
        }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Func<T1, T2, Task<TReturn>> Callback, MethodCallBuilder2 Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add(((_, _) => Task.FromResult(capturedValue), this));
                    _interceptor._hasReturnValue = false;
                    _interceptor._returnValue = default!;
                    _interceptor._returnValueTracking = null;
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        // Explicit interface implementations
        IMethodReturnSequence<Func<T1, T2, Task<TReturn>>> IMethodReturnBuilderArgs<Func<T1, T2, Task<TReturn>>, (T1, T2)>.ThenReturn(Func<T1, T2, Task<TReturn>> callback) => ThenReturn(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTrackingArgs<(T1, T2)> IMethodTrackingArgs<(T1, T2)>.Verifiable() => Verifiable();
        IMethodTrackingArgs<(T1, T2)> IMethodTrackingArgs<(T1, T2)>.Verifiable(Called called) => Verifiable(called);
        IMethodReturnBuilderArgs<Func<T1, T2, Task<TReturn>>, (T1, T2)> IMethodReturnBuilderArgs<Func<T1, T2, Task<TReturn>>, (T1, T2)>.Verifiable() => Verifiable();
        IMethodReturnBuilderArgs<Func<T1, T2, Task<TReturn>>, (T1, T2)> IMethodReturnBuilderArgs<Func<T1, T2, Task<TReturn>>, (T1, T2)>.Verifiable(Called called) => Verifiable(called);
    }

    // ========================================================================
    // Inner class: MethodSequence2
    // ========================================================================

    /// <summary>Sequence for async non-void methods. Supports ThenReturn chaining.</summary>
    public sealed class MethodSequence2 : IMethodReturnSequence<Func<T1, T2, Task<TReturn>>>, IMethodReturnSequence, IMethodSequence
    {
        private readonly AsyncMethodInterceptor2<T1, T2, TReturn> _interceptor;

        internal MethodSequence2(AsyncMethodInterceptor2<T1, T2, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another async callback to the sequence.</summary>
        public MethodSequence2 ThenReturn(Func<T1, T2, Task<TReturn>> asyncCallback)
        {
            var tracking = new MethodCallBuilder2(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, tracking));
            return this;
        }

        /// <summary>Adds another sync callback to the sequence (auto-wraps).</summary>
        public MethodSequence2 ThenReturn(Func<T1, T2, TReturn> callback)
        {
            return ThenReturn((T1 arg1, T2 arg2) => Task.FromResult(callback(arg1, arg2)));
        }

        /// <summary>Adds a value to the sequence.</summary>
        public MethodSequence2 ThenReturn(TReturn value)
        {
            return ThenReturn((T1 _, T2 _) => Task.FromResult(value));
        }

        /// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
        public MethodSequence2 ThenReturn(params TReturn[] values)
        {
            foreach (var value in values) ThenReturn(value);
            return this;
        }

        /// <summary>Verifies the entire sequence was executed.</summary>
        public void Verify()
        {
            if (_interceptor._sequence == null) return;
            var sequenceLength = _interceptor._sequence.Count;
            var completedCount = _interceptor._sequenceIndex;
            if (completedCount < sequenceLength)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("method", sequenceLength, completedCount));
        }

        /// <summary>Resets all tracking in the sequence.</summary>
        public void Reset() => _interceptor.Reset();

        /// <summary>Marks this sequence for verification by Stub.Verify().</summary>
        public MethodSequence2 Verifiable()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
            return this;
        }

        /// <summary>Terminates sequence with default instead of repeating last value.</summary>
        public void ThenDefault()
        {
            _interceptor._repeatLastValue = false;
        }

        // Explicit interface implementations
        IMethodReturnSequence<Func<T1, T2, Task<TReturn>>> IMethodReturnSequence<Func<T1, T2, Task<TReturn>>>.ThenReturn(Func<T1, T2, Task<TReturn>> callback) => ThenReturn(callback);
        IMethodReturnSequence<Func<T1, T2, Task<TReturn>>> IMethodReturnSequence<Func<T1, T2, Task<TReturn>>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: WhenBuilder2
    // ========================================================================

    /// <summary>Builder for When matchers. Captures predicate, awaits Return(value).</summary>
    public sealed class WhenBuilder2
    {
        private readonly AsyncMethodInterceptor2<T1, T2, TReturn> _interceptor;
        private readonly Func<T1, T2, bool> _predicate;

        internal WhenBuilder2(AsyncMethodInterceptor2<T1, T2, TReturn> interceptor, Func<T1, T2, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
        }

        /// <summary>Configures the return value for this When match.</summary>
        public WhenChain2 Return(TReturn value)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));
            return new WhenChain2(_interceptor);
        }
    }

    // ========================================================================
    // Inner class: WhenChain2
    // ========================================================================

    /// <summary>When chain with ThenWhen, ThenCall, ThenNone, verification support.</summary>
    public sealed class WhenChain2
    {
        private readonly AsyncMethodInterceptor2<T1, T2, TReturn> _interceptor;

        internal WhenChain2(AsyncMethodInterceptor2<T1, T2, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another matcher with exact value matching.</summary>
        public WhenBuilder2 ThenWhen(T1 arg1, T2 arg2)
        {
            return new WhenBuilder2(_interceptor, (a1, a2) => object.Equals(a1, arg1) && object.Equals(a2, arg2));
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public WhenBuilder2 ThenWhen(Func<T1, T2, bool> predicate)
        {
            return new WhenBuilder2(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional async callback as terminal matcher.</summary>
        public WhenChain2 ThenCall(Func<T1, T2, Task<TReturn>> asyncCallback)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCall(asyncCallback));
            return this;
        }

        /// <summary>Adds an unconditional sync callback as terminal matcher (auto-wraps).</summary>
        public WhenChain2 ThenCall(Func<T1, T2, TReturn> callback)
        {
            return ThenCall((T1 arg1, T2 arg2) => Task.FromResult(callback(arg1, arg2)));
        }

        /// <summary>Closes chain with no matcher.</summary>
        public WhenChain2 ThenNone()
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherNone());
            return this;
        }

        /// <summary>Verifies the When chain was fully consumed.</summary>
        public void Verify()
        {
            if (_interceptor._whenChain == null || _interceptor._whenChain.Count == 0) return;
            var head = _interceptor._whenChainHead;
            var count = _interceptor._whenChain.Count;
            if (head < count && !_interceptor._whenChain[head].IsTerminal && _interceptor._whenChain[head].CallCount == 0)
            {
                throw new VerificationException(VerificationFailure.SequenceIncomplete("When chain", count, head));
            }
        }

        /// <summary>Resets When chain HEAD and all matcher call counts.</summary>
        public void Reset()
        {
            _interceptor._whenChainHead = 0;
            if (_interceptor._whenChain != null)
            {
                foreach (var matcher in _interceptor._whenChain)
                    matcher.CallCount = 0;
            }
        }

        /// <summary>Marks this When chain for verification by Stub.Verify().</summary>
        public WhenChain2 Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
