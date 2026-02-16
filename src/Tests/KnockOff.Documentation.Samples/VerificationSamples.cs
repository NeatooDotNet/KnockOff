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

public delegate int VerifyArithmeticOp(int a, int b);

// =============================================================================
// Stubs for Verification Samples
// =============================================================================

[KnockOff]
public partial class RepoVerifyStub : IRepoVerify { }

[KnockOff]
public partial class SvcVerifyStub : ISvcVerify { }

[KnockOff]
public partial class ConfigVerifyStub : IConfigVerify { }

[KnockOff<VerifyArithmeticOp>]
public partial class DelegateVerificationHost { }

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
        stub.GetById.Return((id) => new User { Id = id }).Verifiable();

        IRepoVerify repository = stub;
        repository.GetById(42);

        stub.Verify();
        #endregion
    }

    [Fact]
    public void Verify_WithTimesOnce()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Save.Call((user) => { });

        IRepoVerify repository = stub;
        repository.Save(new User { Id = 1 });

        #region verify-times-once
        // Verify exactly one call
        tracking.Verify(Called.Once);
        #endregion
    }

    [Fact]
    public void Verify_WithTimesAtLeast()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.Call(() => { });

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        #region verify-times-atleast
        // Verify at least N calls
        tracking.Verify(Called.AtLeast(2));
        #endregion
    }

    [Fact]
    public void Verify_WithTimesNever()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.Call(() => { });

        IRepoVerify repository = stub;
        // Don't call Refresh

        #region verify-times-never
        // Verify method was never called
        tracking.Verify(Called.Never);
        #endregion
    }

    [Fact]
    public void Verify_WithTimesExactly()
    {
        var stub = new RepoVerifyStub();
        var tracking = stub.Refresh.Call(() => { });

        IRepoVerify repository = stub;
        repository.Refresh();
        repository.Refresh();
        repository.Refresh();

        // Verify exactly N calls
        tracking.Verify(Called.Exactly(3));
    }

    [Fact]
    public void Verifiable_WithTimesConstraint()
    {
        var stub = new RepoVerifyStub();

        #region verify-verifiable-times
        // Mark with Times constraint for batch verification
        stub.Refresh.Call(() => { }).Verifiable(Called.Exactly(2));
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
        stub.GetById.Return((id) => new User { Id = id });
        stub.Save.Call((user) => { });

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
        var tracking = stub.GetById.Return((id) => new User { Id = id });

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
        var tracking = stub.Update.Call((id, name) => { });

        ISvcVerify service = stub;
        service.Update(42, "Alice");

        #region verify-lastcallargs-tuple
        // LastArgs is a named tuple for multi-parameter methods
        var (id, name) = tracking.LastArgs!.Value;
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
        stub.Save.Call((user) => { saveCount++; });
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
    public void Return_CapturesAllCallsToList()
    {
        var stub = new RepoVerifyStub();

        #region verify-call-history
        // Capture all calls to a list for history inspection
        var calls = new List<int>();
        stub.GetById.Return((id) =>
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

        stub.Save.Call((user) => saveOrder = ++order);
        stub.Refresh.Call(() => refreshOrder = ++order);
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
        stub.GetById.Return((id) => new User { Id = id }).Verifiable();
        stub.Save.Call((user) => { }).Verifiable();
        stub.Refresh.Call(() => { }).Verifiable();
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
        stub.MaxRetries.Get(5);

        IConfigVerify config = stub;
        _ = config.MaxRetries;
        _ = config.MaxRetries;

        #region verify-property-get
        // VerifyGet checks how many times property was read
        stub.MaxRetries.VerifyGet(Called.Exactly(2));
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
        stub.Timeout.VerifySet(Called.Once);

        // LastSetValue contains the assigned value
        Assert.Equal(30, stub.Timeout.LastSetValue);
        #endregion
    }

    [Fact]
    public void Verify_ChecksTotalPropertyAccess()
    {
        var stub = new ConfigVerifyStub();
        stub.MaxRetries.Get(3);

        IConfigVerify config = stub;
        _ = config.MaxRetries;
        _ = config.MaxRetries;
        config.MaxRetries = 5;
        config.MaxRetries = 10;

        #region verify-property-combined
        // Verify checks combined get + set count (2 gets + 2 sets = 4)
        stub.MaxRetries.Verify(Called.Exactly(4));
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
        var getTracking = stub.GetById.Return((id) =>
        {
            getIdHistory.Add(id);
            getOrder = ++order;
            return new User { Id = id, Name = $"User{id}" };
        }).Verifiable(Called.Exactly(2));

        stub.Save.Call((user) => { saveOrder = ++order; }).Verifiable(Called.Once);
        stub.Refresh.Call(() => { refreshOrder = ++order; }).Verifiable(Called.Once);

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

// =============================================================================
// Verification Reference Samples (for verification.md)
// =============================================================================

public class VerifyRefDirectTests
{
    [Fact]
    public void Verify_Direct()
    {
        var stub = new RepoVerifyStub();
        stub.GetById.Return((id) => new User { Id = id });
        stub.Refresh.Call(() => { });

        IRepoVerify repo = stub;
        repo.GetById(1);
        repo.GetById(2);

        #region verification-ref-direct
        stub.GetById.Verify();                  // At least once (default)
        stub.GetById.Verify(Called.Exactly(2)); // Exactly twice
        stub.Refresh.Verify(Called.Never);       // Never called
        #endregion
    }
}

public class VerifyRefBatchTests
{
    [Fact]
    public void Verify_Batch()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-batch
        // Step 1: Mark during setup
        stub.GetById.Return((id) => new User { Id = id }).Verifiable();
        stub.Save.Call((u) => { }).Verifiable(Called.Exactly(2));
        stub.Refresh.Call(() => { }).Verifiable(Called.Never);

        // Step 2: Exercise code
        IRepoVerify repository = stub;
        repository.GetById(1);
        repository.Save(new User { Id = 1 });
        repository.Save(new User { Id = 2 });

        // Step 3: Verify all marked interceptors
        stub.Verify();  // Checks GetById (AtLeastOnce), Save (Exactly(2)), Refresh (Never)
        #endregion
    }
}

public class VerifyRefVerifyAllTests
{
    [Fact]
    public void Verify_All()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-verifyall
        stub.GetById.Return((id) => new User { Id = id });
        stub.Save.Call((user) => { });

        IRepoVerify repo = stub;
        repo.GetById(1);
        repo.Save(new User { Id = 1 });

        stub.VerifyAll(); // Checks all configured members were called at least once
        #endregion
    }

    [Fact]
    public void Verify_All_Throws()
    {
        var stub = new RepoVerifyStub();

        stub.GetById.Return((id) => new User { Id = id });
        stub.Save.Call((user) => { });

        IRepoVerify repo = stub;
        repo.GetById(1);
        // repo.Save not called

        #region verification-ref-verifyall-throws
        // VerifyAll THROWS if any configured member was not called
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
        #endregion
    }
}

public class VerifyRefSequenceTests
{
    [Fact]
    public void Verify_Sequence()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-sequence
        var sequence = stub.GetById.Return(
            new User { Id = 1 },
            new User { Id = 2 },
            new User { Id = 3 });

        IRepoVerify repo = stub;
        repo.GetById(1);
        repo.GetById(2);
        repo.GetById(3);

        sequence.Verify(); // Passes -- all 3 consumed
        #endregion
    }
}

public class VerifyRefWhenChainTests
{
    [Fact]
    public void Verify_WhenChain()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-when-chain
        var chain = stub.GetById
            .When(1).Return(new User { Id = 1, Name = "Alice" })
            .ThenWhen(2).Return(new User { Id = 2, Name = "Bob" })
            .ThenCall((id) => new User { Id = id });

        IRepoVerify repo = stub;
        repo.GetById(1);
        repo.GetById(2);
        repo.GetById(3);

        chain.Verify(); // Passes -- all matchers consumed
        #endregion
    }
}

public class VerifyRefExceptionTests
{
    [Fact]
    public void Verify_Exception_CollectsAll()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-exception
        stub.GetById.Return((id) => new User { Id = id }).Verifiable();
        stub.Save.Call((user) => { }).Verifiable();
        stub.Refresh.Call(() => { }).Verifiable();

        IRepoVerify repo = stub;
        repo.GetById(1); // Only GetById called

        try { stub.Verify(); }
        catch (VerificationException ex)
        {
            // ex.Failures contains Save AND Refresh failures
            Assert.True(ex.Failures.Count >= 2);
        }
        #endregion
    }
}

public class VerifyRefVerifiableChainingTests
{
    [Fact]
    public void Verify_VerifiableChaining()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-verifiable-chaining
        // Chain with Return
        stub.GetById.Return((id) => new User { Id = id }).Verifiable(Called.Exactly(2));

        // Chain with Call
        stub.Save.Call((u) => { }).Verifiable(Called.Once);
        #endregion

        IRepoVerify repo = stub;
        repo.GetById(1);
        repo.GetById(2);
        repo.Save(new User { Id = 1 });

        stub.Verify();
    }
}

public class VerifyRefResetTests
{
    [Fact]
    public void Verify_ResetPreservesVerifiable()
    {
        var stub = new RepoVerifyStub();

        #region verification-ref-reset
        stub.GetById.Return((id) => new User { Id = id }).Verifiable();
        IRepoVerify repo = stub;
        repo.GetById(1);
        stub.Verify(); // Passes

        stub.GetById.Reset();
        // stub.Verify(); // Would FAIL -- count reset to 0

        repo.GetById(2);
        stub.Verify(); // Passes again
        #endregion
    }
}

// =============================================================================
// Delegate Verification
// =============================================================================

public partial class DelegateVerificationHost
{
    [Fact]
    public void DelegateVerification_Basic()
    {
        #region verify-delegate-basic
        // Create delegate stub and configure with Verifiable()
        var stub = new DelegateVerificationHost.Stubs.VerifyArithmeticOp();
        stub.Interceptor.Return((a, b) => a + b).Verifiable();

        VerifyArithmeticOp op = stub;
        op(2, 3);

        // Direct verification with Times
        stub.Interceptor.Verify(Called.Once);

        // Argument tracking via LastArgs
        Assert.Equal((2, 3), stub.Interceptor.LastArgs);

        // Verifiable pattern - checks all marked interceptors
        stub.Interceptor.Verify();
        #endregion
    }
}
