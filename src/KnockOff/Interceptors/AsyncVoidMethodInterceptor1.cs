#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for void methods with exactly 1 parameter.
/// TDelegate is a generated delegate type providing named callback parameters.
/// TArg is the raw parameter type.
/// Handles Task and ValueTask interface methods (no inner return type).
/// All behavioral logic (Call, When, sequences, verification, builders) is pre-compiled.
/// Expression trees bridge between TDelegate invocation and TArg matching.
/// </summary>
/// <typeparam name="TDelegate">The generated delegate type for async void callbacks (returns Task).</typeparam>
/// <typeparam name="TSyncDelegate">The generated delegate type for simplified sync callbacks (returns void).</typeparam>
/// <typeparam name="TArg">The single argument type.</typeparam>
public sealed class AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> : IInterceptor
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    // Static expression tree invoker -- compiled once per closed generic type combo
    // Bridges TDelegate invocation: (del, arg) => del(arg) : Task
    private static readonly Func<TDelegate, TArg, Task> s_asyncVoidInvoker
        = DelegateInvokerFactory.BuildAsyncVoidInvoker<TDelegate, TArg>();

    // Static sync void invoker for TSyncDelegate callback bridging
    private static readonly Action<TSyncDelegate, TArg> s_syncVoidInvoker
        = DelegateInvokerFactory.BuildVoidInvoker<TSyncDelegate, TArg>();

    private readonly string _memberName;

    // Callback (stored as converted Func<TArg, Task>)
    private Func<TArg, Task>? _call;
    private MethodCallBuilder? _callTracking;

    // Sequence (stored as converted Func<TArg, Task>)
    private List<(Func<TArg, Task> Callback, MethodCallBuilder Tracking)>? _sequence;
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
    private TArg? _unconfiguredLastArg;

    // Fallback delegates (stored as raw TDelegate, invoked via s_asyncVoidInvoker at call time)
    private TDelegate? _fallback;
    private TDelegate? _sourceFallback;

    public AsyncVoidMethodInterceptor1(string memberName)
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
    public TArg? LastArg
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
    public async Task Invoke(bool strict, TArg arg)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(arg))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                await matcher.Call(arg).ConfigureAwait(false);
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
            tracking.RecordCall(arg);
            _sequenceIndex++;
            await callback(arg).ConfigureAwait(false);
            return;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg);
            await _call(arg).ConfigureAwait(false);
            return;
        }

        // Nothing handled - unconfigured path
        _unconfiguredCallCount++;
        _unconfiguredLastArg = arg;

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(arg);
                await callback(arg).ConfigureAwait(false);
                return;
            }
            return;
        }

        // Fallback (stub override) -- stored as raw TDelegate, invoked via expression tree
        if (_fallback != null) { await s_asyncVoidInvoker(_fallback, arg).ConfigureAwait(false); return; }

        // Source fallback
        if (_sourceFallback != null) { await s_asyncVoidInvoker(_sourceFallback, arg).ConfigureAwait(false); return; }

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);
    }

    // ========================================================================
    // Call / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures async callback using TDelegate. Converts to internal Func via expression tree.</summary>
    public MethodCallBuilder Call(TDelegate asyncCallback)
    {
        var builder = new MethodCallBuilder(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = (arg) => s_asyncVoidInvoker(asyncCallback, arg);
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures simplified sync callback via TSyncDelegate. Wraps in Task.CompletedTask pattern.</summary>
    public MethodCallBuilder Call(TSyncDelegate syncCallback)
    {
        var builder = new MethodCallBuilder(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = (arg) => { s_syncVoidInvoker(syncCallback, arg); return Task.CompletedTask; };
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures parameter-specific matching with exact value.</summary>
    public VoidWhenBuilder When(TArg arg)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder(this, (a) => object.Equals(a, arg));
    }

    /// <summary>Configures parameter-specific matching with predicate.</summary>
    public VoidWhenBuilder When(Func<TArg, bool> predicate)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder(this, predicate);
    }

    /// <summary>Sets the fallback delegate for stub overrides. Stored as raw TDelegate.</summary>
    public void SetFallback(TDelegate? fallback) => _fallback = fallback;

    /// <summary>Sets the source fallback delegate for source delegation. Stored as raw TDelegate.</summary>
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
    public void Verifiable() { _isVerifiable = true; _verifiableTimes = null; }

    /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
    public void Verifiable(Called times) { _isVerifiable = true; _verifiableTimes = times; }

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
            foreach (var (_, tracking) in _sequence) tracking.Reset();
        _sequenceIndex = 0;
        _whenChainHead = 0;
        if (_whenChain != null)
            foreach (var matcher in _whenChain) matcher.CallCount = 0;
    }

    // ========================================================================
    // Inner class: VoidWhenMatcherBase (abstract)
    // ========================================================================

    private abstract class VoidWhenMatcherBase
    {
        public abstract bool Matches(TArg arg);
        public abstract Task Call(TArg arg);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and optionally invokes a callback.</summary>
    private sealed class VoidWhenMatcherPredicate : VoidWhenMatcherBase
    {
        private readonly Func<TArg, bool> _predicate;
        private Func<TArg, Task>? _callback;

        public VoidWhenMatcherPredicate(Func<TArg, bool> predicate) => _predicate = predicate;

        public override bool Matches(TArg arg) => _predicate(arg);
        public override Task Call(TArg arg) => _callback?.Invoke(arg) ?? Task.CompletedTask;
        public override bool IsTerminal => false;

        public void SetCallback(Func<TArg, Task> callback) => _callback = callback;
    }

    /// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>
    private sealed class VoidWhenMatcherCall : VoidWhenMatcherBase
    {
        private readonly Func<TArg, Task> _callback;

        public VoidWhenMatcherCall(Func<TArg, Task> callback) => _callback = callback;

        public override bool Matches(TArg arg) => true;
        public override Task Call(TArg arg) => _callback(arg);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class VoidWhenMatcherNone : VoidWhenMatcherBase
    {
        public override bool Matches(TArg arg) => false;
        public override Task Call(TArg arg) => Task.CompletedTask;
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder : IMethodCallBuilder<TDelegate, TArg?>
    {
        private readonly AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> _interceptor;
        internal int _callCount;
        private TArg? _lastArg;

        internal MethodCallBuilder(AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Last argument passed to this callback.</summary>
        public TArg? LastArg => _lastArg;

        internal void RecordCall(TArg arg)
        {
            _callCount++;
            _lastArg = arg;
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

        /// <summary>Elevates to sequence and adds async TDelegate callback.</summary>
        public MethodSequence ThenCall(TDelegate asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add(((arg) => s_asyncVoidInvoker(asyncCallback, arg), nextBuilder));
            return new MethodSequence(_interceptor);
        }

        /// <summary>Elevates to sequence and adds simplified sync callback via TSyncDelegate.</summary>
        public MethodSequence ThenCall(TSyncDelegate syncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder(_interceptor);
            Func<TArg, Task> wrapped = (arg) => { s_syncVoidInvoker(syncCallback, arg); return Task.CompletedTask; };
            _interceptor._sequence!.Add((wrapped, nextBuilder));
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
                _interceptor._sequence = new List<(Func<TArg, Task> Callback, MethodCallBuilder Tracking)>();
                if (_interceptor._call != null)
                    _interceptor._sequence.Add((_interceptor._call, this));
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        // Explicit interface implementations
        IMethodCallSequence<TDelegate> IMethodCallBuilder<TDelegate, TArg?>.ThenCall(TDelegate callback) => ThenCall(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTracking<TArg?> IMethodTracking<TArg?>.Verifiable() => Verifiable();
        IMethodTracking<TArg?> IMethodTracking<TArg?>.Verifiable(Called called) => Verifiable(called);
        IMethodCallBuilder<TDelegate, TArg?> IMethodCallBuilder<TDelegate, TArg?>.Verifiable() => Verifiable();
        IMethodCallBuilder<TDelegate, TArg?> IMethodCallBuilder<TDelegate, TArg?>.Verifiable(Called called) => Verifiable(called);
        TArg? IMethodTracking<TArg?>.LastArg => _lastArg;
    }

    // ========================================================================
    // Inner class: MethodSequence
    // ========================================================================

    /// <summary>Sequence for async void methods. Supports ThenCall chaining.</summary>
    public sealed class MethodSequence : IMethodCallSequence<TDelegate>, IMethodCallSequence, IMethodSequence
    {
        private readonly AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> _interceptor;

        internal MethodSequence(AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds async TDelegate callback to the sequence.</summary>
        public MethodSequence ThenCall(TDelegate asyncCallback)
        {
            var tracking = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add(((arg) => s_asyncVoidInvoker(asyncCallback, arg), tracking));
            return this;
        }

        /// <summary>Adds simplified sync callback via TSyncDelegate to the sequence.</summary>
        public MethodSequence ThenCall(TSyncDelegate syncCallback)
        {
            var tracking = new MethodCallBuilder(_interceptor);
            Func<TArg, Task> wrapped = (arg) => { s_syncVoidInvoker(syncCallback, arg); return Task.CompletedTask; };
            _interceptor._sequence!.Add((wrapped, tracking));
            return this;
        }

        /// <summary>Verifies the entire sequence was executed.</summary>
        public void Verify()
        {
            if (_interceptor._sequence == null) return;
            if (_interceptor._sequenceIndex < _interceptor._sequence.Count)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("method", _interceptor._sequence.Count, _interceptor._sequenceIndex));
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
        public void ThenDefault() => _interceptor._repeatLastValue = false;

        // Explicit interface implementations
        IMethodCallSequence<TDelegate> IMethodCallSequence<TDelegate>.ThenCall(TDelegate callback) => ThenCall(callback);
        IMethodCallSequence<TDelegate> IMethodCallSequence<TDelegate>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: VoidWhenBuilder
    // ========================================================================

    /// <summary>Builder for When matchers on async void methods. Captures predicate, awaits Call().</summary>
    public sealed class VoidWhenBuilder
    {
        private readonly AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> _interceptor;
        private readonly Func<TArg, bool> _predicate;
        private int _matcherIndex = -1;

        internal VoidWhenBuilder(AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> interceptor, Func<TArg, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicate(_predicate);
            _interceptor._whenChain.Add(matcher);
            _matcherIndex = _interceptor._whenChain.Count - 1;
        }

        /// <summary>Configures async TDelegate callback for this When match.</summary>
        public VoidWhenChain Call(TDelegate asyncCallback)
        {
            ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex])
                .SetCallback((arg) => s_asyncVoidInvoker(asyncCallback, arg));
            return new VoidWhenChain(_interceptor, _matcherIndex);
        }

        /// <summary>Configures simplified sync callback via TSyncDelegate for this When match.</summary>
        public VoidWhenChain Call(TSyncDelegate syncCallback)
        {
            ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex])
                .SetCallback((arg) => { s_syncVoidInvoker(syncCallback, arg); return Task.CompletedTask; });
            return new VoidWhenChain(_interceptor, _matcherIndex);
        }

        /// <summary>Adds an unconditional async TDelegate callback as terminal matcher.</summary>
        public VoidWhenChain ThenCall(TDelegate asyncCallback)
        {
            _interceptor._whenChain!.Add(new VoidWhenMatcherCall((arg) => s_asyncVoidInvoker(asyncCallback, arg)));
            return new VoidWhenChain(_interceptor, _matcherIndex);
        }

        /// <summary>Adds an unconditional sync callback via TSyncDelegate as terminal matcher.</summary>
        public VoidWhenChain ThenCall(TSyncDelegate syncCallback)
        {
            _interceptor._whenChain!.Add(new VoidWhenMatcherCall((arg) => { s_syncVoidInvoker(syncCallback, arg); return Task.CompletedTask; }));
            return new VoidWhenChain(_interceptor, _matcherIndex);
        }

        /// <summary>Verifies this matcher was called the expected number of times.</summary>
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
        private readonly AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> _interceptor;
        private readonly int _currentMatcherIndex;

        internal VoidWhenChain(AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg> interceptor, int currentMatcherIndex)
        {
            _interceptor = interceptor;
            _currentMatcherIndex = currentMatcherIndex;
        }

        /// <summary>Adds another matcher with exact value matching.</summary>
        public VoidWhenBuilder ThenWhen(TArg arg)
        {
            return new VoidWhenBuilder(_interceptor, (a) => object.Equals(a, arg));
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public VoidWhenBuilder ThenWhen(Func<TArg, bool> predicate)
        {
            return new VoidWhenBuilder(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional async TDelegate callback as terminal matcher.</summary>
        public VoidWhenChain ThenCall(TDelegate asyncCallback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall((arg) => s_asyncVoidInvoker(asyncCallback, arg)));
            return this;
        }

        /// <summary>Adds an unconditional sync callback via TSyncDelegate as terminal matcher.</summary>
        public VoidWhenChain ThenCall(TSyncDelegate syncCallback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall((arg) => { s_syncVoidInvoker(syncCallback, arg); return Task.CompletedTask; }));
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
                throw new VerificationException(VerificationFailure.SequenceIncomplete("When chain", count, head));
        }

        /// <summary>Verifies this specific matcher was called the expected number of times.</summary>
        public void Verify(Called times)
        {
            if (_interceptor._whenChain == null || _currentMatcherIndex >= _interceptor._whenChain.Count) return;
            var callCount = _interceptor._whenChain[_currentMatcherIndex].CallCount;
            if (!times.Validate(callCount))
                throw new VerificationException(new VerificationFailure("When matcher", times, callCount));
        }

        /// <summary>Resets When chain HEAD and all matcher call counts.</summary>
        public void Reset()
        {
            _interceptor._whenChainHead = 0;
            if (_interceptor._whenChain != null)
                foreach (var matcher in _interceptor._whenChain) matcher.CallCount = 0;
        }

        /// <summary>Marks this When chain for verification by Stub.Verify().</summary>
        public VoidWhenChain Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
