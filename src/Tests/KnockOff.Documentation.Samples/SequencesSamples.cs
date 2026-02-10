using KnockOff;
using KnockOff.Documentation.Samples.Methods;
using KnockOff.Documentation.Samples.Indexers;

namespace KnockOff.Documentation.Samples.Sequences;

// =============================================================================
// Interfaces for Sequence Samples
// =============================================================================

public interface ISeqCalc
{
    int Add(int a, int b);
    void Reset();
}

public interface ISeqNameSvc
{
    string Name { get; set; }
}

// =============================================================================
// Stubs for Sequence Samples
// =============================================================================

[KnockOff]
public partial class SeqCalcStub : ISeqCalc { }

[KnockOff]
public partial class SeqNameSvcStub : ISeqNameSvc { }

// =============================================================================
// Method Sequences for sequences.md
// =============================================================================

public class SeqMethodTests
{
    [Fact]
    public void Sequences_Params()
    {
        var stub = new SeqCalcStub();

        #region sequences-method-params
        // NSubstitute-style concise syntax
        stub.Add.Return(1, 2, 3);
        #endregion

        ISeqCalc calc = stub;
        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0)); // repeats last
    }

    [Fact]
    public void Sequences_SingleVsParams()
    {
        var stub = new SeqCalcStub();

        #region sequences-single-vs-params
        stub.Add.Return(42);       // Single value -- repeats forever (no sequence)
        stub.Add.Return(1, 2, 3);  // Params -- sequence, last repeats after exhaustion
        #endregion

        ISeqCalc calc = stub;
        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
    }

    [Fact]
    public void Sequences_Callback()
    {
        var stub = new SeqCalcStub();

        #region sequences-method-callback
        stub.Add
            .Return((a, b) => a + b)     // First: computed
            .ThenReturn((a, b) => a * b) // Second: computed
            .ThenReturn(999);            // Third+: constant
        #endregion

        ISeqCalc calc = stub;
        Assert.Equal(3, calc.Add(1, 2));   // 1 + 2
        Assert.Equal(6, calc.Add(2, 3));   // 2 * 3
        Assert.Equal(999, calc.Add(0, 0)); // constant
        Assert.Equal(999, calc.Add(0, 0)); // repeats
    }

    [Fact]
    public void Sequences_CallbackThenParams()
    {
        var stub = new SeqCalcStub();

        #region sequences-callback-then-params
        stub.Add.Return((a, b) => a + b).ThenReturn(100, 200, 300);

        ISeqCalc calc = stub;
        calc.Add(1, 2); // 3 (computed)
        calc.Add(0, 0); // 100
        calc.Add(0, 0); // 200
        calc.Add(0, 0); // 300
        calc.Add(0, 0); // 300 (repeats)
        #endregion
    }

    [Fact]
    public void Sequences_ValueBased()
    {
        var stub = new SeqCalcStub();

        #region sequences-value-based
        stub.Add.Return(1).ThenReturn(2).ThenReturn(3);
        // Equivalent to: stub.Add.Return(1, 2, 3);
        #endregion

        ISeqCalc calc = stub;
        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
    }

    [Fact]
    public void Sequences_ThenDefault()
    {
        var stub = new SeqCalcStub();

        #region sequences-method-thendefault
        stub.Add.Return((a, b) => 1).ThenReturn((a, b) => 999).ThenDefault();

        ISeqCalc calc = stub;
        calc.Add(0, 0); // 1
        calc.Add(0, 0); // 999
        calc.Add(0, 0); // 0 (default)
        #endregion
    }

    [Fact]
    public void Sequences_VoidMethod()
    {
        var stub = new SeqCalcStub();
        var log = new List<string>();

        #region sequences-void-method
        stub.Reset
            .Call(() => log.Add("First"))
            .ThenCall(() => log.Add("Second"))
            .ThenCall(() => log.Add("Subsequent"));

        ISeqCalc calc = stub;
        calc.Reset(); // "First"
        calc.Reset(); // "Second"
        calc.Reset(); // "Subsequent"
        calc.Reset(); // "Subsequent" (repeats last)
        #endregion

        Assert.Equal(["First", "Second", "Subsequent", "Subsequent"], log);
    }

    [Fact]
    public async Task Sequences_AsyncAutoWrap()
    {
        var stub = new DataSvcStub();

        #region sequences-async-autowrap
        stub.GetDataAsync.Return("first", "second", "third");

        IDataSvc service = stub;
        var r1 = await service.GetDataAsync(1); // "first"
        var r2 = await service.GetDataAsync(2); // "second"
        var r3 = await service.GetDataAsync(3); // "third"
        var r4 = await service.GetDataAsync(4); // "third" (repeats)
        #endregion

        Assert.Equal("first", r1);
        Assert.Equal("third", r4);
    }
}

// =============================================================================
// Property Sequences for sequences.md
// =============================================================================

public class SeqPropertyTests
{
    [Fact]
    public void Sequences_PropertyGetterValue()
    {
        var stub = new SeqNameSvcStub();

        #region sequences-property-get-value
        stub.Name
            .Get("First")
            .ThenGet("Second")
            .ThenGet("Third");
        #endregion

        ISeqNameSvc service = stub;
        Assert.Equal("First", service.Name);
        Assert.Equal("Second", service.Name);
        Assert.Equal("Third", service.Name);
        Assert.Equal("Third", service.Name); // repeats
    }

    [Fact]
    public void Sequences_PropertyGetterCallback()
    {
        var stub = new SeqNameSvcStub();

        #region sequences-property-get-callback
        stub.Name
            .Get(() => "First")
            .ThenGet(() => "Second")
            .ThenGet(() => "Third");
        #endregion

        ISeqNameSvc service = stub;
        Assert.Equal("First", service.Name);
        Assert.Equal("Second", service.Name);
        Assert.Equal("Third", service.Name);
    }

    [Fact]
    public void Sequences_PropertySetter()
    {
        var stub = new SeqNameSvcStub();
        string? firstWrite = null;
        string? secondWrite = null;

        #region sequences-property-set
        stub.Name
            .Set((v) => firstWrite = v)
            .ThenSet((v) => secondWrite = v);
        #endregion

        ISeqNameSvc service = stub;
        service.Name = "A"; // firstWrite = "A"
        service.Name = "B"; // secondWrite = "B"
        service.Name = "C"; // secondWrite = "C" (repeats last)

        Assert.Equal("A", firstWrite);
        Assert.Equal("C", secondWrite);
    }

    [Fact]
    public void Sequences_PropertyThenDefault()
    {
        var stub = new SeqNameSvcStub();

        #region sequences-property-thendefault
        stub.Name.Get("first").ThenGet("second").ThenDefault();

        ISeqNameSvc service = stub;
        _ = service.Name; // "first"
        _ = service.Name; // "second"
        _ = service.Name; // null (default)
        #endregion
    }
}

// =============================================================================
// Indexer Sequences for sequences.md
// =============================================================================

public class SeqIndexerTests
{
    [Fact]
    public void Sequences_IndexerAllKeysGet()
    {
        var stub = new ConfigStoreStub();

        #region sequences-indexer-allkeys-get
        stub.Indexer.Get((k) => k.Length.ToString())
            .ThenGet((k) => "100")
            .ThenGet((k) => "999");

        IConfigStore collection = stub;
        _ = collection["hello"]; // "5" (first callback)
        _ = collection["world"]; // "100" (second callback)
        _ = collection["foo"];   // "999" (third)
        _ = collection["bar"];   // "999" (repeats)
        #endregion
    }

    [Fact]
    public void Sequences_IndexerAllKeysSet()
    {
        var stub = new ConfigStoreStub();
        var log = new List<string>();

        #region sequences-indexer-allkeys-set
        stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Final: {k}={v}"));
        #endregion

        IConfigStore config = stub;
        config["a"] = "1"; // "First: a=1"
        config["b"] = "2"; // "Final: b=2"

        Assert.Equal(2, log.Count);
    }

    [Fact]
    public void Sequences_IndexerPerKey()
    {
        var stub = new ConfigStoreStub();

        #region sequences-indexer-perkey
        stub.Indexer["key"].Returns("1").ThenReturns("2").ThenReturns("3");

        IConfigStore collection = stub;
        _ = collection["key"]; // "1"
        _ = collection["key"]; // "2"
        _ = collection["key"]; // "3"
        _ = collection["key"]; // "3" (repeats)
        #endregion
    }

    [Fact]
    public void Sequences_IndexerGlobalNotPerKey()
    {
        var stub = new ConfigStoreStub();

        #region sequences-indexer-global
        stub.Indexer.Get((k) => "1").ThenGet((k) => "2").ThenGet((k) => "3");

        IConfigStore collection = stub;
        _ = collection["a"]; // "1"
        _ = collection["b"]; // "2" (advanced despite different key!)
        _ = collection["c"]; // "3"
        #endregion
    }
}

// =============================================================================
// Exhaustion Behavior for sequences.md
// =============================================================================

public class SeqExhaustionTests
{
    [Fact]
    public void Sequences_StrictThrows()
    {
        var stub = new SeqCalcStub();

        #region sequences-strict-exhaustion
        stub.Strict = true;
        stub.Add.Return((a, b) => 100).ThenReturn((a, b) => 200);

        ISeqCalc calc = stub;
        calc.Add(0, 0); // 100
        calc.Add(0, 0); // 200
        Assert.Throws<StubException>(() => calc.Add(0, 0)); // StubException.SequenceExhausted
        #endregion
    }
}

// =============================================================================
// Sequence Verification for sequences.md
// =============================================================================

public class SeqVerificationTests
{
    [Fact]
    public void Sequences_Verify()
    {
        var stub = new SeqCalcStub();

        #region sequences-verify
        var sequence = stub.Add.Return(1, 2, 3);

        ISeqCalc calc = stub;
        calc.Add(0, 0); // 1
        calc.Add(0, 0); // 2
        calc.Add(0, 0); // 3

        sequence.Verify(); // Passes -- all 3 consumed
        #endregion
    }
}

// =============================================================================
// When Chains + Sequences for sequences.md
// =============================================================================

public class SeqWhenInteractionTests
{
    [Fact]
    public void Sequences_WhenInteraction()
    {
        var stub = new SeqCalcStub();

        #region sequences-when-interaction
        stub.Add.Return((a, b) => 1).ThenReturn((a, b) => 2);
        stub.Add.When(99, 99).Return(9999);

        ISeqCalc calc = stub;
        calc.Add(0, 0);   // 1 (sequence)
        calc.Add(99, 99);  // 9999 (When match -- doesn't advance sequence)
        calc.Add(0, 0);   // 2 (sequence advances)
        calc.Add(99, 99);  // 9999 (When still matches)
        #endregion
    }
}

// =============================================================================
// Reset and Sequences for sequences.md
// =============================================================================

public class SeqResetTests
{
    [Fact]
    public void Sequences_Reset()
    {
        var stub = new SeqCalcStub();

        #region sequences-reset
        stub.Add.Return(1, 2, 3);
        ISeqCalc calc = stub;
        calc.Add(0, 0); // 1
        calc.Add(0, 0); // 2

        stub.Add.Reset();

        calc.Add(0, 0); // 1 (restarted from beginning)
        #endregion

        Assert.Equal(2, calc.Add(0, 0)); // continues: 2
    }
}
