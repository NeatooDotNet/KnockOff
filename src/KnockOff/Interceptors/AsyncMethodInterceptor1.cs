#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords
#pragma warning disable CA1030 // Use events where appropriate

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for non-void methods with exactly 1 parameter.
/// TDelegate is a generated delegate type providing named callback parameters.
/// TArg is the raw parameter type.
/// TReturn is the inner return type (e.g., int for Task&lt;int&gt;).
/// All behavioral logic (Return, When, sequences, verification, builders) is pre-compiled.
/// Expression trees bridge between TDelegate invocation and TArg matching.
/// </summary>
/// <typeparam name="TDelegate">The generated delegate type for async callbacks (returns Task&lt;TReturn&gt;).</typeparam>
/// <typeparam name="TSyncDelegate">The generated delegate type for simplified sync callbacks (returns TReturn).</typeparam>
/// <typeparam name="TArg">The single argument type.</typeparam>
/// <typeparam name="TReturn">The inner return type (e.g., int for Task&lt;int&gt;).</typeparam>
public sealed class AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> : IInterceptor
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    // Static expression tree invoker -- compiled once per closed generic type combo
    // Bridges TDelegate invocation: (del, arg) => del(arg) : Task<TReturn>
    private static readonly Func<TDelegate, TArg, Task<TReturn>> s_asyncInvoker
        = DelegateInvokerFactory.BuildAsyncInvoker<TDelegate, TArg, TReturn>();

    // Static sync invoker for TSyncDelegate callback bridging
    private static readonly Func<TSyncDelegate, TArg, TReturn> s_syncInvoker
        = DelegateInvokerFactory.BuildInvoker<TSyncDelegate, TArg, TReturn>();

    // Static sync value delegate factory for ThenReturn(TReturn value) routing
    private static readonly Func<TReturn, TSyncDelegate> s_syncValueDelegate
        = DelegateInvokerFactory.BuildValueDelegate<TSyncDelegate, TReturn>();

    private readonly string _memberName;

    // Callback (stored as converted Func<TArg, Task<TReturn>>)
    private Func<TArg, Task<TReturn>>? _call;
    private MethodCallBuilder? _callTracking;

    // Return value
    private TReturn _returnValue = default!;
    private bool _hasReturnValue;
    private MethodCallBuilder? _returnValueTracking;

    // Sequence (stored as converted Func<TArg, Task<TReturn>>)
    private List<(Func<TArg, Task<TReturn>> Callback, MethodCallBuilder Tracking)>? _sequence;
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
    private TArg? _unconfiguredLastArg;

    // Fallback delegates (stored as raw TDelegate, invoked via s_asyncInvoker at call time)
    private TDelegate? _fallback;
    private TDelegate? _sourceFallback;

    // Smart default factory (for NewInstance/ThrowException strategies)
    private readonly Func<TReturn>? _defaultFactory;

    public AsyncMethodInterceptor1(string memberName)
    {
        _memberName = memberName;
    }

    /// <summary>Constructor with smart default factory for non-strict unconfigured calls.</summary>
    public AsyncMethodInterceptor1(string memberName, Func<TReturn> defaultFactory)
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

    /// <summary>Last argument from the most recently called registration.</summary>
    public TArg? LastArg
    {
        get
        {
            if ((_returnValueTracking?._callCount ?? 0) > 0)
                return _returnValueTracking!.LastArg;
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
    public async Task<TReturn> Invoke(bool strict, TArg arg)
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
                return await matcher.CallReturn(arg).ConfigureAwait(false);
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
            return await callback(arg).ConfigureAwait(false);
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall(arg);
            return _returnValue;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg);
            return await _call(arg).ConfigureAwait(false);
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
                return await callback(arg).ConfigureAwait(false);
            }
            return default!;
        }

        // Fallback (stub override) -- stored as raw TDelegate, invoked via expression tree
        if (_fallback != null) return await s_asyncInvoker(_fallback, arg).ConfigureAwait(false);

        // Source fallback
        if (_sourceFallback != null) return await s_asyncInvoker(_sourceFallback, arg).ConfigureAwait(false);

        // Strict mode
        if (strict) throw StubException.NotConfigured("", _memberName);

        // Smart default (NewInstance or ThrowException)
        if (_defaultFactory != null) return _defaultFactory();
        return default!;
    }

    // ========================================================================
    // Return / When / Verify / Reset
    // ========================================================================

    /// <summary>Configures async callback using TDelegate. Converts to internal Func via expression tree.</summary>
    public MethodCallBuilder Return(TDelegate asyncCallback)
    {
        var builder = new MethodCallBuilder(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = (arg) => s_asyncInvoker(asyncCallback, arg);
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures simplified sync callback via TSyncDelegate. Wraps result in Task.FromResult.</summary>
    public MethodCallBuilder Return(TSyncDelegate syncCallback)
    {
        var builder = new MethodCallBuilder(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = (arg) => Task.FromResult(s_syncInvoker(syncCallback, arg));
        _callTracking = builder;
        return builder;
    }

    /// <summary>Configures return value that repeats indefinitely.</summary>
    public MethodCallBuilder Return(TReturn value)
    {
        var builder = new MethodCallBuilder(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
        return builder;
    }

    /// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
    public MethodSequence Return(TReturn first, params TReturn[] rest)
    {
        var builder = Return(first);
        if (rest.Length == 0) return builder.ThenReturn(first);
        var seq = builder.ThenReturn(rest[0]);
        for (int i = 1; i < rest.Length; i++) seq.ThenReturn(rest[i]);
        return seq;
    }

    /// <summary>Configures parameter-specific matching with exact value.</summary>
    public WhenBuilder When(TArg arg)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder(this, (a) => object.Equals(a, arg));
    }

    /// <summary>Configures parameter-specific matching with predicate.</summary>
    public WhenBuilder When(Func<TArg, bool> predicate)
    {
        _whenChain ??= new List<WhenMatcherBase>();
        return new WhenBuilder(this, predicate);
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
        _returnValueTracking?.Reset();
        if (_sequence != null)
            foreach (var (_, tracking) in _sequence) tracking.Reset();
        _sequenceIndex = 0;
        _whenChainHead = 0;
        if (_whenChain != null)
            foreach (var matcher in _whenChain) matcher.CallCount = 0;
    }

    // ========================================================================
    // Inner class: WhenMatcherBase (abstract)
    // ========================================================================

    private abstract class WhenMatcherBase
    {
        public abstract bool Matches(TArg arg);
        public abstract Task<TReturn> CallReturn(TArg arg);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and returns a stored value.</summary>
    private sealed class WhenMatcherValue : WhenMatcherBase
    {
        private readonly Func<TArg, bool> _predicate;
        private readonly TReturn _value;

        public WhenMatcherValue(Func<TArg, bool> predicate, TReturn value)
        {
            _predicate = predicate;
            _value = value;
        }

        public override bool Matches(TArg arg) => _predicate(arg);
        public override Task<TReturn> CallReturn(TArg arg) => Task.FromResult(_value);
        public override bool IsTerminal => false;
    }

    /// <summary>Matcher that always matches and invokes a stored callback. Terminal.</summary>
    private sealed class WhenMatcherCall : WhenMatcherBase
    {
        private readonly Func<TArg, Task<TReturn>> _callback;

        public WhenMatcherCall(Func<TArg, Task<TReturn>> callback) => _callback = callback;

        public override bool Matches(TArg arg) => true;
        public override Task<TReturn> CallReturn(TArg arg) => _callback(arg);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    private sealed class WhenMatcherNone : WhenMatcherBase
    {
        public override bool Matches(TArg arg) => false;
        public override Task<TReturn> CallReturn(TArg arg) => Task.FromResult(default(TReturn)!);
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: MethodCallBuilder
    // ========================================================================

    /// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>
    public sealed class MethodCallBuilder : IMethodReturnBuilder<TDelegate, TArg?>
    {
        private readonly AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> _interceptor;
        internal int _callCount;
        private TArg? _lastArg;

        internal MethodCallBuilder(AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> interceptor)
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
        public MethodSequence ThenReturn(TDelegate asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add(((arg) => s_asyncInvoker(asyncCallback, arg), nextBuilder));
            return new MethodSequence(_interceptor);
        }

        /// <summary>Elevates to sequence and adds simplified sync callback via TSyncDelegate.</summary>
        public MethodSequence ThenReturn(TSyncDelegate syncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add(((arg) => Task.FromResult(s_syncInvoker(syncCallback, arg)), nextBuilder));
            return new MethodSequence(_interceptor);
        }

        /// <summary>Elevates to sequence and adds a value.</summary>
        public MethodSequence ThenReturn(TReturn value)
        {
            return ThenReturn(s_syncValueDelegate(value));
        }

        /// <summary>Adds multiple values to the sequence.</summary>
        public MethodSequence ThenReturn(params TReturn[] values)
        {
            if (values.Length == 0)
            {
                ElevateToSequence();
                return new MethodSequence(_interceptor);
            }
            var seq = ThenReturn(values[0]);
            for (int i = 1; i < values.Length; i++)
                seq.ThenReturn(values[i]);
            return seq;
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
                _interceptor._sequence = new List<(Func<TArg, Task<TReturn>> Callback, MethodCallBuilder Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add(((_) => Task.FromResult(capturedValue), this));
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
        IMethodReturnSequence<TDelegate> IMethodReturnBuilder<TDelegate, TArg?>.ThenReturn(TDelegate callback) => ThenReturn(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTracking<TArg?> IMethodTracking<TArg?>.Verifiable() => Verifiable();
        IMethodTracking<TArg?> IMethodTracking<TArg?>.Verifiable(Called called) => Verifiable(called);
        IMethodReturnBuilder<TDelegate, TArg?> IMethodReturnBuilder<TDelegate, TArg?>.Verifiable() => Verifiable();
        IMethodReturnBuilder<TDelegate, TArg?> IMethodReturnBuilder<TDelegate, TArg?>.Verifiable(Called called) => Verifiable(called);
        TArg? IMethodTracking<TArg?>.LastArg => _lastArg;
    }

    // ========================================================================
    // Inner class: MethodSequence
    // ========================================================================

    /// <summary>Sequence for async non-void methods. Supports ThenReturn chaining.</summary>
    public sealed class MethodSequence : IMethodReturnSequence<TDelegate>, IMethodReturnSequence, IMethodSequence
    {
        private readonly AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> _interceptor;

        internal MethodSequence(AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds async TDelegate callback to the sequence.</summary>
        public MethodSequence ThenReturn(TDelegate asyncCallback)
        {
            var tracking = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add(((arg) => s_asyncInvoker(asyncCallback, arg), tracking));
            return this;
        }

        /// <summary>Adds simplified sync callback via TSyncDelegate to the sequence.</summary>
        public MethodSequence ThenReturn(TSyncDelegate syncCallback)
        {
            var tracking = new MethodCallBuilder(_interceptor);
            _interceptor._sequence!.Add(((arg) => Task.FromResult(s_syncInvoker(syncCallback, arg)), tracking));
            return this;
        }

        /// <summary>Adds a value to the sequence.</summary>
        public MethodSequence ThenReturn(TReturn value)
        {
            return ThenReturn(s_syncValueDelegate(value));
        }

        /// <summary>Adds multiple values to the sequence.</summary>
        public MethodSequence ThenReturn(params TReturn[] values)
        {
            foreach (var v in values) ThenReturn(v);
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
        IMethodReturnSequence<TDelegate> IMethodReturnSequence<TDelegate>.ThenReturn(TDelegate callback) => ThenReturn(callback);
        IMethodReturnSequence<TDelegate> IMethodReturnSequence<TDelegate>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    // ========================================================================
    // Inner class: WhenBuilder
    // ========================================================================

    /// <summary>Builder for When matchers. Captures predicate, awaits Return(value).</summary>
    public sealed class WhenBuilder
    {
        private readonly AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> _interceptor;
        private readonly Func<TArg, bool> _predicate;

        internal WhenBuilder(AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> interceptor, Func<TArg, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
        }

        /// <summary>Configures the return value for this When match.</summary>
        public WhenChain Return(TReturn value)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));
            return new WhenChain(_interceptor);
        }
    }

    // ========================================================================
    // Inner class: WhenChain
    // ========================================================================

    /// <summary>When chain with ThenWhen, ThenCall, ThenNone, verification support.</summary>
    public sealed class WhenChain
    {
        private readonly AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> _interceptor;

        internal WhenChain(AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another matcher with exact value matching.</summary>
        public WhenBuilder ThenWhen(TArg arg)
        {
            return new WhenBuilder(_interceptor, (a) => object.Equals(a, arg));
        }

        /// <summary>Adds another matcher with predicate matching.</summary>
        public WhenBuilder ThenWhen(Func<TArg, bool> predicate)
        {
            return new WhenBuilder(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional async TDelegate callback as terminal matcher.</summary>
        public WhenChain ThenCall(TDelegate asyncCallback)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCall((arg) => s_asyncInvoker(asyncCallback, arg)));
            return this;
        }

        /// <summary>Adds an unconditional sync callback via TSyncDelegate as terminal matcher.</summary>
        public WhenChain ThenCall(TSyncDelegate syncCallback)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCall((arg) => Task.FromResult(s_syncInvoker(syncCallback, arg))));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public WhenChain ThenNone()
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
                throw new VerificationException(VerificationFailure.SequenceIncomplete("When chain", count, head));
        }

        /// <summary>Resets When chain HEAD and all matcher call counts.</summary>
        public void Reset()
        {
            _interceptor._whenChainHead = 0;
            if (_interceptor._whenChain != null)
                foreach (var matcher in _interceptor._whenChain) matcher.CallCount = 0;
        }

        /// <summary>Marks this When chain for verification by Stub.Verify().</summary>
        public WhenChain Verifiable()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }
}
