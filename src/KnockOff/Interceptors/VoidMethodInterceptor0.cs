#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled interceptor for void methods with 0 parameters.
/// Replaces generated interceptor classes by parameterizing on individual argument types.
/// All behavioral logic (Call, When, sequences, verification, builders) is pre-compiled.
/// </summary>
public sealed class VoidMethodInterceptor0
{
    private readonly string _memberName;

    // Callback
    private Action? _call;
    private MethodCallBuilder0? _callTracking;

    // Sequence
    private List<(Action Callback, MethodCallBuilder0 Tracking)>? _sequence;
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

    // Fallback delegates
    private Action? _fallback;
    private Action? _sourceFallback;

    public VoidMethodInterceptor0(string memberName)
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

    // ========================================================================
    // Invoke
    // ========================================================================

    /// <summary>Invokes the configured behavior. Called by generated interface implementation.</summary>
    public void Invoke(bool strict)
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
                matcher.Call();
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
            tracking.RecordCall();
            _sequenceIndex++;
            callback();
            return;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall();
            _call();
            return;
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
                callback();
                return;
            }
            return; // exhausted but no repeat - just return (void)
        }

        // Fallback (stub override)
        if (_fallback != null) { _fallback(); return; }

        // Source fallback
        if (_sourceFallback != null) { _sourceFallback(); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
    }

    // ========================================================================
    // Call / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures callback that repeats indefinitely. Returns builder for sequence chaining.</summary>
    public MethodCallBuilder0 Call(Action callback)
    {
        var builder = new MethodCallBuilder0(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = callback;
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures parameter-specific matching with predicate.</summary>
    public VoidWhenBuilder0 When(Func<bool> predicate)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder0(this, predicate);
    }

    /// <summary>Sets the fallback delegate for stub overrides.</summary>
    public void SetFallback(Action? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation.</summary>
    public void SetSourceFallback(Action? sourceFallback) => _sourceFallback = sourceFallback;

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
        public abstract bool Matches();
        public abstract void Call();
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and optionally invokes a callback.</summary>
    private sealed class VoidWhenMatcherPredicate : VoidWhenMatcherBase
    {
        private readonly Func<bool> _predicate;
        private Action? _callback;

        public VoidWhenMatcherPredicate(Func<bool> predicate) => _predicate = predicate;

        public override bool Matches() => _predicate();
        public override void Call() => _callback?.Invoke();
        public override bool IsTerminal => false;

        public void SetCallback(Action callback) => _callback = callback;
    }

    /// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>
    private sealed class VoidWhenMatcherCall : VoidWhenMatcherBase
    {
        private readonly Action _callback;

        public VoidWhenMatcherCall(Action callback) => _callback = callback;

        public override bool Matches() => true;
        public override void Call() => _callback();
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class VoidWhenMatcherNone : VoidWhenMatcherBase
    {
        public override bool Matches() => false;
        public override void Call() { }
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder0
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder0 : IMethodCallBuilder<Action>
    {
        private readonly VoidMethodInterceptor0 _interceptor;
        internal int _callCount;

        internal MethodCallBuilder0(VoidMethodInterceptor0 interceptor)
        {
            _interceptor = interceptor;
        }

        internal void RecordCall()
        {
            _callCount++;
        }

        /// <summary>Resets tracking state.</summary>
        public void Reset()
        {
            _callCount = 0;
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
        public MethodSequence0 ThenCall(Action callback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder0(_interceptor);
            _interceptor._sequence!.Add((callback, nextBuilder));
            return new MethodSequence0(_interceptor);
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public MethodCallBuilder0 Verifiable()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
            return this;
        }

        /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
        public MethodCallBuilder0 Verifiable(Called times)
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = times;
            return this;
        }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Action Callback, MethodCallBuilder0 Tracking)>();
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
        IMethodCallSequence<Action> IMethodCallBuilder<Action>.ThenCall(Action callback) => ThenCall(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodCallBuilder<Action> IMethodCallBuilder<Action>.Verifiable() => Verifiable();
        IMethodCallBuilder<Action> IMethodCallBuilder<Action>.Verifiable(Called called) => Verifiable(called);
    }

    // ========================================================================
    // Inner class: MethodSequence0
    // ========================================================================

    /// <summary>Sequence for void methods. Supports ThenCall chaining.</summary>
    public sealed class MethodSequence0 : IMethodCallSequence<Action>, IMethodCallSequence, IMethodSequence
    {
        private readonly VoidMethodInterceptor0 _interceptor;

        internal MethodSequence0(VoidMethodInterceptor0 interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another callback to the sequence.</summary>
        public MethodSequence0 ThenCall(Action callback)
        {
            var tracking = new MethodCallBuilder0(_interceptor);
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
        public MethodSequence0 Verifiable()
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
        IMethodCallSequence<Action> IMethodCallSequence<Action>.ThenCall(Action callback) => ThenCall(callback);
        IMethodCallSequence<Action> IMethodCallSequence<Action>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: VoidWhenBuilder0
    // ========================================================================

    /// <summary>Builder for When matchers on void methods. Captures predicate, awaits Call().</summary>
    public sealed class VoidWhenBuilder0
    {
        private readonly VoidMethodInterceptor0 _interceptor;
        private readonly Func<bool> _predicate;
        private int _matcherIndex = -1;

        internal VoidWhenBuilder0(VoidMethodInterceptor0 interceptor, Func<bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
            // Register matcher immediately so Invoke() can track calls before Call() is configured
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicate(_predicate);
            _interceptor._whenChain.Add(matcher);
            _matcherIndex = _interceptor._whenChain.Count - 1;
        }

        /// <summary>Configures the callback for this When match.</summary>
        public VoidWhenChain0 Call(Action callback)
        {
            // Matcher already registered in constructor - just set the callback
            ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex]).SetCallback(callback);
            return new VoidWhenChain0(_interceptor, _matcherIndex);
        }

        /// <summary>Adds a terminal callback matcher to the chain (When-then-ThenCall pattern).</summary>
        public VoidWhenChain0 ThenCall(Action callback)
        {
            // Matcher already registered in constructor - add the terminal callback
            _interceptor._whenChain!.Add(new VoidWhenMatcherCall(callback));
            return new VoidWhenChain0(_interceptor, _matcherIndex);
        }

        /// <summary>Verifies this specific When matcher was called the expected number of times (When-as-tracker pattern).</summary>
        public void Verify(Called times)
        {
            if (_interceptor._whenChain == null || _matcherIndex >= _interceptor._whenChain.Count) return;
            var callCount = _interceptor._whenChain[_matcherIndex].CallCount;
            if (!times.Validate(callCount))
                throw new VerificationException(new VerificationFailure("When matcher", times, callCount));
        }
    }

    // ========================================================================
    // Inner class: VoidWhenChain0
    // ========================================================================

    /// <summary>Void When chain with ThenWhen, ThenCall, ThenNone, verification support.</summary>
    public sealed class VoidWhenChain0
    {
        private readonly VoidMethodInterceptor0 _interceptor;
        private readonly int _currentMatcherIndex;

        internal VoidWhenChain0(VoidMethodInterceptor0 interceptor, int currentMatcherIndex)
        {
            _interceptor = interceptor;
            _currentMatcherIndex = currentMatcherIndex;
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public VoidWhenBuilder0 ThenWhen(Func<bool> predicate)
        {
            return new VoidWhenBuilder0(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional callback as terminal matcher.</summary>
        public VoidWhenChain0 ThenCall(Action callback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall(callback));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public VoidWhenChain0 ThenNone()
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
        public VoidWhenChain0 Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
