#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1716 // Identifiers should not match keywords

namespace KnockOff.Interceptors;

/// <summary>
/// Pre-compiled async interceptor for void methods with 0 parameters.
/// Handles Task and ValueTask interface methods (no inner return type).
/// </summary>
public sealed class AsyncVoidMethodInterceptor0
{
    private readonly string _memberName;

    private Func<Task>? _call;
    private MethodCallBuilder0? _callTracking;

    private List<(Func<Task> Callback, MethodCallBuilder0 Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;

    private List<VoidWhenMatcherBase>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;

    private bool _isVerifiable;
    private Called? _verifiableTimes;

    private int _unconfiguredCallCount;
    

    private Func<Task>? _fallback;
    private Func<Task>? _sourceFallback;

    public AsyncVoidMethodInterceptor0(string memberName)
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

    // ========================================================================
    // Invoke
    // ========================================================================

    public async Task Invoke(bool strict)
    {
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches())
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1)
                    _whenChainHead++;
                await matcher.Call().ConfigureAwait(false);
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
            tracking.RecordCall();
            _sequenceIndex++;
            await callback().ConfigureAwait(false);
            return;
        }

        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall();
            await _call().ConfigureAwait(false);
            return;
        }

        _unconfiguredCallCount++;

        if (_sequence != null && _sequenceIndex >= _sequence.Count)
        {
            if (strict) throw StubException.SequenceExhausted(_memberName);
            if (_repeatLastValue && _sequence.Count > 0)
            {
                var (callback, tracking) = _sequence[_sequence.Count - 1];
                tracking.RecordCall();
                await callback().ConfigureAwait(false);
                return;
            }
            return;
        }

        if (_fallback != null) { await _fallback().ConfigureAwait(false); return; }
        if (_sourceFallback != null) { await _sourceFallback().ConfigureAwait(false); return; }
        if (strict) throw StubException.NotConfigured("", _memberName);
    }

    // ========================================================================
    // Call / When / Verify / Reset
    // ========================================================================

    public MethodCallBuilder0 Call(Func<Task> asyncCallback)
    {
        var builder = new MethodCallBuilder0(this);
        _sequence = null; _sequenceIndex = 0;
        _isVerifiable = false; _verifiableTimes = null;
        _call = asyncCallback;
        _callTracking = builder;
        return builder;
    }

    public MethodCallBuilder0 Call(Action callback)
    {
        return Call(() => { callback(); return Task.CompletedTask; });
    }

    public VoidWhenBuilder0 When(Func<bool> predicate)
    {
        _whenChain ??= new List<VoidWhenMatcherBase>();
        return new VoidWhenBuilder0(this, predicate);
    }

    public void SetFallback(Func<Task>? fallback) => _fallback = fallback;
    public void SetFallback(Action? fallback) =>
        _fallback = fallback != null ? () => { fallback(); return Task.CompletedTask; } : null;
    public void SetSourceFallback(Func<Task>? sourceFallback) => _sourceFallback = sourceFallback;
    public void SetSourceFallback(Action? sourceFallback) =>
        _sourceFallback = sourceFallback != null ? () => { sourceFallback(); return Task.CompletedTask; } : null;

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
        public abstract bool Matches();
        public abstract Task Call();
        public abstract bool IsTerminal { get; }
        public int CallCount { get; set; }
    }

    private sealed class VoidWhenMatcherPredicate : VoidWhenMatcherBase
    {
        private readonly Func<bool> _predicate;
        private Func<Task>? _callback;
        public VoidWhenMatcherPredicate(Func<bool> predicate) => _predicate = predicate;
        public override bool Matches() => _predicate();
        public override Task Call() => _callback?.Invoke() ?? Task.CompletedTask;
        public override bool IsTerminal => false;
        public void SetCallback(Func<Task> callback) => _callback = callback;
    }

    private sealed class VoidWhenMatcherCall : VoidWhenMatcherBase
    {
        private readonly Func<Task> _callback;
        public VoidWhenMatcherCall(Func<Task> callback) => _callback = callback;
        public override bool Matches() => true;
        public override Task Call() => _callback();
        public override bool IsTerminal => true;
    }

    private sealed class VoidWhenMatcherNone : VoidWhenMatcherBase
    {
        public override bool Matches() => false;
        public override Task Call() => Task.CompletedTask;
        public override bool IsTerminal => true;
    }

    public sealed class MethodCallBuilder0 : IMethodCallBuilder<Func<Task>>
    {
        private readonly AsyncVoidMethodInterceptor0 _interceptor;
        internal int _callCount;


        internal MethodCallBuilder0(AsyncVoidMethodInterceptor0 interceptor) => _interceptor = interceptor;

        internal void RecordCall()
        {
            _callCount++;
        }

        public void Reset()
        {
            _callCount = 0;
        }

        public void Verify() => Verify(Called.AtLeastOnce);
        public void Verify(Called called)
        {
            if (!called.Validate(_callCount))
                throw new VerificationException(new VerificationFailure("method", called, _callCount));
        }

        public MethodSequence0 ThenCall(Func<Task> asyncCallback)
        {
            ElevateToSequence();
            var nextBuilder = new MethodCallBuilder0(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, nextBuilder));
            return new MethodSequence0(_interceptor);
        }

        public MethodSequence0 ThenCall(Action callback) => ThenCall(() => { callback(); return Task.CompletedTask; });

        public MethodCallBuilder0 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public MethodCallBuilder0 Verifiable(Called times) { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = times; return this; }

        private void ElevateToSequence()
        {
            if (_interceptor._sequence == null)
            {
                _interceptor._sequence = new List<(Func<Task> Callback, MethodCallBuilder0 Tracking)>();
                if (_interceptor._call != null)
                    _interceptor._sequence.Add((_interceptor._call, this));
                _interceptor._call = null;
                _interceptor._callTracking = null;
                _interceptor._sequenceIndex = 0;
            }
        }

        IMethodCallSequence<Func<Task>> IMethodCallBuilder<Func<Task>>.ThenCall(Func<Task> callback) => ThenCall(callback);
        IMethodTracking IMethodTracking.Verifiable() => Verifiable();
        IMethodTracking IMethodTracking.Verifiable(Called called) => Verifiable(called);
        IMethodCallBuilder<Func<Task>> IMethodCallBuilder<Func<Task>>.Verifiable() => Verifiable();
        IMethodCallBuilder<Func<Task>> IMethodCallBuilder<Func<Task>>.Verifiable(Called called) => Verifiable(called);
    }

    public sealed class MethodSequence0 : IMethodCallSequence<Func<Task>>, IMethodCallSequence, IMethodSequence
    {
        private readonly AsyncVoidMethodInterceptor0 _interceptor;
        internal MethodSequence0(AsyncVoidMethodInterceptor0 interceptor) => _interceptor = interceptor;

        public MethodSequence0 ThenCall(Func<Task> asyncCallback)
        {
            var tracking = new MethodCallBuilder0(_interceptor);
            _interceptor._sequence!.Add((asyncCallback, tracking));
            return this;
        }

        public MethodSequence0 ThenCall(Action callback) => ThenCall(() => { callback(); return Task.CompletedTask; });

        public void Verify()
        {
            if (_interceptor._sequence == null) return;
            if (_interceptor._sequenceIndex < _interceptor._sequence.Count)
                throw new VerificationException(VerificationFailure.SequenceIncomplete("method", _interceptor._sequence.Count, _interceptor._sequenceIndex));
        }

        public void Reset() => _interceptor.Reset();
        public MethodSequence0 Verifiable() { _interceptor._isVerifiable = true; _interceptor._verifiableTimes = null; return this; }
        public void ThenDefault() => _interceptor._repeatLastValue = false;

        IMethodCallSequence<Func<Task>> IMethodCallSequence<Func<Task>>.ThenCall(Func<Task> callback) => ThenCall(callback);
        IMethodCallSequence<Func<Task>> IMethodCallSequence<Func<Task>>.Verifiable() => Verifiable();
        IMethodSequence IMethodSequence.Verifiable() => Verifiable();
    }

    public sealed class VoidWhenBuilder0
    {
        private readonly AsyncVoidMethodInterceptor0 _interceptor;
        private readonly Func<bool> _predicate;
        private int _matcherIndex = -1;
        internal VoidWhenBuilder0(AsyncVoidMethodInterceptor0 interceptor, Func<bool> predicate)
        {
            _interceptor = interceptor;
            _predicate = predicate;
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicate(_predicate);
            _interceptor._whenChain.Add(matcher);
            _matcherIndex = _interceptor._whenChain.Count - 1;
        }

        public VoidWhenChain0 Call(Func<Task> asyncCallback)
        {
            ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex]).SetCallback(asyncCallback);
            return new VoidWhenChain0(_interceptor, _matcherIndex);
        }

        public VoidWhenChain0 Call(Action callback) => Call(() => { callback(); return Task.CompletedTask; });

        public VoidWhenChain0 ThenCall(Func<Task> asyncCallback)
        {
            _interceptor._whenChain!.Add(new VoidWhenMatcherCall(asyncCallback));
            return new VoidWhenChain0(_interceptor, _matcherIndex);
        }

        public VoidWhenChain0 ThenCall(Action callback) => ThenCall(() => { callback(); return Task.CompletedTask; });

        public void Verify(Called times)
        {
            if (_interceptor._whenChain == null || _matcherIndex >= _interceptor._whenChain.Count) return;
            var callCount = _interceptor._whenChain[_matcherIndex].CallCount;
            if (!times.Validate(callCount))
                throw new VerificationException(new VerificationFailure("When matcher", times, callCount));
        }
    }

    public sealed class VoidWhenChain0
    {
        private readonly AsyncVoidMethodInterceptor0 _interceptor;
        private readonly int _currentMatcherIndex;
        internal VoidWhenChain0(AsyncVoidMethodInterceptor0 interceptor, int currentMatcherIndex)
        {
            _interceptor = interceptor;
            _currentMatcherIndex = currentMatcherIndex;
        }

        public VoidWhenBuilder0 ThenWhen(Func<bool> predicate) => new VoidWhenBuilder0(_interceptor, predicate);

        public VoidWhenChain0 ThenCall(Func<Task> asyncCallback)
        {
            _interceptor._whenChain ??= new List<VoidWhenMatcherBase>();
            _interceptor._whenChain.Add(new VoidWhenMatcherCall(asyncCallback));
            return this;
        }

        public VoidWhenChain0 ThenCall(Action callback) => ThenCall(() => { callback(); return Task.CompletedTask; });

        public VoidWhenChain0 ThenNone()
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

        public VoidWhenChain0 Verifiable() { _interceptor._whenVerifiable = true; return this; }
    }
}

