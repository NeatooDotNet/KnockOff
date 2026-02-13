using System.Data;
using KnockOff;
using Prototype.Stubs.Refactored;
using Xunit;

namespace Prototype.Tests;

/// <summary>
/// Representative tests for the refactored DataReaderStub.
/// Tests a cross-section of all interceptor types:
/// - Property (get-only)
/// - Method (void 0-param, non-void 0-param, non-void 1-param int->T)
/// - Indexer (dual-key)
/// - Multi-param method
/// - Verification (Verify, VerifyAll)
/// </summary>
public class DataReaderTests
{
    // ========================================================================
    // 1. Property: FieldCount get-only int property
    // ========================================================================

    [Fact]
    public void FieldCount_Get_ReturnsConfiguredValue()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnFieldCount.Get(5);
        IDataReader reader = stub;
        Assert.Equal(5, reader.FieldCount);
    }

    [Fact]
    public void FieldCount_Unconfigured_ReturnsDefault()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        IDataReader reader = stub;
        Assert.Equal(0, reader.FieldCount);
    }

    [Fact]
    public void IsClosed_Get_ReturnsBoolValue()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnIsClosed.Get(true);
        IDataReader reader = stub;
        Assert.True(reader.IsClosed);
    }

    // ========================================================================
    // 2. Non-void 1-param method: GetString (int -> string)
    // ========================================================================

    [Fact]
    public void GetString_ReturnWithCallback_InvokesCallback()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetString.Return(i => i == 0 ? "Alice" : "Bob");
        IDataReader reader = stub;
        Assert.Equal("Alice", reader.GetString(0));
        Assert.Equal("Bob", reader.GetString(1));
    }

    [Fact]
    public void GetString_ReturnWithValue_ReturnsValueForAnyArg()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetString.Return("fixed");
        IDataReader reader = stub;
        Assert.Equal("fixed", reader.GetString(0));
        Assert.Equal("fixed", reader.GetString(99));
    }

    [Fact]
    public void GetInt32_ReturnWithCallback_InvokesCallback()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetInt32.Return(i => i * 10);
        IDataReader reader = stub;
        Assert.Equal(0, reader.GetInt32(0));
        Assert.Equal(30, reader.GetInt32(3));
    }

    [Fact]
    public void GetBoolean_ReturnWithValue_ReturnsConfiguredValue()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetBoolean.Return(true);
        IDataReader reader = stub;
        Assert.True(reader.GetBoolean(0));
    }

    // ========================================================================
    // 3. Non-void 0-param method: Read (bool)
    // ========================================================================

    [Fact]
    public void Read_ReturnValue_ReturnsConfiguredBool()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnRead.Return(true);
        IDataReader reader = stub;
        Assert.True(reader.Read());
    }

    [Fact]
    public void Read_ReturnCallback_InvokesCallback()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        var callCount = 0;
        stub.OnRead.Return((DataReaderStubs.ReadDelegate)(() => ++callCount <= 2));
        IDataReader reader = stub;
        Assert.True(reader.Read());   // callCount=1
        Assert.True(reader.Read());   // callCount=2
        Assert.False(reader.Read());  // callCount=3
    }

    [Fact]
    public void NextResult_ReturnValue_ReturnsFalse()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnNextResult.Return(false);
        IDataReader reader = stub;
        Assert.False(reader.NextResult());
    }

    // ========================================================================
    // 4. Void 0-param method: Close
    // ========================================================================

    [Fact]
    public void Close_WithCallback_InvokesCallback()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        var closed = false;
        stub.OnClose.Call(() => closed = true);
        IDataReader reader = stub;
        reader.Close();
        Assert.True(closed);
    }

    [Fact]
    public void Dispose_WithCallback_InvokesCallback()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        var disposed = false;
        stub.OnDispose.Call(() => disposed = true);
        IDataReader reader = stub;
        ((IDisposable)reader).Dispose();
        Assert.True(disposed);
    }

    // ========================================================================
    // 5. Dual-key indexer: this[int] and this[string]
    // ========================================================================

    [Fact]
    public void Indexer_IntKey_PerKeyReturns()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnIndexer[0].Returns("Alice");
        stub.OnIndexer[1].Returns(42);
        IDataReader reader = stub;
        Assert.Equal("Alice", reader[0]);
        Assert.Equal(42, reader[1]);
    }

    [Fact]
    public void Indexer_StringKey_PerKeyReturns()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnIndexer["Name"].Returns("Alice");
        stub.OnIndexer["Age"].Returns(30);
        IDataReader reader = stub;
        Assert.Equal("Alice", reader["Name"]);
        Assert.Equal(30, reader["Age"]);
    }

    [Fact]
    public void Indexer_MixedKeys_WorkIndependently()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnIndexer[0].Returns("ByIndex");
        stub.OnIndexer["Col"].Returns("ByName");
        IDataReader reader = stub;
        Assert.Equal("ByIndex", reader[0]);
        Assert.Equal("ByName", reader["Col"]);
    }

    // ========================================================================
    // 6. Multi-param method: GetBytes
    // ========================================================================

    [Fact]
    public void GetBytes_ReturnWithCallback_InvokesWithAllArgs()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetBytes.Return((i, fieldOffset, buffer, bufferOffset, length) =>
        {
            if (buffer != null)
            {
                buffer[bufferOffset] = 0xAB;
            }
            return 1L;
        });
        IDataReader reader = stub;
        var buf = new byte[10];
        var result = reader.GetBytes(0, 0, buf, 2, 1);
        Assert.Equal(1L, result);
        Assert.Equal(0xAB, buf[2]);
    }

    [Fact]
    public void GetBytes_ReturnValue_ReturnsFixedValue()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetBytes.Return(42L);
        IDataReader reader = stub;
        Assert.Equal(42L, reader.GetBytes(0, 0, null, 0, 0));
    }

    // ========================================================================
    // 7. String->int method: GetOrdinal
    // ========================================================================

    [Fact]
    public void GetOrdinal_ReturnWithCallback_ResolvesColumnName()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetOrdinal.Return(name => name == "Name" ? 0 : name == "Age" ? 1 : -1);
        IDataReader reader = stub;
        Assert.Equal(0, reader.GetOrdinal("Name"));
        Assert.Equal(1, reader.GetOrdinal("Age"));
        Assert.Equal(-1, reader.GetOrdinal("Unknown"));
    }

    [Fact]
    public void GetOrdinal_ReturnValue_ReturnsFixedValue()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetOrdinal.Return(5);
        IDataReader reader = stub;
        Assert.Equal(5, reader.GetOrdinal("anything"));
    }

    // ========================================================================
    // 8. Strict mode
    // ========================================================================

    [Fact]
    public void Strict_UnconfiguredProperty_Throws()
    {
        var stub = new DataReaderStubs.DataReaderStub(strict: true);
        IDataReader reader = stub;
        Assert.Throws<StubException>(() => reader.FieldCount);
    }

    [Fact]
    public void Strict_UnconfiguredMethod_Throws()
    {
        var stub = new DataReaderStubs.DataReaderStub(strict: true);
        IDataReader reader = stub;
        Assert.Throws<StubException>(() => reader.GetString(0));
    }

    [Fact]
    public void Strict_UnconfiguredVoidMethod_Throws()
    {
        var stub = new DataReaderStubs.DataReaderStub(strict: true);
        IDataReader reader = stub;
        Assert.Throws<StubException>(() => reader.Close());
    }

    // ========================================================================
    // 9. Verification
    // ========================================================================

    [Fact]
    public void Verify_ConfiguredAndCalled_Passes()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetString.Return("x");
        stub.OnGetString.Verifiable();
        IDataReader reader = stub;
        _ = reader.GetString(0);
        stub.Verify(); // Should not throw
    }

    [Fact]
    public void Verify_ConfiguredNotCalled_Throws()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnGetString.Return("x");
        stub.OnGetString.Verifiable();
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void VerifyAll_ConfiguredAndCalled_Passes()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnRead.Return(true);
        stub.OnFieldCount.Get(5);
        IDataReader reader = stub;
        _ = reader.Read();
        _ = reader.FieldCount;
        stub.VerifyAll(); // Should not throw
    }

    [Fact]
    public void VerifyAll_ConfiguredNotCalled_Throws()
    {
        var stub = new DataReaderStubs.DataReaderStub();
        stub.OnRead.Return(true);
        // Configured but never called
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    // ========================================================================
    // 10. Multiple interceptor types working together (integration)
    // ========================================================================

    [Fact]
    public void Integration_ReadLoop_WithMultipleInterceptors()
    {
        var stub = new DataReaderStubs.DataReaderStub();

        // Configure FieldCount
        stub.OnFieldCount.Get(3);

        // Configure column names
        stub.OnGetName.Return(i => i switch { 0 => "Id", 1 => "Name", 2 => "Active", _ => "" });

        // Configure Read to return true twice then false
        var readCount = 0;
        stub.OnRead.Return((DataReaderStubs.ReadDelegate)(() => ++readCount <= 2));

        // Configure data via indexer per-key
        // Row 1 data
        var row = 0;
        stub.OnIndexer[0].Get(() =>
        {
            row++;
            return row == 1 ? (object)1 : (object)2;
        });
        stub.OnIndexer["Name"].Get(() => (object)(row == 1 ? "Alice" : "Bob"));
        stub.OnIndexer["Active"].Get(() => (object)true);

        IDataReader reader = stub;

        // Read loop
        var results = new List<(int Id, string Name, bool Active)>();
        Assert.Equal(3, reader.FieldCount);

        while (reader.Read())
        {
            var id = (int)reader[0];
            var name = (string)reader["Name"];
            var active = (bool)reader["Active"];
            results.Add((id, name, active));
        }

        Assert.Equal(2, results.Count);
        Assert.Equal((1, "Alice", true), results[0]);
        Assert.Equal((2, "Bob", true), results[1]);
    }
}
