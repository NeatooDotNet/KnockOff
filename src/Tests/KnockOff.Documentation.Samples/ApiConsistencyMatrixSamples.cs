using KnockOff;
using KnockOff.Documentation.Samples; // For User, Order, DataEventArgs types

namespace KnockOff.Documentation.Samples.ApiConsistencyMatrix;

// =============================================================================
// Interfaces for API Consistency Matrix Samples
// =============================================================================

public interface IMatrixService
{
    string GetData(int id);
    void Save(string data);
}

public interface IMatrixConfig
{
    string Name { get; set; }
}

public interface IMatrixCache
{
    string this[string key] { get; set; }
}

public interface IMatrixPublisher
{
    event EventHandler<DataEventArgs>? DataReceived;
}

public interface IMatrixCalculator
{
    int Add(int a, int b);
}

public interface IMatrixStatusService
{
    string GetStatus();
}

public interface IMatrixAsyncService
{
    Task<string> GetDataAsync(int id);
}

// =============================================================================
// Stubs
// =============================================================================

[KnockOff]
public partial class MatrixServiceStub : IMatrixService { }

[KnockOff]
public partial class MatrixConfigStub : IMatrixConfig { }

[KnockOff]
public partial class MatrixCacheStub : IMatrixCache { }

[KnockOff]
public partial class MatrixPublisherStub : IMatrixPublisher { }

[KnockOff]
public partial class MatrixCalculatorStub : IMatrixCalculator { }

[KnockOff]
public partial class MatrixStatusStub : IMatrixStatusService { }

[KnockOff]
public partial class MatrixAsyncStub : IMatrixAsyncService { }

// Stub overrides stub for Feature 11
#region matrix-stub-overrides-interface
[KnockOff]
public partial class MatrixStubOverrideStub : IMatrixCalculator
{
    protected override int Add_(int a, int b) => a + b;
}
#endregion

// =============================================================================
// Feature 2: Method Interception
// =============================================================================

public class MethodInterceptionTests
{
    [Fact]
    public void MethodInterception_AllApis()
    {
        var stub = new MatrixServiceStub();
        IMatrixService svc = stub;

        #region matrix-method-interception
        // Configure behavior
        stub.GetData.Return("test-value");
        stub.GetData.Call((id) => $"Data-{id}");

        // Verify calls
        stub.GetData.Verify(Called.Never);
        #endregion

        svc.GetData(1);
        svc.GetData(2);

        stub.GetData.Verify(Called.Exactly(2));

        // Access call history
        Assert.Equal(2, stub.GetData.LastArg);
    }
}

// =============================================================================
// Feature 3: Property Interception
// =============================================================================

public class PropertyInterceptionTests
{
    [Fact]
    public void PropertyInterception_AllApis()
    {
        var stub = new MatrixConfigStub();
        IMatrixConfig config = stub;

        #region matrix-property-interception
        // Configure getter
        stub.Name.Get("test-name");

        // Configure setter
        stub.Name.Set((value) => { /* capture or validate */ });

        // Verify
        stub.Name.VerifyGet(Called.Never);
        stub.Name.VerifySet(Called.Never);

        // Access history
        // var lastSet = stub.Name.LastSetValue;
        #endregion

        _ = config.Name;
        config.Name = "updated";

        stub.Name.VerifyGet(Called.Once);
        stub.Name.VerifySet(Called.Once);
        Assert.Equal("updated", stub.Name.LastSetValue);
    }
}

// =============================================================================
// Feature 4: Indexer Interception
// =============================================================================

public class IndexerInterceptionTests
{
    [Fact]
    public void IndexerInterception_AllApis()
    {
        var stub = new MatrixCacheStub();
        IMatrixCache cache = stub;

        #region matrix-indexer-interception
        // Per-key Returns
        stub.Indexer["preloaded"].Returns("data");

        // Configure getter callback (fallback for unconfigured keys)
        stub.Indexer.Get((key) => $"value-{key}");

        // Configure setter
        stub.Indexer.Set((key, value) => { });

        // Verify
        stub.Indexer.VerifyGet(Called.Never);
        stub.Indexer.VerifySet(Called.Never);

        // Access history
        // var lastKey = stub.Indexer.LastGetKey;
        // var lastEntry = stub.Indexer.LastSetEntry;
        #endregion

        _ = cache["test"];
        cache["key"] = "val";

        stub.Indexer.VerifyGet(Called.Once);
        stub.Indexer.VerifySet(Called.Once);
        Assert.Equal("test", stub.Indexer.LastGetKey);
        Assert.Equal("key", stub.Indexer.LastSetEntry!.Value.Key);
    }
}

// =============================================================================
// Feature 5: Event Interception
// =============================================================================

public class EventInterceptionTests
{
    [Fact]
    public void EventInterception_AllApis()
    {
        var stub = new MatrixPublisherStub();
        IMatrixPublisher pub = stub;

        #region matrix-event-interception
        // Raise event
        stub.DataReceived.Raise(stub, new DataEventArgs { Data = "test" });

        // Check subscription
        bool hasSubscribers = stub.DataReceived.HasSubscribers;

        // Verify add/remove
        stub.DataReceived.VerifyAdd(Called.Never);
        stub.DataReceived.VerifyRemove(Called.Never);
        #endregion

        Assert.False(hasSubscribers);

        DataEventArgs? received = null;
        pub.DataReceived += (s, e) => received = e;
        stub.DataReceived.VerifyAdd(Called.Once);

        stub.DataReceived.Raise(stub, new DataEventArgs { Data = "hello" });
        Assert.Equal("hello", received?.Data);
    }
}

// =============================================================================
// Feature 6: Sequences
// =============================================================================

public class SequenceTests
{
    [Fact]
    public void Sequences_AllApis()
    {
        var stub = new MatrixStatusStub();
        var configStub = new MatrixConfigStub();
        IMatrixStatusService svc = stub;
        IMatrixConfig config = configStub;

        #region matrix-sequences
        // Return different values on successive calls
        stub.GetStatus
            .Call(() => "Pending")
            .ThenReturn(() => "Processing")
            .ThenReturn(() => "Complete");
        // Call 1: "Pending", Call 2: "Processing", Call 3+: "Complete" (repeats last)

        // Properties support sequences too
        configStub.Name
            .Get("first")
            .ThenGet("second");
        #endregion

        Assert.Equal("Pending", svc.GetStatus());
        Assert.Equal("Processing", svc.GetStatus());
        Assert.Equal("Complete", svc.GetStatus());
        Assert.Equal("Complete", svc.GetStatus());

        Assert.Equal("first", config.Name);
        Assert.Equal("second", config.Name);
    }
}

// =============================================================================
// Feature 7: Conditional Matching (When)
// =============================================================================

public class WhenChainsTests
{
    [Fact]
    public void WhenChains_AllApis()
    {
        var stub = new MatrixCalculatorStub();
        IMatrixCalculator calc = stub;

        #region matrix-when-chains
        // Chain multiple conditions (sequential - each consumed once)
        stub.Add
            .When(1, 2).Return(100)
            .ThenWhen(3, 4).Return(200)
            .ThenWhen((int a, int b) => a < 0).Return(0);

        // Fallback for non-matching calls or after chain is consumed
        stub.Add.Return(42);
        #endregion

        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(200, calc.Add(3, 4));
        Assert.Equal(0, calc.Add(-1, 5));
        Assert.Equal(42, calc.Add(10, 20));
    }
}

// =============================================================================
// Feature 8: Verification
// =============================================================================

public class VerificationTests
{
    [Fact]
    public void Verification_AllApis()
    {
        var stub = new MatrixServiceStub();
        IMatrixService svc = stub;

        #region matrix-verification
        // Mark for verification
        stub.GetData.Call((id) => "data").Verifiable();

        // Verify only marked items
        // stub.Verify();  // Throws if any Verifiable() not called

        // Verify all configured items
        // stub.VerifyAll();  // Throws if any configured member not called

        // Individual member verification
        // stub.GetData.Verify(Called.Once);
        #endregion

        svc.GetData(1);
        stub.Verify();
        stub.VerifyAll();
        stub.GetData.Verify(Called.Once);
    }
}

// =============================================================================
// Feature 9: Strict Mode
// =============================================================================

public class StrictModeTests
{
    [Fact]
    public void StrictMode_AllApis()
    {
        #region matrix-strict-mode
        // Enable strict mode via property
        var stub = new MatrixServiceStub();
        stub.Strict = true;
        // Or fluently
        var fluentStub = new MatrixServiceStub().Strict();
        #endregion

        Assert.True(stub.Strict);
        Assert.True(fluentStub.Strict);

        // Unconfigured method throws in strict mode
        IMatrixService svc = stub;
        Assert.Throws<StubException>(() => svc.GetData(1));
    }
}

// =============================================================================
// Feature 10: Reset
// =============================================================================

public class ResetTests
{
    [Fact]
    public void Reset_AllApis()
    {
        var stub = new MatrixServiceStub();
        IMatrixService svc = stub;

        stub.GetData.Call((id) => "data");
        svc.GetData(1);
        stub.GetData.Verify(Called.Once);

        #region matrix-reset
        // Reset individual member
        stub.GetData.Reset();
        stub.Save.Reset();
        #endregion

        stub.GetData.Verify(Called.Never);
    }
}

// =============================================================================
// Feature 11: Stub Overrides (already defined above as MatrixStubOverrideStub)
// =============================================================================

public class StubOverridesTests
{
    [Fact]
    public void StubOverrides_UsagePattern()
    {
        #region matrix-stub-overrides-interface-usage
        var stub = new MatrixStubOverrideStub();
        IMatrixCalculator calc = stub;

        // Stub override provides default behavior
        var result = calc.Add(3, 4);
        Assert.Equal(7, result);

        // Return supersedes stub override
        stub.Add.Call((int a, int b) => 999);
        var overridden = calc.Add(3, 4);
        Assert.Equal(999, overridden);
        #endregion
    }
}

// =============================================================================
// Feature 12: Async Method Auto-Wrapping
// =============================================================================

public class AsyncAutoWrapTests
{
    [Fact]
    public async Task AsyncAutoWrap_AllTiers()
    {
        var stub = new MatrixAsyncStub();
        IMatrixAsyncService svc = stub;

        #region matrix-async-autowrap
        // Given: Task<string> GetDataAsync(int id)

        // Tier 1: Returns(unwrappedValue) - auto-wraps in Task.FromResult
        stub.GetDataAsync.Return("hello");

        // Tier 2: Return(simplified callback) - returns T, auto-wrapped
        stub.GetDataAsync.Call((id) => $"Data-{id}");

        // Tier 3: Return(full delegate) - returns Task<T> directly
        stub.GetDataAsync.Call((int id) => Task.FromResult($"Full-{id}"));
        #endregion

        var result = await svc.GetDataAsync(42);
        Assert.Equal("Full-42", result);
    }
}

// =============================================================================
// Quick Reference: All Patterns Side-by-Side
// =============================================================================

#region matrix-instantiation
// Pattern 1: Standalone Interface
[KnockOff]
public partial class MatrixCalcStub : IMatrixCalculator { }
#endregion

public class InstantiationTests
{
    [Fact]
    public void StandalonePatterns_Instantiation()
    {
        #region matrix-all-patterns
        // Pattern 1: Standalone Interface
        var calcStub = new MatrixCalcStub();
        IMatrixCalculator calc = calcStub;

        // Configure and use - same API across all patterns
        calcStub.Add.Call((int a, int b) => a + b);
        var result = calc.Add(3, 4);
        Assert.Equal(7, result);

        // Verification - same API across all patterns
        calcStub.Add.Verify(Called.Once);
        Assert.Equal((3, 4), calcStub.Add.LastArgs);
        #endregion
    }
}
