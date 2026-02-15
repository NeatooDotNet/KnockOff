#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for void methods with 1 parameter.
/// Handles Task and ValueTask interface methods (no inner return type).
/// </summary>
/// <typeparam name="T1">The type of parameter 1.</typeparam>
public sealed class AsyncVoidMethodInterceptor1<T1>
{
    private readonly string _memberName;

    private Func<T1, Task>? _call;
    private MethodCallBuilder1? _callTracking;

    private List<(Func<T1, Task> Callback, MethodCallBuilder1 Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;

    private List<VoidWhenMatcherBase>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;

    private bool _isVerifiable;
    private Called? _verifiableTimes;

    private int _unconfiguredCallCount;
        private T1? _unconfiguredLastArg;

    private Func<T1, Task>? _fallback;
    private Func<T1, Task>? _sourceFallback;

    public AsyncVoidMethodInterceptor1(string memberName)
    {
        _memberName = memberName;
    }

    public int UnconfiguredCallCount => _unconfiguredCallCount;

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

    public bool IsConfigured => _call != null || (_sequence?.Count ?? 0) > 0 || (_whenChain?.Count ?? 0) > 0;

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

    public async Task Invoke(bool strict, T1 arg1)
    {
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(arg1))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                await matcher.Call(arg1).ConfigureAwait(false);
                return;
            }
            else if (matcher.IsTerminal)
            {
                _whenChainHead++;
            }
        }

        if (_sequence != null && _sequenceIndex < _sequence.Count)
        {
            var (callback, tracking) = _sequence[_sequenceIndex];
            tracking.RecordCall(arg1);
            _sequenceIndex++;
            await callback(arg1).ConfigureAwait(false);
            return;
        }

        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(arg1);
            await _call(arg1).ConfigureAwait(false);
            return;
        }

        _unconfiguredCallCount++;
        _unconfiguredLastArg = arg1;

        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall(arg1);
                await callback(arg1).ConfigureAwait(false);
                return;
            }
            return;
        }

        if (_fallback != null) { await _fallback(arg1).ConfigureAwait(false); return; }
        if (_sourceFallback != null) { await _sourceFallback(arg1).ConfigureAwait(false); return; }
        if (strict) throw StubException.NotConfigured("", _memberName);
    }

    // ========================================================================
    // Call / When / Verify / Reset
    // ========================================================================

    public MethodCallBuilder1 Call(Func<T1, Task> asyncCallback)
    {
        var builder = new MethodCallBuilder1(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = asyncCallback;
        _callTracking = builder;
        return builder;
    }

    public MethodCallBuilder1 Call(Action<T1> callback)
    {
        return Call((T1 arg1) => { callback(arg1); return Task.CompletedTask; });
    }

    public VoidWhenBuilder1 When(T1 arg1)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder1(this, (a1) => object.Equals(a1, arg1));
    }

    public VoidWhenBuilder1 When(Func<T1, bool> predicate)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder1(this, predicate);
    }

    public void SetFallback(Func<T1, Task>? fallback) => _fallback = fallback;
    public void SetFallback(Action<T1>? fallback) =>
        _fallback = fallback != null ? (T1 a1) => { fallback(a1); return Task.CompletedTask; } : null;
    public void SetSourceFallback(Func<T1, Task>? sourceFallback) => _sourceFallback = sourceFallback;
    public void SetSourceFallback(Action<T1>? sourceFallback) =>
        _sourceFallback = sourceFallback != null ? (T1 a1) => { sourceFallback(a1); return Task.CompletedTask; } : null;

    public void Verify() => Verify(Called.AtLeastOnce);
    public void Verify(Called times)
    {
        if (!times.Validate(TotalCallCount))
            throw new VerificationException(new VerificationFailure(_memberName, times, TotalCallCount));
    }

    public void Verifiable() { _isVerifiable = true; _verifiableTimes = null; }
    public void Verifiable(Called times) { _isVerifiable = true; _verifiableTimes = times; }
    public bool IsVerifiable => _isVerifiable;

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
    // Inner classes
    // ========================================================================

    private abstract class VoidWhenMatcherBase
    {
        public abstract bool Matches(T1 arg1);
        public abstract Task Call(T1 arg1);
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    private sealed class VoidWhenMatcherPredicate : VoidWhenMatcherBase
    {
        private readonly Func<T1, bool> _predicate;
        private Func<T1, Task>? _callback;
        public VoidWhenMatcherPredicate(Func<T1, bool> predicate) => _predicate = predicate;
        public override bool Matches(T1 arg1) => _predicate(arg1);
        public override Task Call(T1 arg1) => _callback?.Invoke(arg1) ?? Task.CompletedTask;
        public override bool IsTerminal => false;
        public void SetCallback(Func<T1, Task> callback) => _callback = callback;
    }

    private sealed class VoidWhenMatcherCall : VoidWhenMatcherBase
    {
        private readonly Func<T1, Task> _callback;
        public VoidWhenMatcherCall(Func<T1, Task> callback) => _callback = callback;
        public override bool Matches(T1 arg1) => true;
        public override Task Call(T1 arg1) => _callback(arg1);
        public override bool IsTerminal => true;
    }

    private sealed class VoidWhenMatcherNone : VoidWhenMatcherBase
    {
        public override bool Matches(T1 arg1) => false;
        public override Task Call(T1 arg1) => Task.CompletedTask;
        public override bool IsTerminal => true;
    }

    public sealed class MethodCallBuilder1 : IMethodCallBuilder<Func<T1, Task>, T1?>
    {
        private readonly AsyncVoidMethodInterceptor1<T1> _interceptor;
        internal int _callCount;
        private T1? _lastArg;

        internal MethodCallBuilder1(AsyncVoidMethodInterceptor1<T1> interceptor) => _interceptor = interceptor;

        public T1? LastArg => _lastArg;

        internal void RecordCall(T1 arg1)
        {
            _callCount++;
            _lastArg = arg1;
        }

        public void Reset()
        {
            _callCount = 0;
            _lastArg = default;
        }

        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("method", called, _callCount));
        }

        public MethodSequence1 ThenCall(Func<T1, Task> asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder1(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, nextBuilder));
            return new MethodSequence1(_interceptor);
        }

        public MethodSequence1 ThenCall(Action<T1> callback) => ThenCall((T1 arg1) => { callback(arg1); return Task.CompletedTask; });

        public MethodCallBuilder1 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public MethodCallBuilder1 Verifiable(Called times) { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = times; return this; }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Func<T1, Task> Callback, MethodCallBuilder1 Tracking)>();
                if (_interceptor._call != null)
                    _interceptor._sequence.Add((_interceptor._call, this));
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        IMethodCallSequence<Func<T1, Task>> IMethodCallBuilder<Func<T1, Task>, T1?>.ThenCall(Func<T1, Task> callback) => ThenCall(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodTracking<T1?> IMethodTracking<T1?>.Verifiable() => Verifiable();
        IMethodTracking<T1?> IMethodTracking<T1?>.Verifiable(Called called) => Verifiable(called);
        IMethodCallBuilder<Func<T1, Task>, T1?> IMethodCallBuilder<Func<T1, Task>, T1?>.Verifiable() => Verifiable();
        IMethodCallBuilder<Func<T1, Task>, T1?> IMethodCallBuilder<Func<T1, Task>, T1?>.Verifiable(Called called) => Verifiable(called);
    }

    public sealed class MethodSequence1 : IMethodCallSequence<Func<T1, Task>>, IMethodCallSequence, IMethodSequence
    {
        private readonly AsyncVoidMethodInterceptor1<T1> _interceptor;
        internal MethodSequence1(AsyncVoidMethodInterceptor1<T1> interceptor) => _interceptor = interceptor;

        public MethodSequence1 ThenCall(Func<T1, Task> asyncCallback)
        {
            var tracking = new MethodCallBuilder1(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, tracking));
            return this;
        }

        public MethodSequence1 ThenCall(Action<T1> callback) => ThenCall((T1 arg1) => { callback(arg1); return Task.CompletedTask; });

        public void Verify()
        {
            if (_interceptor._sequence == null) return;
            if (_interceptor._sequenceIndex < _interceptor._sequence.Count)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("method", _interceptor._sequence.Count, _interceptor._sequenceIndex));
        }

        public void Reset() => _interceptor.Reset();
        public MethodSequence1 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public void ThenDefault() => _interceptor._repeatLastValue = false;

        IMethodCallSequence<Func<T1, Task>> IMethodCallSequence<Func<T1, Task>>.ThenCall(Func<T1, Task> callback) => ThenCall(callback);
        IMethodCallSequence<Func<T1, Task>> IMethodCallSequence<Func<T1, Task>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    public sealed class VoidWhenBuilder1
    {
        private readonly AsyncVoidMethodInterceptor1<T1> _interceptor;
        private readonly Func<T1, bool> _predicate;
        internal VoidWhenBuilder1(AsyncVoidMethodInterceptor1<T1> interceptor, Func<T1, bool> predicate) { _interceptor = interceptor; _predicate = predicate; }

        public VoidWhenChain1 Call(Func<T1, Task> asyncCallback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicate(_predicate);
            matcher.SetCallback(asyncCallback);
            _interceptor._whenChain.Add(matcher);
            var matcherIndex = _interceptor._whenChain.Count - 1;
            return new VoidWhenChain1(_interceptor, matcherIndex);
        }

        public VoidWhenChain1 Call(Action<T1> callback) => Call((T1 arg1) => { callback(arg1); return Task.CompletedTask; });
    }

    public sealed class VoidWhenChain1
    {
        private readonly AsyncVoidMethodInterceptor1<T1> _interceptor;
        private readonly int _currentMatcherIndex;
        internal VoidWhenChain1(AsyncVoidMethodInterceptor1<T1> interceptor, int currentMatcherIndex)
        {
            _interceptor = interceptor;
            _currentMatcherIndex = currentMatcherIndex;
        }

        public VoidWhenBuilder1 ThenWhen(T1 arg1)
        {
            return new VoidWhenBuilder1(_interceptor, (a1) => object.Equals(a1, arg1));
        }

        public VoidWhenBuilder1 ThenWhen(Func<T1, bool> predicate) => new VoidWhenBuilder1(_interceptor, predicate);

        public VoidWhenChain1 ThenCall(Func<T1, Task> asyncCallback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall(asyncCallback));
            return this;
        }

        public VoidWhenChain1 ThenCall(Action<T1> callback) => ThenCall((T1 arg1) => { callback(arg1); return Task.CompletedTask; });

        public VoidWhenChain1 ThenNone()
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherNone());
            return this;
        }

        public void Verify()
        {
            if (_interceptor._whenChain == null || _interceptor._whenChain.Count == 0) return;
            var head = _interceptor._whenChainHead;
            var count = _interceptor._whenChain.Count;
            if (head < count && !_interceptor._whenChain[head].IsTerminal && _interceptor._whenChain[head].CallCount == 0)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("When chain", count, head));
        }

        public void Verify(Called times)
        {
            if (_interceptor._whenChain == null || _currentMatcherIndex >= _interceptor._whenChain.Count) return;
            var callCount = _interceptor._whenChain[_currentMatcherIndex].CallCount;
            if (!times.Validate(callCount))
                throw new VerificationException(new VerificationFailure("When matcher", times, callCount));
        }

        public void Reset()
        {
            _interceptor._whenChainHead = 0;
            if (_interceptor._whenChain != null)
                foreach (var matcher in _interceptor._whenChain) matcher.CallCount = 0;
        }

        public VoidWhenChain1 Verifiable() { _interceptor._whenVerifiable = true; return this; }
    }
}

