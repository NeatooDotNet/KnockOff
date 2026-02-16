using KnockOff.Interceptors;

namespace KnockOff.Tests.Interceptors;

// ============================================================================
// Test delegate types for slot extension tests
// ============================================================================

// Void family delegates
delegate void VoidSlotDelegate1(string data);
delegate void VoidSlotDelegate2(string data, int priority);

// Non-void family delegates
delegate int MethodSlotDelegate1(string data);
delegate int MethodSlotDelegate2(string data, int priority);

// Async void family delegates
delegate Task AsyncVoidSlotDelegate1(string data);
delegate Task AsyncVoidSlotDelegate2(string data, int priority);

// Sync void delegate equivalents for TSyncDelegate
delegate void SyncVoidSlotDelegate1(string data);
delegate void SyncVoidSlotDelegate2(string data, int priority);

// Async non-void family delegates
delegate Task<int> AsyncMethodSlotDelegate1(string data);
delegate Task<int> AsyncMethodSlotDelegate2(string data, int priority);

// Sync non-void delegate equivalents for TSyncDelegate
delegate int SyncMethodSlotDelegate1(string data);
delegate int SyncMethodSlotDelegate2(string data, int priority);

// ============================================================================
// Hand-written compositors implementing slot interfaces
// ============================================================================

/// <summary>Void compositor with 2 overloads.</summary>
class VoidTestCompositor
    : IVoidOverloadSlot1<VoidSlotDelegate1, string>,
      IVoidOverloadSlot2<VoidSlotDelegate2, (string data, int priority)>
{
    private readonly VoidMethodInterceptor<VoidSlotDelegate1, string> _interceptor1 = new("Process");
    private readonly VoidMethodInterceptor<VoidSlotDelegate2, (string data, int priority)> _interceptor2 = new("Process");

    VoidMethodInterceptor<VoidSlotDelegate1, string>
        IVoidOverloadSlot1<VoidSlotDelegate1, string>.VoidSlot1Interceptor => _interceptor1;
    VoidMethodInterceptor<VoidSlotDelegate2, (string data, int priority)>
        IVoidOverloadSlot2<VoidSlotDelegate2, (string data, int priority)>.VoidSlot2Interceptor => _interceptor2;

    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor1, _interceptor2 };
}

/// <summary>Non-void compositor with 2 overloads.</summary>
class MethodTestCompositor
    : IMethodOverloadSlot1<MethodSlotDelegate1, string, int>,
      IMethodOverloadSlot2<MethodSlotDelegate2, (string data, int priority), int>
{
    private readonly MethodInterceptor<MethodSlotDelegate1, string, int> _interceptor1 = new("Calculate");
    private readonly MethodInterceptor<MethodSlotDelegate2, (string data, int priority), int> _interceptor2 = new("Calculate");

    MethodInterceptor<MethodSlotDelegate1, string, int>
        IMethodOverloadSlot1<MethodSlotDelegate1, string, int>.MethodSlot1Interceptor => _interceptor1;
    MethodInterceptor<MethodSlotDelegate2, (string data, int priority), int>
        IMethodOverloadSlot2<MethodSlotDelegate2, (string data, int priority), int>.MethodSlot2Interceptor => _interceptor2;

    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor1, _interceptor2 };
}

/// <summary>Async void compositor with 2 overloads.</summary>
class AsyncVoidTestCompositor
    : IAsyncVoidOverloadSlot1<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>,
      IAsyncVoidOverloadSlot2<AsyncVoidSlotDelegate2, SyncVoidSlotDelegate2, (string data, int priority)>
{
    private readonly AsyncVoidMethodInterceptor<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string> _interceptor1 = new("ExecuteAsync");
    private readonly AsyncVoidMethodInterceptor<AsyncVoidSlotDelegate2, SyncVoidSlotDelegate2, (string data, int priority)> _interceptor2 = new("ExecuteAsync");

    AsyncVoidMethodInterceptor<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>
        IAsyncVoidOverloadSlot1<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>.AsyncVoidSlot1Interceptor => _interceptor1;
    AsyncVoidMethodInterceptor<AsyncVoidSlotDelegate2, SyncVoidSlotDelegate2, (string data, int priority)>
        IAsyncVoidOverloadSlot2<AsyncVoidSlotDelegate2, SyncVoidSlotDelegate2, (string data, int priority)>.AsyncVoidSlot2Interceptor => _interceptor2;

    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor1, _interceptor2 };
}

/// <summary>Async non-void compositor with 2 overloads.</summary>
class AsyncMethodTestCompositor
    : IAsyncMethodOverloadSlot1<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>,
      IAsyncMethodOverloadSlot2<AsyncMethodSlotDelegate2, SyncMethodSlotDelegate2, (string data, int priority), int>
{
    private readonly AsyncMethodInterceptor<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int> _interceptor1 = new("FetchAsync");
    private readonly AsyncMethodInterceptor<AsyncMethodSlotDelegate2, SyncMethodSlotDelegate2, (string data, int priority), int> _interceptor2 = new("FetchAsync");

    AsyncMethodInterceptor<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>
        IAsyncMethodOverloadSlot1<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>.AsyncMethodSlot1Interceptor => _interceptor1;
    AsyncMethodInterceptor<AsyncMethodSlotDelegate2, SyncMethodSlotDelegate2, (string data, int priority), int>
        IAsyncMethodOverloadSlot2<AsyncMethodSlotDelegate2, SyncMethodSlotDelegate2, (string data, int priority), int>.AsyncMethodSlot2Interceptor => _interceptor2;

    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor1, _interceptor2 };
}

// Mixed family delegate types
delegate void MixedVoidDelegate(string data);
delegate int MixedMethodDelegate(string data, int priority);

/// <summary>Mixed compositor with void slot 1 and non-void slot 1.</summary>
class MixedFamilyTestCompositor
    : IVoidOverloadSlot1<MixedVoidDelegate, string>,
      IMethodOverloadSlot1<MixedMethodDelegate, (string data, int priority), int>
{
    private readonly VoidMethodInterceptor<MixedVoidDelegate, string> _voidInterceptor = new("DoWork");
    private readonly MethodInterceptor<MixedMethodDelegate, (string data, int priority), int> _methodInterceptor = new("DoWork");

    VoidMethodInterceptor<MixedVoidDelegate, string>
        IVoidOverloadSlot1<MixedVoidDelegate, string>.VoidSlot1Interceptor => _voidInterceptor;
    MethodInterceptor<MixedMethodDelegate, (string data, int priority), int>
        IMethodOverloadSlot1<MixedMethodDelegate, (string data, int priority), int>.MethodSlot1Interceptor => _methodInterceptor;

    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _voidInterceptor, _methodInterceptor };
}

// Single slot delegate types
delegate void SingleSlotDelegate(string data);

/// <summary>Single-slot compositor to verify extensions work with just one interface.</summary>
class SingleSlotTestCompositor
    : IVoidOverloadSlot1<SingleSlotDelegate, string>
{
    private readonly VoidMethodInterceptor<SingleSlotDelegate, string> _interceptor = new("Process");

    VoidMethodInterceptor<SingleSlotDelegate, string>
        IVoidOverloadSlot1<SingleSlotDelegate, string>.VoidSlot1Interceptor => _interceptor;

    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor };
}

// ============================================================================
// Tests
// ============================================================================

/// <summary>
/// Tests for slot extension method overload resolution across all 4 families.
/// Verifies that extension methods on numbered slot interfaces resolve correctly
/// when a compositor implements multiple slot interfaces with different TDelegate/TArgs.
/// </summary>
public class SlotExtensionTests
{
    // ========================================================================
    // Void family
    // ========================================================================

    [Fact]
    public void VoidSlot_Call_Slot1_ResolvesCorrectly()
    {
        var compositor = new VoidTestCompositor();
        string captured = "";
        VoidSlotDelegate1 callback = (string data) => captured = data;

        // Call on the compositor with delegate matching slot 1
        compositor.Call(callback);

        // Invoke via the interceptor and verify
        ((IVoidOverloadSlot1<VoidSlotDelegate1, string>)compositor)
            .VoidSlot1Interceptor.Invoke(false, "hello");
        Assert.Equal("hello", captured);
    }

    [Fact]
    public void VoidSlot_Call_Slot2_ResolvesCorrectly()
    {
        var compositor = new VoidTestCompositor();
        string capturedData = "";
        int capturedPriority = 0;
        VoidSlotDelegate2 callback = (string data, int priority) => { capturedData = data; capturedPriority = priority; };

        // Call on the compositor with delegate matching slot 2
        compositor.Call(callback);

        // Invoke via the interceptor and verify
        ((IVoidOverloadSlot2<VoidSlotDelegate2, (string, int)>)compositor)
            .VoidSlot2Interceptor.Invoke(false, ("world", 5));
        Assert.Equal("world", capturedData);
        Assert.Equal(5, capturedPriority);
    }

    [Fact]
    public void VoidSlot_When_ExactMatch_Slot1_ResolvesCorrectly()
    {
        var compositor = new VoidTestCompositor();

        // When on the compositor with TArgs matching slot 1 (string)
        var whenBuilder = compositor.When("expected");

        // Verify it's the right builder type
        Assert.NotNull(whenBuilder);
        Assert.IsType<VoidMethodInterceptor<VoidSlotDelegate1, string>.VoidWhenBuilder>(whenBuilder);
    }

    [Fact]
    public void VoidSlot_When_ExactMatch_Slot2_ResolvesCorrectly()
    {
        var compositor = new VoidTestCompositor();

        // When on the compositor with TArgs matching slot 2 (tuple)
        var whenBuilder = compositor.When(("expected", 42));

        Assert.NotNull(whenBuilder);
        Assert.IsType<VoidMethodInterceptor<VoidSlotDelegate2, (string, int)>.VoidWhenBuilder>(whenBuilder);
    }

    [Fact]
    public void VoidSlot_When_Predicate_Slot1_ResolvesCorrectly()
    {
        var compositor = new VoidTestCompositor();

        // When with predicate on slot 1
        var whenBuilder = compositor.When((string s) => s.StartsWith("test"));

        Assert.NotNull(whenBuilder);
        Assert.IsType<VoidMethodInterceptor<VoidSlotDelegate1, string>.VoidWhenBuilder>(whenBuilder);
    }

    // ========================================================================
    // Non-void (Method) family
    // ========================================================================

    [Fact]
    public void MethodSlot_Return_Callback_Slot1_ResolvesCorrectly()
    {
        var compositor = new MethodTestCompositor();
        MethodSlotDelegate1 callback = (string data) => data.Length;

        // Return on the compositor with delegate matching slot 1
        var builder = compositor.Return(callback);

        // Invoke and verify
        var result = ((IMethodOverloadSlot1<MethodSlotDelegate1, string, int>)compositor)
            .MethodSlot1Interceptor.Invoke(false, "hello");
        Assert.Equal(5, result);
    }

    [Fact]
    public void MethodSlot_Return_Callback_Slot2_ResolvesCorrectly()
    {
        var compositor = new MethodTestCompositor();
        MethodSlotDelegate2 callback = (string data, int priority) => data.Length + priority;

        // Return on the compositor with delegate matching slot 2
        var builder = compositor.Return(callback);

        var result = ((IMethodOverloadSlot2<MethodSlotDelegate2, (string, int), int>)compositor)
            .MethodSlot2Interceptor.Invoke(false, ("hello", 10));
        Assert.Equal(15, result);
    }

    [Fact]
    public void MethodSlot_Return_Value_Slot1_ResolvesCorrectly()
    {
        // For Return(TReturn), the compiler needs to resolve which slot based on the
        // `this` parameter. Since the compositor implements multiple IMethodOverloadSlot
        // interfaces, Return(value) is ambiguous when called on the compositor directly.
        // This is expected -- value Return goes through the interceptor directly.
        // The extension method works when cast to a specific slot.
        var compositor = new MethodTestCompositor();
        var slot1 = (IMethodOverloadSlot1<MethodSlotDelegate1, string, int>)compositor;
        slot1.Return(42);

        var result = slot1.MethodSlot1Interceptor.Invoke(false, "anything");
        Assert.Equal(42, result);
    }

    [Fact]
    public void MethodSlot_When_ExactMatch_Slot1_ResolvesCorrectly()
    {
        var compositor = new MethodTestCompositor();

        // When on slot 1 (TArgs = string)
        var whenBuilder = compositor.When("expected");

        Assert.NotNull(whenBuilder);
        Assert.IsType<MethodInterceptor<MethodSlotDelegate1, string, int>.WhenBuilder>(whenBuilder);
    }

    [Fact]
    public void MethodSlot_When_ExactMatch_Slot2_ResolvesCorrectly()
    {
        var compositor = new MethodTestCompositor();

        // When on slot 2 (TArgs = tuple)
        var whenBuilder = compositor.When(("expected", 10));

        Assert.NotNull(whenBuilder);
        Assert.IsType<MethodInterceptor<MethodSlotDelegate2, (string, int), int>.WhenBuilder>(whenBuilder);
    }

    [Fact]
    public void MethodSlot_When_Return_EndToEnd()
    {
        var compositor = new MethodTestCompositor();

        // When("specific").Return(100)
        compositor.When("specific").Return(100);

        var interceptor = ((IMethodOverloadSlot1<MethodSlotDelegate1, string, int>)compositor)
            .MethodSlot1Interceptor;
        var result = interceptor.Invoke(false, "specific");
        Assert.Equal(100, result);
    }

    // ========================================================================
    // Async void family
    // ========================================================================

    [Fact]
    public async Task AsyncVoidSlot_Call_Delegate_Slot1_ResolvesCorrectly()
    {
        var compositor = new AsyncVoidTestCompositor();
        string captured = "";
        AsyncVoidSlotDelegate1 callback = async (string data) =>
        {
            await Task.CompletedTask;
            captured = data;
        };

        // Call on slot 1 via extension
        compositor.Call(callback);

        // Invoke
        await ((IAsyncVoidOverloadSlot1<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>)compositor)
            .AsyncVoidSlot1Interceptor.Invoke(false, "async-hello");
        Assert.Equal("async-hello", captured);
    }

    [Fact]
    public async Task AsyncVoidSlot_Call_SyncAction_Slot1_ResolvesCorrectly()
    {
        var compositor = new AsyncVoidTestCompositor();
        string captured = "";

        // Call with simplified sync Action<TArgs>
        compositor.Call((string data) => captured = data);

        await ((IAsyncVoidOverloadSlot1<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>)compositor)
            .AsyncVoidSlot1Interceptor.Invoke(false, "sync-action");
        Assert.Equal("sync-action", captured);
    }

    [Fact]
    public async Task AsyncVoidSlot_Call_Slot2_ResolvesCorrectly()
    {
        var compositor = new AsyncVoidTestCompositor();
        string capturedData = "";
        int capturedPriority = 0;
        AsyncVoidSlotDelegate2 callback = async (string data, int priority) =>
        {
            await Task.CompletedTask;
            capturedData = data;
            capturedPriority = priority;
        };

        compositor.Call(callback);

        await ((IAsyncVoidOverloadSlot2<AsyncVoidSlotDelegate2, SyncVoidSlotDelegate2, (string, int)>)compositor)
            .AsyncVoidSlot2Interceptor.Invoke(false, ("world", 7));
        Assert.Equal("world", capturedData);
        Assert.Equal(7, capturedPriority);
    }

    [Fact]
    public void AsyncVoidSlot_When_ExactMatch_Slot1_ResolvesCorrectly()
    {
        var compositor = new AsyncVoidTestCompositor();

        var whenBuilder = compositor.When("async-expected");

        Assert.NotNull(whenBuilder);
        Assert.IsType<AsyncVoidMethodInterceptor<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>.VoidWhenBuilder>(whenBuilder);
    }

    // ========================================================================
    // Async non-void (Method) family
    // ========================================================================

    [Fact]
    public async Task AsyncMethodSlot_Return_Delegate_Slot1_ResolvesCorrectly()
    {
        var compositor = new AsyncMethodTestCompositor();
        AsyncMethodSlotDelegate1 callback = async (string data) =>
        {
            await Task.CompletedTask;
            return data.Length;
        };

        compositor.Return(callback);

        var result = await ((IAsyncMethodOverloadSlot1<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>)compositor)
            .AsyncMethodSlot1Interceptor.Invoke(false, "hello");
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task AsyncMethodSlot_Return_SyncCallback_Slot1_ResolvesCorrectly()
    {
        var compositor = new AsyncMethodTestCompositor();

        // Return with simplified sync Func<TArgs, TReturn>
        compositor.Return((string data) => data.Length * 2);

        var result = await ((IAsyncMethodOverloadSlot1<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>)compositor)
            .AsyncMethodSlot1Interceptor.Invoke(false, "hello");
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task AsyncMethodSlot_Return_Delegate_Slot2_ResolvesCorrectly()
    {
        var compositor = new AsyncMethodTestCompositor();
        AsyncMethodSlotDelegate2 callback = async (string data, int priority) =>
        {
            await Task.CompletedTask;
            return data.Length + priority;
        };

        compositor.Return(callback);

        var result = await ((IAsyncMethodOverloadSlot2<AsyncMethodSlotDelegate2, SyncMethodSlotDelegate2, (string, int), int>)compositor)
            .AsyncMethodSlot2Interceptor.Invoke(false, ("hello", 10));
        Assert.Equal(15, result);
    }

    [Fact]
    public async Task AsyncMethodSlot_Return_Value_ViaSlotCast()
    {
        var compositor = new AsyncMethodTestCompositor();
        var slot1 = (IAsyncMethodOverloadSlot1<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>)compositor;
        slot1.Return(99);

        var result = await slot1.AsyncMethodSlot1Interceptor.Invoke(false, "anything");
        Assert.Equal(99, result);
    }

    [Fact]
    public void AsyncMethodSlot_When_ExactMatch_Slot1_ResolvesCorrectly()
    {
        var compositor = new AsyncMethodTestCompositor();

        var whenBuilder = compositor.When("async-expected");

        Assert.NotNull(whenBuilder);
        Assert.IsType<AsyncMethodInterceptor<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>.WhenBuilder>(whenBuilder);
    }

    [Fact]
    public void AsyncMethodSlot_When_ExactMatch_Slot2_ResolvesCorrectly()
    {
        var compositor = new AsyncMethodTestCompositor();

        var whenBuilder = compositor.When(("expected", 5));

        Assert.NotNull(whenBuilder);
        Assert.IsType<AsyncMethodInterceptor<AsyncMethodSlotDelegate2, SyncMethodSlotDelegate2, (string, int), int>.WhenBuilder>(whenBuilder);
    }

    [Fact]
    public async Task AsyncMethodSlot_When_Return_EndToEnd()
    {
        var compositor = new AsyncMethodTestCompositor();

        compositor.When("specific").Return(200);

        var interceptor = ((IAsyncMethodOverloadSlot1<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>)compositor)
            .AsyncMethodSlot1Interceptor;
        var result = await interceptor.Invoke(false, "specific");
        Assert.Equal(200, result);
    }

    // ========================================================================
    // Mixed family compositor
    // ========================================================================

    [Fact]
    public void MixedFamily_Call_ResolvesToVoidSlot()
    {
        var compositor = new MixedFamilyTestCompositor();
        string captured = "";
        MixedVoidDelegate callback = (string data) => captured = data;

        // Call resolves to void slot 1
        compositor.Call(callback);

        ((IVoidOverloadSlot1<MixedVoidDelegate, string>)compositor)
            .VoidSlot1Interceptor.Invoke(false, "mixed-void");
        Assert.Equal("mixed-void", captured);
    }

    [Fact]
    public void MixedFamily_Return_ResolvesToMethodSlot()
    {
        var compositor = new MixedFamilyTestCompositor();
        MixedMethodDelegate callback = (string data, int priority) => data.Length + priority;

        // Return resolves to method slot 1
        compositor.Return(callback);

        var result = ((IMethodOverloadSlot1<MixedMethodDelegate, (string, int), int>)compositor)
            .MethodSlot1Interceptor.Invoke(false, ("hello", 10));
        Assert.Equal(15, result);
    }

    [Fact]
    public void MixedFamily_CallAndReturn_ResolveIndependently()
    {
        var compositor = new MixedFamilyTestCompositor();
        string captured = "";
        MixedVoidDelegate voidCallback = (string data) => captured = data;
        MixedMethodDelegate methodCallback = (string data, int priority) => 42;

        // Both should resolve without ambiguity
        compositor.Call(voidCallback);
        compositor.Return(methodCallback);

        ((IVoidOverloadSlot1<MixedVoidDelegate, string>)compositor)
            .VoidSlot1Interceptor.Invoke(false, "test");
        Assert.Equal("test", captured);

        var result = ((IMethodOverloadSlot1<MixedMethodDelegate, (string, int), int>)compositor)
            .MethodSlot1Interceptor.Invoke(false, ("data", 1));
        Assert.Equal(42, result);
    }

    // ========================================================================
    // IInterceptor collection: VerifyAll and ResetAll
    // ========================================================================

    [Fact]
    public void VerifyAll_NoConfiguredInterceptors_DoesNotThrow()
    {
        var compositor = new VoidTestCompositor();

        // None configured, VerifyAll should not throw (CheckVerificationAll returns null for unconfigured)
        compositor.Interceptors.VerifyAll();
    }

    [Fact]
    public void VerifyAll_ConfiguredAndCalled_DoesNotThrow()
    {
        var compositor = new MethodTestCompositor();
        MethodSlotDelegate1 callback = (string data) => data.Length;
        compositor.Return(callback);

        // Invoke so it's been called
        ((IMethodOverloadSlot1<MethodSlotDelegate1, string, int>)compositor)
            .MethodSlot1Interceptor.Invoke(false, "hello");

        // VerifyAll should pass since configured + called
        compositor.Interceptors.VerifyAll();
    }

    [Fact]
    public void VerifyAll_ConfiguredButNotCalled_Throws()
    {
        var compositor = new MethodTestCompositor();
        MethodSlotDelegate1 callback = (string data) => data.Length;
        compositor.Return(callback);

        // Configured but not called -- VerifyAll should throw
        Assert.Throws<VerificationException>(() => compositor.Interceptors.VerifyAll());
    }

    [Fact]
    public void ResetAll_ResetsAllInterceptors()
    {
        var compositor = new VoidTestCompositor();
        VoidSlotDelegate1 callback1 = (string data) => { };
        compositor.Call(callback1);

        // Invoke so there's tracking
        ((IVoidOverloadSlot1<VoidSlotDelegate1, string>)compositor)
            .VoidSlot1Interceptor.Invoke(false, "test");
        Assert.Equal(1, ((IVoidOverloadSlot1<VoidSlotDelegate1, string>)compositor)
            .VoidSlot1Interceptor.TotalCallCount);

        // ResetAll
        compositor.Interceptors.ResetAll();

        Assert.Equal(0, ((IVoidOverloadSlot1<VoidSlotDelegate1, string>)compositor)
            .VoidSlot1Interceptor.TotalCallCount);
    }

    [Fact]
    public void ResetAll_MultipleFamilies_ResetsAll()
    {
        var compositor = new MixedFamilyTestCompositor();
        MixedVoidDelegate voidCallback = (string data) => { };
        MixedMethodDelegate methodCallback = (string data, int priority) => 1;

        compositor.Call(voidCallback);
        compositor.Return(methodCallback);

        // Invoke both
        ((IVoidOverloadSlot1<MixedVoidDelegate, string>)compositor)
            .VoidSlot1Interceptor.Invoke(false, "test");
        ((IMethodOverloadSlot1<MixedMethodDelegate, (string, int), int>)compositor)
            .MethodSlot1Interceptor.Invoke(false, ("test", 1));

        Assert.Equal(1, ((IVoidOverloadSlot1<MixedVoidDelegate, string>)compositor)
            .VoidSlot1Interceptor.TotalCallCount);
        Assert.Equal(1, ((IMethodOverloadSlot1<MixedMethodDelegate, (string, int), int>)compositor)
            .MethodSlot1Interceptor.TotalCallCount);

        compositor.Interceptors.ResetAll();

        Assert.Equal(0, ((IVoidOverloadSlot1<MixedVoidDelegate, string>)compositor)
            .VoidSlot1Interceptor.TotalCallCount);
        Assert.Equal(0, ((IMethodOverloadSlot1<MixedMethodDelegate, (string, int), int>)compositor)
            .MethodSlot1Interceptor.TotalCallCount);
    }

    // ========================================================================
    // Single slot compositor
    // ========================================================================

    [Fact]
    public void SingleSlot_Call_ResolvesCorrectly()
    {
        var compositor = new SingleSlotTestCompositor();
        string captured = "";
        SingleSlotDelegate callback = (string data) => captured = data;

        compositor.Call(callback);

        ((IVoidOverloadSlot1<SingleSlotDelegate, string>)compositor)
            .VoidSlot1Interceptor.Invoke(false, "single");
        Assert.Equal("single", captured);
    }

    [Fact]
    public void SingleSlot_When_ResolvesCorrectly()
    {
        var compositor = new SingleSlotTestCompositor();

        var whenBuilder = compositor.When("expected");

        Assert.NotNull(whenBuilder);
        Assert.IsType<VoidMethodInterceptor<SingleSlotDelegate, string>.VoidWhenBuilder>(whenBuilder);
    }

    [Fact]
    public void SingleSlot_VerifyAll_Works()
    {
        var compositor = new SingleSlotTestCompositor();

        // Not configured, should pass
        compositor.Interceptors.VerifyAll();
    }

    // ========================================================================
    // IInterceptor interface verification
    // ========================================================================

    [Fact]
    public void AllTTupleTypes_ImplementIInterceptor()
    {
        // Verify TTuple types implement IInterceptor
        IInterceptor methodInterceptor = new MethodInterceptor<MethodSlotDelegate1, string, int>("test");
        IInterceptor voidInterceptor = new VoidMethodInterceptor<VoidSlotDelegate1, string>("test");
        IInterceptor asyncMethodInterceptor = new AsyncMethodInterceptor<AsyncMethodSlotDelegate1, SyncMethodSlotDelegate1, string, int>("test");
        IInterceptor asyncVoidInterceptor = new AsyncVoidMethodInterceptor<AsyncVoidSlotDelegate1, SyncVoidSlotDelegate1, string>("test");

        // Verify zero-param types implement IInterceptor
        IInterceptor methodInterceptor0 = new MethodInterceptor0<int>("test");
        IInterceptor voidInterceptor0 = new VoidMethodInterceptor0("test");
        IInterceptor asyncMethodInterceptor0 = new AsyncMethodInterceptor0<int>("test");
        IInterceptor asyncVoidInterceptor0 = new AsyncVoidMethodInterceptor0("test");

        // All should have CheckVerification, CheckVerificationAll, Reset
        Assert.Null(methodInterceptor.CheckVerification());
        Assert.Null(voidInterceptor.CheckVerificationAll());
        asyncMethodInterceptor.Reset();
        asyncVoidInterceptor.Reset();
        Assert.Null(methodInterceptor0.CheckVerification());
        Assert.Null(voidInterceptor0.CheckVerificationAll());
        asyncMethodInterceptor0.Reset();
        asyncVoidInterceptor0.Reset();
    }

    [Fact]
    public void IInterceptor_CheckVerification_ReturnsFailure_WhenMarkedVerifiable()
    {
        var interceptor = new VoidMethodInterceptor<VoidSlotDelegate1, string>("Process");
        interceptor.Call((string s) => { });
        interceptor.Verifiable();

        IInterceptor iInterceptor = interceptor;
        var failure = iInterceptor.CheckVerification();

        // Configured + verifiable but not called -> failure
        Assert.NotNull(failure);
    }

    [Fact]
    public void IInterceptor_CheckVerificationAll_ReturnsFailure_WhenConfiguredButNotCalled()
    {
        var interceptor = new MethodInterceptor<MethodSlotDelegate1, string, int>("Calculate");
        MethodSlotDelegate1 callback = (string data) => data.Length;
        interceptor.Return(callback);

        IInterceptor iInterceptor = interceptor;
        var failure = iInterceptor.CheckVerificationAll();

        // Configured but not called -> failure
        Assert.NotNull(failure);
    }

    [Fact]
    public void IInterceptor_Reset_ClearsTracking()
    {
        var interceptor = new VoidMethodInterceptor<VoidSlotDelegate1, string>("Process");
        interceptor.Call((string s) => { });
        interceptor.Invoke(false, "test");
        Assert.Equal(1, interceptor.TotalCallCount);

        IInterceptor iInterceptor = interceptor;
        iInterceptor.Reset();

        Assert.Equal(0, interceptor.TotalCallCount);
    }
}
