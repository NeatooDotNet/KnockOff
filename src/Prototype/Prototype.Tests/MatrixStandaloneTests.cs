using KnockOff;
using Prototype.Stubs.Interfaces;
using Prototype.Stubs.Refactored;

namespace Prototype.Tests;

public class MatrixStandalonePropertyTests
{
    [Fact]
    public void Rows_GetValue_ReturnsConfiguredValue()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        var m = (IMatrix)stub;
        Assert.Equal(5, m.Rows);
    }

    [Fact]
    public void Rows_GetCallback_InvokesCallback()
    {
        var stub = new MatrixStandaloneStub();
        var counter = 0;
        stub.OnRows.Get(() => ++counter);
        var m = (IMatrix)stub;
        Assert.Equal(1, m.Rows);
        Assert.Equal(2, m.Rows);
    }

    [Fact]
    public void Rows_Sequence_ReturnsValuesInOrder()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(1).ThenGet(2).ThenGet(3);
        var m = (IMatrix)stub;
        Assert.Equal(1, m.Rows);
        Assert.Equal(2, m.Rows);
        Assert.Equal(3, m.Rows);
        // Last repeats
        Assert.Equal(3, m.Rows);
    }

    [Fact]
    public void Rows_Sequence_ThenDefault_ReturnsDefault()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(1).ThenGet(2).ThenDefault();
        var m = (IMatrix)stub;
        Assert.Equal(1, m.Rows);
        Assert.Equal(2, m.Rows);
        Assert.Equal(0, m.Rows); // default(int) = 0
    }

    [Fact]
    public void Rows_Verify_ThrowsWhenNotCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        Assert.Throws<VerificationException>(() => stub.OnRows.Verify());
    }

    [Fact]
    public void Rows_Verify_PassesWhenCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        var m = (IMatrix)stub;
        _ = m.Rows;
        stub.OnRows.Verify();
    }

    [Fact]
    public void Rows_VerifyGet_Works()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        var m = (IMatrix)stub;
        _ = m.Rows;
        stub.OnRows.VerifyGet();
    }

    [Fact]
    public void Rows_Verifiable_CheckedByStubVerify()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5).Verifiable();
        var m = (IMatrix)stub;
        _ = m.Rows;
        stub.Verify(); // should not throw
    }

    [Fact]
    public void Rows_Verifiable_ThrowsWhenNotCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5).Verifiable();
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Rows_Reset_ClearsTracking()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        var m = (IMatrix)stub;
        _ = m.Rows;
        stub.OnRows.Reset();
        Assert.Throws<VerificationException>(() => stub.OnRows.Verify());
    }

    [Fact]
    public void Rows_Strict_ThrowsWhenNotConfigured()
    {
        var stub = new MatrixStandaloneStub { Strict = true };
        var m = (IMatrix)stub;
        Assert.Throws<StubException>(() => _ = m.Rows);
    }

    [Fact]
    public void Rows_NotConfigured_ReturnsDefault()
    {
        var stub = new MatrixStandaloneStub();
        var m = (IMatrix)stub;
        Assert.Equal(0, m.Rows);
    }

    [Fact]
    public void Columns_GetValue_Works()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnColumns.Get(10);
        var m = (IMatrix)stub;
        Assert.Equal(10, m.Columns);
    }

    [Fact]
    public void StubVerifyAll_ThrowsWhenConfiguredButNotCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void StubVerifyAll_PassesWhenConfiguredAndCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        var m = (IMatrix)stub;
        _ = m.Rows;
        stub.VerifyAll();
    }

    [Fact]
    public void Rows_HasGet_FalseWhenNotConfigured()
    {
        var stub = new MatrixStandaloneStub();
        Assert.False(stub.OnRows.HasGet);
    }

    [Fact]
    public void Rows_HasGet_TrueWhenConfigured()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnRows.Get(5);
        Assert.True(stub.OnRows.HasGet);
    }
}

// ========================================================================
// Indexer Tests
// ========================================================================

public class MatrixStandaloneIndexerTests
{
    // --- Get with callback ---

    [Fact]
    public void Indexer_Get_WithCallback_ReceivesKey()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(key => key.row * 10 + key.col);
        var m = (IMatrix)stub;
        Assert.Equal(23.0, m[2, 3]);
        Assert.Equal(15.0, m[1, 5]);
    }

    // --- Set with callback ---

    [Fact]
    public void Indexer_Set_WithCallback_ReceivesKeyAndValue()
    {
        var stub = new MatrixStandaloneStub();
        (int row, int col)? capturedKey = null;
        double capturedValue = 0;
        stub.OnIndexer.Set((key, val) =>
        {
            capturedKey = key;
            capturedValue = val;
        });
        var m = (IMatrix)stub;
        m[3, 4] = 99.5;
        Assert.Equal((3, 4), capturedKey);
        Assert.Equal(99.5, capturedValue);
    }

    // --- Backing dictionary pattern ---

    [Fact]
    public void Indexer_BackingDictionary_GetAndSetWork()
    {
        var stub = new MatrixStandaloneStub();
        var dict = new Dictionary<(int, int), double>();
        stub.OnIndexer.Get(key => dict.TryGetValue(key, out var v) ? v : 0.0);
        stub.OnIndexer.Set((key, val) => dict[key] = val);
        var m = (IMatrix)stub;
        m[1, 1] = 5.0;
        m[2, 2] = 10.0;
        Assert.Equal(5.0, m[1, 1]);
        Assert.Equal(10.0, m[2, 2]);
        Assert.Equal(0.0, m[0, 0]);
    }

    // --- LastGetKey / LastSetEntry ---

    [Fact]
    public void Indexer_LastGetKey_CapturesLastKeyAccessed()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        var m = (IMatrix)stub;
        _ = m[1, 2];
        _ = m[3, 4];
        Assert.Equal((3, 4), stub.OnIndexer.LastGetKey);
    }

    [Fact]
    public void Indexer_LastSetEntry_CapturesLastSet()
    {
        var stub = new MatrixStandaloneStub();
        var m = (IMatrix)stub;
        m[1, 1] = 1.0;
        m[2, 2] = 2.0;
        var entry = stub.OnIndexer.LastSetEntry;
        Assert.NotNull(entry);
        Assert.Equal((2, 2), entry.Value.Key);
        Assert.Equal(2.0, entry.Value.Value);
    }

    // --- Per-key builder ---

    [Fact]
    public void Indexer_PerKey_Returns_ConfiguresSpecificKey()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer[0, 0].Returns(1.0);
        stub.OnIndexer[1, 1].Returns(2.0);
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(2.0, m[1, 1]);
    }

    [Fact]
    public void Indexer_PerKey_ThenReturns_Sequence()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer[0, 0].Returns(1.0).ThenReturns(2.0).ThenReturns(3.0);
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(2.0, m[0, 0]);
        Assert.Equal(3.0, m[0, 0]);
        // Repeats last
        Assert.Equal(3.0, m[0, 0]);
    }

    [Fact]
    public void Indexer_PerKey_Get_WithCallback()
    {
        var stub = new MatrixStandaloneStub();
        var counter = 0;
        stub.OnIndexer[0, 0].Get(() => ++counter * 10.0);
        var m = (IMatrix)stub;
        Assert.Equal(10.0, m[0, 0]);
        Assert.Equal(20.0, m[0, 0]);
    }

    [Fact]
    public void Indexer_PerKey_Set_WithCallback()
    {
        var stub = new MatrixStandaloneStub();
        var captured = 0.0;
        stub.OnIndexer[1, 1].Set(v => captured = v);
        var m = (IMatrix)stub;
        m[1, 1] = 42.0;
        Assert.Equal(42.0, captured);
    }

    [Fact]
    public void Indexer_PerKey_VerifyGet()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer[0, 0].Returns(1.0);
        var m = (IMatrix)stub;
        _ = m[0, 0];
        stub.OnIndexer[0, 0].VerifyGet();
    }

    [Fact]
    public void Indexer_PerKey_VerifyGet_ThrowsWhenNotCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer[0, 0].Returns(1.0);
        Assert.Throws<VerificationException>(() => stub.OnIndexer[0, 0].VerifyGet());
    }

    [Fact]
    public void Indexer_PerKey_VerifySet()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer[0, 0].Set(_ => { });
        var m = (IMatrix)stub;
        m[0, 0] = 1.0;
        stub.OnIndexer[0, 0].VerifySet();
    }

    // --- Get sequences (global) ---

    [Fact]
    public void Indexer_GetSequence_ThenGet_CreatesSequence()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(k => 1.0).ThenGet(k => 2.0).ThenGet(k => 3.0);
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(2.0, m[0, 1]);
        Assert.Equal(3.0, m[1, 0]);
    }

    [Fact]
    public void Indexer_GetSequence_RepeatsLastAfterExhaustion()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(k => 1.0).ThenGet(k => 999.0);
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(999.0, m[0, 0]);
        Assert.Equal(999.0, m[0, 0]); // repeats
    }

    [Fact]
    public void Indexer_GetSequence_ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(k => 1.0).ThenGet(k => 999.0).ThenDefault();
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(999.0, m[0, 0]);
        Assert.Equal(0.0, m[0, 0]); // default(double) = 0.0
    }

    [Fact]
    public void Indexer_GetSequence_IsGlobalNotPerKey()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(k => 1.0).ThenGet(k => 2.0);
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]); // First in sequence
        Assert.Equal(2.0, m[0, 0]); // Second in sequence (same key!)
    }

    // --- Set sequences (global) ---

    [Fact]
    public void Indexer_SetSequence_ThenSet_CreatesSequence()
    {
        var stub = new MatrixStandaloneStub();
        var log = new List<string>();
        stub.OnIndexer.Set((k, v) => log.Add($"First:{k.row},{k.col}={v}"))
            .ThenSet((k, v) => log.Add($"Second:{k.row},{k.col}={v}"))
            .ThenSet((k, v) => log.Add($"Third:{k.row},{k.col}={v}"));
        var m = (IMatrix)stub;
        m[0, 0] = 1.0;
        m[1, 1] = 2.0;
        m[2, 2] = 3.0;
        Assert.Equal(["First:0,0=1", "Second:1,1=2", "Third:2,2=3"], log);
    }

    [Fact]
    public void Indexer_SetSequence_RepeatsLastAfterExhaustion()
    {
        var stub = new MatrixStandaloneStub();
        var log = new List<string>();
        stub.OnIndexer.Set((k, v) => log.Add($"First:{v}"))
            .ThenSet((k, v) => log.Add($"Final:{v}"));
        var m = (IMatrix)stub;
        m[0, 0] = 1.0;
        m[0, 0] = 2.0;
        m[0, 0] = 3.0; // repeats last
        Assert.Equal(["First:1", "Final:2", "Final:3"], log);
    }

    [Fact]
    public void Indexer_SetSequence_ThenDefault_SkipsAfterExhaustion()
    {
        var stub = new MatrixStandaloneStub();
        var log = new List<string>();
        stub.OnIndexer.Set((k, v) => log.Add($"First:{v}"))
            .ThenSet((k, v) => log.Add($"Final:{v}"))
            .ThenDefault();
        var m = (IMatrix)stub;
        m[0, 0] = 1.0;
        m[0, 0] = 2.0;
        m[0, 0] = 3.0; // exhausted - no callback
        Assert.Equal(["First:1", "Final:2"], log);
    }

    // --- Verification ---

    [Fact]
    public void Indexer_VerifyGet_ChecksGetterAccess()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        var m = (IMatrix)stub;
        _ = m[0, 0];
        _ = m[1, 1];
        stub.OnIndexer.VerifyGet(Called.Exactly(2));
    }

    [Fact]
    public void Indexer_VerifySet_ChecksSetterAccess()
    {
        var stub = new MatrixStandaloneStub();
        var m = (IMatrix)stub;
        m[0, 0] = 1.0;
        m[1, 1] = 2.0;
        m[2, 2] = 3.0;
        stub.OnIndexer.VerifySet(Called.Exactly(3));
    }

    [Fact]
    public void Indexer_Verify_ThrowsWhenNotCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        Assert.Throws<VerificationException>(() => stub.OnIndexer.Verify());
    }

    [Fact]
    public void Indexer_Verify_PassesWhenCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        var m = (IMatrix)stub;
        _ = m[0, 0];
        stub.OnIndexer.Verify();
    }

    // --- Unconfigured / strict ---

    [Fact]
    public void Indexer_Unconfigured_ReturnsDefault()
    {
        var stub = new MatrixStandaloneStub();
        var m = (IMatrix)stub;
        Assert.Equal(0.0, m[0, 0]); // default(double) = 0.0
    }

    [Fact]
    public void Indexer_Strict_Get_ThrowsWhenNotConfigured()
    {
        var stub = new MatrixStandaloneStub { Strict = true };
        var m = (IMatrix)stub;
        Assert.Throws<StubException>(() => _ = m[0, 0]);
    }

    [Fact]
    public void Indexer_Strict_Set_ThrowsWhenNotConfigured()
    {
        var stub = new MatrixStandaloneStub { Strict = true };
        var m = (IMatrix)stub;
        Assert.Throws<StubException>(() => m[0, 0] = 1.0);
    }

    // --- When chain (getter) ---

    [Fact]
    public void Indexer_WhenGet_Returns_MatchesPredicate()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.When(k => k.row == 0 && k.col == 0).Returns(42.0);
        var m = (IMatrix)stub;
        Assert.Equal(42.0, m[0, 0]);
    }

    [Fact]
    public void Indexer_WhenGet_ThenWhen_Chain()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.When(k => k.row == 0).Returns(1.0)
            .ThenWhen(k => k.row == 1).Returns(2.0)
            .ThenNone();
        var m = (IMatrix)stub;
        Assert.Equal(1.0, m[0, 0]);
        Assert.Equal(2.0, m[1, 0]);
    }

    [Fact]
    public void Indexer_WhenGet_WithCallback()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.When(k => k.row == k.col).Get(k => (double)(k.row * k.col));
        var m = (IMatrix)stub;
        Assert.Equal(0.0, m[0, 0]); // 0*0
        Assert.Equal(1.0, m[1, 1]); // 1*1
    }

    // --- When chain (setter) ---

    [Fact]
    public void Indexer_WhenSet_InvokesCallback()
    {
        var stub = new MatrixStandaloneStub();
        var captured = new List<(int, int, double)>();
        stub.OnIndexer.When(k => k.row == 0).Set((k, v) => captured.Add((k.row, k.col, v)));
        var m = (IMatrix)stub;
        m[0, 1] = 5.0;
        Assert.Single(captured);
        Assert.Equal((0, 1, 5.0), captured[0]);
    }

    // --- Stub-level integration ---

    [Fact]
    public void StubVerifyAll_IncludesIndexer()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void StubVerifyAll_PassesWhenIndexerCalled()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        var m = (IMatrix)stub;
        _ = m[0, 0];
        stub.VerifyAll();
    }

    // --- Reset ---

    [Fact]
    public void Indexer_Reset_ClearsTracking()
    {
        var stub = new MatrixStandaloneStub();
        stub.OnIndexer.Get(_ => 0.0);
        var m = (IMatrix)stub;
        _ = m[0, 0];
        stub.OnIndexer.Reset();
        Assert.Throws<VerificationException>(() => stub.OnIndexer.Verify());
    }
}
