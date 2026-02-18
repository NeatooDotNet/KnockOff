using KnockOff;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for .When() API support on stub override interceptors.
/// When chains have highest priority; stub override is the final fallback.
/// </summary>
public class StubOverrideWhenTests
{
    #region Basic When Matching

    [Fact]
    public void When_ValueMatch_ReturnsWhenValue()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("special").Return("[WHEN MATCHED]");

        // Act
        IWhenStubOverrideTest service = stub;
        var result = service.Process("special");

        // Assert - When chain wins
        Assert.Equal("[WHEN MATCHED]", result);
    }

    [Fact]
    public void When_NoMatch_FallsToStubOverride()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("special").Return("[WHEN MATCHED]");

        // Act
        IWhenStubOverrideTest service = stub;
        var result = service.Process("normal");

        // Assert - Stub override is called as fallback
        Assert.Equal("[USER: normal]", result);
    }

    [Fact]
    public void When_MultipleMatchers_FirstMatchWins()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("first").Return("[FIRST]");
        stub.Process.When("second").Return("[SECOND]");

        // Act
        IWhenStubOverrideTest service = stub;
        var result1 = service.Process("first");
        var result2 = service.Process("second");
        var result3 = service.Process("other");

        // Assert
        Assert.Equal("[FIRST]", result1);
        Assert.Equal("[SECOND]", result2);
        Assert.Equal("[USER: other]", result3);
    }

    #endregion

    #region Predicate When Matching

    [Fact]
    public void When_PredicateMatch_ReturnsWhenValue()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When(s => s.Length > 10).Return("[LONG STRING]");

        // Act
        IWhenStubOverrideTest service = stub;
        var result = service.Process("short");
        var longResult = service.Process("this is a long string");

        // Assert
        Assert.Equal("[USER: short]", result);
        Assert.Equal("[LONG STRING]", longResult);
    }

    #endregion

    #region When Chain with ThenWhen

    [Fact]
    public void When_ThenWhen_MatchesInSequence()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("first").Return("[1]")
            .ThenWhen("second").Return("[2]")
            .ThenNone();

        // Act
        IWhenStubOverrideTest service = stub;
        var r1 = service.Process("first");
        var r2 = service.Process("second");
        var r3 = service.Process("third"); // Falls to stub override

        // Assert
        Assert.Equal("[1]", r1);
        Assert.Equal("[2]", r2);
        Assert.Equal("[USER: third]", r3);
    }

    [Fact]
    public void When_ThenCall_ExecutesCallback()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("callback").Return("[INITIAL]")
            .ThenCall(s => $"[CALLBACK: {s}]");

        // Act
        IWhenStubOverrideTest service = stub;
        var r1 = service.Process("callback");
        var r2 = service.Process("anything");

        // Assert
        Assert.Equal("[INITIAL]", r1);
        Assert.Equal("[CALLBACK: anything]", r2);
    }

    #endregion

    #region Void Method When Chains

    [Fact]
    public void When_VoidMethod_CallsCallback()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        var callbackInvoked = false;
        stub.Execute.When("trigger").Call(cmd => callbackInvoked = true);

        // Act
        IWhenStubOverrideTest service = stub;
        service.Execute("trigger");

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void When_VoidMethod_NoMatch_FallsToStubOverride()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Execute.When("trigger").Call(cmd => { });

        // Act
        IWhenStubOverrideTest service = stub;
        service.Execute("other"); // Falls to stub override

        // Assert - No exception, stub override was called
        stub.Execute.Verify(Called.Once);
    }

    #endregion

    #region Async Method When Chains

    [Fact]
    public async Task When_AsyncMethod_AutoWrapsReturnValue()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.GetAsync.When(1).Return("async-result"); // Auto-wraps in Task

        // Act
        IWhenStubOverrideTest service = stub;
        var result = await service.GetAsync(1);
        var fallbackResult = await service.GetAsync(999);

        // Assert
        Assert.Equal("async-result", result);
        Assert.Equal("[ASYNC USER: 999]", fallbackResult);
    }

    #endregion

    #region Sequences with Stub Override Fallback

    [Fact]
    public void Returns_Sequence_ThenFallsToStubOverride()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.Return("[1]", "[2]");

        // Act
        IWhenStubOverrideTest service = stub;
        var r1 = service.Process("a");
        var r2 = service.Process("b");
        var r3 = service.Process("c"); // Repeats last

        // Assert
        Assert.Equal("[1]", r1);
        Assert.Equal("[2]", r2);
        Assert.Equal("[2]", r3); // Sequence repeats last value
    }

    [Fact]
    public void OnCall_ThenReturns_Sequence()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.Call(s => "[FIRST]").ThenReturn("[SECOND]");

        // Act
        IWhenStubOverrideTest service = stub;
        var r1 = service.Process("a");
        var r2 = service.Process("b");
        var r3 = service.Process("c"); // Repeats last

        // Assert
        Assert.Equal("[FIRST]", r1);
        Assert.Equal("[SECOND]", r2);
        Assert.Equal("[SECOND]", r3);
    }

    #endregion

    #region Verification with When Chains

    [Fact]
    public void When_Verifiable_VerifiesChainConsumed()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        // Need to add ThenCall or ThenNone to make the chain terminal
        stub.Process.When("expected").Return("[FIRST]")
            .ThenCall(s => "[TERMINAL]")
            .Verifiable();

        // Act
        IWhenStubOverrideTest service = stub;
        service.Process("expected");  // Consume first
        service.Process("anything");  // Reach terminal

        // Assert - No exception - chain completed
        stub.Verify();
    }

    [Fact]
    public void When_Verifiable_ThrowsWhenNotConsumed()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("first").Return("[1]");
        stub.Process.When("second").Return("[2]").Verifiable();

        // Act - Only call first matcher, second not consumed
        IWhenStubOverrideTest service = stub;
        service.Process("first");

        // Assert - Second matcher not consumed
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Verify_CountsStubOverrideCalls()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        // No When chain configured - all calls go to stub override

        // Act
        IWhenStubOverrideTest service = stub;
        service.Process("one");
        service.Process("two");

        // Assert - Both stub override calls are tracked
        stub.Process.Verify(Called.Exactly(2));
    }

    [Fact]
    public void Verify_WhenChainCallsIncludedInTotalCount()
    {
        // Arrange - When chain calls are included in TotalCallCount
        var stub = new WhenStubOverrideStub();
        stub.Process.When("special").Return("[SPECIAL]").ThenNone();

        // Act
        IWhenStubOverrideTest service = stub;
        service.Process("special");  // When chain - tracked in matcher.CallCount (included in TotalCallCount)
        service.Process("normal");   // Stub override - tracked in _unconfiguredCallCount

        // Assert - Both When chain and stub override calls count in TotalCallCount
        stub.Process.Verify(Called.Exactly(2)); // 1 When chain call + 1 stub override call
    }

    #endregion

    #region Mixed When + OnCall Scenarios

    [Fact]
    public void When_HasPriorityOverOnCall()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.Call(s => "[ONCALL]");
        stub.Process.When("special").Return("[WHEN]");

        // Act
        IWhenStubOverrideTest service = stub;
        var whenResult = service.Process("special");
        var onCallResult = service.Process("other");

        // Assert - When has priority, OnCall is fallback before stub override
        Assert.Equal("[WHEN]", whenResult);
        Assert.Equal("[ONCALL]", onCallResult);
    }

    #endregion

    #region LastArg Tracking

    [Fact]
    public void LastArg_TracksAcrossAllCallTypes()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Process.When("when-value").Return("[WHEN]");

        // Act
        IWhenStubOverrideTest service = stub;
        service.Process("when-value");  // When chain
        service.Process("user-value");  // Stub override fallback

        // Assert - LastArg tracks most recent call
        Assert.Equal("user-value", stub.Process.LastArg);
    }

    #endregion

    #region Multi-Parameter When

    [Fact]
    public void When_MultipleParameters_MatchesAll()
    {
        // Arrange
        var stub = new WhenStubOverrideStub();
        stub.Calculate.When(0, 0).Return(0);
        stub.Calculate.When(args => args.a < 0 && args.b < 0).Return(-1);

        // Act
        IWhenStubOverrideTest service = stub;
        var zero = service.Calculate(0, 0);
        var negative = service.Calculate(-5, -3);
        var normal = service.Calculate(10, 20);

        // Assert
        Assert.Equal(0, zero);
        Assert.Equal(-1, negative);
        Assert.Equal(30, normal); // Stub override: a + b
    }

    #endregion

    #region Generic Standalone When

    [Fact]
    public void GenericStandalone_When_WithStubOverrideFallback()
    {
        // Arrange
        var stub = new GenericWhenStubOverrideStub<string>();
        stub.Process.When("special").Return("[WHEN: special]");

        // Act
        IGenericWhenStubOverrideService<string> service = stub;
        var whenResult = service.Process("special");
        var userResult = service.Process("normal");

        // Assert - When match returns When value, non-match falls to stub override
        Assert.Equal("[WHEN: special]", whenResult);
        Assert.Equal("[USER: normal]", userResult);
    }

    [Fact]
    public void GenericStandalone_When_PredicateWithStubOverrideFallback()
    {
        // Arrange
        var stub = new GenericWhenStubOverrideStub<int>();
        stub.Process.When(x => x > 100).Return(-1);

        // Act
        IGenericWhenStubOverrideService<int> service = stub;
        var whenResult = service.Process(999);
        var userResult = service.Process(42);

        // Assert
        Assert.Equal(-1, whenResult);
        Assert.Equal(42, userResult); // Stub override returns the input as-is
    }

    [Fact]
    public void GenericStandalone_When_ThenWhenChainWithStubOverrideFallback()
    {
        // Arrange
        var stub = new GenericWhenStubOverrideStub<string>();
        stub.Process.When("first").Return("[1]")
            .ThenWhen("second").Return("[2]")
            .ThenNone();

        // Act
        IGenericWhenStubOverrideService<string> service = stub;
        var r1 = service.Process("first");
        var r2 = service.Process("second");
        var r3 = service.Process("third"); // Falls to stub override after ThenNone

        // Assert
        Assert.Equal("[1]", r1);
        Assert.Equal("[2]", r2);
        Assert.Equal("[USER: third]", r3);
    }

    #endregion

    #region Overloaded Stub Overrides When

    [Fact]
    public void Overloaded_When_OnStubOverrideOverload()
    {
        // Arrange - OverloadedStubOverrideStub has stub override on Format(string) only
        var stub = new OverloadedStubOverrideStub();
        stub.Format.When("special").Return("[WHEN]");

        // Act
        IOverloadedStubOverrideService service = stub;
        var whenResult = service.Format("special");
        var userResult = service.Format("normal");

        // Assert - When match returns When value, non-match falls to stub override
        Assert.Equal("[WHEN]", whenResult);
        Assert.Equal("USER:normal", userResult); // Stub override returns "USER:" + input
    }

    [Fact]
    public void Overloaded_When_OnNonStubOverrideOverload()
    {
        // Arrange - Format2 (two-param overload) has no stub override
        var stub = new OverloadedStubOverrideStub();
        stub.Format2.When("hello", true).Return("[WHEN UPPER]");
        stub.Format2.Return("[DEFAULT]");

        // Act
        IOverloadedStubOverrideService service = stub;
        var whenResult = service.Format("hello", true);
        var fallbackResult = service.Format("other", false);

        // Assert - When match on non-user-method overload, fallback to Returns
        Assert.Equal("[WHEN UPPER]", whenResult);
        Assert.Equal("[DEFAULT]", fallbackResult);
    }

    [Fact]
    public void Overloaded_When_BothOverloadsIndependent()
    {
        // Arrange - Configure When on both overloads independently
        var stub = new OverloadedStubOverrideStub();
        stub.Format.When("special").Return("[WHEN1]");
        stub.Format2.When("special", true).Return("[WHEN2]");
        stub.Format2.Return("[DEFAULT2]");

        // Act
        IOverloadedStubOverrideService service = stub;
        var when1 = service.Format("special");       // Matches When on stub override overload
        var user1 = service.Format("normal");         // Falls to stub override
        var when2 = service.Format("special", true);  // Matches When on non-user overload
        var fall2 = service.Format("normal", false);   // Falls to Returns

        // Assert - Each overload has independent When chain
        Assert.Equal("[WHEN1]", when1);
        Assert.Equal("USER:normal", user1);
        Assert.Equal("[WHEN2]", when2);
        Assert.Equal("[DEFAULT2]", fall2);
    }

    #endregion
}

#region Test Interface and Stub

public interface IWhenStubOverrideTest
{
    string Process(string input);
    void Execute(string command);
    Task<string> GetAsync(int id);
    int Calculate(int a, int b);
}

[KnockOff]
public partial class WhenStubOverrideStub : IWhenStubOverrideTest
{
}

public partial class WhenStubOverrideStub
{
    protected override string Process_(string input)
    {
        return $"[USER: {input}]";
    }

    protected override void Execute_(string command)
    {
        // Stub override - does nothing but proves fallback works
    }

    protected override Task<string> GetAsync_(int id)
    {
        return Task.FromResult($"[ASYNC USER: {id}]");
    }

    protected override int Calculate_(int a, int b)
    {
        return a + b;
    }
}

#endregion

#region Generic Standalone Test Types

/// <summary>Generic interface for testing When chains on generic standalone stubs with stub overrides.</summary>
public interface IGenericWhenStubOverrideService<T>
{
    T Process(T input);
}

/// <summary>Generic standalone stub with stub override override for Process.</summary>
[KnockOff]
public partial class GenericWhenStubOverrideStub<T> : IGenericWhenStubOverrideService<T>
{
}

/// <summary>Stub override implementations for GenericWhenStubOverrideStub.</summary>
public partial class GenericWhenStubOverrideStub<T>
{
    protected override T Process_(T input)
    {
        // For string types, prefix with "[USER: ", otherwise return as-is
        if (input is string s)
            return (T)(object)$"[USER: {s}]";
        return input;
    }
}

#endregion

