using KnockOff;
using Prototype.Stubs.Interfaces;
using Prototype.Stubs.Refactored;

namespace Prototype.Tests;

public class BasicUserMethodTests
{
    // ========================================================================
    // Process (non-void, 1-param): Return(value), Return(callback), sequences, When, verification
    // ========================================================================

    [Fact]
    public void Process_ReturnValue_ReturnsConfiguredValue()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("hello");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("hello", svc.Process("anything"));
    }

    [Fact]
    public void Process_ReturnCallback_InvokesCallback()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return(new BasicUserMethodStub.ProcessInterceptor.ProcessDelegate(input => input.ToUpper()));
        var svc = (IStubOverrideService)stub;
        Assert.Equal("TEST", svc.Process("test"));
    }

    [Fact]
    public void Process_Sequence_ReturnsValuesInOrder()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("first", "second", "third");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("first", svc.Process("a"));
        Assert.Equal("second", svc.Process("b"));
        Assert.Equal("third", svc.Process("c"));
        // Last repeats
        Assert.Equal("third", svc.Process("d"));
    }

    [Fact]
    public void Process_ThenReturn_CreatesSequence()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("first").ThenReturn("second").ThenReturn("third");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("first", svc.Process("a"));
        Assert.Equal("second", svc.Process("b"));
        Assert.Equal("third", svc.Process("c"));
    }

    [Fact]
    public void Process_When_MatchesExactValue()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.When("hello").Return("world");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("world", svc.Process("hello"));
    }

    [Fact]
    public void Process_When_WithPredicate()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.When(s => s.StartsWith("h")).Return("matched");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("matched", svc.Process("hello"));
    }

    [Fact]
    public void Process_WhenChain_ThenWhen()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.When("a").Return("1")
            .ThenWhen("b").Return("2")
            .ThenCall(new BasicUserMethodStub.ProcessInterceptor.ProcessDelegate(x => "fallback"));
        var svc = (IStubOverrideService)stub;
        Assert.Equal("1", svc.Process("a"));
        Assert.Equal("2", svc.Process("b"));
        Assert.Equal("fallback", svc.Process("c"));
    }

    [Fact]
    public void Process_Verify_ThrowsWhenNotCalled()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x");
        Assert.Throws<VerificationException>(() => stub.Process.Verify());
    }

    [Fact]
    public void Process_Verify_PassesWhenCalled()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x");
        var svc = (IStubOverrideService)stub;
        svc.Process("input");
        stub.Process.Verify();
    }

    [Fact]
    public void Process_Verifiable_CheckedByStubVerify()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x").Verifiable();
        var svc = (IStubOverrideService)stub;
        svc.Process("input");
        stub.Verify(); // should not throw
    }

    [Fact]
    public void Process_Verifiable_StubVerifyThrowsWhenNotCalled()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x").Verifiable();
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Process_LastArg_TracksLastArgument()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x");
        var svc = (IStubOverrideService)stub;
        svc.Process("first");
        svc.Process("second");
        Assert.Equal("second", stub.Process.LastArg);
    }

    [Fact]
    public void Process_Reset_ClearsTracking()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x");
        var svc = (IStubOverrideService)stub;
        svc.Process("input");
        stub.Process.Reset();
        Assert.Throws<VerificationException>(() => stub.Process.Verify());
    }

    [Fact]
    public void Process_Strict_ThrowsWhenNotConfigured()
    {
        var stub = new BasicUserMethodStub { Strict = true };
        var svc = (IStubOverrideService)stub;
        Assert.Throws<StubException>(() => svc.Process("input"));
    }

    // ========================================================================
    // Calculate (non-void, 2-param): tuple args, When with multi-param
    // ========================================================================

    [Fact]
    public void Calculate_ReturnValue_Works()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.Return(42);
        var svc = (IStubOverrideService)stub;
        Assert.Equal(42, svc.Calculate(1, 2));
    }

    [Fact]
    public void Calculate_ReturnCallback_UnpacksArgs()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.Return(new BasicUserMethodStub.CalculateInterceptor.CalculateDelegate((a, b) => a + b));
        var svc = (IStubOverrideService)stub;
        Assert.Equal(7, svc.Calculate(3, 4));
    }

    [Fact]
    public void Calculate_When_MatchesExactArgs()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.When(1, 2).Return(3);
        var svc = (IStubOverrideService)stub;
        Assert.Equal(3, svc.Calculate(1, 2));
    }

    [Fact]
    public void Calculate_When_WithPredicate()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.When((a, b) => a > 0 && b > 0).Return(99);
        var svc = (IStubOverrideService)stub;
        Assert.Equal(99, svc.Calculate(5, 10));
    }

    [Fact]
    public void Calculate_LastArgs_TracksLastArguments()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.Return(0);
        var svc = (IStubOverrideService)stub;
        svc.Calculate(3, 4);
        Assert.Equal((3, 4), stub.Calculate.LastArgs);
    }

    [Fact]
    public void Calculate_Sequence_Works()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.Return(1, 2, 3);
        var svc = (IStubOverrideService)stub;
        Assert.Equal(1, svc.Calculate(0, 0));
        Assert.Equal(2, svc.Calculate(0, 0));
        Assert.Equal(3, svc.Calculate(0, 0));
        Assert.Equal(3, svc.Calculate(0, 0)); // repeats last
    }

    // ========================================================================
    // Execute (void, 1-param): Call, When chain, verification
    // ========================================================================

    [Fact]
    public void Execute_Call_InvokesCallback()
    {
        var stub = new BasicUserMethodStub();
        var captured = "";
        stub.Execute.Call(cmd => captured = cmd);
        var svc = (IStubOverrideService)stub;
        svc.Execute("test-command");
        Assert.Equal("test-command", captured);
    }

    [Fact]
    public void Execute_When_MatchesAndInvokesCallback()
    {
        var stub = new BasicUserMethodStub();
        var captured = "";
        stub.Execute.When("go").Call(cmd => captured = cmd);
        var svc = (IStubOverrideService)stub;
        svc.Execute("go");
        Assert.Equal("go", captured);
    }

    [Fact]
    public void Execute_WhenChain_ThenWhenThenCall()
    {
        var stub = new BasicUserMethodStub();
        var calls = new List<string>();
        stub.Execute.When("a").Call(cmd => calls.Add("matched-a"))
            .ThenWhen("b").Call(cmd => calls.Add("matched-b"))
            .ThenCall(cmd => calls.Add("fallback"));
        var svc = (IStubOverrideService)stub;
        svc.Execute("a");
        svc.Execute("b");
        svc.Execute("c");
        Assert.Equal(["matched-a", "matched-b", "fallback"], calls);
    }

    [Fact]
    public void Execute_Verify_Works()
    {
        var stub = new BasicUserMethodStub();
        stub.Execute.Call(_ => { });
        var svc = (IStubOverrideService)stub;
        svc.Execute("x");
        stub.Execute.Verify();
    }

    [Fact]
    public void Execute_LastArg_Tracks()
    {
        var stub = new BasicUserMethodStub();
        stub.Execute.Call(_ => { });
        var svc = (IStubOverrideService)stub;
        svc.Execute("first");
        svc.Execute("second");
        Assert.Equal("second", stub.Execute.LastArg);
    }

    [Fact]
    public void Execute_Sequence_Works()
    {
        var stub = new BasicUserMethodStub();
        var calls = new List<string>();
        stub.Execute.Call(cmd => calls.Add("1:" + cmd))
            .ThenCall(cmd => calls.Add("2:" + cmd));
        var svc = (IStubOverrideService)stub;
        svc.Execute("a");
        svc.Execute("b");
        Assert.Equal(["1:a", "2:b"], calls);
    }

    // ========================================================================
    // FindById (non-void, 1-param, nullable return)
    // ========================================================================

    [Fact]
    public void FindById_ReturnNullValue_Works()
    {
        var stub = new BasicUserMethodStub();
        stub.FindById.Return((string?)null);
        var svc = (IStubOverrideService)stub;
        Assert.Null(svc.FindById(1));
    }

    [Fact]
    public void FindById_ReturnValue_Works()
    {
        var stub = new BasicUserMethodStub();
        stub.FindById.Return("found");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("found", svc.FindById(42));
    }

    [Fact]
    public void FindById_When_MatchesExactId()
    {
        var stub = new BasicUserMethodStub();
        stub.FindById.When(1).Return("item-1")
            .ThenWhen(2).Return("item-2")
            .ThenNone();
        var svc = (IStubOverrideService)stub;
        Assert.Equal("item-1", svc.FindById(1));
        Assert.Equal("item-2", svc.FindById(2));
    }

    // ========================================================================
    // Stub-level: Verify, VerifyAll, Source
    // ========================================================================

    [Fact]
    public void StubVerify_ChecksAllVerifiable()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x").Verifiable();
        stub.Calculate.Return(1).Verifiable();
        var svc = (IStubOverrideService)stub;
        svc.Process("a");
        svc.Calculate(1, 2);
        stub.Verify(); // should not throw
    }

    [Fact]
    public void StubVerify_ThrowsWhenAnyVerifiableNotCalled()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Return("x").Verifiable();
        stub.Calculate.Return(1).Verifiable();
        var svc = (IStubOverrideService)stub;
        svc.Process("a");
        // Calculate not called
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    // ========================================================================
    // Stub override fallback
    // ========================================================================

    [Fact]
    public void Process_StubOverride_FallsBackToBaseMethod()
    {
        var stub = new BasicUserMethodStubWithOverride();
        var svc = (IStubOverrideService)stub;
        // No Return configured, so should fall through to Process_
        Assert.Equal("override:test", svc.Process("test"));
    }

    [Fact]
    public void Process_StubOverride_ConfiguredReturnTakesPriority()
    {
        var stub = new BasicUserMethodStubWithOverride();
        stub.Process.Return("configured");
        var svc = (IStubOverrideService)stub;
        Assert.Equal("configured", svc.Process("test"));
    }

    private class BasicUserMethodStubWithOverride : BasicUserMethodStub
    {
        protected override string Process_(string input) => "override:" + input;
        protected override int Calculate_(int a, int b) => a * b;
        protected override void Execute_(string command) { }
        protected override string? FindById_(int id) => "found-" + id;
    }
}
