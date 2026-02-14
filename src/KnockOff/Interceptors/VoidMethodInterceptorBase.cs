// These base classes use protected fields for inheritance by generated code,
// List<T> for performance-critical tracking, and 'Call' as a domain method name.
#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

using KnockOff;

namespace KnockOff.Interceptors;

/// <summary>
/// Base class for void method interceptors.
/// Holds all shared fields, priority chain logic, verification, and inner class base types.
/// </summary>
/// <typeparam name="TDelegate">The method's delegate type (e.g., Action&lt;string&gt;).</typeparam>
/// <typeparam name="TArgs">The argument type: single type for 1-param, ValueTuple for multi-param, Unit for 0-param.</typeparam>
public abstract class VoidMethodInterceptorBase<TDelegate, TArgs> where TDelegate : Delegate
{
    protected readonly string _memberName;

    // Callback
    protected TDelegate? _call;
    protected MethodCallBuilderBase? _callTracking;

    // Sequence
    protected List<(TDelegate Callback, MethodCallBuilderBase Tracking)>? _sequence;
    protected int _sequenceIndex;
    protected bool _repeatLastValue = true;

    // When chain
    protected List<VoidWhenMatcherBase>? _whenChain;
    protected int _whenChainHead;
    protected bool _whenVerifiable;

    // Verification
    protected bool _isVerifiable;
    protected Called? _verifiableTimes;

    // Unconfigured tracking
    protected int _unconfiguredCallCount;

    protected VoidMethodInterceptorBase(string memberName)
    {
        _memberName = memberName;
    }

    /// <summary>Count of calls not handled by any configured behavior.</summary>
    public int UnconfiguredCallCount => _unconfiguredCallCount;

    /// <summary>Total call count across all configured behaviors and unconfigured calls.</summary>
    protected int TotalCallCount
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

    /// <summary>Whether this interceptor has been configured.</summary>
    public bool IsConfigured => _call != null || (_sequence?.Count ?? 0) > 0 || (_whenChain?.Count ?? 0) > 0;

    /// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>
    public virtual void Reset()
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

    /// <summary>
    /// Runs the void priority chain: When chain, sequence, callback.
    /// Returns true if something handled the call.
    /// </summary>
    protected bool RunVoidPriorityChain(TArgs args)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(args))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                {
                    _whenChainHead++;
                }
                matcher.Call(args);
                return true;
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
            tracking.RecordCallBase();
            RecordArgs(args, tracking);
            _sequenceIndex++;
            InvokeVoidDelegate(callback, args);
            return true;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCallBase();
            RecordArgs(args, _callTracking);
            InvokeVoidDelegate(_call, args);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles the unconfigured tail: sequence exhaustion repeat.
    /// Returns true if sequence repeat handled the call.
    /// </summary>
    protected bool HandleSequenceExhaustedRepeat(bool strict, TArgs args)
    {
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCallBase();
                RecordArgs(args, tracking);
                InvokeVoidDelegate(callback, args);
                return true;
            }
            return true; // exhausted but no repeat - just return (void)
        }
        return false;
    }

    /// <summary>Resets callback fields and sets up a new callback with its builder.</summary>
    protected void SetupCallback(TDelegate callback, MethodCallBuilderBase builder)
    {
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = callback;
        _callTracking = builder;
    }

    /// <summary>
    /// Searches tracking objects (callback, then sequence in reverse) for the last one with calls,
    /// casts it to TBuilder, and extracts a value using the selector.
    /// </summary>
    protected TResult? FindLastArgInTracking<TBuilder, TResult>(Func<TBuilder, TResult> selector)
        where TBuilder : class
    {
        if (_callTracking?._callCount > 0 && _callTracking is TBuilder ct)
            return selector(ct);
        if (_sequence != null)
            for (int i = _sequence.Count - 1; i >= 0; i--)
                if (_sequence[i].Tracking._callCount > 0 && _sequence[i].Tracking is TBuilder st)
                    return selector(st);
        return default;
    }

    /// <summary>Invokes the delegate with the given args. Must be overridden to unpack TArgs.</summary>
    protected abstract void InvokeVoidDelegate(TDelegate del, TArgs args);

    /// <summary>Records args on the tracking builder. Must be overridden to set typed LastArg/LastArgs.</summary>
    protected abstract void RecordArgs(TArgs args, MethodCallBuilderBase tracking);

    /// <summary>Records unconfigured args. Must be overridden to set typed unconfigured last arg.</summary>
    protected abstract void RecordUnconfiguredArgs(TArgs args);

    // ========================================================================
    // Inner class: VoidWhenMatcherBase
    // ========================================================================

    /// <summary>Abstract base for void When chain matchers.</summary>
    public abstract class VoidWhenMatcherBase
    {
        public abstract bool Matches(TArgs args);
        public abstract void Call(TArgs args);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    /// <summary>Matcher that uses a predicate and optionally invokes a callback.</summary>
    public class VoidWhenMatcherPredicateBase : VoidWhenMatcherBase
    {
        private readonly Func<TArgs, bool> _predicate;
        private Action<TArgs>? _callback;

        public VoidWhenMatcherPredicateBase(Func<TArgs, bool> predicate) => _predicate = predicate;

        public override bool Matches(TArgs args) => _predicate(args);
        public override void Call(TArgs args) => _callback?.Invoke(args);
        public override bool IsTerminal => false;

        public void SetCallback(Action<TArgs> callback) => _callback = callback;
    }

    /// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>
    public class VoidWhenMatcherCallBase : VoidWhenMatcherBase
    {
        private readonly Action<TArgs> _callback;

        public VoidWhenMatcherCallBase(Action<TArgs> callback) => _callback = callback;

        public override bool Matches(TArgs args) => true;
        public override void Call(TArgs args) => _callback(args);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    public class VoidWhenMatcherNoneBase : VoidWhenMatcherBase
    {
        public override bool Matches(TArgs args) => false;
        public override void Call(TArgs args) { }
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: VoidWhenChainBase
    // ========================================================================

    /// <summary>Void When chain with Call, ThenWhen, ThenCall, ThenNone, verification.</summary>
    public class VoidWhenChainBase
    {
        protected readonly VoidMethodInterceptorBase<TDelegate, TArgs> _interceptor;
        protected readonly VoidWhenMatcherBase _currentMatcher;

        public VoidWhenChainBase(VoidMethodInterceptorBase<TDelegate, TArgs> interceptor, VoidWhenMatcherBase currentMatcher)
        {
            _interceptor = interceptor;
            _currentMatcher = currentMatcher;
        }

        /// <summary>Adds a predicate-based matcher and returns a new chain segment.</summary>
        public VoidWhenChainBase ThenWhenBase(Func<TArgs, bool> predicate)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicateBase(predicate);
            _interceptor._whenChain.Add(matcher);
            return new VoidWhenChainBase(_interceptor, matcher);
        }

        /// <summary>Adds an unconditional callback as terminal matcher.</summary>
        public VoidWhenChainBase ThenCallBase(Action<TArgs> callback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCallBase(callback));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public VoidWhenChainBase ThenNone()
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherNoneBase());
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
            if (!times.Validate(_currentMatcher.CallCount))
            {
                throw new VerificationException(new VerificationFailure("When matcher", times, _currentMatcher.CallCount));
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
        public VoidWhenChainBase VerifiableBase()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }

    // ========================================================================
    // Inner class: MethodCallBuilderBase
    // ========================================================================

    /// <summary>Base class for method call builders. Holds call count, verification, sequence elevation.</summary>
    public class MethodCallBuilderBase
    {
        protected readonly VoidMethodInterceptorBase<TDelegate, TArgs> _interceptor;

        public MethodCallBuilderBase(VoidMethodInterceptorBase<TDelegate, TArgs> interceptor)
        {
            _interceptor = interceptor;
        }

        public int _callCount;

        /// <summary>Increments the call count.</summary>
        public void RecordCallBase() => _callCount++;

        /// <summary>Resets tracking state.</summary>
        public virtual void Reset() => _callCount = 0;

        /// <summary>Verifies callback was invoked at least once.</summary>
        public void Verify() => Verify(Called.AtLeastOnce);

        /// <summary>Verifies call count satisfies the Called constraint.</summary>
        public void Verify(Called times)
        {
            if (!times.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("method", times, _callCount));
        }

        /// <summary>Elevates to sequence mode and adds another callback.</summary>
        protected MethodSequenceBase ThenCallBase(TDelegate callback)
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(TDelegate Callback, MethodCallBuilderBase Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
            var nextBuilder = CreateNextBuilder();
            _interceptor._sequence.Add((callback, nextBuilder));
            return new MethodSequenceBase(_interceptor, CreateNextBuilder);
        }

        /// <summary>Creates a new builder instance for sequence entries. Override to return typed builder.</summary>
        protected virtual MethodCallBuilderBase CreateNextBuilder()
        {
            return new MethodCallBuilderBase(_interceptor);
        }

        /// <summary>Marks for verification by Stub.Verify().</summary>
        public void VerifiableBase()
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = null;
        }

        /// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>
        public void VerifiableBase(Called times)
        {
            _interceptor._isVerifiable = true;
            _interceptor._verifiableTimes = times;
        }
    }

    // ========================================================================
    // Inner class: MethodSequenceBase
    // ========================================================================

    /// <summary>Sequence for void method callbacks. Implements IMethodCallSequence directly.</summary>
    public class MethodSequenceBase : IMethodCallSequence<TDelegate>, IMethodCallSequence, IMethodSequence
    {
        protected readonly VoidMethodInterceptorBase<TDelegate, TArgs> _interceptor;
        private readonly Func<MethodCallBuilderBase>? _builderFactory;

        public MethodSequenceBase(VoidMethodInterceptorBase<TDelegate, TArgs> interceptor)
        {
            _interceptor = interceptor;
        }

        public MethodSequenceBase(VoidMethodInterceptorBase<TDelegate, TArgs> interceptor, Func<MethodCallBuilderBase> builderFactory)
        {
            _interceptor = interceptor;
            _builderFactory = builderFactory;
        }

        protected int SequenceTotalCallCount
        {
            get
            {
                if (_interceptor._sequence == null) return 0;
                var total = 0;
                foreach (var (_, tracking) in _interceptor._sequence)
                    total += tracking._callCount;
                return total;
            }
        }

        /// <summary>Adds another callback to the sequence.</summary>
        public MethodSequenceBase ThenCall(TDelegate callback)
        {
            var tracking = _builderFactory != null ? _builderFactory() : CreateNextBuilder();
            _interceptor._sequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Adds a callback+tracking pair to the sequence. For use by subclasses in other assemblies.</summary>
        protected void AddToSequence(TDelegate callback, MethodCallBuilderBase tracking)
        {
            _interceptor._sequence!.Add((callback, tracking));
        }

        /// <summary>Creates a new builder for sequence entries.</summary>
        protected virtual MethodCallBuilderBase CreateNextBuilder()
        {
            return new MethodCallBuilderBase(_interceptor);
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
        public MethodSequenceBase Verifiable()
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

        // Explicit interface implementations for covariant return types
        IMethodCallSequence<TDelegate> IMethodCallSequence<TDelegate>.ThenCall(TDelegate callback) => ThenCall(callback);
        IMethodCallSequence<TDelegate> IMethodCallSequence<TDelegate>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }
}
