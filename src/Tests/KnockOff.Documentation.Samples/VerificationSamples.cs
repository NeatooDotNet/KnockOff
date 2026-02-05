using KnockOff;

namespace KnockOff.Documentation.Samples.Verification;

// =============================================================================
// Interfaces for Verification Samples
// =============================================================================

public interface IRepoVerify
{
    User? GetById(int id);
    void Save(User user);
    void Refresh();
}

public interface ISvcVerify
{
    void Update(int id, string name);
}

public interface IConfigVerify
{
    int MaxRetries { get; set; }
    int Timeout { get; set; }
}

// =============================================================================
// Stubs for Verification Samples
// =============================================================================

[KnockOff]
public partial class RepoVerifyStub : IRepoVerify { }

[KnockOff]
public partial class SvcVerifyStub : ISvcVerify { }

[KnockOff]
public partial class ConfigVerifyStub : IConfigVerify { }

// =============================================================================
// Basic Call Verification
// =============================================================================

public class BasicCallVerificationTests
{
    [Fact]
    public void Verifiable_MarksForBatchVerification()
    {
        var stub = new RepoVerifyStub();

        #region verify-verifiable
        // Mark for batch verification, then verify all marked members
        stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

        IRepoVerify repository = stub;
        repository.GetById(42);

        stub.Verify();
        #endregion
    }

    [Fact]
    public void Verify_WithTimesOnce()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Save.OnCall((user) => { });

        IRepoVerify repository = stub;
        repository.Save(new User { Id = 1 });

        #region verify-times-once
        // Verify exactly one call
        tracking.Verify(Times.Once);
        #endregion
    }

    [Fact]
    public void Verify_WithTimesAtLeast()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall(() => { });

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        #region verify-times-atleast
        // Verify at least N calls
        tracking.Verify(Times.AtLeast(2));
        #endregion
    }

    [Fact]
    public void Verify_WithTimesNever()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall(() => { });

        IRepoVerify repository = stub;
        // Don't call Refresh

        #region verify-times-never
        // Verify method was never called
        tracking.Verify(Times.Never);
        #endregion
    }

    [Fact]
    public void Verify_WithTimesExactly()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall(() => { });

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        // Verify exactly N calls
        tracking.Verify(Times.Exactly(3));
    }

    [Fact]
    public void Verifiable_WithTimesConstraint()
    {
        var stub = new RepoVerifyStub();

        #region verify-verifiable-times
        // Mark with Times constraint for batch verification
        stub.Refresh.OnCall(() => { }).Verifiable(Times.Exactly(2));
        #endregion

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();

        stub.Verify();
    }

    [Fact]
    public void VerifyAll_ChecksAllConfiguredMembers()
    {
        var stub = new RepoVerifyStub();
        stub.GetById.OnCall((id) => new User { Id = id });
        stub.Save.OnCall((user) => { });

        IRepoVerify repository = stub;
        repository.GetById(1);
        repository.Save(new User { Id = 1 });

        #region verify-verifyall
        // VerifyAll checks all configured members were called at least once
        stub.VerifyAll();
        #endregion
    }
}

// =============================================================================
// Argument Verification
// =============================================================================

public class ArgumentVerificationTests
{
    [Fact]
    public void LastArg_VerifiesSingleParameter()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.GetById.OnCall((id) => new User { Id = id });

        IRepoVerify repository = stub;
        repository.GetById(42);

        #region verify-lastcallarg
        // LastArg contains the most recent argument value
        Assert.Equal(42, tracking.LastArg);
        #endregion
    }

    [Fact]
    public void LastArgs_VerifiesMultipleParameters()
    {
        var stub = new SvcVerifyStub();
        var tracking = stub.Update.OnCall((id, name) => { });

        ISvcVerify service = stub;
        service.Update(42, "Alice");

        #region verify-lastcallargs-tuple
        // LastArgs is a named tuple for multi-parameter methods
        var (id, name) = tracking.LastArgs;
        Assert.Equal(42, id);
        Assert.Equal("Alice", name);
        #endregion
    }
}

// =============================================================================
// Call Count Tracking
// =============================================================================

public class CallCountTests
{
    [Fact]
    public void TrackCallCount_WithCallback()
    {
        var stub = new RepoVerifyStub();

        #region verify-callcount-tracking
        // Track call count in the callback for custom assertions
        var saveCount = 0;
        stub.Save.OnCall((user) => { saveCount++; });
        #endregion

        IRepoVerify repository = stub;
        repository.Save(new User { Id = 1 });
        repository.Save(new User { Id = 2 });

        Assert.True(saveCount >= 2, "Expected at least 2 saves");
    }
}

// =============================================================================
// Call History Tracking
// =============================================================================

public class CallHistoryTests
{
    [Fact]
    public void OnCall_CapturesAllCallsToList()
    {
        var stub = new RepoVerifyStub();

        #region verify-call-history
        // Capture all calls to a list for history inspection
        var calls = new List<int>();
        stub.GetById.OnCall((id) =>
        {
            calls.Add(id);
            return new User { Id = id };
        });
        #endregion

        IRepoVerify repository = stub;
        repository.GetById(1);
        repository.GetById(2);
        repository.GetById(3);

        Assert.Equal(new[] { 1, 2, 3 }, calls);
    }
}

// =============================================================================
// Call Order Verification
// =============================================================================

public class CallOrderTests
{
    [Fact]
    public void CallOrder_VerifiedWithCounter()
    {
        var stub = new RepoVerifyStub();

        #region verify-call-order
        // Track call order with a shared counter
        var order = 0;
        var saveOrder = 0;
        var refreshOrder = 0;

        stub.Save.OnCall((user) => saveOrder = ++order);
        stub.Refresh.OnCall(() => refreshOrder = ++order);
        #endregion

        IRepoVerify repository = stub;
        repository.Save(new User { Id = 1 });
        repository.Refresh();

        Assert.True(saveOrder < refreshOrder, "Save should be called before Refresh");
    }
}

// =============================================================================
// Cross-Interceptor Verification
// =============================================================================

public class CrossInterceptorTests
{
    [Fact]
    public void CrossInterceptor_VerifyMultipleMethodsCalled()
    {
        var stub = new RepoVerifyStub();

        #region verify-cross-interceptor
        // Mark multiple methods as verifiable
        stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable();
        stub.Refresh.OnCall(() => { }).Verifiable();
        #endregion

        IRepoVerify repository = stub;
        repository.GetById(1);
        repository.Save(new User { Id = 1 });
        repository.Refresh();

        // Single Verify() checks all marked members
        stub.Verify();
    }
}

// =============================================================================
// Property Verification
// =============================================================================

public class PropertyVerificationTests
{
    [Fact]
    public void VerifyGet_ChecksPropertyReadCount()
    {
        var stub = new ConfigVerifyStub();
        stub.MaxRetries.OnGet(5);

        IConfigVerify config = stub;
        _ = config.MaxRetries;
        _ = config.MaxRetries;

        #region verify-property-get
        // VerifyGet checks how many times property was read
        stub.MaxRetries.VerifyGet(Times.Exactly(2));
        #endregion
    }

    [Fact]
    public void VerifySet_ChecksPropertyWriteAndValue()
    {
        var stub = new ConfigVerifyStub();

        IConfigVerify config = stub;
        config.Timeout = 30;

        #region verify-property-set
        // VerifySet checks property was written
        stub.Timeout.VerifySet(Times.Once);

        // LastSetValue contains the assigned value
        Assert.Equal(30, stub.Timeout.LastSetValue);
        #endregion
    }

    [Fact]
    public void Verify_ChecksTotalPropertyAccess()
    {
        var stub = new ConfigVerifyStub();
        stub.MaxRetries.OnGet(3);

        IConfigVerify config = stub;
        _ = config.MaxRetries;
        _ = config.MaxRetries;
        config.MaxRetries = 5;
        config.MaxRetries = 10;

        #region verify-property-combined
        // Verify checks combined get + set count (2 gets + 2 sets = 4)
        stub.MaxRetries.Verify(Times.Exactly(4));
        #endregion
    }
}

// =============================================================================
// Complete Verification Example
// =============================================================================

public class CompleteVerificationTests
{
    [Fact]
    public void CompleteVerification_AllTechniques()
    {
        var stub = new RepoVerifyStub();

        // Track call order and history
        var order = 0;
        var getOrder = 0;
        var saveOrder = 0;
        var refreshOrder = 0;
        var getIdHistory = new List<int>();

        // Configure with tracking and verification
        var getTracking = stub.GetById.OnCall((id) =>
        {
            getIdHistory.Add(id);
            getOrder = ++order;
            return new User { Id = id, Name = $"User{id}" };
        }).Verifiable(Times.Exactly(2));

        stub.Save.OnCall((user) => { saveOrder = ++order; }).Verifiable(Times.Once);
        stub.Refresh.OnCall(() => { refreshOrder = ++order; }).Verifiable(Times.Once);

        IRepoVerify repository = stub;
        repository.GetById(1);
        repository.GetById(2);
        repository.Save(new User { Id = 1, Name = "Updated" });
        repository.Refresh();

        #region verify-complete-example
        // 1. Batch verification - checks all Times constraints
        stub.Verify();

        // 2. Argument verification via tracking
        Assert.Equal(2, getTracking.LastArg);

        // 3. Call history verification
        Assert.Equal(new[] { 1, 2 }, getIdHistory);

        // 4. Call order verification
        Assert.True(getOrder < saveOrder, "Get before Save");
        #endregion
    }
}
