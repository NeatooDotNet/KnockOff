using KnockOff;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for .When() API support on user method interceptors.
/// When chains have highest priority; user method is the final fallback.
/// </summary>
public class UserMethodWhenTests
{
    #region Basic When Matching

    [Fact]
    public void When_ValueMatch_ReturnsWhenValue()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.When("special").Return("[WHEN MATCHED]");

        // Act
        IWhenUserMethodTest service = stub;
        var result = service.Process("special");

        // Assert - When chain wins
        Assert.Equal("[WHEN MATCHED]", result);
    }

    [Fact]
    public void When_NoMatch_FallsToUserMethod()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.When("special").Return("[WHEN MATCHED]");

        // Act
        IWhenUserMethodTest service = stub;
        var result = service.Process("normal");

        // Assert - User method is called as fallback
        Assert.Equal("[USER: normal]", result);
    }

    [Fact]
    public void When_MultipleMatchers_FirstMatchWins()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.When("first").Return("[FIRST]");
        stub.Process.When("second").Return("[SECOND]");

        // Act
        IWhenUserMethodTest service = stub;
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
        var stub = new WhenUserMethodStub();
        stub.Process.When(s => s.Length > 10).Return("[LONG STRING]");

        // Act
        IWhenUserMethodTest service = stub;
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
        var stub = new WhenUserMethodStub();
        stub.Process.When("first").Return("[1]")
            .ThenWhen("second").Return("[2]")
            .ThenNone();

        // Act
        IWhenUserMethodTest service = stub;
        var r1 = service.Process("first");
        var r2 = service.Process("second");
        var r3 = service.Process("third"); // Falls to user method

        // Assert
        Assert.Equal("[1]", r1);
        Assert.Equal("[2]", r2);
        Assert.Equal("[USER: third]", r3);
    }

    [Fact]
    public void When_ThenCall_ExecutesCallback()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.When("callback").Return("[INITIAL]")
            .ThenCall(s => $"[CALLBACK: {s}]");

        // Act
        IWhenUserMethodTest service = stub;
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
        var stub = new WhenUserMethodStub();
        var callbackInvoked = false;
        stub.Execute.When("trigger").Call(cmd => callbackInvoked = true);

        // Act
        IWhenUserMethodTest service = stub;
        service.Execute("trigger");

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void When_VoidMethod_NoMatch_FallsToUserMethod()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Execute.When("trigger").Call(cmd => { });

        // Act
        IWhenUserMethodTest service = stub;
        service.Execute("other"); // Falls to user method

        // Assert - No exception, user method was called
        stub.Execute.Verify(Times.Once);
    }

    #endregion

    #region Async Method When Chains

    [Fact]
    public async Task When_AsyncMethod_AutoWrapsReturnValue()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.GetAsync.When(1).Return("async-result"); // Auto-wraps in Task

        // Act
        IWhenUserMethodTest service = stub;
        var result = await service.GetAsync(1);
        var fallbackResult = await service.GetAsync(999);

        // Assert
        Assert.Equal("async-result", result);
        Assert.Equal("[ASYNC USER: 999]", fallbackResult);
    }

    #endregion

    #region Sequences with User Method Fallback

    [Fact]
    public void Returns_Sequence_ThenFallsToUserMethod()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.Return("[1]", "[2]");

        // Act
        IWhenUserMethodTest service = stub;
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
        var stub = new WhenUserMethodStub();
        stub.Process.Return(s => "[FIRST]").ThenReturn("[SECOND]");

        // Act
        IWhenUserMethodTest service = stub;
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
        var stub = new WhenUserMethodStub();
        // Need to add ThenCall or ThenNone to make the chain terminal
        stub.Process.When("expected").Return("[FIRST]")
            .ThenCall(s => "[TERMINAL]")
            .Verifiable();

        // Act
        IWhenUserMethodTest service = stub;
        service.Process("expected");  // Consume first
        service.Process("anything");  // Reach terminal

        // Assert - No exception - chain completed
        stub.Verify();
    }

    [Fact]
    public void When_Verifiable_ThrowsWhenNotConsumed()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.When("first").Return("[1]");
        stub.Process.When("second").Return("[2]").Verifiable();

        // Act - Only call first matcher, second not consumed
        IWhenUserMethodTest service = stub;
        service.Process("first");

        // Assert - Second matcher not consumed
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Verify_CountsUserMethodCalls()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        // No When chain configured - all calls go to user method

        // Act
        IWhenUserMethodTest service = stub;
        service.Process("one");
        service.Process("two");

        // Assert - Both user method calls are tracked
        stub.Process.Verify(Times.Exactly(2));
    }

    [Fact]
    public void Verify_WhenChainCallsIncludedInTotalCount()
    {
        // Arrange - When chain calls are included in TotalCallCount
        var stub = new WhenUserMethodStub();
        stub.Process.When("special").Return("[SPECIAL]").ThenNone();

        // Act
        IWhenUserMethodTest service = stub;
        service.Process("special");  // When chain - tracked in matcher.CallCount (included in TotalCallCount)
        service.Process("normal");   // User method - tracked in _unconfiguredCallCount

        // Assert - Both When chain and user method calls count in TotalCallCount
        stub.Process.Verify(Times.Exactly(2)); // 1 When chain call + 1 user method call
    }

    #endregion

    #region Mixed When + OnCall Scenarios

    [Fact]
    public void When_HasPriorityOverOnCall()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.Return(s => "[ONCALL]");
        stub.Process.When("special").Return("[WHEN]");

        // Act
        IWhenUserMethodTest service = stub;
        var whenResult = service.Process("special");
        var onCallResult = service.Process("other");

        // Assert - When has priority, OnCall is fallback before user method
        Assert.Equal("[WHEN]", whenResult);
        Assert.Equal("[ONCALL]", onCallResult);
    }

    #endregion

    #region LastArg Tracking

    [Fact]
    public void LastArg_TracksAcrossAllCallTypes()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Process.When("when-value").Return("[WHEN]");

        // Act
        IWhenUserMethodTest service = stub;
        service.Process("when-value");  // When chain
        service.Process("user-value");  // User method fallback

        // Assert - LastArg tracks most recent call
        Assert.Equal("user-value", stub.Process.LastArg);
    }

    #endregion

    #region Multi-Parameter When

    [Fact]
    public void When_MultipleParameters_MatchesAll()
    {
        // Arrange
        var stub = new WhenUserMethodStub();
        stub.Calculate.When(0, 0).Return(0);
        stub.Calculate.When((a, b) => a < 0 && b < 0).Return(-1);

        // Act
        IWhenUserMethodTest service = stub;
        var zero = service.Calculate(0, 0);
        var negative = service.Calculate(-5, -3);
        var normal = service.Calculate(10, 20);

        // Assert
        Assert.Equal(0, zero);
        Assert.Equal(-1, negative);
        Assert.Equal(30, normal); // User method: a + b
    }

    #endregion

    #region Generic Standalone When

    [Fact]
    public void GenericStandalone_When_WithUserMethodFallback()
    {
        // Arrange
        var stub = new GenericWhenUserMethodStub<string>();
        stub.Process.When("special").Return("[WHEN: special]");

        // Act
        IGenericWhenUserMethodService<string> service = stub;
        var whenResult = service.Process("special");
        var userResult = service.Process("normal");

        // Assert - When match returns When value, non-match falls to user method
        Assert.Equal("[WHEN: special]", whenResult);
        Assert.Equal("[USER: normal]", userResult);
    }

    [Fact]
    public void GenericStandalone_When_PredicateWithUserMethodFallback()
    {
        // Arrange
        var stub = new GenericWhenUserMethodStub<int>();
        stub.Process.When(x => x > 100).Return(-1);

        // Act
        IGenericWhenUserMethodService<int> service = stub;
        var whenResult = service.Process(999);
        var userResult = service.Process(42);

        // Assert
        Assert.Equal(-1, whenResult);
        Assert.Equal(42, userResult); // User method returns the input as-is
    }

    [Fact]
    public void GenericStandalone_When_ThenWhenChainWithUserMethodFallback()
    {
        // Arrange
        var stub = new GenericWhenUserMethodStub<string>();
        stub.Process.When("first").Return("[1]")
            .ThenWhen("second").Return("[2]")
            .ThenNone();

        // Act
        IGenericWhenUserMethodService<string> service = stub;
        var r1 = service.Process("first");
        var r2 = service.Process("second");
        var r3 = service.Process("third"); // Falls to user method after ThenNone

        // Assert
        Assert.Equal("[1]", r1);
        Assert.Equal("[2]", r2);
        Assert.Equal("[USER: third]", r3);
    }

    #endregion

    #region Overloaded User Methods When

    [Fact]
    public void Overloaded_When_OnUserMethodOverload()
    {
        // Arrange - OverloadedUserMethodStub has user override on Format(string) only
        var stub = new OverloadedUserMethodStub();
        stub.Format.When("special").Return("[WHEN]");

        // Act
        IOverloadedUserMethodService service = stub;
        var whenResult = service.Format("special");
        var userResult = service.Format("normal");

        // Assert - When match returns When value, non-match falls to user method
        Assert.Equal("[WHEN]", whenResult);
        Assert.Equal("USER:normal", userResult); // User method returns "USER:" + input
    }

    [Fact]
    public void Overloaded_When_OnNonUserMethodOverload()
    {
        // Arrange - Format2 (two-param overload) has no user override
        var stub = new OverloadedUserMethodStub();
        stub.Format2.When("hello", true).Return("[WHEN UPPER]");
        stub.Format2.Return("[DEFAULT]");

        // Act
        IOverloadedUserMethodService service = stub;
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
        var stub = new OverloadedUserMethodStub();
        stub.Format.When("special").Return("[WHEN1]");
        stub.Format2.When("special", true).Return("[WHEN2]");
        stub.Format2.Return("[DEFAULT2]");

        // Act
        IOverloadedUserMethodService service = stub;
        var when1 = service.Format("special");       // Matches When on user method overload
        var user1 = service.Format("normal");         // Falls to user method
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

public interface IWhenUserMethodTest
{
    string Process(string input);
    void Execute(string command);
    Task<string> GetAsync(int id);
    int Calculate(int a, int b);
}

[KnockOff]
public partial class WhenUserMethodStub : IWhenUserMethodTest
{
}

public partial class WhenUserMethodStub
{
    protected override string Process_(string input)
    {
        return $"[USER: {input}]";
    }

    protected override void Execute_(string command)
    {
        // User method - does nothing but proves fallback works
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

/// <summary>Generic interface for testing When chains on generic standalone stubs with user methods.</summary>
public interface IGenericWhenUserMethodService<T>
{
    T Process(T input);
}

/// <summary>Generic standalone stub with user method override for Process.</summary>
[KnockOff]
public partial class GenericWhenUserMethodStub<T> : IGenericWhenUserMethodService<T>
{
}

/// <summary>User method implementations for GenericWhenUserMethodStub.</summary>
public partial class GenericWhenUserMethodStub<T>
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

