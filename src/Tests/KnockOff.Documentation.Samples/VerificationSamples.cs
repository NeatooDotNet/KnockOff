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

// =============================================================================
// Stubs for Verification Samples
// =============================================================================

[KnockOff]
public partial class RepoVerifyStub : IRepoVerify { }

[KnockOff]
public partial class SvcVerifyStub : ISvcVerify { }

// =============================================================================
// Basic Call Verification
// =============================================================================

public class BasicCallVerificationTests
{
    #region verify-verifiable
    [Fact]
    public void Verifiable_MarksForBatchVerification()
    {
        var stub = new RepoVerifyStub();

        // Chain .Verifiable() to mark for batch verification
        stub.GetById.OnCall((ko, id) => new User { Id = id }).Verifiable();

        IRepoVerify repository = stub;
        repository.GetById(42);

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
    }
    #endregion

    #region verify-times-once
    [Fact]
    public void Verify_WithTimesOnce()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Save.OnCall((ko, user) => { });

        IRepoVerify repository = stub;
        repository.Save(new User { Id = 1 });

        // Verify exactly one call using Times.Once
        tracking.Verify(Times.Once);
    }
    #endregion

    #region verify-times-atleast
    [Fact]
    public void Verify_WithTimesAtLeast()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall((ko) => { });

        IRepoVerify repository = stub;

        // Simulate multiple refreshes
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        // Verify at least 2 calls
        tracking.Verify(Times.AtLeast(2));
    }
    #endregion

    #region verify-times-never
    [Fact]
    public void Verify_WithTimesNever()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall((ko) => { });

        IRepoVerify repository = stub;
        // Don't call Refresh

        // Verify method was never called via tracking
        tracking.Verify(Times.Never);
    }
    #endregion

    #region verify-times-exactly
    [Fact]
    public void Verify_WithTimesExactly()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall((ko) => { });

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        // Verify exactly 3 calls
        tracking.Verify(Times.Exactly(3));
    }
    #endregion

    #region verify-verifiable-times
    [Fact]
    public void Verifiable_WithTimesConstraint()
    {
        var stub = new RepoVerifyStub();

        // Mark with Times constraint for batch verification
        stub.Refresh.OnCall((ko) => { }).Verifiable(Times.Exactly(2));

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();

        // Verify() respects the Times constraint
        stub.Verify();
    }
    #endregion

    #region verify-verifyall
    [Fact]
    public void VerifyAll_ChecksAllConfiguredMembers()
    {
        var stub = new RepoVerifyStub();

        // Configure multiple members (no need to mark Verifiable)
        stub.GetById.OnCall((ko, id) => new User { Id = id });
        stub.Save.OnCall((ko, user) => { });

        IRepoVerify repository = stub;
        repository.GetById(1);
        repository.Save(new User { Id = 1 });

        // VerifyAll() checks all configured members were called at least once
        stub.VerifyAll();
    }
    #endregion
}

// =============================================================================
// Argument Verification
// =============================================================================

public class ArgumentVerificationTests
{
    #region verify-lastcallarg
    [Fact]
    public void LastArg_VerifiesSingleParameter()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.GetById.OnCall((ko, id) => new User { Id = id });

        IRepoVerify repository = stub;
        repository.GetById(42);

        // Verify the parameter value via tracking
        Assert.Equal(42, tracking.LastArg);
    }
    #endregion

    #region verify-lastcallargs-tuple
    [Fact]
    public void LastArgs_VerifiesMultipleParameters()
    {
        var stub = new SvcVerifyStub();
        var tracking = stub.Update.OnCall((ko, id, name) => { });

        ISvcVerify service = stub;
        service.Update(42, "Alice");

        // Destructure the named tuple for verification
        var (id, name) = tracking.LastArgs;
        Assert.Equal(42, id);
        Assert.Equal("Alice", name);
    }
    #endregion
}

// =============================================================================
// Call History Tracking
// =============================================================================

public class CallHistoryTests
{
    #region verify-call-history
    [Fact]
    public void OnCall_CapturesAllCallsToList()
    {
        var stub = new RepoVerifyStub();

        // Capture all calls to a list within the callback
        var calls = new List<int>();
        var tracking = stub.GetById.OnCall((ko, id) =>
        {
            calls.Add(id);
            return new User { Id = id };
        });

        IRepoVerify repository = stub;

        repository.GetById(1);
        repository.GetById(2);
        repository.GetById(3);

        // Verify the complete call history
        Assert.Equal(new[] { 1, 2, 3 }, calls);
    }
    #endregion
}

// =============================================================================
// Call Order Verification
// =============================================================================

public class CallOrderTests
{
    #region verify-call-order
    [Fact]
    public void CallOrder_VerifiedWithCounter()
    {
        var stub = new RepoVerifyStub();

        var order = 0;
        var saveOrder = 0;
        var refreshOrder = 0;

        var saveTracking = stub.Save.OnCall((ko, user) => saveOrder = ++order);
        var refreshTracking = stub.Refresh.OnCall((ko) => refreshOrder = ++order);

        IRepoVerify repository = stub;

        // Execute operations
        repository.Save(new User { Id = 1 });
        repository.Refresh();

        // Verify Save was called before Refresh
        Assert.True(saveOrder < refreshOrder, "Save should be called before Refresh");
    }
    #endregion
}

// =============================================================================
// Cross-Interceptor Verification
// =============================================================================

public class CrossInterceptorTests
{
    #region verify-cross-interceptor
    [Fact]
    public void CrossInterceptor_VerifyMultipleMethodsCalled()
    {
        var stub = new RepoVerifyStub();

        // Mark all methods as verifiable
        stub.GetById.OnCall((ko, id) => new User { Id = id }).Verifiable();
        stub.Save.OnCall((ko, user) => { }).Verifiable();
        stub.Refresh.OnCall((ko) => { }).Verifiable();

        IRepoVerify repository = stub;

        // Execute operations
        repository.GetById(1);
        repository.Save(new User { Id = 1 });
        repository.Refresh();

        // Single Verify() checks all marked members
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Complete Verification Example
// =============================================================================

public class CompleteVerificationTests
{
    #region verify-complete-example
    [Fact]
    public void CompleteVerification_AllTechniques()
    {
        var stub = new RepoVerifyStub();

        // Track call order
        var order = 0;
        var getOrder = 0;
        var saveOrder = 0;
        var refreshOrder = 0;

        // Track call history
        var getIdHistory = new List<int>();

        // Mark all methods as verifiable with specific constraints
        var getTracking = stub.GetById.OnCall((ko, id) =>
        {
            getIdHistory.Add(id);
            getOrder = ++order;
            return new User { Id = id, Name = $"User{id}" };
        }).Verifiable(Times.Exactly(2));

        var saveTracking = stub.Save.OnCall((ko, user) =>
        {
            saveOrder = ++order;
        }).Verifiable(Times.Once);

        var refreshTracking = stub.Refresh.OnCall((ko) =>
        {
            refreshOrder = ++order;
        }).Verifiable(Times.Once);

        IRepoVerify repository = stub;

        // Execute operations
        repository.GetById(1);
        repository.GetById(2);
        repository.Save(new User { Id = 1, Name = "Updated" });
        repository.Refresh();

        // 1. Batch verification - checks all Times constraints
        stub.Verify();

        // 2. Argument verification
        Assert.Equal(2, getTracking.LastArg); // Last call was GetById(2)

        // 3. Call history verification
        Assert.Equal(new[] { 1, 2 }, getIdHistory);

        // 4. Call order verification
        Assert.True(getOrder < saveOrder, "Get before Save");
        Assert.True(saveOrder < refreshOrder, "Save before Refresh");
    }
    #endregion
}
