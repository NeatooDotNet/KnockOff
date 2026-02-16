#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled interceptor for void methods with 1+ parameters using TTuple approach.
/// TDelegate is a generated delegate type providing named callback parameters.
/// TArgs is either a raw type (1 param) or a ValueTuple (2+ params) providing named When parameters.
/// All behavioral logic (Call, When, sequences, verification, builders) is pre-compiled.
/// Expression trees bridge between TDelegate invocation and TArgs matching.
/// </summary>
/// <typeparam name="TDelegate">The generated delegate type for callbacks (e.g., ExecuteDelegate).</typeparam>
/// <typeparam name="TArgs">The argument type: raw type for 1 param, ValueTuple for 2+ params.</typeparam>
public sealed class VoidMethodInterceptor<TDelegate, TArgs> : IInterceptor where TDelegate : Delegate where TArgs : struct
{
    // Static expression tree invoker -- compiled once per closed generic type combo
    private static readonly Action<TDelegate, TArgs> s_voidInvoker
        = DelegateInvokerFactory.BuildVoidInvoker<TDelegate, TArgs>();

    private readonly string _memberName;

    // Callback
    private TDelegate? _call;
    private MethodCallBuilder? _callTracking;

    // Sequence
    private List<(TDelegate Callback, MethodCallBuilder Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;

    // When chain
    private List<VoidWhenMatcherBase>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;

    // Verification
    private bool _isVerifiable;
    private Called? _verifiableTimes;

    // Unconfigured tracking
    private int _unconfiguredCallCount;
    private TArgs? _unconfiguredLastArgs;

    // Fallback delegates
    private TDelegate? _fallback;
    private TDelegate? _sourceFallback;

    public VoidMethodInterceptor(string memberName)
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
            var sum = _unconfiguredCallCount + (_callTracking?._callCount ?? 0);
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
    public bool IsConfigured => _call != null || (_sequence?.Count ?? 0) > 0 || (_whenChain?.Count ?? 0) > 0;

    /// <summary>Last arguments from the most recently called registration.</summary>
    public TArgs? LastArgs
    {
        get
        {
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
    public void Invoke(bool strict, TArgs args)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(args))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                matcher.Call(args);
                return;
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
            tracking.RecordCall(args);
            _sequenceIndex++;
            s_voidInvoker(callback, args);
            return;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(args);
            s_voidInvoker(_call, args);
            return;
        }

        // Nothing handled - unconfigured path
        _unconfiguredCallCount++;
        _unconfiguredLastArgs = args;

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(args);
                s_voidInvoker(callback, args);
                return;
            }
            return; // exhausted but no repeat - just return (void)
        }

        // Fallback (stub override)
        if (_fallback != null) { s_voidInvoker(_fallback, args); return; }

        // Source fallback
        if (_sourceFallback != null) { s_voidInvoker(_sourceFallback, args); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
    }

    // ========================================================================
    // Call / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures callback that repeats indefinitely. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder Call(TDelegate callback)
    {
        var builder = new MethodCallBuilder(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = callback;
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures parameter-specific matching with exact value.</summary>
    public VoidWhenBuilder When(TArgs args)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder(this, (a) => object.Equals(a, args));
    }

    /// <summary>Configures parameter-specific matching with predicate.</summary>
    public VoidWhenBuilder When(Func<TArgs, bool> predicate)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder(this, predicate);
    }

    /// <summary>Sets the fallback delegate for stub overrides.</summary>
    public void SetFallback(TDelegate? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation.</summary>
    public void SetSourceFallback(TDelegate? sourceFallback) => _sourceFallback = sourceFallback;

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
    // Inner class: VoidWhenMatcherBase (abstract)
    // ========================================================================

    private abstract class VoidWhenMatcherBase
    {
        public abstract bool Matches(TArgs args);
        public abstract void Call(TArgs args);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and optionally invokes a callback via expression tree.</summary>
    private sealed class VoidWhenMatcherPredicate : VoidWhenMatcherBase
    {
        private readonly Func<TArgs, bool> _predicate;
        private TDelegate? _callback;

        public VoidWhenMatcherPredicate(Func<TArgs, bool> predicate) => _predicate = predicate;

        public override bool Matches(TArgs args) => _predicate(args);
        public override void Call(TArgs args) { if (_callback != null) s_voidInvoker(_callback, args); }
        public override bool IsTerminal => false;

        public void SetCallback(TDelegate callback) => _callback = callback;
    }

    /// <summary>Matcher that always matches and invokes a callback via expression tree. Terminal.</summary>
    private sealed class VoidWhenMatcherCall : VoidWhenMatcherBase
    {
        private readonly TDelegate _callback;

        public VoidWhenMatcherCall(TDelegate callback) => _callback = callback;

        public override bool Matches(TArgs args) => true;
        public override void Call(TArgs args) => s_voidInvoker(_callback, args);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class VoidWhenMatcherNone : VoidWhenMatcherBase
    {
        public override bool Matches(TArgs args) => false;
        public override void Call(TArgs args) { }
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder : IMethodCallBuilderArgs<TDelegate, TArgs?>
    {
        private readonly VoidMethodInterceptor<TDelegate, TArgs> _interceptor;
        internal int _callCount;
        private TArgs? _lastArgs;

        internal MethodCallBuilder(VoidMethodInterceptor<TDelegate, TArgs> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Last arguments passed to this callback.</summary>
        public TArgs? LastArgs => _lastArgs;

        internal void RecordCall(TArgs args)
        {
            _callCount++;
            _lastArgs = args;
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
        public MethodSequence ThenCall(TDelegate callback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add((callback, nextBuilder));
            return new MethodSequence(_interceptor);
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public MethodCallBuilder Verifiable()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
            return this;
        }

        /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
        public MethodCallBuilder Verifiable(Called times)
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = times;
            return this;
        }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(TDelegate Callback, MethodCallBuilder Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        // Explicit interface implementations
        IMethodCallSequence<TDelegate> IMethodCallBuilderArgs<TDelegate, TArgs?>.ThenCall(TDelegate callback) => ThenCall(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTrackingArgs<TArgs?> IMethodTrackingArgs<TArgs?>.Verifiable() => Verifiable();
        IMethodTrackingArgs<TArgs?> IMethodTrackingArgs<TArgs?>.Verifiable(Called called) => Verifiable(called);
        IMethodCallBuilderArgs<TDelegate, TArgs?> IMethodCallBuilderArgs<TDelegate, TArgs?>.Verifiable() => Verifiable();
        IMethodCallBuilderArgs<TDelegate, TArgs?> IMethodCallBuilderArgs<TDelegate, TArgs?>.Verifiable(Called called) => Verifiable(called);
    }

    // ========================================================================
    // Inner class: MethodSequence
    // ========================================================================

    /// <summary>Sequence for void methods. Supports ThenCall chaining.</summary>
    public sealed class MethodSequence : IMethodCallSequence<TDelegate>, IMethodCallSequence, IMethodSequence
    {
        private readonly VoidMethodInterceptor<TDelegate, TArgs> _interceptor;

        internal MethodSequence(VoidMethodInterceptor<TDelegate, TArgs> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another callback to the sequence.</summary>
        public MethodSequence ThenCall(TDelegate callback)
        {
            var tracking = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add((callback, tracking));
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
        public MethodSequence Verifiable()
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
        IMethodCallSequence<TDelegate> IMethodCallSequence<TDelegate>.ThenCall(TDelegate callback) => ThenCall(callback);
        IMethodCallSequence<TDelegate> IMethodCallSequence<TDelegate>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: VoidWhenBuilder
    // ========================================================================

    /// <summary>Builder for When matchers on void methods. Captures predicate, awaits Call().</summary>
    public sealed class VoidWhenBuilder
    {
        private readonly VoidMethodInterceptor<TDelegate, TArgs> _interceptor;
        private readonly Func<TArgs, bool> _predicate;
        private int _matcherIndex = -1;

        internal VoidWhenBuilder(VoidMethodInterceptor<TDelegate, TArgs> interceptor, Func<TArgs, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicate(_predicate);
            _interceptor._whenChain.Add(matcher);
            _matcherIndex = _interceptor._whenChain.Count - 1;
        }

        public VoidWhenChain Call(TDelegate callback)
        {
            ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex]).SetCallback(callback);
            return new VoidWhenChain(_interceptor, _matcherIndex);
        }

        public VoidWhenChain ThenCall(TDelegate callback)
        {
            _interceptor._whenChain!.Add(new VoidWhenMatcherCall(callback));
            return new VoidWhenChain(_interceptor, _matcherIndex);
        }

        public void Verify(Called times)
        {
            if (_interceptor._whenChain == null || _matcherIndex >= _interceptor._whenChain.Count) return;
            var callCount = _interceptor._whenChain[_matcherIndex].CallCount;
            if (!times.Validate(callCount))
                throw new VerificationException(new VerificationFailure("When matcher", times, callCount));
        }
    }

    // ========================================================================
    // Inner class: VoidWhenChain
    // ========================================================================

    /// <summary>Void When chain with ThenWhen, ThenCall, ThenNone, verification support.</summary>
    public sealed class VoidWhenChain
    {
        private readonly VoidMethodInterceptor<TDelegate, TArgs> _interceptor;
        private readonly int _currentMatcherIndex;

        internal VoidWhenChain(VoidMethodInterceptor<TDelegate, TArgs> interceptor, int currentMatcherIndex)
        {
            _interceptor = interceptor;
            _currentMatcherIndex = currentMatcherIndex;
        }

        /// <summary>Adds another matcher with exact value matching.</summary>
        public VoidWhenBuilder ThenWhen(TArgs args)
        {
            return new VoidWhenBuilder(_interceptor, (a) => object.Equals(a, args));
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public VoidWhenBuilder ThenWhen(Func<TArgs, bool> predicate)
        {
            return new VoidWhenBuilder(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional callback as terminal matcher.</summary>
        public VoidWhenChain ThenCall(TDelegate callback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall(callback));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public VoidWhenChain ThenNone()
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherNone());
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

        /// <summary>Verifies this specific matcher was called the expected number of times.</summary>
        public void Verify(Called times)
        {
            if (_interceptor._whenChain == null || _currentMatcherIndex >= _interceptor._whenChain.Count) return;
            var callCount = _interceptor._whenChain[_currentMatcherIndex].CallCount;
            if (!times.Validate(callCount))
            {
                throw new VerificationException(new VerificationFailure("When matcher", times, callCount));
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
        public VoidWhenChain Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
