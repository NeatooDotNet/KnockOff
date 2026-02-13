using KnockOff;
using Prototype.Library.Interceptors;
using Prototype.Stubs.Interfaces;

namespace Prototype.Stubs.Refactored;

/// <summary>
/// Base class for MatrixStandaloneStub (mirrors generated Base.g.cs).
/// </summary>
public class MatrixStandaloneStubBase
{
    protected virtual int Rows_() => default!;
    protected virtual int Columns_() => default!;
}

/// <summary>
/// Refactored MatrixStandaloneStub using property and indexer base classes.
/// </summary>
public partial class MatrixStandaloneStub : MatrixStandaloneStubBase, IMatrix, IKnockOffStub
{
    // ========================================================================
    // RowsInterceptor: get-only property (int)
    // ========================================================================

    public sealed class RowsInterceptor : PropertyGetInterceptorBase<int>
    {
        internal IMatrix? _source;

        public RowsInterceptor() : base("Rows") { }

        protected override int InvokeGetUnconfigured(bool strict)
        {
            if (_source is { } src) return src.Rows;
            if (strict) throw StubException.NotConfigured("", "Rows");
            return default!;
        }

        public override void Reset()
        {
            base.Reset();
            _source = null;
        }

        // Public API
        public new IPropertyGetBuilder<int> Get(Func<int> callback)
        {
            _getSequence = null; _getSequenceIndex = 0;
            _isGetVerifiable = false; _getVerifiableTimes = null;
            _get = callback;
            var builder = new PropertyGetBuilderImpl(this);
            _getTracking = builder;
            return builder;
        }

        public new IPropertyGetBuilder<int> Get(int value) => Get(() => value);

        // Inner classes
        public sealed class PropertyGetBuilderImpl : PropertyGetBuilderBase, IPropertyGetBuilder<int>
        {
            private readonly RowsInterceptor _typedInterceptor;

            public PropertyGetBuilderImpl(RowsInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IPropertyGetSequence<int> ThenGet(Func<int> callback)
            {
                ThenGetBase(callback);
                return new PropertyGetSequenceImpl(_typedInterceptor);
            }
            public IPropertyGetSequence<int> ThenGet(int value) => ThenGet(() => value);
            public IPropertyGetSequence<int> ThenGet(params int[] values)
            {
                if (values.Length == 0) { ThenGetBase(() => default!); return new PropertyGetSequenceImpl(_typedInterceptor); }
                var seq = ThenGet(values[0]);
                for (int i = 1; i < values.Length; i++) seq = seq.ThenGet(values[i]);
                return seq;
            }

            public IPropertyGetBuilder<int> Verifiable() { VerifiableBase(); return this; }
            IPropertyGetTracking IPropertyGetTracking.Verifiable() => Verifiable();
            IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => Verifiable();
        }

        public sealed class PropertyGetSequenceImpl : PropertyGetSequenceBase, IPropertyGetSequence<int>
        {
            private readonly RowsInterceptor _typedInterceptor;

            public PropertyGetSequenceImpl(RowsInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IPropertyGetSequence<int> ThenGet(Func<int> callback) { ThenGetBase(callback); return this; }
            public IPropertyGetSequence<int> ThenGet(int value) => ThenGet(() => value);
            public IPropertyGetSequence<int> ThenGet(params int[] values)
            {
                foreach (var v in values) ThenGet(v);
                return this;
            }
            public IPropertyGetSequence<int> Verifiable() { VerifiableBase(); return this; }
        }
    }

    // ========================================================================
    // ColumnsInterceptor: get-only property (int) -- identical structure to Rows
    // ========================================================================

    public sealed class ColumnsInterceptor : PropertyGetInterceptorBase<int>
    {
        internal IMatrix? _source;

        public ColumnsInterceptor() : base("Columns") { }

        protected override int InvokeGetUnconfigured(bool strict)
        {
            if (_source is { } src) return src.Columns;
            if (strict) throw StubException.NotConfigured("", "Columns");
            return default!;
        }

        public override void Reset()
        {
            base.Reset();
            _source = null;
        }

        public new IPropertyGetBuilder<int> Get(Func<int> callback)
        {
            _getSequence = null; _getSequenceIndex = 0;
            _isGetVerifiable = false; _getVerifiableTimes = null;
            _get = callback;
            var builder = new PropertyGetBuilderImpl(this);
            _getTracking = builder;
            return builder;
        }

        public new IPropertyGetBuilder<int> Get(int value) => Get(() => value);

        public sealed class PropertyGetBuilderImpl : PropertyGetBuilderBase, IPropertyGetBuilder<int>
        {
            private readonly ColumnsInterceptor _typedInterceptor;
            public PropertyGetBuilderImpl(ColumnsInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public IPropertyGetSequence<int> ThenGet(Func<int> callback) { ThenGetBase(callback); return new PropertyGetSequenceImpl(_typedInterceptor); }
            public IPropertyGetSequence<int> ThenGet(int value) => ThenGet(() => value);
            public IPropertyGetSequence<int> ThenGet(params int[] values)
            {
                if (values.Length == 0) { ThenGetBase(() => default!); return new PropertyGetSequenceImpl(_typedInterceptor); }
                var seq = ThenGet(values[0]);
                for (int i = 1; i < values.Length; i++) seq = seq.ThenGet(values[i]);
                return seq;
            }
            public IPropertyGetBuilder<int> Verifiable() { VerifiableBase(); return this; }
            IPropertyGetTracking IPropertyGetTracking.Verifiable() => Verifiable();
            IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => Verifiable();
        }

        public sealed class PropertyGetSequenceImpl : PropertyGetSequenceBase, IPropertyGetSequence<int>
        {
            private readonly ColumnsInterceptor _typedInterceptor;
            public PropertyGetSequenceImpl(ColumnsInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }

            public IPropertyGetSequence<int> ThenGet(Func<int> callback) { ThenGetBase(callback); return this; }
            public IPropertyGetSequence<int> ThenGet(int value) => ThenGet(() => value);
            public IPropertyGetSequence<int> ThenGet(params int[] values) { foreach (var v in values) ThenGet(v); return this; }
            public IPropertyGetSequence<int> Verifiable() { VerifiableBase(); return this; }
        }
    }

    // ========================================================================
    // IndexerInterceptor: get+set indexer using base class
    // ========================================================================

    public sealed class IndexerInterceptor : IndexerGetSetInterceptorBase<(int row, int col), double>
    {
        internal IMatrix? _source;

        protected override double InvokeGetUnconfigured(bool strict, (int row, int col) key)
        {
            if (_source is { } src) return src[key.row, key.col];
            if (strict) throw StubException.NotConfigured("", "Indexer");
            return default!;
        }

        protected override void InvokeSetUnconfigured(bool strict, (int row, int col) key, double value)
        {
            if (_source is { } src) { src[key.row, key.col] = value; return; }
            if (strict) throw StubException.NotConfigured("", "Indexer");
        }

        public override void Reset()
        {
            base.Reset();
            _source = null;
        }

        // PerKey access with unpacked params
        public PerKeyBuilder this[int row, int col] => GetOrCreatePerKeyBuilder((row, col));

        // Get API
        public IIndexerGetBuilder<(int row, int col), double> Get(Func<(int row, int col), double> callback)
        {
            _getSequence = null; _getSequenceIndex = 0;
            _isGetVerifiable = false; _getVerifiableTimes = null;
            _get = callback;
            var builder = new IndexerGetBuilderImpl(this);
            _getTracking = builder;
            return builder;
        }

        // Set API
        public IIndexerSetBuilder<(int row, int col), double> Set(Action<(int row, int col), double> callback)
        {
            _setSequence = null; _setSequenceIndex = 0;
            _isSetVerifiable = false; _setVerifiableTimes = null;
            _set = callback;
            var builder = new IndexerSetBuilderImpl(this);
            _setTracking = builder;
            return builder;
        }

        // When API
        public IndexerWhenBuilder When(Func<(int row, int col), bool> predicate)
        {
            return new IndexerWhenBuilder(this, predicate);
        }

        // Inner classes
        public sealed class IndexerGetBuilderImpl : IndexerGetBuilderBase, IIndexerGetBuilder<(int row, int col), double>
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerGetBuilderImpl(IndexerInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IIndexerGetSequence<(int row, int col), double> ThenGet(Func<(int row, int col), double> callback)
            {
                ThenGetBase(callback);
                return new IndexerGetSequenceImpl(_typedInterceptor);
            }

            public IIndexerGetBuilder<(int row, int col), double> Verifiable() { VerifiableBase(); return this; }
            IIndexerGetTracking<(int row, int col)> IIndexerGetTracking<(int row, int col)>.Verifiable() => Verifiable();
            IIndexerGetTracking<(int row, int col)> IIndexerGetTracking<(int row, int col)>.Verifiable(Called times) => Verifiable();
        }

        public sealed class IndexerGetSequenceImpl : IndexerGetSequenceBase, IIndexerGetSequence<(int row, int col), double>
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerGetSequenceImpl(IndexerInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IIndexerGetSequence<(int row, int col), double> ThenGet(Func<(int row, int col), double> callback)
            {
                ThenGetBase(callback);
                return this;
            }

            public IIndexerGetSequence<(int row, int col), double> Verifiable() { VerifiableBase(); return this; }
        }

        public sealed class IndexerSetBuilderImpl : IndexerSetBuilderBase, IIndexerSetBuilder<(int row, int col), double>
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerSetBuilderImpl(IndexerInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IIndexerSetSequence<(int row, int col), double> ThenSet(Action<(int row, int col), double> callback)
            {
                ThenSetBase(callback);
                return new IndexerSetSequenceImpl(_typedInterceptor);
            }

            public IIndexerSetBuilder<(int row, int col), double> Verifiable() { VerifiableBase(); return this; }
            IIndexerSetTracking<(int row, int col), double> IIndexerSetTracking<(int row, int col), double>.Verifiable() => Verifiable();
            IIndexerSetTracking<(int row, int col), double> IIndexerSetTracking<(int row, int col), double>.Verifiable(Called times) => Verifiable();
        }

        public sealed class IndexerSetSequenceImpl : IndexerSetSequenceBase, IIndexerSetSequence<(int row, int col), double>
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerSetSequenceImpl(IndexerInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IIndexerSetSequence<(int row, int col), double> ThenSet(Action<(int row, int col), double> callback)
            {
                ThenSetBase(callback);
                return this;
            }

            public IIndexerSetSequence<(int row, int col), double> Verifiable() { VerifiableBase(); return this; }
        }

        public sealed class IndexerWhenBuilder : IndexerWhenBuilderBase
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerWhenBuilder(IndexerInterceptor interceptor, Func<(int row, int col), bool> predicate)
                : base(interceptor, predicate)
            {
                _typedInterceptor = interceptor;
            }

            public IndexerGetWhenChain Returns(double value)
            {
                var chain = ReturnsBase(value);
                return new IndexerGetWhenChain(_typedInterceptor);
            }

            public IndexerGetWhenChain Get(Func<(int row, int col), double> callback)
            {
                GetBase(callback);
                return new IndexerGetWhenChain(_typedInterceptor);
            }

            public IndexerSetWhenChain Set(Action<(int row, int col), double> callback)
            {
                SetBase(callback);
                return new IndexerSetWhenChain(_typedInterceptor);
            }
        }

        public sealed class IndexerGetWhenChain : IndexerGetWhenChainBase
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerGetWhenChain(IndexerInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IndexerWhenBuilder ThenWhen(Func<(int row, int col), bool> predicate)
            {
                return new IndexerWhenBuilder(_typedInterceptor, predicate);
            }

            public IndexerGetWhenChain Verifiable()
            {
                VerifiableBase();
                return this;
            }
        }

        public sealed class IndexerSetWhenChain : IndexerSetWhenChainBase
        {
            private readonly IndexerInterceptor _typedInterceptor;

            public IndexerSetWhenChain(IndexerInterceptor interceptor) : base(interceptor)
            {
                _typedInterceptor = interceptor;
            }

            public IndexerWhenBuilder ThenWhen(Func<(int row, int col), bool> predicate)
            {
                return new IndexerWhenBuilder(_typedInterceptor, predicate);
            }

            public IndexerSetWhenChain Verifiable()
            {
                VerifiableBase();
                return this;
            }
        }
    }

    // ========================================================================
    // Stub-level members
    // ========================================================================

    public RowsInterceptor OnRows { get; } = new();
    public ColumnsInterceptor OnColumns { get; } = new();
    public IndexerInterceptor OnIndexer { get; } = new();

    public bool Strict { get; set; } = false;

    int IMatrix.Rows => OnRows.InvokeGet(Strict);
    int IMatrix.Columns => OnColumns.InvokeGet(Strict);

    double IMatrix.this[int row, int col]
    {
        get => OnIndexer.InvokeGet(Strict, (row, col));
        set => OnIndexer.InvokeSet(Strict, (row, col), value);
    }

    public void Verify()
    {
        var failures = new List<VerificationFailure>();
        if (OnRows.CheckVerification() is { } rf) failures.Add(rf);
        if (OnColumns.CheckVerification() is { } cf) failures.Add(cf);
        if (OnIndexer.CheckVerification() is { } inf) failures.Add(inf);
        if (failures.Count > 0) throw new VerificationException(failures);
    }

    public void VerifyAll()
    {
        var failures = new List<VerificationFailure>();
        if (OnRows.CheckVerificationAll() is { } rf) failures.Add(rf);
        if (OnColumns.CheckVerificationAll() is { } cf) failures.Add(cf);
        if (OnIndexer.CheckVerificationAll() is { } inf) failures.Add(inf);
        if (failures.Count > 0) throw new VerificationException(failures);
    }

    public void Source(IMatrix? source)
    {
        OnRows._source = source;
        OnColumns._source = source;
        OnIndexer._source = source;
    }
}
