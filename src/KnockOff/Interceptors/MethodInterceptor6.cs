#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled interceptor for non-void methods with 6 parameters.
/// Replaces generated interceptor classes by parameterizing on individual argument types.
/// All behavioral logic (Return, When, sequences, verification, builders) is pre-compiled.
/// </summary>
/// <typeparam name="T1">The type of parameter 1.</typeparam>
/// <typeparam name="T2">The type of parameter 2.</typeparam>
/// <typeparam name="T3">The type of parameter 3.</typeparam>
/// <typeparam name="T4">The type of parameter 4.</typeparam>
/// <typeparam name="T5">The type of parameter 5.</typeparam>
/// <typeparam name="T6">The type of parameter 6.</typeparam>
/// <typeparam name="TReturn">The return type.</typeparam>
public sealed class MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn>
{
    private readonly string _memberName;

    // Callback
    private Func<T1, T2, T3, T4, T5, T6, TReturn>? _call;
    private MethodCallBuilder6? _callTracking;

    // Return value
    private TReturn _returnValue = default!;
    private bool _hasReturnValue;
    private MethodCallBuilder6? _returnValueTracking;

    // Sequence
    private List<(Func<T1, T2, T3, T4, T5, T6, TReturn> Callback, MethodCallBuilder6 Tracking)>? _sequence;
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
    private (T1?, T2?, T3?, T4?, T5?, T6?)? _unconfiguredLastArgs;

    // Fallback delegates
    private Func<T1, T2, T3, T4, T5, T6, TReturn>? _fallback;
    private Func<T1, T2, T3, T4, T5, T6, TReturn>? _sourceFallback;

    public MethodInterceptor6(string memberName)
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

    /// <summary>Last arguments from the most recently called registration.</summary>
    public (T1?, T2?, T3?, T4?, T5?, T6?)? LastArgs
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

    /// <summary>Invokes the configured behavior. Called by generated interface implementation.</summary>
    public TReturn Invoke(bool strict, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(arg1, arg2, arg3, arg4, arg5, arg6))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                return matcher.CallReturn(arg1, arg2, arg3, arg4, arg5, arg6);
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
            tracking.RecordCall(arg1, arg2, arg3, arg4, arg5, arg6);
            _sequenceIndex++;
            return callback(arg1, arg2, arg3, arg4, arg5, arg6);
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall(arg1, arg2, arg3, arg4, arg5, arg6);
            return _returnValue;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg1, arg2, arg3, arg4, arg5, arg6);
            return _call(arg1, arg2, arg3, arg4, arg5, arg6);
        }

        // Nothing handled - unconfigured path
        _unconfiguredCallCount++;
        _unconfiguredLastArgs = (arg1, arg2, arg3, arg4, arg5, arg6);

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(arg1, arg2, arg3, arg4, arg5, arg6);
                return callback(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            return default!;
        }

        // Fallback (stub override)
        if (_fallback != null) return _fallback(arg1, arg2, arg3, arg4, arg5, arg6);

        // Source fallback
        if (_sourceFallback != null) return _sourceFallback(arg1, arg2, arg3, arg4, arg5, arg6);

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
        return default!;
    }

    // ========================================================================
    // Return / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures callback that repeats indefinitely. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder6 Return(Func<T1, T2, T3, T4, T5, T6, TReturn> callback)
    {
        var builder = new MethodCallBuilder6(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = callback;
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures return value that repeats indefinitely. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder6 Return(TReturn value)
    {
        var builder = new MethodCallBuilder6(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
        return builder;
    }

    /// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
    public MethodSequence6 Return(TReturn first, params TReturn[] rest)
    {
        var builder = Return((_, _, _, _, _, _) => first);
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
    public WhenBuilder6 When(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder6(this, (a1, a2, a3, a4, a5, a6) => object.Equals(a1, arg1) && object.Equals(a2, arg2) && object.Equals(a3, arg3) && object.Equals(a4, arg4) && object.Equals(a5, arg5) && object.Equals(a6, arg6));
    }

    /// <summary>Configures parameter-specific matching with predicate. Returns builder for Return().</summary>
    public WhenBuilder6 When(Func<T1, T2, T3, T4, T5, T6, bool> predicate)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder6(this, predicate);
    }

    /// <summary>Sets the fallback delegate for stub overrides.</summary>
    public void SetFallback(Func<T1, T2, T3, T4, T5, T6, TReturn>? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation.</summary>
    public void SetSourceFallback(Func<T1, T2, T3, T4, T5, T6, TReturn>? sourceFallback) => _sourceFallback = sourceFallback;

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
        public abstract bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public abstract TReturn CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and returns a stored value.</summary>
    private sealed class WhenMatcherValue : WhenMatcherBase
    {
        private readonly Func<T1, T2, T3, T4, T5, T6, bool> _predicate;
        private readonly TReturn _value;

        public WhenMatcherValue(Func<T1, T2, T3, T4, T5, T6, bool> predicate, TReturn value)
        {
            _predicate = predicate;
            _value = value;
        }

        public override bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _predicate(arg1, arg2, arg3, arg4, arg5, arg6);
        public override TReturn CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _value;
        public override bool IsTerminal => false;
    }

    /// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>
    private sealed class WhenMatcherCall : WhenMatcherBase
    {
        private readonly Func<T1, T2, T3, T4, T5, T6, TReturn> _callback;

        public WhenMatcherCall(Func<T1, T2, T3, T4, T5, T6, TReturn> callback) => _callback = callback;

        public override bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => true;
        public override TReturn CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => _callback(arg1, arg2, arg3, arg4, arg5, arg6);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class WhenMatcherNone : WhenMatcherBase
    {
        public override bool Matches(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => false;
        public override TReturn CallReturn(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => default!;
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder6
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder6 : IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, T5, T6, TReturn>, (T1?, T2?, T3?, T4?, T5?, T6?)>
    {
        private readonly MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> _interceptor;
        internal int _callCount;
        private (T1?, T2?, T3?, T4?, T5?, T6?) _lastArgs;

        internal MethodCallBuilder6(MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Last arguments passed to this callback.</summary>
        public (T1?, T2?, T3?, T4?, T5?, T6?) LastArgs => _lastArgs;

        internal void RecordCall(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            _callCount++;
            _lastArgs = (arg1, arg2, arg3, arg4, arg5, arg6);
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

        /// <summary>Elevates to sequence mode and adds another callback. Returns sequence for further chaining.</summary>
        public MethodSequence6 ThenReturn(Func<T1, T2, T3, T4, T5, T6, TReturn> callback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder6(_interceptor);
            _interceptor._sequence!.Add((callback, nextBuilder));
            return new MethodSequence6(_interceptor);
        }

        /// <summary>Elevates to sequence mode and adds a value. Returns sequence for further chaining.</summary>
        public MethodSequence6 ThenReturn(TReturn value)
        {
            return ThenReturn((_, _, _, _, _, _) => value);
        }

        /// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
        public MethodSequence6 ThenReturn(params TReturn[] values)
        {
            if (values.Length == 0)
            {
                ElevateToSequence();
                return new MethodSequence6(_interceptor);
            }
            var seq = ThenReturn(values[0]);
            for (int i = 1; i < values.Length; i++)
                seq.ThenReturn(values[i]);
            return seq;
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public MethodCallBuilder6 Verifiable()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
            return this;
        }

        /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
        public MethodCallBuilder6 Verifiable(Called times)
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = times;
            return this;
        }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Func<T1, T2, T3, T4, T5, T6, TReturn> Callback, MethodCallBuilder6 Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add(((_, _, _, _, _, _) => capturedValue, this));
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
        IMethodReturnSequence<Func<T1, T2, T3, T4, T5, T6, TReturn>> IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, T5, T6, TReturn>, (T1?, T2?, T3?, T4?, T5?, T6?)>.ThenReturn(Func<T1, T2, T3, T4, T5, T6, TReturn> callback) => ThenReturn(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTrackingArgs<(T1?, T2?, T3?, T4?, T5?, T6?)> IMethodTrackingArgs<(T1?, T2?, T3?, T4?, T5?, T6?)>.Verifiable() => Verifiable();
        IMethodTrackingArgs<(T1?, T2?, T3?, T4?, T5?, T6?)> IMethodTrackingArgs<(T1?, T2?, T3?, T4?, T5?, T6?)>.Verifiable(Called called) => Verifiable(called);
        IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, T5, T6, TReturn>, (T1?, T2?, T3?, T4?, T5?, T6?)> IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, T5, T6, TReturn>, (T1?, T2?, T3?, T4?, T5?, T6?)>.Verifiable() => Verifiable();
        IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, T5, T6, TReturn>, (T1?, T2?, T3?, T4?, T5?, T6?)> IMethodReturnBuilderArgs<Func<T1, T2, T3, T4, T5, T6, TReturn>, (T1?, T2?, T3?, T4?, T5?, T6?)>.Verifiable(Called called) => Verifiable(called);
    }

    // ========================================================================
    // Inner class: MethodSequence6
    // ========================================================================

    /// <summary>Sequence for non-void methods. Supports ThenReturn chaining.</summary>
    public sealed class MethodSequence6 : IMethodReturnSequence<Func<T1, T2, T3, T4, T5, T6, TReturn>>, IMethodReturnSequence, IMethodSequence
    {
        private readonly MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> _interceptor;

        internal MethodSequence6(MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another callback to the sequence.</summary>
        public MethodSequence6 ThenReturn(Func<T1, T2, T3, T4, T5, T6, TReturn> callback)
        {
            var tracking = new MethodCallBuilder6(_interceptor);
            _interceptor._sequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Adds a value to the sequence.</summary>
        public MethodSequence6 ThenReturn(TReturn value)
        {
            return ThenReturn((_, _, _, _, _, _) => value);
        }

        /// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
        public MethodSequence6 ThenReturn(params TReturn[] values)
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
        public MethodSequence6 Verifiable()
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
        IMethodReturnSequence<Func<T1, T2, T3, T4, T5, T6, TReturn>> IMethodReturnSequence<Func<T1, T2, T3, T4, T5, T6, TReturn>>.ThenReturn(Func<T1, T2, T3, T4, T5, T6, TReturn> callback) => ThenReturn(callback);
        IMethodReturnSequence<Func<T1, T2, T3, T4, T5, T6, TReturn>> IMethodReturnSequence<Func<T1, T2, T3, T4, T5, T6, TReturn>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: WhenBuilder6
    // ========================================================================

    /// <summary>Builder for When matchers. Captures predicate, awaits Return(value).</summary>
    public sealed class WhenBuilder6
    {
        private readonly MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> _interceptor;
        private readonly Func<T1, T2, T3, T4, T5, T6, bool> _predicate;

        internal WhenBuilder6(MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> interceptor, Func<T1, T2, T3, T4, T5, T6, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
        }

        /// <summary>Configures the return value for this When match.</summary>
        public WhenChain6 Return(TReturn value)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));
            return new WhenChain6(_interceptor);
        }
    }

    // ========================================================================
    // Inner class: WhenChain6
    // ========================================================================

    /// <summary>When chain with ThenWhen, ThenCall, ThenNone, verification support.</summary>
    public sealed class WhenChain6
    {
        private readonly MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> _interceptor;

        internal WhenChain6(MethodInterceptor6<T1, T2, T3, T4, T5, T6, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another matcher with exact value matching.</summary>
        public WhenBuilder6 ThenWhen(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return new WhenBuilder6(_interceptor, (a1, a2, a3, a4, a5, a6) => object.Equals(a1, arg1) && object.Equals(a2, arg2) && object.Equals(a3, arg3) && object.Equals(a4, arg4) && object.Equals(a5, arg5) && object.Equals(a6, arg6));
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public WhenBuilder6 ThenWhen(Func<T1, T2, T3, T4, T5, T6, bool> predicate)
        {
            return new WhenBuilder6(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional callback as terminal matcher.</summary>
        public WhenChain6 ThenCall(Func<T1, T2, T3, T4, T5, T6, TReturn> callback)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCall(callback));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public WhenChain6 ThenNone()
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
        public WhenChain6 Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
