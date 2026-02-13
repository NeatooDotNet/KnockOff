using KnockOff;
using Prototype.Library.Interceptors;
using Prototype.Stubs.Interfaces;

namespace Prototype.Stubs.Refactored;

/// <summary>
/// Base class with stub override methods (mirrors the generated Base.g.cs).
/// </summary>
public class BasicUserMethodStubBase
{
    protected virtual string Process_(string input) => default!;
    protected virtual int Calculate_(int a, int b) => default!;
    protected virtual void Execute_(string command) { }
    protected virtual string? FindById_(int id) => default!;
}

/// <summary>
/// Refactored BasicUserMethodStub using interceptor base classes.
/// Implements IStubOverrideService. Each interceptor inherits from a base class.
/// </summary>
public partial class BasicUserMethodStub : BasicUserMethodStubBase, IStubOverrideService, IKnockOffStub
{
    // ========================================================================
    // ProcessInterceptor: non-void, 1-param (string -> string)
    // ========================================================================

    public sealed class ProcessInterceptor : MethodInterceptorBase<ProcessInterceptor.ProcessDelegate, string, string>
    {
        internal IStubOverrideService? _source;

        public delegate string ProcessDelegate(string input);

        public ProcessInterceptor() : base("Process") { }

        // --- Abstract overrides ---
        protected override string InvokeDelegate(ProcessDelegate del, string args) => del(args);
        protected override void RecordArgs(string args, MethodCallBuilderBase tracking)
        {
            if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args);
        }
        protected override void RecordUnconfiguredArgs(string args) => _unconfiguredLastArg = args;
        protected override ProcessDelegate CreateValueDelegate(string value) => (_) => value;

        private string? _unconfiguredLastArg;

        /// <summary>Last argument from the most recently called registration.</summary>
        public string? LastArg
        {
            get
            {
                var found = FindLastArgInTracking<MethodCallBuilderImpl, string>(b => b.LastArg);
                return found ?? (_unconfiguredCallCount > 0 ? _unconfiguredLastArg : default);
            }
        }

        // --- Public API (stays generated) ---
        public MethodCallBuilderImpl Return(ProcessDelegate callback)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public MethodCallBuilderImpl Return(string value)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupReturnValue(value, builder);
            return builder;
        }

        public MethodSequenceImpl Return(string first, params string[] rest)
        {
            var builder = Return((_) => first);
            if (rest.Length == 0) return builder.ThenReturn(first);
            var seq = builder.ThenReturn(rest[0]);
            for (int i = 1; i < rest.Length; i++) seq = seq.ThenReturn(rest[i]);
            return seq;
        }

        public WhenBuilder When(string input)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            return new WhenBuilder(this, (_arg0) => object.Equals(_arg0, input));
        }

        public WhenBuilder When(Func<string, bool> predicate)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            return new WhenBuilder(this, predicate);
        }

        internal string Invoke(bool strict, BasicUserMethodStub stub, string input)
        {
            var (handled, result) = RunPriorityChain(input);
            if (handled) return result;
            _unconfiguredCallCount++;
            RecordUnconfiguredArgs(input);
            var (seqHandled, seqResult) = HandleNonVoidSequenceExhaustedRepeat(strict, input);
            if (seqHandled) return seqResult;
            if (_source is { } src) return src.Process(input);
            if (strict) throw StubException.NotConfigured("", "Process");
            return stub.Process_(input);
        }

        public override void Reset()
        {
            base.Reset();
            _unconfiguredLastArg = default;
            _source = null;
        }

        // --- Inner classes (thin wrappers) ---

        public sealed class MethodCallBuilderImpl : ReturnMethodCallBuilderBase
        {
            private readonly ProcessInterceptor _typedInterceptor;
            private string _lastArg = default!;

            public MethodCallBuilderImpl(ProcessInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public string LastArg => _lastArg;
            public void RecordArg(string input) => _lastArg = input;

            public override void Reset() { base.Reset(); _lastArg = default!; }

            public MethodSequenceImpl ThenReturn(ProcessDelegate callback)
            {
                ThenReturnBase(callback);
                return new MethodSequenceImpl(_typedInterceptor);
            }

            public MethodSequenceImpl ThenReturn(string value) => ThenReturn((_) => value);

            public MethodSequenceImpl ThenReturn(params string[] values)
            {
                if (values.Length == 0) { ThenReturnBase(_typedInterceptor.CreateValueDelegate(default!)); return new MethodSequenceImpl(_typedInterceptor); }
                var seq = ThenReturn(values[0]);
                for (int i = 1; i < values.Length; i++) seq = seq.ThenReturn(values[i]);
                return seq;
            }

            public MethodCallBuilderImpl Verifiable() { VerifiableBase(); return this; }
            public MethodCallBuilderImpl Verifiable(Called times) { VerifiableBase(times); return this; }

            protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class MethodSequenceImpl : ReturnMethodSequenceBase
        {
            private readonly ProcessInterceptor _typedInterceptor;

            public MethodSequenceImpl(ProcessInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public MethodSequenceImpl ThenReturn(ProcessDelegate callback)
            {
                ThenReturnBase(callback);
                return this;
            }

            public MethodSequenceImpl ThenReturn(string value) => ThenReturn((_) => value);

            public MethodSequenceImpl ThenReturn(params string[] values)
            {
                foreach (var value in values) ThenReturn(value);
                return this;
            }

            public MethodSequenceImpl Verifiable() { VerifiableBase(); return this; }

            protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class WhenBuilder : WhenBuilderBase
        {
            public WhenBuilder(ProcessInterceptor interceptor, Func<string, bool> predicate)
                : base(interceptor, predicate) { }

            public WhenChain Return(string value)
            {
                ReturnBase(value);
                return new WhenChain((ProcessInterceptor)_interceptor);
            }
        }

        public sealed class WhenChain : WhenChainBase
        {
            private readonly ProcessInterceptor _typedInterceptor;

            public WhenChain(ProcessInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public WhenBuilder ThenWhen(string input)
            {
                return new WhenBuilder(_typedInterceptor, (_arg0) => object.Equals(_arg0, input));
            }

            public WhenBuilder ThenWhen(Func<string, bool> predicate)
            {
                return new WhenBuilder(_typedInterceptor, predicate);
            }

            public WhenChain ThenCall(ProcessDelegate callback)
            {
                ThenCallBase((args) => callback(args));
                return this;
            }

            public new WhenChain ThenNone()
            {
                base.ThenNone();
                return this;
            }

            public WhenChain Verifiable()
            {
                VerifiableBase();
                return this;
            }
        }
    }

    // ========================================================================
    // CalculateInterceptor: non-void, 2-param ((int, int) -> int)
    // ========================================================================

    public sealed class CalculateInterceptor : MethodInterceptorBase<CalculateInterceptor.CalculateDelegate, (int a, int b), int>
    {
        internal IStubOverrideService? _source;

        public delegate int CalculateDelegate(int a, int b);

        public CalculateInterceptor() : base("Calculate") { }

        protected override int InvokeDelegate(CalculateDelegate del, (int a, int b) args) => del(args.a, args.b);
        protected override void RecordArgs((int a, int b) args, MethodCallBuilderBase tracking)
        {
            if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args);
        }
        protected override void RecordUnconfiguredArgs((int a, int b) args) => _unconfiguredLastArgs = (args.a, args.b);
        protected override CalculateDelegate CreateValueDelegate(int value) => (_, _) => value;

        private (int? a, int? b)? _unconfiguredLastArgs;

        public (int? a, int? b)? LastArgs
        {
            get
            {
                if ((_returnValueTracking?._callCount ?? 0) > 0) return ((MethodCallBuilderImpl)_returnValueTracking!).LastArgs;
                if ((_callTracking?._callCount ?? 0) > 0) return ((MethodCallBuilderImpl)_callTracking!).LastArgs;
                if (_sequence != null)
                    for (int i = _sequence.Count - 1; i >= 0; i--)
                        if (_sequence[i].Tracking._callCount > 0) return ((MethodCallBuilderImpl)_sequence[i].Tracking).LastArgs;
                return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default;
            }
        }

        public MethodCallBuilderImpl Return(CalculateDelegate callback)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public MethodCallBuilderImpl Return(int value)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupReturnValue(value, builder);
            return builder;
        }

        public MethodSequenceImpl Return(int first, params int[] rest)
        {
            var builder = Return((_, _) => first);
            if (rest.Length == 0) return builder.ThenReturn(first);
            var seq = builder.ThenReturn(rest[0]);
            for (int i = 1; i < rest.Length; i++) seq = seq.ThenReturn(rest[i]);
            return seq;
        }

        public WhenBuilder When(int a, int b)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            return new WhenBuilder(this, (args) => object.Equals(args.a, a) && object.Equals(args.b, b));
        }

        public WhenBuilder When(Func<int, int, bool> predicate)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            return new WhenBuilder(this, (args) => predicate(args.a, args.b));
        }

        internal int Invoke(bool strict, BasicUserMethodStub stub, int a, int b)
        {
            var args = (a, b);
            var (handled, result) = RunPriorityChain(args);
            if (handled) return result;
            _unconfiguredCallCount++;
            RecordUnconfiguredArgs(args);
            var (seqHandled, seqResult) = HandleNonVoidSequenceExhaustedRepeat(strict, args);
            if (seqHandled) return seqResult;
            if (_source is { } src) return src.Calculate(a, b);
            if (strict) throw StubException.NotConfigured("", "Calculate");
            return stub.Calculate_(a, b);
        }

        public override void Reset()
        {
            base.Reset();
            _unconfiguredLastArgs = default;
            _source = null;
        }

        public sealed class MethodCallBuilderImpl : ReturnMethodCallBuilderBase
        {
            private readonly CalculateInterceptor _typedInterceptor;
            private (int? a, int? b) _lastArgs;

            public MethodCallBuilderImpl(CalculateInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public (int? a, int? b) LastArgs => _lastArgs;
            public void RecordArg((int a, int b) args) => _lastArgs = (args.a, args.b);

            public override void Reset() { base.Reset(); _lastArgs = default; }

            public MethodSequenceImpl ThenReturn(CalculateDelegate callback)
            {
                ThenReturnBase(callback);
                return new MethodSequenceImpl(_typedInterceptor);
            }
            public MethodSequenceImpl ThenReturn(int value) => ThenReturn((_, _) => value);
            public MethodSequenceImpl ThenReturn(params int[] values)
            {
                if (values.Length == 0) { ThenReturnBase(_typedInterceptor.CreateValueDelegate(default!)); return new MethodSequenceImpl(_typedInterceptor); }
                var seq = ThenReturn(values[0]);
                for (int i = 1; i < values.Length; i++) seq = seq.ThenReturn(values[i]);
                return seq;
            }
            public MethodCallBuilderImpl Verifiable() { VerifiableBase(); return this; }
            public MethodCallBuilderImpl Verifiable(Called times) { VerifiableBase(times); return this; }
            protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class MethodSequenceImpl : ReturnMethodSequenceBase
        {
            private readonly CalculateInterceptor _typedInterceptor;
            public MethodSequenceImpl(CalculateInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public MethodSequenceImpl ThenReturn(CalculateDelegate callback) { ThenReturnBase(callback); return this; }
            public MethodSequenceImpl ThenReturn(int value) => ThenReturn((_, _) => value);
            public MethodSequenceImpl ThenReturn(params int[] values) { foreach (var v in values) ThenReturn(v); return this; }
            public MethodSequenceImpl Verifiable() { VerifiableBase(); return this; }
            protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class WhenBuilder : WhenBuilderBase
        {
            public WhenBuilder(CalculateInterceptor interceptor, Func<(int a, int b), bool> predicate) : base(interceptor, predicate) { }
            public WhenChain Return(int value) { ReturnBase(value); return new WhenChain((CalculateInterceptor)_interceptor); }
        }

        public sealed class WhenChain : WhenChainBase
        {
            private readonly CalculateInterceptor _typedInterceptor;
            public WhenChain(CalculateInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public WhenBuilder ThenWhen(int a, int b) => new WhenBuilder(_typedInterceptor, (args) => object.Equals(args.a, a) && object.Equals(args.b, b));
            public WhenBuilder ThenWhen(Func<int, int, bool> predicate) => new WhenBuilder(_typedInterceptor, (args) => predicate(args.a, args.b));
            public WhenChain ThenCall(CalculateDelegate callback) { ThenCallBase((args) => callback(args.a, args.b)); return this; }
            public new WhenChain ThenNone() { base.ThenNone(); return this; }
            public WhenChain Verifiable() { VerifiableBase(); return this; }
        }
    }

    // ========================================================================
    // ExecuteInterceptor: void, 1-param (string -> void)
    // ========================================================================

    public sealed class ExecuteInterceptor : VoidMethodInterceptorBase<Action<string>, string>
    {
        internal IStubOverrideService? _source;

        public ExecuteInterceptor() : base("Execute") { }

        protected override void InvokeVoidDelegate(Action<string> del, string args) => del(args);
        protected override void RecordArgs(string args, MethodCallBuilderBase tracking)
        {
            if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args);
        }
        protected override void RecordUnconfiguredArgs(string args) => _unconfiguredLastArg = args;

        private string? _unconfiguredLastArg;

        public string? LastArg
        {
            get
            {
                var found = FindLastArgInTracking<MethodCallBuilderImpl, string>(b => b.LastArg);
                return found ?? (_unconfiguredCallCount > 0 ? _unconfiguredLastArg : default);
            }
        }

        public MethodCallBuilderImpl Call(Action<string> callback)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupCallback(callback, builder);
            return builder;
        }

        public VoidWhenChain When(string command)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicateBase((_arg0) => object.Equals(_arg0, command));
            _whenChain.Add(matcher);
            return new VoidWhenChain(this, matcher);
        }

        public VoidWhenChain When(Func<string, bool> predicate)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            var matcher = new VoidWhenMatcherPredicateBase(predicate);
            _whenChain.Add(matcher);
            return new VoidWhenChain(this, matcher);
        }

        internal void Invoke(bool strict, BasicUserMethodStub stub, string command)
        {
            if (RunVoidPriorityChain(command)) return;
            _unconfiguredCallCount++;
            RecordUnconfiguredArgs(command);
            if (HandleSequenceExhaustedRepeat(strict, command)) return;
            if (_source is { } src) { src.Execute(command); return; }
            if (strict) throw StubException.NotConfigured("", "Execute");
            stub.Execute_(command);
        }

        public override void Reset()
        {
            base.Reset();
            _unconfiguredLastArg = default;
            _source = null;
        }

        public sealed class MethodCallBuilderImpl : MethodCallBuilderBase
        {
            private readonly ExecuteInterceptor _typedInterceptor;
            private string _lastArg = default!;

            public MethodCallBuilderImpl(ExecuteInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public string LastArg => _lastArg;
            public void RecordArg(string command) => _lastArg = command;

            public override void Reset() { base.Reset(); _lastArg = default!; }

            public MethodSequenceImpl ThenCall(Action<string> callback)
            {
                ThenCallBase(callback);
                return new MethodSequenceImpl(_typedInterceptor);
            }
            public MethodCallBuilderImpl Verifiable() { VerifiableBase(); return this; }
            public MethodCallBuilderImpl Verifiable(Called times) { VerifiableBase(times); return this; }
            protected override MethodCallBuilderBase CreateNextBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class MethodSequenceImpl : MethodSequenceBase
        {
            private readonly ExecuteInterceptor _typedInterceptor;
            public MethodSequenceImpl(ExecuteInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public MethodSequenceImpl ThenCall(Action<string> callback)
            {
                var tracking = new MethodCallBuilderImpl(_typedInterceptor);
                AddToSequence(callback, tracking);
                return this;
            }
            public MethodSequenceImpl Verifiable() { VerifiableBase(); return this; }
            protected override MethodCallBuilderBase CreateNextBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class VoidWhenChain : VoidWhenChainBase
        {
            private readonly ExecuteInterceptor _typedInterceptor;
            private readonly VoidWhenMatcherPredicateBase? _typedMatcher;

            public VoidWhenChain(ExecuteInterceptor interceptor, VoidWhenMatcherBase matcher) : base(interceptor, matcher)
            {
                _typedInterceptor = interceptor;
                _typedMatcher = matcher as VoidWhenMatcherPredicateBase;
            }

            public VoidWhenChain Call(Action<string> callback)
            {
                _typedMatcher?.SetCallback(callback);
                return this;
            }

            public VoidWhenChain ThenWhen(string command)
            {
                _typedInterceptor._whenChain ??= new List<VoidWhenMatcherBase>();
                var matcher = new VoidWhenMatcherPredicateBase((_arg0) => object.Equals(_arg0, command));
                _typedInterceptor._whenChain.Add(matcher);
                return new VoidWhenChain(_typedInterceptor, matcher);
            }

            public VoidWhenChain ThenWhen(Func<string, bool> predicate)
            {
                _typedInterceptor._whenChain ??= new List<VoidWhenMatcherBase>();
                var matcher = new VoidWhenMatcherPredicateBase(predicate);
                _typedInterceptor._whenChain.Add(matcher);
                return new VoidWhenChain(_typedInterceptor, matcher);
            }

            public VoidWhenChain ThenCall(Action<string> callback)
            {
                _typedInterceptor._whenChain ??= new List<VoidWhenMatcherBase>();
                _typedInterceptor._whenChain.Add(new VoidWhenMatcherCallBase(callback));
                return this;
            }

            public new VoidWhenChain ThenNone()
            {
                base.ThenNone();
                return this;
            }

            public VoidWhenChain Verifiable()
            {
                VerifiableBase();
                return this;
            }
        }
    }

    // ========================================================================
    // FindByIdInterceptor: non-void, 1-param (int -> string?)
    // ========================================================================

    public sealed class FindByIdInterceptor : MethodInterceptorBase<FindByIdInterceptor.FindByIdDelegate, int, string?>
    {
        internal IStubOverrideService? _source;

        public delegate string? FindByIdDelegate(int id);

        public FindByIdInterceptor() : base("FindById") { }

        protected override string? InvokeDelegate(FindByIdDelegate del, int args) => del(args);
        protected override void RecordArgs(int args, MethodCallBuilderBase tracking)
        {
            if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args);
        }
        protected override void RecordUnconfiguredArgs(int args) => _unconfiguredLastArg = args;
        protected override FindByIdDelegate CreateValueDelegate(string? value) => (_) => value;

        private int? _unconfiguredLastArg;

        public int? LastArg
        {
            get
            {
                var found = FindLastArgInTracking<MethodCallBuilderImpl, int?>(b => b.LastArg);
                return found ?? (_unconfiguredCallCount > 0 ? _unconfiguredLastArg : default);
            }
        }

        public MethodCallBuilderImpl Return(FindByIdDelegate callback)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public MethodCallBuilderImpl Return(string? value)
        {
            var builder = new MethodCallBuilderImpl(this);
            SetupReturnValue(value, builder);
            return builder;
        }

        public WhenBuilder When(int id)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            return new WhenBuilder(this, (_arg0) => object.Equals(_arg0, id));
        }

        public WhenBuilder When(Func<int, bool> predicate)
        {
            _whenChain ??= new List<VoidWhenMatcherBase>();
            return new WhenBuilder(this, predicate);
        }

        internal string? Invoke(bool strict, BasicUserMethodStub stub, int id)
        {
            var (handled, result) = RunPriorityChain(id);
            if (handled) return result;
            _unconfiguredCallCount++;
            RecordUnconfiguredArgs(id);
            var (seqHandled, seqResult) = HandleNonVoidSequenceExhaustedRepeat(strict, id);
            if (seqHandled) return seqResult;
            if (_source is { } src) return src.FindById(id);
            if (strict) throw StubException.NotConfigured("", "FindById");
            return stub.FindById_(id);
        }

        public override void Reset()
        {
            base.Reset();
            _unconfiguredLastArg = default;
            _source = null;
        }

        public sealed class MethodCallBuilderImpl : ReturnMethodCallBuilderBase
        {
            private readonly FindByIdInterceptor _typedInterceptor;
            private int? _lastArg;

            public MethodCallBuilderImpl(FindByIdInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public int? LastArg => _lastArg;
            public void RecordArg(int id) => _lastArg = id;
            public override void Reset() { base.Reset(); _lastArg = default; }

            public MethodSequenceImpl ThenReturn(FindByIdDelegate callback)
            {
                ThenReturnBase(callback);
                return new MethodSequenceImpl(_typedInterceptor);
            }
            public MethodSequenceImpl ThenReturn(string? value) => ThenReturn((_) => value);
            public MethodCallBuilderImpl Verifiable() { VerifiableBase(); return this; }
            public MethodCallBuilderImpl Verifiable(Called times) { VerifiableBase(times); return this; }
            protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class MethodSequenceImpl : ReturnMethodSequenceBase
        {
            private readonly FindByIdInterceptor _typedInterceptor;
            public MethodSequenceImpl(FindByIdInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public MethodSequenceImpl ThenReturn(FindByIdDelegate callback) { ThenReturnBase(callback); return this; }
            public MethodSequenceImpl ThenReturn(string? value) => ThenReturn((_) => value);
            public MethodSequenceImpl Verifiable() { VerifiableBase(); return this; }
            protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new MethodCallBuilderImpl(_typedInterceptor);
        }

        public sealed class WhenBuilder : WhenBuilderBase
        {
            public WhenBuilder(FindByIdInterceptor interceptor, Func<int, bool> predicate) : base(interceptor, predicate) { }
            public WhenChain Return(string? value) { ReturnBase(value); return new WhenChain((FindByIdInterceptor)_interceptor); }
        }

        public sealed class WhenChain : WhenChainBase
        {
            private readonly FindByIdInterceptor _typedInterceptor;
            public WhenChain(FindByIdInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public WhenBuilder ThenWhen(int id) => new WhenBuilder(_typedInterceptor, (_arg0) => object.Equals(_arg0, id));
            public WhenBuilder ThenWhen(Func<int, bool> predicate) => new WhenBuilder(_typedInterceptor, predicate);
            public WhenChain ThenCall(FindByIdDelegate callback) { ThenCallBase((args) => callback(args)); return this; }
            public new WhenChain ThenNone() { base.ThenNone(); return this; }
            public WhenChain Verifiable() { VerifiableBase(); return this; }
        }
    }

    // ========================================================================
    // Stub-level members
    // ========================================================================

    public ProcessInterceptor Process { get; } = new();
    public CalculateInterceptor Calculate { get; } = new();
    public ExecuteInterceptor Execute { get; } = new();
    public FindByIdInterceptor FindById { get; } = new();

    public bool Strict { get; set; } = false;

    public void Verify()
    {
        var failures = new List<VerificationFailure>();
        if (Process.CheckVerification() is { } pf) failures.Add(pf);
        if (Calculate.CheckVerification() is { } cf) failures.Add(cf);
        if (Execute.CheckVerification() is { } ef) failures.Add(ef);
        if (FindById.CheckVerification() is { } ff) failures.Add(ff);
        if (failures.Count > 0) throw new VerificationException(failures);
    }

    public void VerifyAll()
    {
        var failures = new List<VerificationFailure>();
        if (Process.CheckVerificationAll() is { } pf) failures.Add(pf);
        if (Calculate.CheckVerificationAll() is { } cf) failures.Add(cf);
        if (Execute.CheckVerificationAll() is { } ef) failures.Add(ef);
        if (FindById.CheckVerificationAll() is { } ff) failures.Add(ff);
        if (failures.Count > 0) throw new VerificationException(failures);
    }

    public void Source(IStubOverrideService? source)
    {
        Process._source = source;
        Calculate._source = source;
        Execute._source = source;
        FindById._source = source;
    }

    // Explicit interface implementation
    string IStubOverrideService.Process(string input) => Process.Invoke(Strict, this, input);
    int IStubOverrideService.Calculate(int a, int b) => Calculate.Invoke(Strict, this, a, b);
    void IStubOverrideService.Execute(string command) => Execute.Invoke(Strict, this, command);
    string? IStubOverrideService.FindById(int id) => FindById.Invoke(Strict, this, id);
}
