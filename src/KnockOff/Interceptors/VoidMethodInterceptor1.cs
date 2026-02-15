#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled interceptor for void methods with 1 parameter.
/// Replaces generated interceptor classes by parameterizing on individual argument types.
/// All behavioral logic (Call, When, sequences, verification, builders) is pre-compiled.
/// </summary>
/// <typeparam name="T1">The type of the first parameter.</typeparam>
public sealed class VoidMethodInterceptor1<T1>
{
    private readonly string _memberName;

    // Callback
    private Action<T1>? _call;
    private MethodCallBuilder1? _callTracking;

    // Sequence
    private List<(Action<T1> Callback, MethodCallBuilder1 Tracking)>? _sequence;
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
    private T1? _unconfiguredLastArg;

    // Fallback delegates
    private Action<T1>? _fallback;
    private Action<T1>? _sourceFallback;

    public VoidMethodInterceptor1(string memberName)
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

    /// <summary>Last argument from the most recently called registration.</summary>
    public T1? LastArg
    {
        get
        {
            if ((_callTracking?._callCount ?? 0) > 0)
                return _callTracking!.LastArg;
            if (_sequence != null)
                for (int i = _sequence.Count - 1; i >= 0; i--)
                    if (_sequence[i].Tracking._callCount > 0)
                        return _sequence[i].Tracking.LastArg;
            return _unconfiguredCallCount > 0 ? _unconfiguredLastArg : default;
        }
    }

    // ========================================================================
    // Invoke
    // ========================================================================

    /// <summary>Invokes the configured behavior. Called by generated interface implementation.</summary>
    public void Invoke(bool strict, T1 arg1)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(arg1))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                matcher.Call(arg1);
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
            tracking.RecordCall(arg1);
            _sequenceIndex++;
            callback(arg1);
            return;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg1);
            _call(arg1);
            return;
        }

        // Nothing handled - unconfigured path
        _unconfiguredCallCount++;
        _unconfiguredLastArg = arg1;

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(arg1);
                callback(arg1);
                return;
            }
            return; // exhausted but no repeat - just return (void)
        }

        // Fallback (stub override)
        if (_fallback != null) { _fallback(arg1); return; }

        // Source fallback
        if (_sourceFallback != null) { _sourceFallback(arg1); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
    }

    // ========================================================================
    // Call / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures callback that repeats indefinitely. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder1 Call(Action<T1> callback)
    {
        var builder = new MethodCallBuilder1(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = callback;
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures parameter-specific matching with exact value.</summary>
    public VoidWhenBuilder1 When(T1 arg1)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder1(this, (a1) => object.Equals(a1, arg1));
    }

    /// <summary>Configures parameter-specific matching with predicate.</summary>
    public VoidWhenBuilder1 When(Func<T1, bool> predicate)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder1(this, predicate);
    }

    /// <summary>Sets the fallback delegate for stub overrides.</summary>
    public void SetFallback(Action<T1>? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation.</summary>
    public void SetSourceFallback(Action<T1>? sourceFallback) => _sourceFallback = sourceFallback;

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
        _unconfiguredLastArg = default;
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
        public abstract bool Matches(T1 arg1);
        public abstract void Call(T1 arg1);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and optionally invokes a callback.</summary>
    private sealed class VoidWhenMatcherPredicate : VoidWhenMatcherBase
    {
        private readonly Func<T1, bool> _predicate;
        private Action<T1>? _callback;

        public VoidWhenMatcherPredicate(Func<T1, bool> predicate) => _predicate = predicate;

        public override bool Matches(T1 arg1) => _predicate(arg1);
        public override void Call(T1 arg1) => _callback?.Invoke(arg1);
        public override bool IsTerminal => false;

        public void SetCallback(Action<T1> callback) => _callback = callback;
    }

    /// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>
    private sealed class VoidWhenMatcherCall : VoidWhenMatcherBase
    {
        private readonly Action<T1> _callback;

        public VoidWhenMatcherCall(Action<T1> callback) => _callback = callback;

        public override bool Matches(T1 arg1) => true;
        public override void Call(T1 arg1) => _callback(arg1);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class VoidWhenMatcherNone : VoidWhenMatcherBase
    {
        public override bool Matches(T1 arg1) => false;
        public override void Call(T1 arg1) { }
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder1
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder1 : IMethodCallBuilder<Action<T1>, T1?>
    {
        private readonly VoidMethodInterceptor1<T1> _interceptor;
        internal int _callCount;
        private T1? _lastArg;

        internal MethodCallBuilder1(VoidMethodInterceptor1<T1> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Last argument passed to this callback.</summary>
        public T1? LastArg => _lastArg;

        internal void RecordCall(T1 arg1)
        {
            _callCount++;
            _lastArg = arg1;
        }

        /// <summary>Resets tracking state.</summary>
        public void Reset()
        {
            _callCount = 0;
            _lastArg = default;
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
        public MethodSequence1 ThenCall(Action<T1> callback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder1(_interceptor);
            _interceptor._sequence!.Add((callback, nextBuilder));
            return new MethodSequence1(_interceptor);
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public MethodCallBuilder1 Verifiable()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
            return this;
        }

        /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
        public MethodCallBuilder1 Verifiable(Called times)
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = times;
            return this;
        }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Action<T1> Callback, MethodCallBuilder1 Tracking)>();
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
        IMethodCallSequence<Action<T1>> IMethodCallBuilder<Action<T1>, T1?>.ThenCall(Action<T1> callback) => ThenCall(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTracking<T1?> IMethodTracking<T1?>.Verifiable() => Verifiable();
        IMethodTracking<T1?> IMethodTracking<T1?>.Verifiable(Called called) => Verifiable(called);
        IMethodCallBuilder<Action<T1>, T1?> IMethodCallBuilder<Action<T1>, T1?>.Verifiable() => Verifiable();
        IMethodCallBuilder<Action<T1>, T1?> IMethodCallBuilder<Action<T1>, T1?>.Verifiable(Called called) => Verifiable(called);
    }

    // ========================================================================
    // Inner class: MethodSequence1
    // ========================================================================

    /// <summary>Sequence for void methods. Supports ThenCall chaining.</summary>
    public sealed class MethodSequence1 : IMethodCallSequence<Action<T1>>, IMethodCallSequence, IMethodSequence
    {
        private readonly VoidMethodInterceptor1<T1> _interceptor;

        internal MethodSequence1(VoidMethodInterceptor1<T1> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another callback to the sequence.</summary>
        public MethodSequence1 ThenCall(Action<T1> callback)
        {
            var tracking = new MethodCallBuilder1(_interceptor);
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
        public MethodSequence1 Verifiable()
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
        IMethodCallSequence<Action<T1>> IMethodCallSequence<Action<T1>>.ThenCall(Action<T1> callback) => ThenCall(callback);
        IMethodCallSequence<Action<T1>> IMethodCallSequence<Action<T1>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: VoidWhenBuilder1
    // ========================================================================

    /// <summary>Builder for When matchers on void methods. Captures predicate, awaits Call().</summary>
    public sealed class VoidWhenBuilder1
    {
        private readonly VoidMethodInterceptor1<T1> _interceptor;
        private readonly Func<T1, bool> _predicate;
        private int _matcherIndex = -1;

        internal VoidWhenBuilder1(VoidMethodInterceptor1<T1> interceptor, Func<T1, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicate(_predicate);
            _interceptor._whenChain.Add(matcher);
            _matcherIndex = _interceptor._whenChain.Count - 1;
        }

        public VoidWhenChain1 Call(Action<T1> callback)
        {
            ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex]).SetCallback(callback);
            return new VoidWhenChain1(_interceptor, _matcherIndex);
        }

        public VoidWhenChain1 ThenCall(Action<T1> callback)
        {
            _interceptor._whenChain!.Add(new VoidWhenMatcherCall(callback));
            return new VoidWhenChain1(_interceptor, _matcherIndex);
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
    // Inner class: VoidWhenChain1
    // ========================================================================

    /// <summary>Void When chain with ThenWhen, ThenCall, ThenNone, verification support.</summary>
    public sealed class VoidWhenChain1
    {
        private readonly VoidMethodInterceptor1<T1> _interceptor;
        private readonly int _currentMatcherIndex;

        internal VoidWhenChain1(VoidMethodInterceptor1<T1> interceptor, int currentMatcherIndex)
        {
            _interceptor = interceptor;
            _currentMatcherIndex = currentMatcherIndex;
        }

        /// <summary>Adds another matcher with exact value matching.</summary>
        public VoidWhenBuilder1 ThenWhen(T1 arg1)
        {
            return new VoidWhenBuilder1(_interceptor, (a1) => object.Equals(a1, arg1));
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public VoidWhenBuilder1 ThenWhen(Func<T1, bool> predicate)
        {
            return new VoidWhenBuilder1(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional callback as terminal matcher.</summary>
        public VoidWhenChain1 ThenCall(Action<T1> callback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall(callback));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public VoidWhenChain1 ThenNone()
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
        public VoidWhenChain1 Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
