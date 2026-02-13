#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

using KnockOff;

namespace KnockOff.Interceptors;

/// <summary>
/// Base class for non-void method interceptors.
/// Adds return value support to VoidMethodInterceptorBase.
/// </summary>
/// <typeparam name="TDelegate">The method's delegate type.</typeparam>
/// <typeparam name="TArgs">The argument type.</typeparam>
/// <typeparam name="TReturn">The return type.</typeparam>
public abstract class MethodInterceptorBase<TDelegate, TArgs, TReturn> : VoidMethodInterceptorBase<TDelegate, TArgs>
    where TDelegate : Delegate
{
    // Return value fields
    protected TReturn _returnValue = default!;
    protected bool _hasReturnValue;
    protected ReturnMethodCallBuilderBase? _returnValueTracking;

    protected MethodInterceptorBase(string memberName) : base(memberName) { }

    /// <summary>Whether this interceptor has been configured (includes return value).</summary>
    public new bool IsConfigured => _hasReturnValue || base.IsConfigured;

    /// <summary>Total call count including return value tracking.</summary>
    protected new int TotalCallCount
    {
        get
        {
            var sum = _unconfiguredCallCount + (_callTracking?._callCount ?? 0) + (_returnValueTracking?._callCount ?? 0);
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
    public new void Verify() => Verify(Called.AtLeastOnce);

    /// <summary>Verifies call count satisfies the Called constraint.</summary>
    public new void Verify(Called times)
    {
        if (!times.Validate(TotalCallCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, TotalCallCount));
    }

    /// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>
    public new VerificationFailure? CheckVerificationAll()
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

    /// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>
    public new VerificationFailure? CheckVerification()
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

    public override void Reset()
    {
        base.Reset();
        _returnValueTracking?.Reset();
    }

    /// <summary>
    /// Runs the non-void priority chain: When chain, sequence, return value, callback.
    /// Returns (true, result) if handled, (false, default) otherwise.
    /// </summary>
    protected (bool Handled, TReturn Result) RunPriorityChain(TArgs args)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = (WhenMatcherBase)_whenChain[_whenChainHead];
            if (matcher.Matches(args))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                {
                    _whenChainHead++;
                }
                return (true, matcher.CallReturn(args));
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
            return (true, InvokeDelegate(callback, args));
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCallBase();
            RecordArgs(args, _returnValueTracking);
            return (true, _returnValue);
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCallBase();
            RecordArgs(args, _callTracking);
            return (true, InvokeDelegate(_call, args));
        }

        return (false, default!);
    }

    /// <summary>
    /// Handles the unconfigured tail for non-void: sequence exhaustion repeat.
    /// Returns (true, result) if handled, (false, default) otherwise.
    /// </summary>
    protected (bool Handled, TReturn Result) HandleNonVoidSequenceExhaustedRepeat(bool strict, TArgs args)
    {
        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCallBase();
                RecordArgs(args, tracking);
                return (true, InvokeDelegate(callback, args));
            }
            return (true, default!);
        }
        return (false, default!);
    }

    /// <summary>Resets all fields and sets up a new callback with its builder.</summary>
    protected void SetupReturnCallback(TDelegate callback, ReturnMethodCallBuilderBase builder)
    {
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
        _call = callback;
        _callTracking = builder;
    }

    /// <summary>Resets all fields and sets up a return value with its builder.</summary>
    protected void SetupReturnValue(TReturn value, ReturnMethodCallBuilderBase builder)
    {
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
    }

    /// <summary>
    /// Searches tracking objects (return value, callback, then sequence in reverse) for the last one with calls,
    /// casts it to TBuilder, and extracts a value using the selector. Non-void version checks _returnValueTracking first.
    /// </summary>
    protected new TResult? FindLastArgInTracking<TBuilder, TResult>(Func<TBuilder, TResult> selector)
        where TBuilder : class
    {
        if (_returnValueTracking?._callCount > 0 && _returnValueTracking is TBuilder rvt)
            return selector(rvt);
        if (_callTracking?._callCount > 0 && _callTracking is TBuilder ct)
            return selector(ct);
        if (_sequence != null)
            for (int i = _sequence.Count - 1; i >= 0; i--)
                if (_sequence[i].Tracking._callCount > 0 && _sequence[i].Tracking is TBuilder st)
                    return selector(st);
        return default;
    }

    /// <summary>Invokes the delegate and returns the result.</summary>
    protected abstract TReturn InvokeDelegate(TDelegate del, TArgs args);

    /// <summary>Not used for non-void interceptors. Throws.</summary>
    protected override void InvokeVoidDelegate(TDelegate del, TArgs args)
    {
        throw new InvalidOperationException("Non-void interceptor should use InvokeDelegate, not InvokeVoidDelegate.");
    }

    // ========================================================================
    // Inner class: WhenMatcherBase (non-void)
    // ========================================================================

    /// <summary>Abstract base for non-void When chain matchers.</summary>
    public abstract class WhenMatcherBase : VoidMethodInterceptorBase<TDelegate, TArgs>.VoidWhenMatcherBase
    {
        public abstract TReturn CallReturn(TArgs args);
        public override void Call(TArgs args) => CallReturn(args);
    }

    /// <summary>Matcher that uses a predicate and returns a stored value.</summary>
    public class WhenMatcherValueBase : WhenMatcherBase
    {
        private readonly Func<TArgs, bool> _predicate;
        private readonly TReturn _value;

        public WhenMatcherValueBase(Func<TArgs, bool> predicate, TReturn value)
        {
            _predicate = predicate;
            _value = value;
        }

        public override bool Matches(TArgs args) => _predicate(args);
        public override TReturn CallReturn(TArgs args) => _value;
        public override bool IsTerminal => false;
    }

    /// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>
    public class WhenMatcherCallBase : WhenMatcherBase
    {
        private readonly Func<TArgs, TReturn> _callback;

        public WhenMatcherCallBase(Func<TArgs, TReturn> callback) => _callback = callback;

        public override bool Matches(TArgs args) => true;
        public override TReturn CallReturn(TArgs args) => _callback(args);
        public override bool IsTerminal => true;
    }

    /// <summary>Matcher that never matches. Terminal.</summary>
    public class WhenMatcherNoneBase : WhenMatcherBase
    {
        public override bool Matches(TArgs args) => false;
        public override TReturn CallReturn(TArgs args) => default!;
        public override bool IsTerminal => true;
    }

    // ========================================================================
    // Inner class: WhenBuilderBase (non-void)
    // ========================================================================

    /// <summary>Builder for When matchers. Captures predicate, awaits Return(value).</summary>
    public class WhenBuilderBase
    {
        protected readonly MethodInterceptorBase<TDelegate, TArgs, TReturn> _interceptor;
        protected readonly Func<TArgs, bool> _predicate;

        public WhenBuilderBase(MethodInterceptorBase<TDelegate, TArgs, TReturn> interceptor, Func<TArgs, bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
        }

        public WhenChainBase ReturnBase(TReturn value)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherValueBase(_predicate, value));
            return new WhenChainBase(_interceptor);
        }
    }

    // ========================================================================
    // Inner class: WhenChainBase (non-void)
    // ========================================================================

    /// <summary>Non-void When chain with ThenWhen, ThenCall, ThenNone, verification.</summary>
    public class WhenChainBase
    {
        protected readonly MethodInterceptorBase<TDelegate, TArgs, TReturn> _interceptor;

        public WhenChainBase(MethodInterceptorBase<TDelegate, TArgs, TReturn> interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds a predicate-based matcher and returns a new WhenBuilder.</summary>
        public WhenBuilderBase ThenWhenBase(Func<TArgs, bool> predicate)
        {
            return new WhenBuilderBase(_interceptor, predicate);
        }

        /// <summary>Adds an unconditional callback as terminal matcher.</summary>
        public WhenChainBase ThenCallBase(Func<TArgs, TReturn> callback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherCallBase(callback));
            return this;
        }

        /// <summary>Closes chain with no matcher.</summary>
        public WhenChainBase ThenNone()
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new WhenMatcherNoneBase());
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
        public WhenChainBase VerifiableBase()
        {
            _interceptor._whenVerifiable = true;
            return this;
        }
    }

    // ========================================================================
    // Inner class: ReturnMethodCallBuilderBase (non-void)
    // ========================================================================

    /// <summary>Builder for non-void methods. Adds return-value sequence elevation.</summary>
    public class ReturnMethodCallBuilderBase : MethodCallBuilderBase
    {
        private new readonly MethodInterceptorBase<TDelegate, TArgs, TReturn> _interceptor;

        public ReturnMethodCallBuilderBase(MethodInterceptorBase<TDelegate, TArgs, TReturn> interceptor)
            : base(interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Elevates to sequence mode without adding a new entry. Used by empty params array overloads.</summary>
        protected void ElevateToSequenceBase()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(TDelegate Callback, MethodCallBuilderBase Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add((_interceptor.CreateValueDelegate(capturedValue), this));
                    _interceptor._hasReturnValue = false;
                    _interceptor._returnValue = default!;
                    _interceptor._returnValueTracking = null;
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        /// <summary>Elevates to sequence mode and adds another callback.</summary>
        protected ReturnMethodSequenceBase ThenReturnBase(TDelegate callback)
        {
            ElevateToSequenceBase();
            var nextBuilder = CreateNextReturnBuilder();
            _interceptor._sequence!.Add((callback, nextBuilder));
            return new ReturnMethodSequenceBase(_interceptor);
        }

        /// <summary>Elevates to sequence mode and adds a value.</summary>
        protected ReturnMethodSequenceBase ThenReturnValueBase(TReturn value)
        {
            return ThenReturnBase(_interceptor.CreateValueDelegate(value));
        }

        /// <summary>Creates a new return builder for sequence entries.</summary>
        protected virtual ReturnMethodCallBuilderBase CreateNextReturnBuilder()
        {
            return new ReturnMethodCallBuilderBase(_interceptor);
        }

        protected override MethodCallBuilderBase CreateNextBuilder() => CreateNextReturnBuilder();
    }

    // ========================================================================
    // Inner class: ReturnMethodSequenceBase (non-void)
    // ========================================================================

    /// <summary>Sequence for non-void methods.</summary>
    public class ReturnMethodSequenceBase : MethodSequenceBase
    {
        private new readonly MethodInterceptorBase<TDelegate, TArgs, TReturn> _interceptor;

        public ReturnMethodSequenceBase(MethodInterceptorBase<TDelegate, TArgs, TReturn> interceptor)
            : base(interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds another callback to the sequence.</summary>
        protected ReturnMethodSequenceBase ThenReturnBase(TDelegate callback)
        {
            var tracking = CreateNextReturnBuilder();
            _interceptor._sequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Adds a value to the sequence.</summary>
        protected ReturnMethodSequenceBase ThenReturnValueBase(TReturn value)
        {
            return ThenReturnBase(_interceptor.CreateValueDelegate(value));
        }

        /// <summary>Creates a new return builder for sequence entries.</summary>
        protected virtual ReturnMethodCallBuilderBase CreateNextReturnBuilder()
        {
            return new ReturnMethodCallBuilderBase(_interceptor);
        }

        protected override MethodCallBuilderBase CreateNextBuilder() => CreateNextReturnBuilder();
    }

    /// <summary>Creates a delegate that ignores its args and returns the given value. Must be overridden.</summary>
    protected abstract TDelegate CreateValueDelegate(TReturn value);
}
