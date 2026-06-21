// Non-generic base class for generated interceptor classes.
// Uses object?/Delegate? fields so no generic type parameters leak into IntelliSense.
// Generated classes provide typed wrappers that cast to/from these fields.
//
// Suppressions justified: Generated interceptor classes inherit from this type
// and directly access public fields, nested types, and generic lists.
// Changing visibility or structure would break all generated code across all 9 patterns.
// CA1062: This class has many internal/protected methods called by generated code with
// guaranteed non-null arguments; adding null checks would add overhead with no safety benefit.
// CA1716: Method names like "Return" and "Call" are the natural KnockOff API;
// VB.NET keyword conflicts are accepted for API usability.
#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Non-generic base class for all generated method interceptor classes.
/// Handles runtime logic (tracking, verification, sequence management, When chain matching)
/// using <c>object?</c>/<c>Delegate?</c> fields. Generated subclasses provide typed wrappers.
/// </summary>
public abstract class MethodInterceptorRuntime : IInterceptor
{
    protected readonly string _memberName;

    // Callback (stored as Delegate? to avoid generic type parameters)
    protected Delegate? _call;
    protected MethodCallBuilderBase? _callTracking;

    // Sequence (Delegate stored as object to accommodate value-wrapping delegates)
    protected List<(Delegate Callback, MethodCallBuilderBase Tracking)>? _sequence;
    protected int _sequenceIndex;
    protected bool _repeatLastValue = true;

    // When chain (non-generic matcher base)
    protected List<WhenMatcherBase>? _whenChain;
    protected int _whenChainHead;
    protected bool _whenVerifiable;

    // Return value (non-void only, stored as object? to avoid TReturn)
    protected object? _returnValue;
    protected bool _hasReturnValue;
    protected MethodCallBuilderBase? _returnValueTracking;

    // Fallbacks (stored as Delegate? to avoid TDelegate)
    protected Delegate? _fallback;
    protected Delegate? _sourceFallback;

    // Verification
    protected bool _isVerifiable;
    protected Called? _verifiableTimes;
    protected int _unconfiguredCallCount;

    // Smart default factory
    protected Func<object>? _smartDefaultFactory;

    protected MethodInterceptorRuntime(string memberName)
    {
        _memberName = memberName;
    }

    protected MethodInterceptorRuntime(string memberName, Func<object> smartDefaultFactory)
    {
        _memberName = memberName;
        _smartDefaultFactory = smartDefaultFactory;
    }

    // ========================================================================
    // Public API (non-generic, clean IntelliSense)
    // ========================================================================

    /// <summary>Count of calls not handled by any configured behavior.</summary>
    public int UnconfiguredCallCount => _unconfiguredCallCount;

    /// <summary>Total call count across all configured behaviors and unconfigured calls.</summary>
    public int TotalCallCount
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
    public bool IsConfigured => _hasReturnValue || _call != null || (_sequence?.Count ?? 0) > 0 || (_whenChain?.Count ?? 0) > 0;

    /// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>
    public virtual void Reset()
    {
        _unconfiguredCallCount = 0;
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

    // ========================================================================
    // Priority Chain: Void Methods
    // ========================================================================

    /// <summary>
    /// Runs the void priority chain: When chain, sequence, callback.
    /// Returns true if something handled the call.
    /// </summary>
    protected bool RunVoidPriorityChain(object? args)
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
                matcher.Execute(args);
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
    /// Handles the unconfigured tail for void: sequence exhaustion repeat.
    /// Returns true if sequence repeat handled the call.
    /// </summary>
    protected bool HandleVoidSequenceExhaustedRepeat(bool strict, object? args)
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

    // ========================================================================
    // Priority Chain: Non-Void Methods
    // ========================================================================

    /// <summary>
    /// Runs the non-void priority chain: When chain, sequence, return value, callback.
    /// Returns (true, result) if handled, (false, null) otherwise.
    /// </summary>
    protected (bool Handled, object? Result) RunPriorityChain(object? args)
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
                return (true, matcher.ExecuteReturn(args));
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

        return (false, null);
    }

    /// <summary>
    /// Handles the unconfigured tail for non-void: sequence exhaustion repeat.
    /// Returns (true, result) if handled, (false, null) otherwise.
    /// </summary>
    protected (bool Handled, object? Result) HandleNonVoidSequenceExhaustedRepeat(bool strict, object? args)
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
            return (true, null); // exhausted, no repeat -> default
        }
        return (false, null);
    }

    // ========================================================================
    // Setup Helpers (called by generated typed methods)
    // ========================================================================

    /// <summary>Resets callback fields and sets up a new void callback with its builder.</summary>
    protected void SetupVoidCallback(Delegate callback, MethodCallBuilderBase builder)
    {
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = callback;
        _callTracking = builder;
    }

    /// <summary>Resets all fields and sets up a new non-void callback with its builder.</summary>
    protected void SetupReturnCallback(Delegate callback, MethodCallBuilderBase builder)
    {
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _hasReturnValue = false; _returnValue = null; _returnValueTracking = null;
        _call = callback;
        _callTracking = builder;
    }

    /// <summary>Resets all fields and sets up a return value with its builder.</summary>
    protected void SetupReturnValue(object? value, MethodCallBuilderBase builder)
    {
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = null; _callTracking = null;
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
    }

    /// <summary>Sets a source fallback delegate for Source() delegation.</summary>
    protected void SetSourceFallback(Delegate sourceFallback)
    {
        _sourceFallback = sourceFallback;
    }

    /// <summary>Sets a stub override fallback delegate.</summary>
    protected void SetFallback(Delegate fallback)
    {
        _fallback = fallback;
    }

    // ========================================================================
    // Abstract Methods (type-specific operations overridden by generated code)
    // ========================================================================

    /// <summary>Invokes a void delegate with the given args. Override to cast and invoke.</summary>
    protected abstract void InvokeVoidDelegate(Delegate del, object? args);

    /// <summary>Invokes a non-void delegate and returns the result. Override to cast and invoke.</summary>
    protected abstract object? InvokeDelegate(Delegate del, object? args);

    /// <summary>Records args on the tracking builder. Override to set typed LastArg/LastArgs.</summary>
    protected abstract void RecordArgs(object? args, MethodCallBuilderBase tracking);

    /// <summary>Records unconfigured args. Override to set typed unconfigured last arg.</summary>
    protected abstract void RecordUnconfiguredArgs(object? args);

    /// <summary>Creates a delegate that ignores args and returns the given value. Override for non-void interceptors.</summary>
    protected virtual Delegate CreateValueDelegate(object? value)
    {
        throw new InvalidOperationException("CreateValueDelegate must be overridden by non-void interceptor classes.");
    }

    // ========================================================================
    // Inner class: WhenMatcherBase
    // ========================================================================

    /// <summary>Abstract base for When chain matchers. Non-generic.</summary>
    public abstract class WhenMatcherBase
    {
        /// <summary>Tests whether the args match this matcher's predicate.</summary>
        public abstract bool Matches(object? args);

        /// <summary>Executes the void action for this matcher (When chain for void methods).</summary>
        public abstract void Execute(object? args);

        /// <summary>Executes and returns the result for this matcher (When chain for non-void methods).</summary>
        public abstract object? ExecuteReturn(object? args);

        /// <summary>Whether this matcher is terminal (always matches or never matches).</summary>
        public abstract bool IsTerminal { get; }

        /// <summary>Number of times this matcher has been matched.</summary>
        public int CallCount { get; set; }
    }

    // ========================================================================
    // Inner class: MethodCallBuilderBase
    // ========================================================================

    /// <summary>Base class for method call builders. Holds call count, verification, sequence elevation.</summary>
    public class MethodCallBuilderBase
    {
        protected readonly MethodInterceptorRuntime _interceptor;

        public MethodCallBuilderBase(MethodInterceptorRuntime interceptor)
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
        protected MethodSequenceBase ThenCallBase(Delegate callback)
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Delegate Callback, MethodCallBuilderBase Tracking)>();
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

    /// <summary>Sequence for void method callbacks.</summary>
    public class MethodSequenceBase
    {
        protected readonly MethodInterceptorRuntime _interceptor;
        private readonly Func<MethodCallBuilderBase>? _builderFactory;

        public MethodSequenceBase(MethodInterceptorRuntime interceptor)
        {
            _interceptor = interceptor;
        }

        public MethodSequenceBase(MethodInterceptorRuntime interceptor, Func<MethodCallBuilderBase> builderFactory)
        {
            _interceptor = interceptor;
            _builderFactory = builderFactory;
        }

        /// <summary>Total call count across all sequence entries.</summary>
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
        public MethodSequenceBase ThenCallBase(Delegate callback)
        {
            var tracking = _builderFactory != null ? _builderFactory() : CreateNextBuilder();
            _interceptor._sequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Adds a callback+tracking pair to the sequence. For use by subclasses in other assemblies.</summary>
        protected void AddToSequence(Delegate callback, MethodCallBuilderBase tracking)
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
        public MethodSequenceBase VerifiableBase()
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
    }

    // ========================================================================
    // Inner class: ReturnMethodCallBuilderBase
    // ========================================================================

    /// <summary>Builder for non-void methods. Adds return-value sequence elevation.</summary>
    public class ReturnMethodCallBuilderBase : MethodCallBuilderBase
    {
        private new readonly MethodInterceptorRuntime _interceptor;

        public ReturnMethodCallBuilderBase(MethodInterceptorRuntime interceptor)
            : base(interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Elevates to sequence mode without adding a new entry. Used by empty params array overloads.</summary>
        protected void ElevateToSequenceBase()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Delegate Callback, MethodCallBuilderBase Tracking)>();
                if (_interceptor._call != null)
                {
                    _interceptor._sequence.Add((_interceptor._call, this));
                }
                else if (_interceptor._hasReturnValue)
                {
                    var capturedValue = _interceptor._returnValue;
                    _interceptor._sequence.Add((_interceptor.CreateValueDelegate(capturedValue), this));
                    _interceptor._hasReturnValue = false;
                    _interceptor._returnValue = null;
                    _interceptor._returnValueTracking = null;
                }
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        /// <summary>Elevates to sequence mode and adds another callback.</summary>
        protected ReturnMethodSequenceBase ThenReturnCallbackBase(Delegate callback)
        {
            ElevateToSequenceBase();
            var nextBuilder = CreateNextReturnBuilder();
            _interceptor._sequence!.Add((callback, nextBuilder));
            return new ReturnMethodSequenceBase(_interceptor, CreateNextReturnBuilder);
        }

        /// <summary>Elevates to sequence mode and adds a value.</summary>
        protected ReturnMethodSequenceBase ThenReturnValueBase(object? value)
        {
            return ThenReturnCallbackBase(_interceptor.CreateValueDelegate(value));
        }

        /// <summary>Creates a new return builder for sequence entries.</summary>
        protected virtual ReturnMethodCallBuilderBase CreateNextReturnBuilder()
        {
            return new ReturnMethodCallBuilderBase(_interceptor);
        }

        protected override MethodCallBuilderBase CreateNextBuilder() => CreateNextReturnBuilder();
    }

    // ========================================================================
    // Inner class: ReturnMethodSequenceBase
    // ========================================================================

    /// <summary>Sequence for non-void methods.</summary>
    public class ReturnMethodSequenceBase
    {
        private readonly MethodInterceptorRuntime _interceptor;
        private readonly Func<ReturnMethodCallBuilderBase>? _returnBuilderFactory;

        public ReturnMethodSequenceBase(MethodInterceptorRuntime interceptor)
        {
            _interceptor = interceptor;
        }

        public ReturnMethodSequenceBase(MethodInterceptorRuntime interceptor, Func<ReturnMethodCallBuilderBase> returnBuilderFactory)
        {
            _interceptor = interceptor;
            _returnBuilderFactory = returnBuilderFactory;
        }

        /// <summary>Adds another callback to the sequence.</summary>
        public ReturnMethodSequenceBase ThenReturnCallbackBase(Delegate callback)
        {
            var tracking = _returnBuilderFactory != null ? _returnBuilderFactory() : CreateNextReturnBuilder();
            _interceptor._sequence!.Add((callback, tracking));
            return this;
        }

        /// <summary>Adds a value to the sequence.</summary>
        public ReturnMethodSequenceBase ThenReturnValueBase(object? value)
        {
            return ThenReturnCallbackBase(_interceptor.CreateValueDelegate(value));
        }

        /// <summary>Creates a new return builder for sequence entries.</summary>
        protected virtual ReturnMethodCallBuilderBase CreateNextReturnBuilder()
        {
            return new ReturnMethodCallBuilderBase(_interceptor);
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
        public ReturnMethodSequenceBase VerifiableBase()
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
    }

    // ========================================================================
    // Inner class: VoidWhenChainBase
    // ========================================================================

    /// <summary>Void When chain with Call, ThenWhen, ThenCall, ThenNone, verification.</summary>
    public class VoidWhenChainBase
    {
        protected readonly MethodInterceptorRuntime _interceptor;
        protected readonly WhenMatcherBase _currentMatcher;

        public VoidWhenChainBase(MethodInterceptorRuntime interceptor, WhenMatcherBase currentMatcher)
        {
            _interceptor = interceptor;
            _currentMatcher = currentMatcher;
        }

        /// <summary>Adds a matcher to the When chain.</summary>
        protected VoidWhenChainBase AddMatcher(WhenMatcherBase matcher)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(matcher);
            return new VoidWhenChainBase(_interceptor, matcher);
        }

        /// <summary>Adds a terminal matcher to the When chain.</summary>
        protected VoidWhenChainBase AddTerminalMatcher(WhenMatcherBase matcher)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(matcher);
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
    // Inner class: WhenBuilderBase (non-void)
    // ========================================================================

    /// <summary>Builder for non-void When matchers. Captures a matcher predicate, awaits Return(value).</summary>
    public class WhenBuilderBase
    {
        protected readonly MethodInterceptorRuntime _interceptor;

        public WhenBuilderBase(MethodInterceptorRuntime interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds a value matcher to the When chain.</summary>
        protected WhenChainBase AddValueMatcher(WhenMatcherBase matcher)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(matcher);
            return new WhenChainBase(_interceptor);
        }
    }

    // ========================================================================
    // Inner class: WhenChainBase (non-void)
    // ========================================================================

    /// <summary>Non-void When chain with ThenWhen, ThenCall, ThenNone, verification.</summary>
    public class WhenChainBase
    {
        protected readonly MethodInterceptorRuntime _interceptor;

        public WhenChainBase(MethodInterceptorRuntime interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>Adds a terminal callback matcher to the When chain.</summary>
        protected WhenChainBase AddTerminalCallbackMatcher(WhenMatcherBase matcher)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(matcher);
            return this;
        }

        /// <summary>Closes chain with no matcher (a "none" terminal).</summary>
        protected WhenChainBase AddNoneMatcher(WhenMatcherBase matcher)
        {
            _interceptor._whenChain ??= new List<WhenMatcherBase>();
            _interceptor._whenChain.Add(matcher);
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
}
