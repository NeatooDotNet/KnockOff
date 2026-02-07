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

// User methods stub for Feature 11
[KnockOff]
public partial class MatrixUserMethodStub : IMatrixCalculator { }

#region matrix-user-methods-interface
public partial class MatrixUserMethodStub
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
        stub.GetData.Return((id) => $"Data-{id}");

        // Verify calls
        stub.GetData.Verify(Times.Never);
        #endregion

        svc.GetData(1);
        svc.GetData(2);

        stub.GetData.Verify(Times.Exactly(2));

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
        stub.Name.OnGet("test-name");

        // Configure setter
        stub.Name.OnSet((value) => { /* capture or validate */ });

        // Verify
        stub.Name.VerifyGet(Times.Never);
        stub.Name.VerifySet(Times.Never);

        // Access history
        // var lastSet = stub.Name.LastSetValue;
        #endregion

        _ = config.Name;
        config.Name = "updated";

        stub.Name.VerifyGet(Times.Once);
        stub.Name.VerifySet(Times.Once);
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
        // Configure getter
        stub.Indexer.OnGet((key) => $"value-{key}");

        // Configure setter
        stub.Indexer.OnSet((key, value) => { });

        // Use backing dictionary
        stub.Indexer.Backing["preloaded"] = "data";

        // Verify
        stub.Indexer.VerifyGet(Times.Never);
        stub.Indexer.VerifySet(Times.Never);

        // Access history
        // var lastKey = stub.Indexer.LastGetKey;
        // var lastEntry = stub.Indexer.LastSetEntry;
        #endregion

        _ = cache["test"];
        cache["key"] = "val";

        stub.Indexer.VerifyGet(Times.Once);
        stub.Indexer.VerifySet(Times.Once);
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
        stub.DataReceived.VerifyAdd(Times.Never);
        stub.DataReceived.VerifyRemove(Times.Never);
        #endregion

        Assert.False(hasSubscribers);

        DataEventArgs? received = null;
        pub.DataReceived += (s, e) => received = e;
        stub.DataReceived.VerifyAdd(Times.Once);

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
            .Return(() => "Pending")
            .ThenReturn(() => "Processing")
            .ThenReturn(() => "Complete");
        // Call 1: "Pending", Call 2: "Processing", Call 3+: "Complete" (repeats last)

        // Properties support sequences too
        configStub.Name
            .OnGet("first")
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
            .ThenWhen((a, b) => a < 0).Return(0);

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
        stub.GetData.Return((id) => "data").Verifiable();

        // Verify only marked items
        // stub.Verify();  // Throws if any Verifiable() not called

        // Verify all configured items
        // stub.VerifyAll();  // Throws if any configured member not called

        // Individual member verification
        // stub.GetData.Verify(Times.Once);
        #endregion

        svc.GetData(1);
        stub.Verify();
        stub.VerifyAll();
        stub.GetData.Verify(Times.Once);
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

        stub.GetData.Return((id) => "data");
        svc.GetData(1);
        stub.GetData.Verify(Times.Once);

        #region matrix-reset
        // Reset individual member
        stub.GetData.Reset();
        stub.Save.Reset();
        #endregion

        stub.GetData.Verify(Times.Never);
    }
}

// =============================================================================
// Feature 11: User Methods (already defined above as MatrixUserMethodStub)
// =============================================================================

public class UserMethodsTests
{
    [Fact]
    public void UserMethods_UsagePattern()
    {
        #region matrix-user-methods-interface-usage
        var stub = new MatrixUserMethodStub();
        IMatrixCalculator calc = stub;

        // User method provides default behavior
        var result = calc.Add(3, 4);
        Assert.Equal(7, result);

        // Return supersedes user method
        stub.Add.Return((a, b) => 999);
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
        stub.GetDataAsync.Return((id) => $"Data-{id}");

        // Tier 3: Return(full delegate) - returns Task<T> directly
        stub.GetDataAsync.Return((int id) => Task.FromResult($"Full-{id}"));
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
        calcStub.Add.Return((a, b) => a + b);
        var result = calc.Add(3, 4);
        Assert.Equal(7, result);

        // Verification - same API across all patterns
        calcStub.Add.Verify(Times.Once);
        Assert.Equal((3, 4), calcStub.Add.LastArgs);
        #endregion
    }
}
