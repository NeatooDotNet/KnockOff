using System.Data;
using KnockOff;
using Prototype.Library;
using Prototype.Library.Interceptors;

namespace Prototype.Stubs.Refactored;

/// <summary>
/// Refactored DataReaderStub using base classes for all 32 interceptors.
/// Demonstrates scale: original generated code was 17,234 lines.
/// </summary>
public static class DataReaderStubs
{
    // ========================================================================
    // Delegate types for each method
    // ========================================================================

    public delegate bool GetBooleanDelegate(int i);
    public delegate byte GetByteDelegate(int i);
    public delegate long GetBytesDelegate(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length);
    public delegate char GetCharDelegate(int i);
    public delegate long GetCharsDelegate(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length);
    public delegate IDataReader GetDataDelegate(int i);
    public delegate string GetDataTypeNameDelegate(int i);
    public delegate DateTime GetDateTimeDelegate(int i);
    public delegate decimal GetDecimalDelegate(int i);
    public delegate double GetDoubleDelegate(int i);
    public delegate Type GetFieldTypeDelegate(int i);
    public delegate float GetFloatDelegate(int i);
    public delegate Guid GetGuidDelegate(int i);
    public delegate short GetInt16Delegate(int i);
    public delegate int GetInt32Delegate(int i);
    public delegate long GetInt64Delegate(int i);
    public delegate string GetNameDelegate(int i);
    public delegate int GetOrdinalDelegate(string name);
    public delegate string GetStringDelegate(int i);
    public delegate object GetValueDelegate(int i);
    public delegate int GetValuesDelegate(object[] values);
    public delegate bool IsDBNullDelegate(int i);
    public delegate DataTable? GetSchemaTableDelegate();
    public delegate bool NextResultDelegate();
    public delegate bool ReadDelegate();

    // ========================================================================
    // Simple 1-param method interceptor (GetBoolean, GetByte, GetChar, etc.)
    // All follow the same pattern: MethodInterceptorBase<TDelegate, int, TReturn>
    // Uses base class Return API (ReturnMethodCallBuilderBase) directly.
    // ========================================================================

    /// <summary>Generic interceptor for simple int->TReturn methods.</summary>
    public class SimpleIntMethodInterceptor<TDelegate, TReturn> : MethodInterceptorBase<TDelegate, int, TReturn>
        where TDelegate : Delegate
    {
        internal IDataReader? _source;
        private readonly Func<IDataReader, int, TReturn>? _sourceInvoke;
        private readonly Func<TReturn, TDelegate> _valueDelegateFactory;
        private readonly Func<TDelegate, int, TReturn> _delegateInvoke;

        public SimpleIntMethodInterceptor(
            string memberName,
            Func<IDataReader, int, TReturn>? sourceInvoke,
            Func<TReturn, TDelegate> valueDelegateFactory,
            Func<TDelegate, int, TReturn> delegateInvoke)
            : base(memberName)
        {
            _sourceInvoke = sourceInvoke;
            _valueDelegateFactory = valueDelegateFactory;
            _delegateInvoke = delegateInvoke;
        }

        public TReturn Invoke(bool strict, int i)
        {
            var (handled, result) = RunPriorityChain(i);
            if (handled) return result;

            var (exhaustHandled, exhaustResult) = HandleNonVoidSequenceExhaustedRepeat(strict, i);
            if (exhaustHandled) return exhaustResult;

            _unconfiguredCallCount++;

            if (_source is { } src && _sourceInvoke != null) return _sourceInvoke(src, i);
            if (strict) throw StubException.NotConfigured("", _memberName);
            return default!;
        }

        protected override TReturn InvokeDelegate(TDelegate del, int args) => _delegateInvoke(del, args);
        protected override void RecordArgs(int args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs(int args) { }
        protected override TDelegate CreateValueDelegate(TReturn value) => _valueDelegateFactory(value);

        public override void Reset() { base.Reset(); _source = null; }

        // Public API - returns base class types (no KnockOff interface implementation needed for prototype)
        public ReturnMethodCallBuilderBase Return(TDelegate callback)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public ReturnMethodCallBuilderBase Return(TReturn value)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnValue(value, builder);
            return builder;
        }
    }

    // ========================================================================
    // 0-param void method interceptor (Close, Dispose)
    // ========================================================================

    public class VoidNoArgInterceptor : VoidMethodInterceptorBase<Action, Unit>
    {
        internal IDataReader? _source;
        private readonly Action<IDataReader>? _sourceInvoke;

        public VoidNoArgInterceptor(string memberName, Action<IDataReader>? sourceInvoke) : base(memberName)
        {
            _sourceInvoke = sourceInvoke;
        }

        public void Invoke(bool strict)
        {
            if (RunVoidPriorityChain(Unit.Value)) return;
            if (HandleSequenceExhaustedRepeat(strict, Unit.Value)) return;

            _unconfiguredCallCount++;

            if (_source is { } src && _sourceInvoke != null) { _sourceInvoke(src); return; }
            if (strict) throw StubException.NotConfigured("", _memberName);
        }

        protected override void InvokeVoidDelegate(Action del, Unit args) => del();
        protected override void RecordArgs(Unit args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs(Unit args) { }

        public override void Reset() { base.Reset(); _source = null; }

        // Public API - returns base class types
        public MethodCallBuilderBase Call(Action callback)
        {
            var builder = new MethodCallBuilderBase(this);
            SetupCallback(callback, builder);
            return builder;
        }
    }

    // ========================================================================
    // 0-param non-void method interceptor (NextResult, Read, GetSchemaTable)
    // ========================================================================

    public class NonVoidNoArgInterceptor<TDelegate, TReturn> : MethodInterceptorBase<TDelegate, Unit, TReturn>
        where TDelegate : Delegate
    {
        internal IDataReader? _source;
        private readonly Func<IDataReader, TReturn>? _sourceInvoke;
        private readonly Func<TReturn, TDelegate> _valueDelegateFactory;
        private readonly Func<TDelegate, TReturn> _delegateInvoke;

        public NonVoidNoArgInterceptor(
            string memberName,
            Func<IDataReader, TReturn>? sourceInvoke,
            Func<TReturn, TDelegate> valueDelegateFactory,
            Func<TDelegate, TReturn> delegateInvoke)
            : base(memberName)
        {
            _sourceInvoke = sourceInvoke;
            _valueDelegateFactory = valueDelegateFactory;
            _delegateInvoke = delegateInvoke;
        }

        public TReturn Invoke(bool strict)
        {
            var (handled, result) = RunPriorityChain(Unit.Value);
            if (handled) return result;

            var (exhaustHandled, exhaustResult) = HandleNonVoidSequenceExhaustedRepeat(strict, Unit.Value);
            if (exhaustHandled) return exhaustResult;

            _unconfiguredCallCount++;

            if (_source is { } src && _sourceInvoke != null) return _sourceInvoke(src);
            if (strict) throw StubException.NotConfigured("", _memberName);
            return default!;
        }

        protected override TReturn InvokeDelegate(TDelegate del, Unit args) => _delegateInvoke(del);
        protected override void RecordArgs(Unit args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs(Unit args) { }
        protected override TDelegate CreateValueDelegate(TReturn value) => _valueDelegateFactory(value);

        public override void Reset() { base.Reset(); _source = null; }

        // Public API - returns base class types
        public ReturnMethodCallBuilderBase Return(TDelegate callback)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public ReturnMethodCallBuilderBase Return(TReturn value)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnValue(value, builder);
            return builder;
        }
    }

    // ========================================================================
    // Multi-param method interceptors (GetBytes, GetChars)
    // ========================================================================

    public class GetBytesInterceptor : MethodInterceptorBase<GetBytesDelegate, (int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length), long>
    {
        internal IDataReader? _source;

        public GetBytesInterceptor() : base("GetBytes") { }

        public long Invoke(bool strict, int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        {
            var args = (i, fieldOffset, buffer, bufferoffset, length);
            var (handled, result) = RunPriorityChain(args);
            if (handled) return result;

            var (exhaustHandled, exhaustResult) = HandleNonVoidSequenceExhaustedRepeat(strict, args);
            if (exhaustHandled) return exhaustResult;

            _unconfiguredCallCount++;

            if (_source is { } src) return src.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
            if (strict) throw StubException.NotConfigured("", "GetBytes");
            return default!;
        }

        protected override long InvokeDelegate(GetBytesDelegate del, (int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) args) => del(args.i, args.fieldOffset, args.buffer, args.bufferoffset, args.length);
        protected override void RecordArgs((int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs((int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) args) { }
        protected override GetBytesDelegate CreateValueDelegate(long value) => (_, _, _, _, _) => value;

        public override void Reset() { base.Reset(); _source = null; }

        public ReturnMethodCallBuilderBase Return(GetBytesDelegate callback)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public ReturnMethodCallBuilderBase Return(long value)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnValue(value, builder);
            return builder;
        }
    }

    public class GetCharsInterceptor : MethodInterceptorBase<GetCharsDelegate, (int i, long fieldoffset, char[]? buffer, int bufferoffset, int length), long>
    {
        internal IDataReader? _source;

        public GetCharsInterceptor() : base("GetChars") { }

        public long Invoke(bool strict, int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        {
            var args = (i, fieldoffset, buffer, bufferoffset, length);
            var (handled, result) = RunPriorityChain(args);
            if (handled) return result;

            var (exhaustHandled, exhaustResult) = HandleNonVoidSequenceExhaustedRepeat(strict, args);
            if (exhaustHandled) return exhaustResult;

            _unconfiguredCallCount++;

            if (_source is { } src) return src.GetChars(i, fieldoffset, buffer, bufferoffset, length);
            if (strict) throw StubException.NotConfigured("", "GetChars");
            return default!;
        }

        protected override long InvokeDelegate(GetCharsDelegate del, (int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) args) => del(args.i, args.fieldoffset, args.buffer, args.bufferoffset, args.length);
        protected override void RecordArgs((int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs((int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) args) { }
        protected override GetCharsDelegate CreateValueDelegate(long value) => (_, _, _, _, _) => value;

        public override void Reset() { base.Reset(); _source = null; }

        public ReturnMethodCallBuilderBase Return(GetCharsDelegate callback)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public ReturnMethodCallBuilderBase Return(long value)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnValue(value, builder);
            return builder;
        }
    }

    // ========================================================================
    // Dual-key indexer interceptor (this[int] and this[string])
    // Composes two IndexerGetSetInterceptorBase instances.
    // ========================================================================

    public sealed class DualKeyIndexerInterceptor
    {
        internal IDataRecord? _source;

        private readonly IntKeyInterceptor _intKey = new();
        private readonly StringKeyInterceptor _stringKey = new();

        public object InvokeGet_Int32(bool strict, int key)
        {
            _intKey._source = _source;
            return _intKey.InvokeGet(strict, key);
        }

        public object InvokeGet_String(bool strict, string key)
        {
            _stringKey._source = _source;
            return _stringKey.InvokeGet(strict, key);
        }

        public int? LastInt32GetKey => _intKey.LastGetKey;
        public string? LastStringGetKey => _stringKey.LastGetKey;

        public VerificationFailure? CheckVerification() => null;
        public VerificationFailure? CheckVerificationAll()
        {
            var intConfigured = _intKey.IsConfigured;
            var strConfigured = _stringKey.IsConfigured;
            if (!intConfigured && !strConfigured) return null;
            var total = _intKey.TotalGetCountPublic + _stringKey.TotalGetCountPublic;
            return total >= 1 ? null : new VerificationFailure("Indexer", Called.AtLeastOnce, total);
        }

        public void Reset()
        {
            _intKey.Reset();
            _stringKey.Reset();
            _source = null;
        }

        // Per-key access
        public IndexerGetSetInterceptorBase<int, object>.PerKeyBuilder this[int key] => _intKey.GetOrCreatePerKeyBuilder(key);
        public IndexerGetSetInterceptorBase<string, object>.PerKeyBuilder this[string key] => _stringKey.GetOrCreatePerKeyBuilder(key);

        public class IntKeyInterceptor : IndexerGetSetInterceptorBase<int, object>
        {
            internal IDataRecord? _source;

            protected override object InvokeGetUnconfigured(bool strict, int key)
            {
                if (_source is { } src) return src[key];
                if (strict) throw StubException.NotConfigured("", "Indexer");
                return default!;
            }

            protected override void InvokeSetUnconfigured(bool strict, int key, object value)
            {
                if (strict) throw StubException.NotConfigured("", "Indexer");
            }

            public int TotalGetCountPublic => TotalGetCount;

            public override void Reset() { base.Reset(); _source = null; }
        }

        public class StringKeyInterceptor : IndexerGetSetInterceptorBase<string, object>
        {
            internal IDataRecord? _source;

            protected override object InvokeGetUnconfigured(bool strict, string key)
            {
                if (_source is { } src) return src[key];
                if (strict) throw StubException.NotConfigured("", "Indexer");
                return default!;
            }

            protected override void InvokeSetUnconfigured(bool strict, string key, object value)
            {
                if (strict) throw StubException.NotConfigured("", "Indexer");
            }

            public int TotalGetCountPublic => TotalGetCount;

            public override void Reset() { base.Reset(); _source = null; }
        }
    }

    // ========================================================================
    // The DataReader stub class
    // ========================================================================

    public class DataReaderStub : IDataReader, IKnockOffStub
    {
        // Properties (get-only, all int or bool)
        public PropertyGetInterceptorBase<int> OnDepth { get; } = new DepthInterceptor();
        public PropertyGetInterceptorBase<bool> OnIsClosed { get; } = new IsClosedInterceptor();
        public PropertyGetInterceptorBase<int> OnRecordsAffected { get; } = new RecordsAffectedInterceptor();
        public PropertyGetInterceptorBase<int> OnFieldCount { get; } = new FieldCountInterceptor();

        // Indexer (dual-key)
        public DualKeyIndexerInterceptor OnIndexer { get; } = new();

        // Void 0-param methods
        public VoidNoArgInterceptor OnClose { get; } = new("Close", src => src.Close());
        public VoidNoArgInterceptor OnDispose { get; } = new("Dispose", src => ((IDisposable)src).Dispose());

        // Non-void 0-param methods
        public NonVoidNoArgInterceptor<GetSchemaTableDelegate, DataTable?> OnGetSchemaTable { get; } = new("GetSchemaTable", src => src.GetSchemaTable(), v => () => v, d => d());
        public NonVoidNoArgInterceptor<NextResultDelegate, bool> OnNextResult { get; } = new("NextResult", src => src.NextResult(), v => () => v, d => d());
        public NonVoidNoArgInterceptor<ReadDelegate, bool> OnRead { get; } = new("Read", src => src.Read(), v => () => v, d => d());

        // Simple 1-param int->T methods (the bulk -- 18 of them)
        public SimpleIntMethodInterceptor<GetBooleanDelegate, bool> OnGetBoolean { get; } = new("GetBoolean", (src, i) => src.GetBoolean(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetByteDelegate, byte> OnGetByte { get; } = new("GetByte", (src, i) => src.GetByte(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetCharDelegate, char> OnGetChar { get; } = new("GetChar", (src, i) => src.GetChar(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetDataDelegate, IDataReader> OnGetData { get; } = new("GetData", (src, i) => src.GetData(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetDataTypeNameDelegate, string> OnGetDataTypeName { get; } = new("GetDataTypeName", (src, i) => src.GetDataTypeName(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetDateTimeDelegate, DateTime> OnGetDateTime { get; } = new("GetDateTime", (src, i) => src.GetDateTime(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetDecimalDelegate, decimal> OnGetDecimal { get; } = new("GetDecimal", (src, i) => src.GetDecimal(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetDoubleDelegate, double> OnGetDouble { get; } = new("GetDouble", (src, i) => src.GetDouble(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetFieldTypeDelegate, Type> OnGetFieldType { get; } = new("GetFieldType", (src, i) => src.GetFieldType(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetFloatDelegate, float> OnGetFloat { get; } = new("GetFloat", (src, i) => src.GetFloat(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetGuidDelegate, Guid> OnGetGuid { get; } = new("GetGuid", (src, i) => src.GetGuid(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetInt16Delegate, short> OnGetInt16 { get; } = new("GetInt16", (src, i) => src.GetInt16(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetInt32Delegate, int> OnGetInt32 { get; } = new("GetInt32", (src, i) => src.GetInt32(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetInt64Delegate, long> OnGetInt64 { get; } = new("GetInt64", (src, i) => src.GetInt64(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetNameDelegate, string> OnGetName { get; } = new("GetName", (src, i) => src.GetName(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetStringDelegate, string> OnGetString { get; } = new("GetString", (src, i) => src.GetString(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<GetValueDelegate, object> OnGetValue { get; } = new("GetValue", (src, i) => src.GetValue(i), v => _ => v, (d, i) => d(i));
        public SimpleIntMethodInterceptor<IsDBNullDelegate, bool> OnIsDBNull { get; } = new("IsDBNull", (src, i) => src.IsDBNull(i), v => _ => v, (d, i) => d(i));

        // 1-param string->int (GetOrdinal)
        public SimpleStringMethodInterceptor OnGetOrdinal { get; } = new();

        // Multi-param methods
        public GetBytesInterceptor OnGetBytes { get; } = new();
        public GetCharsInterceptor OnGetChars { get; } = new();

        // 1-param object[]->int (GetValues)
        public GetValuesInterceptorType OnGetValues { get; } = new();

        public bool Strict { get; set; } = false;

        public DataReaderStub(bool strict = false) { Strict = strict; }

        // === Explicit interface implementation ===

        void IDataReader.Close() => OnClose.Invoke(Strict);
        DataTable? IDataReader.GetSchemaTable() => OnGetSchemaTable.Invoke(Strict);
        bool IDataReader.NextResult() => OnNextResult.Invoke(Strict);
        bool IDataReader.Read() => OnRead.Invoke(Strict);
        int IDataReader.Depth => OnDepth.InvokeGet(Strict);
        bool IDataReader.IsClosed => OnIsClosed.InvokeGet(Strict);
        int IDataReader.RecordsAffected => OnRecordsAffected.InvokeGet(Strict);

        bool IDataRecord.GetBoolean(int i) => OnGetBoolean.Invoke(Strict, i);
        byte IDataRecord.GetByte(int i) => OnGetByte.Invoke(Strict, i);
        long IDataRecord.GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => OnGetBytes.Invoke(Strict, i, fieldOffset, buffer, bufferoffset, length);
        char IDataRecord.GetChar(int i) => OnGetChar.Invoke(Strict, i);
        long IDataRecord.GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => OnGetChars.Invoke(Strict, i, fieldoffset, buffer, bufferoffset, length);
        IDataReader IDataRecord.GetData(int i) => OnGetData.Invoke(Strict, i);
        string IDataRecord.GetDataTypeName(int i) => OnGetDataTypeName.Invoke(Strict, i);
        DateTime IDataRecord.GetDateTime(int i) => OnGetDateTime.Invoke(Strict, i);
        decimal IDataRecord.GetDecimal(int i) => OnGetDecimal.Invoke(Strict, i);
        double IDataRecord.GetDouble(int i) => OnGetDouble.Invoke(Strict, i);
        Type IDataRecord.GetFieldType(int i) => OnGetFieldType.Invoke(Strict, i);
        float IDataRecord.GetFloat(int i) => OnGetFloat.Invoke(Strict, i);
        Guid IDataRecord.GetGuid(int i) => OnGetGuid.Invoke(Strict, i);
        short IDataRecord.GetInt16(int i) => OnGetInt16.Invoke(Strict, i);
        int IDataRecord.GetInt32(int i) => OnGetInt32.Invoke(Strict, i);
        long IDataRecord.GetInt64(int i) => OnGetInt64.Invoke(Strict, i);
        string IDataRecord.GetName(int i) => OnGetName.Invoke(Strict, i);
        int IDataRecord.GetOrdinal(string name) => OnGetOrdinal.Invoke(Strict, name);
        string IDataRecord.GetString(int i) => OnGetString.Invoke(Strict, i);
        object IDataRecord.GetValue(int i) => OnGetValue.Invoke(Strict, i);
        int IDataRecord.GetValues(object[] values) => OnGetValues.Invoke(Strict, values);
        bool IDataRecord.IsDBNull(int i) => OnIsDBNull.Invoke(Strict, i);
        int IDataRecord.FieldCount => OnFieldCount.InvokeGet(Strict);
        object IDataRecord.this[int i] => OnIndexer.InvokeGet_Int32(Strict, i);
        object IDataRecord.this[string name] => OnIndexer.InvokeGet_String(Strict, name);
        void IDisposable.Dispose() => OnDispose.Invoke(Strict);

        public void Verify()
        {
            var failures = new List<VerificationFailure>();
            if (OnDepth.CheckVerification() is { } f1) failures.Add(f1);
            if (OnIsClosed.CheckVerification() is { } f2) failures.Add(f2);
            if (OnRecordsAffected.CheckVerification() is { } f3) failures.Add(f3);
            if (OnFieldCount.CheckVerification() is { } f4) failures.Add(f4);
            if (OnIndexer.CheckVerification() is { } f5) failures.Add(f5);
            if (OnClose.CheckVerification() is { } f6) failures.Add(f6);
            if (OnGetSchemaTable.CheckVerification() is { } f7) failures.Add(f7);
            if (OnNextResult.CheckVerification() is { } f8) failures.Add(f8);
            if (OnRead.CheckVerification() is { } f9) failures.Add(f9);
            if (OnGetBoolean.CheckVerification() is { } f10) failures.Add(f10);
            if (OnGetByte.CheckVerification() is { } f11) failures.Add(f11);
            if (OnGetBytes.CheckVerification() is { } f12) failures.Add(f12);
            if (OnGetChar.CheckVerification() is { } f13) failures.Add(f13);
            if (OnGetChars.CheckVerification() is { } f14) failures.Add(f14);
            if (OnGetData.CheckVerification() is { } f15) failures.Add(f15);
            if (OnGetDataTypeName.CheckVerification() is { } f16) failures.Add(f16);
            if (OnGetDateTime.CheckVerification() is { } f17) failures.Add(f17);
            if (OnGetDecimal.CheckVerification() is { } f18) failures.Add(f18);
            if (OnGetDouble.CheckVerification() is { } f19) failures.Add(f19);
            if (OnGetFieldType.CheckVerification() is { } f20) failures.Add(f20);
            if (OnGetFloat.CheckVerification() is { } f21) failures.Add(f21);
            if (OnGetGuid.CheckVerification() is { } f22) failures.Add(f22);
            if (OnGetInt16.CheckVerification() is { } f23) failures.Add(f23);
            if (OnGetInt32.CheckVerification() is { } f24) failures.Add(f24);
            if (OnGetInt64.CheckVerification() is { } f25) failures.Add(f25);
            if (OnGetName.CheckVerification() is { } f26) failures.Add(f26);
            if (OnGetOrdinal.CheckVerification() is { } f27) failures.Add(f27);
            if (OnGetString.CheckVerification() is { } f28) failures.Add(f28);
            if (OnGetValue.CheckVerification() is { } f29) failures.Add(f29);
            if (OnGetValues.CheckVerification() is { } f30) failures.Add(f30);
            if (OnIsDBNull.CheckVerification() is { } f31) failures.Add(f31);
            if (OnDispose.CheckVerification() is { } f32) failures.Add(f32);
            if (failures.Count > 0) throw new VerificationException(failures);
        }

        public void VerifyAll()
        {
            var failures = new List<VerificationFailure>();
            if (OnDepth.CheckVerificationAll() is { } f1) failures.Add(f1);
            if (OnIsClosed.CheckVerificationAll() is { } f2) failures.Add(f2);
            if (OnRecordsAffected.CheckVerificationAll() is { } f3) failures.Add(f3);
            if (OnFieldCount.CheckVerificationAll() is { } f4) failures.Add(f4);
            if (OnIndexer.CheckVerificationAll() is { } f5) failures.Add(f5);
            if (OnClose.CheckVerificationAll() is { } f6) failures.Add(f6);
            if (OnGetSchemaTable.CheckVerificationAll() is { } f7) failures.Add(f7);
            if (OnNextResult.CheckVerificationAll() is { } f8) failures.Add(f8);
            if (OnRead.CheckVerificationAll() is { } f9) failures.Add(f9);
            if (OnGetBoolean.CheckVerificationAll() is { } f10) failures.Add(f10);
            if (OnGetByte.CheckVerificationAll() is { } f11) failures.Add(f11);
            if (OnGetBytes.CheckVerificationAll() is { } f12) failures.Add(f12);
            if (OnGetChar.CheckVerificationAll() is { } f13) failures.Add(f13);
            if (OnGetChars.CheckVerificationAll() is { } f14) failures.Add(f14);
            if (OnGetData.CheckVerificationAll() is { } f15) failures.Add(f15);
            if (OnGetDataTypeName.CheckVerificationAll() is { } f16) failures.Add(f16);
            if (OnGetDateTime.CheckVerificationAll() is { } f17) failures.Add(f17);
            if (OnGetDecimal.CheckVerificationAll() is { } f18) failures.Add(f18);
            if (OnGetDouble.CheckVerificationAll() is { } f19) failures.Add(f19);
            if (OnGetFieldType.CheckVerificationAll() is { } f20) failures.Add(f20);
            if (OnGetFloat.CheckVerificationAll() is { } f21) failures.Add(f21);
            if (OnGetGuid.CheckVerificationAll() is { } f22) failures.Add(f22);
            if (OnGetInt16.CheckVerificationAll() is { } f23) failures.Add(f23);
            if (OnGetInt32.CheckVerificationAll() is { } f24) failures.Add(f24);
            if (OnGetInt64.CheckVerificationAll() is { } f25) failures.Add(f25);
            if (OnGetName.CheckVerificationAll() is { } f26) failures.Add(f26);
            if (OnGetOrdinal.CheckVerificationAll() is { } f27) failures.Add(f27);
            if (OnGetString.CheckVerificationAll() is { } f28) failures.Add(f28);
            if (OnGetValue.CheckVerificationAll() is { } f29) failures.Add(f29);
            if (OnGetValues.CheckVerificationAll() is { } f30) failures.Add(f30);
            if (OnIsDBNull.CheckVerificationAll() is { } f31) failures.Add(f31);
            if (OnDispose.CheckVerificationAll() is { } f32) failures.Add(f32);
            if (failures.Count > 0) throw new VerificationException(failures);
        }
    }

    // Property interceptors (thin wrappers)
    private class DepthInterceptor : PropertyGetInterceptorBase<int>
    {
        internal IDataReader? _source;
        public DepthInterceptor() : base("Depth") { }
        protected override int InvokeGetUnconfigured(bool strict)
        {
            if (_source is { } src) return src.Depth;
            if (strict) throw StubException.NotConfigured("", "Depth");
            return default!;
        }
        public override void Reset() { base.Reset(); _source = null; }
    }

    private class IsClosedInterceptor : PropertyGetInterceptorBase<bool>
    {
        internal IDataReader? _source;
        public IsClosedInterceptor() : base("IsClosed") { }
        protected override bool InvokeGetUnconfigured(bool strict)
        {
            if (_source is { } src) return src.IsClosed;
            if (strict) throw StubException.NotConfigured("", "IsClosed");
            return default!;
        }
        public override void Reset() { base.Reset(); _source = null; }
    }

    private class RecordsAffectedInterceptor : PropertyGetInterceptorBase<int>
    {
        internal IDataReader? _source;
        public RecordsAffectedInterceptor() : base("RecordsAffected") { }
        protected override int InvokeGetUnconfigured(bool strict)
        {
            if (_source is { } src) return src.RecordsAffected;
            if (strict) throw StubException.NotConfigured("", "RecordsAffected");
            return default!;
        }
        public override void Reset() { base.Reset(); _source = null; }
    }

    private class FieldCountInterceptor : PropertyGetInterceptorBase<int>
    {
        internal IDataRecord? _source;
        public FieldCountInterceptor() : base("FieldCount") { }
        protected override int InvokeGetUnconfigured(bool strict)
        {
            if (_source is { } src) return src.FieldCount;
            if (strict) throw StubException.NotConfigured("", "FieldCount");
            return default!;
        }
        public override void Reset() { base.Reset(); _source = null; }
    }

    // GetOrdinal: string->int (unique 1-param signature)
    public class SimpleStringMethodInterceptor : MethodInterceptorBase<GetOrdinalDelegate, string, int>
    {
        internal IDataReader? _source;

        public SimpleStringMethodInterceptor() : base("GetOrdinal") { }

        public int Invoke(bool strict, string name)
        {
            var (handled, result) = RunPriorityChain(name);
            if (handled) return result;

            var (exhaustHandled, exhaustResult) = HandleNonVoidSequenceExhaustedRepeat(strict, name);
            if (exhaustHandled) return exhaustResult;

            _unconfiguredCallCount++;

            if (_source is { } src) return src.GetOrdinal(name);
            if (strict) throw StubException.NotConfigured("", "GetOrdinal");
            return default!;
        }

        protected override int InvokeDelegate(GetOrdinalDelegate del, string args) => del(args);
        protected override void RecordArgs(string args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs(string args) { }
        protected override GetOrdinalDelegate CreateValueDelegate(int value) => _ => value;

        public override void Reset() { base.Reset(); _source = null; }

        public ReturnMethodCallBuilderBase Return(GetOrdinalDelegate callback)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public ReturnMethodCallBuilderBase Return(int value)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnValue(value, builder);
            return builder;
        }
    }

    // GetValues: object[]->int
    public class GetValuesInterceptorType : MethodInterceptorBase<GetValuesDelegate, object[], int>
    {
        internal IDataReader? _source;

        public GetValuesInterceptorType() : base("GetValues") { }

        public int Invoke(bool strict, object[] values)
        {
            var (handled, result) = RunPriorityChain(values);
            if (handled) return result;

            var (exhaustHandled, exhaustResult) = HandleNonVoidSequenceExhaustedRepeat(strict, values);
            if (exhaustHandled) return exhaustResult;

            _unconfiguredCallCount++;

            if (_source is { } src) return src.GetValues(values);
            if (strict) throw StubException.NotConfigured("", "GetValues");
            return default!;
        }

        protected override int InvokeDelegate(GetValuesDelegate del, object[] args) => del(args);
        protected override void RecordArgs(object[] args, MethodCallBuilderBase tracking) { }
        protected override void RecordUnconfiguredArgs(object[] args) { }
        protected override GetValuesDelegate CreateValueDelegate(int value) => _ => value;

        public override void Reset() { base.Reset(); _source = null; }

        public ReturnMethodCallBuilderBase Return(GetValuesDelegate callback)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnCallback(callback, builder);
            return builder;
        }

        public ReturnMethodCallBuilderBase Return(int value)
        {
            var builder = new ReturnMethodCallBuilderBase(this);
            SetupReturnValue(value, builder);
            return builder;
        }
    }
}
