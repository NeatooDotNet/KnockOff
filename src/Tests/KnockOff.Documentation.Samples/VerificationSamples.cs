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
    #region verify-wascalled
    [Fact]
    public void WasCalled_VerifiesMethodInvoked()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.GetById.OnCall((ko, id) => new User { Id = id });

        IRepoVerify repository = stub;
        repository.GetById(42);

        // WasCalled is true if invoked at least once
        Assert.True(tracking.WasCalled);
    }
    #endregion

    #region verify-callcount-exact
    [Fact]
    public void CallCount_VerifiesExactNumber()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Save.OnCall((ko, user) => { });

        IRepoVerify repository = stub;
        repository.Save(new User { Id = 1 });

        // Verify exactly one call via tracking
        Assert.Equal(1, tracking.CallCount);
    }
    #endregion

    #region verify-callcount-range
    [Fact]
    public void CallCount_VerifiesRange()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.OnCall((ko) => { });

        IRepoVerify repository = stub;

        // Simulate multiple refreshes
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        // Verify at least 2 calls
        Assert.True(tracking.CallCount >= 2);
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

        var getTracking = stub.GetById.OnCall((ko, id) => new User { Id = id });
        var saveTracking = stub.Save.OnCall((ko, user) => { });
        var refreshTracking = stub.Refresh.OnCall((ko) => { });

        IRepoVerify repository = stub;

        // Execute operations
        repository.GetById(1);
        repository.Save(new User { Id = 1 });
        repository.Refresh();

        // Verify all methods were called
        Assert.True(getTracking.WasCalled);
        Assert.True(saveTracking.WasCalled);
        Assert.True(refreshTracking.WasCalled);

        // Verify total interactions
        Assert.Equal(1, getTracking.CallCount);
        Assert.Equal(1, saveTracking.CallCount);
        Assert.Equal(1, refreshTracking.CallCount);
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

        var getTracking = stub.GetById.OnCall((ko, id) =>
        {
            getIdHistory.Add(id);
            getOrder = ++order;
            return new User { Id = id, Name = $"User{id}" };
        });

        var saveTracking = stub.Save.OnCall((ko, user) =>
        {
            saveOrder = ++order;
        });

        var refreshTracking = stub.Refresh.OnCall((ko) =>
        {
            refreshOrder = ++order;
        });

        IRepoVerify repository = stub;

        // Execute operations
        repository.GetById(1);
        repository.GetById(2);
        repository.Save(new User { Id = 1, Name = "Updated" });
        repository.Refresh();

        // 1. Basic call verification
        Assert.True(getTracking.WasCalled);
        Assert.True(saveTracking.WasCalled);
        Assert.True(refreshTracking.WasCalled);

        // 2. Call count verification
        Assert.Equal(2, getTracking.CallCount);
        Assert.Equal(1, saveTracking.CallCount);
        Assert.Equal(1, refreshTracking.CallCount);

        // 3. Argument verification
        Assert.Equal(2, getTracking.LastArg); // Last call was GetById(2)

        // 4. Call history verification
        Assert.Equal(new[] { 1, 2 }, getIdHistory);

        // 5. Call order verification
        Assert.True(getOrder < saveOrder, "Get before Save");
        Assert.True(saveOrder < refreshOrder, "Save before Refresh");
    }
    #endregion
}
