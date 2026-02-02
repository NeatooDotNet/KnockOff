using KnockOff;

namespace KnockOff.Documentation.Samples.WhenApi;

// =============================================================================
// Interfaces for When() API Samples
// =============================================================================

public interface IWhenCalculator
{
    int Add(int a, int b);
    int Calculate(int a, int b);
}

public interface IWhenTransformer
{
    string Transform(string input);
}

public interface IWhenProcessor
{
    void Process(int a, int b);
}

public interface IWhenAsyncDataService
{
    Task<string> GetAsync(string key);
}

public interface IWhenDataFetcher
{
    string? FetchData(string key);
}

public interface IWhenStatusService
{
    string GetStatus();
}

public interface IWhenPaymentGateway
{
    WhenPaymentResult ProcessPayment(decimal amount);
}

public class WhenPaymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public bool RequiresApproval { get; set; }
}

// =============================================================================
// Stubs for When() API Samples
// =============================================================================

[KnockOff]
public partial class WhenCalculatorStub : IWhenCalculator { }

[KnockOff]
public partial class WhenTransformerStub : IWhenTransformer { }

[KnockOff]
public partial class WhenProcessorStub : IWhenProcessor { }

[KnockOff]
public partial class WhenAsyncDataServiceStub : IWhenAsyncDataService { }

[KnockOff]
public partial class WhenDataFetcherStub : IWhenDataFetcher { }

[KnockOff]
public partial class WhenStatusServiceStub : IWhenStatusService { }

[KnockOff]
public partial class WhenPaymentGatewayStub : IWhenPaymentGateway { }

// =============================================================================
// The Problem: One Callback For All Arguments
// =============================================================================

public class WhenProblemTests
{
    [Fact]
    public void Problem_OneCallbackHandlesAllArgs()
    {
        var stub = new WhenCalculatorStub();

        #region when-problem-one-callback-all-args
        // Without When(): callback must handle all argument combinations
        stub.Calculate.OnCall((a, b) =>
        {
            // Complex branching logic inside callback
            if (a == 5 && b == 10)
                return 50;
            else if (a == 1 && b == 2)
                return 100;
            else if (a > 100)
                return 999;
            else
                return a + b;
        });
        #endregion

        IWhenCalculator calc = stub;
        Assert.Equal(50, calc.Calculate(5, 10));
        Assert.Equal(100, calc.Calculate(1, 2));
        Assert.Equal(999, calc.Calculate(200, 1));
        Assert.Equal(7, calc.Calculate(3, 4));
    }
}

// =============================================================================
// The Solution: Match Then Respond
// =============================================================================

public class WhenSolutionTests
{
    [Fact]
    public void Solution_MatchThenRespond()
    {
        var stub = new WhenCalculatorStub();

        #region when-solution-match-then-respond
        // With When(): match arguments, then configure response
        stub.Calculate.When(5, 10).Returns(50);
        #endregion

        IWhenCalculator calc = stub;
        Assert.Equal(50, calc.Calculate(5, 10));
    }
}

// =============================================================================
// Basic Usage: Return Methods
// =============================================================================

public class WhenBasicReturnMethodTests
{
    [Fact]
    public void ValueMatching_ExactArguments()
    {
        var stub = new WhenCalculatorStub();

        #region when-basic-value-matching
        // Configure different returns for different argument values
        stub.Add.When(1, 2).Returns(100);
        stub.Add.When(3, 4).Returns(200);
        #endregion

        IWhenCalculator calc = stub;

        // Matching arguments use configured returns
        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(200, calc.Add(3, 4));

        // Non-matching arguments fall through to default (0)
        Assert.Equal(0, calc.Add(9, 9));
    }

    [Fact]
    public void PredicateMatching_ConditionBased()
    {
        var stub = new WhenCalculatorStub();

        #region when-basic-predicate-matching
        // Match based on condition
        stub.Add.When((a, b) => a > 10).Returns(999);
        #endregion

        IWhenCalculator calc = stub;

        // First param > 10: predicate matches
        Assert.Equal(999, calc.Add(15, 5));
        Assert.Equal(999, calc.Add(100, 1));

        // First param <= 10: predicate doesn't match, falls through
        Assert.Equal(0, calc.Add(5, 20));
    }

    [Fact]
    public void SingleParameter_Works()
    {
        var stub = new WhenTransformerStub();

        #region when-single-parameter
        // When() works with any parameter count
        stub.Transform.When("hello").Returns("HELLO");
        #endregion

        IWhenTransformer transformer = stub;
        Assert.Equal("HELLO", transformer.Transform("hello"));
    }
}

// =============================================================================
// Void Methods: Call() Instead of Returns()
// =============================================================================

public class WhenVoidMethodTests
{
    [Fact]
    public void VoidTracking_WithoutCallback()
    {
        var stub = new WhenProcessorStub();

        #region when-void-tracking-only
        // When() alone tracks parameter-specific calls
        var chain = stub.Process.When(1, 2);
        #endregion

        IWhenProcessor processor = stub;
        processor.Process(1, 2);
        processor.Process(1, 2);

        // Verify this specific parameter combination was called twice
        chain.Verify(Times.Exactly(2));
    }

    [Fact]
    public void VoidWithCallback_ExecutesSideEffect()
    {
        var stub = new WhenProcessorStub();

        var calls = new List<(int, int)>();
        #region when-void-with-callback
        // Call() adds callback for side effects
        stub.Process.When(1, 2).Call((a, b) => calls.Add((a, b)));
        #endregion

        IWhenProcessor processor = stub;
        processor.Process(1, 2);

        Assert.Single(calls);
        Assert.Equal((1, 2), calls[0]);
    }

    [Fact]
    public void VoidPredicate_MatchesCondition()
    {
        var stub = new WhenProcessorStub();

        var matched = new List<(int, int)>();
        #region when-void-predicate
        // Predicate matching works the same for void methods
        stub.Process.When((a, b) => a > 10).Call((a, b) => matched.Add((a, b)));
        #endregion

        IWhenProcessor processor = stub;
        processor.Process(1, 2);   // a <= 10, doesn't match
        processor.Process(15, 20); // a > 10, matches

        Assert.Single(matched);
        Assert.Equal((15, 20), matched[0]);
    }
}

// =============================================================================
// Chaining Multiple Matchers
// =============================================================================

public class WhenChainingTests
{
    [Fact]
    public void ThenWhen_SequentialMatching()
    {
        var stub = new WhenCalculatorStub();

        #region when-chaining-thenwhen
        // Chain matchers with ThenWhen()
        stub.Add
            .When(1, 2).Returns(100)
            .ThenWhen(3, 4).Returns(200)
            .ThenWhen((a, b) => a > 100).Returns(999);
        #endregion

        IWhenCalculator calc = stub;

        // First matching call consumes first matcher
        Assert.Equal(100, calc.Add(1, 2));

        // Second matching call consumes second matcher
        Assert.Equal(200, calc.Add(3, 4));

        // Third matching call consumes third matcher
        Assert.Equal(999, calc.Add(150, 1));

        // Subsequent calls repeat the last matcher
        Assert.Equal(999, calc.Add(200, 5));
    }

    [Fact]
    public void MultipleCalls_AddsToSameChain()
    {
        var stub = new WhenCalculatorStub();

        #region when-multiple-calls
        // Multiple When() calls build the same chain
        stub.Add.When(1, 2).Returns(100);
        stub.Add.When(2, 3).Returns(200);
        stub.Add.When(3, 4).Returns(300);
        #endregion

        IWhenCalculator calc = stub;

        // Each consumed in order
        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(200, calc.Add(2, 3));
        Assert.Equal(300, calc.Add(3, 4));
    }
}

// =============================================================================
// Terminal Matchers
// =============================================================================

public class WhenTerminalTests
{
    [Fact]
    public void ThenCall_UnconditionalFallback()
    {
        var stub = new WhenCalculatorStub();

        #region when-thencall-terminal
        // ThenCall() is an unconditional terminal matcher
        stub.Add
            .When(1, 2).Returns(100)
            .ThenCall((a, b) => a + b);
        #endregion

        IWhenCalculator calc = stub;

        // First call matches When(1, 2)
        Assert.Equal(100, calc.Add(1, 2));

        // Subsequent calls use ThenCall callback
        Assert.Equal(15, calc.Add(5, 10));
        Assert.Equal(30, calc.Add(10, 20));
    }

    [Fact]
    public void ThenNone_ExhaustAndFallThrough()
    {
        var stub = new WhenCalculatorStub();

        #region when-thennone-exhaust
        // ThenNone() closes the chain and falls through
        stub.Add.When(1, 2).Returns(100).ThenNone();
        stub.Add.Returns(999);
        #endregion

        IWhenCalculator calc = stub;

        // First call uses When matcher
        Assert.Equal(100, calc.Add(1, 2));

        // After ThenNone(), falls through to Returns()
        Assert.Equal(999, calc.Add(9, 9));
        Assert.Equal(999, calc.Add(5, 5));
    }
}

// =============================================================================
// Fallback Behavior
// =============================================================================

public class WhenFallbackTests
{
    [Fact]
    public void FallbackToReturns()
    {
        var stub = new WhenCalculatorStub();

        #region when-fallback-returns
        // When() falls through to Returns() when no match
        stub.Add.When(1, 2).Returns(100);
        stub.Add.Returns(999);
        #endregion

        IWhenCalculator calc = stub;

        Assert.Equal(100, calc.Add(1, 2));  // When() matches
        Assert.Equal(999, calc.Add(9, 9));  // Falls through to Returns()
    }

    [Fact]
    public void FallbackToOnCall()
    {
        var stub = new WhenCalculatorStub();

        #region when-fallback-oncall
        // When() falls through to OnCall() when no match
        stub.Add.When(1, 2).Returns(100);
        stub.Add.OnCall((a, b) => a * b);
        #endregion

        IWhenCalculator calc = stub;

        Assert.Equal(100, calc.Add(1, 2));  // When() matches
        Assert.Equal(27, calc.Add(9, 3));   // Falls through to OnCall()
    }

    [Fact]
    public void FallbackNone_NonStrict_ReturnsDefault()
    {
        var stub = new WhenCalculatorStub();

        #region when-fallback-none-nonstrict
        // Non-strict mode: unmatched calls return default
        stub.Strict = false;
        stub.Add.When(1, 2).Returns(100);
        #endregion

        IWhenCalculator calc = stub;

        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(0, calc.Add(9, 9));  // default(int) = 0
    }

    [Fact]
    public void FallbackNone_Strict_Throws()
    {
        var stub = new WhenCalculatorStub();

        #region when-fallback-none-strict
        // Strict mode: unmatched calls throw
        stub.Strict = true;
        stub.Add.When(1, 2).Returns(100);
        #endregion

        IWhenCalculator calc = stub;

        Assert.Equal(100, calc.Add(1, 2));
        Assert.Throws<StubException>(() => calc.Add(9, 9));
    }
}

// =============================================================================
// Combining With Sequences
// =============================================================================

public class WhenPriorityTests
{
    [Fact]
    public void WhenHasPriorityOverSequence()
    {
        var stub = new WhenCalculatorStub();

        #region when-priority-over-sequence
        // Sequence configured via OnCall().ThenCall()
        stub.Add.OnCall((a, b) => 1).ThenCall((a, b) => 2);

        // When() has higher priority
        stub.Add.When(1, 2).Returns(100);
        #endregion

        IWhenCalculator calc = stub;

        // When() matches for (1, 2), sequence not consulted
        Assert.Equal(100, calc.Add(1, 2));

        // Non-matching calls use sequence
        Assert.Equal(1, calc.Add(5, 5));  // First in sequence
        Assert.Equal(2, calc.Add(6, 6));  // Second in sequence
    }
}

// =============================================================================
// Verification
// =============================================================================

public class WhenVerificationTests
{
    [Fact]
    public void ChainVerification_Complete()
    {
        var stub = new WhenCalculatorStub();

        #region when-verification-complete
        // Chain with ThenCall terminal
        var chain = stub.Add
            .When(1, 2).Returns(100)
            .ThenCall((a, b) => 999);
        #endregion

        IWhenCalculator calc = stub;

        // Consume When matcher
        calc.Add(1, 2);

        // Use ThenCall terminal
        calc.Add(9, 9);

        // Verify succeeds - chain reached terminal state
        chain.Verify();
    }

    [Fact]
    public void ChainVerification_IncompleteThrows()
    {
        var stub = new WhenCalculatorStub();

        #region when-verification-incomplete
        // Chain with multiple matchers
        var chain = stub.Add
            .When(1, 2).Returns(100)
            .ThenWhen(2, 3).Returns(200);
        #endregion

        IWhenCalculator calc = stub;

        // Only consume first matcher
        calc.Add(1, 2);

        // Verify fails - second matcher not consumed
        Assert.Throws<VerificationException>(() => chain.Verify());
    }

    [Fact]
    public void VerifiableBatch()
    {
        var stub = new WhenCalculatorStub();

        #region when-verifiable-batch
        // Mark chain for batch verification
        stub.Add
            .When(1, 2).Returns(100)
            .ThenCall((a, b) => 999)
            .Verifiable();
        #endregion

        IWhenCalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(9, 9);

        // stub.Verify() checks all Verifiable() chains
        stub.Verify();
    }

    [Fact]
    public void VoidVerifyTimes()
    {
        var stub = new WhenProcessorStub();

        #region when-void-verify-times
        // Track specific parameter combination
        var chain = stub.Process.When(1, 2);
        #endregion

        IWhenProcessor processor = stub;
        processor.Process(1, 2);  // Matches
        processor.Process(1, 2);  // Matches
        processor.Process(3, 4);  // Doesn't match

        // Verify specific combination was called exactly twice
        chain.Verify(Times.Exactly(2));
    }
}

// =============================================================================
// Resetting
// =============================================================================

public class WhenResetTests
{
    [Fact]
    public void ChainReset_RestartsFromBeginning()
    {
        var stub = new WhenCalculatorStub();

        #region when-reset-chain
        // Chain tracks position - Reset() restarts from beginning
        var chain = stub.Add
            .When(1, 2).Returns(100)
            .ThenCall((a, b) => 999);
        #endregion

        IWhenCalculator calc = stub;

        // Consume When matcher and use ThenCall
        calc.Add(1, 2);
        calc.Add(9, 9);

        // Reset restarts chain from beginning
        chain.Reset();

        // When matcher is available again
        Assert.Equal(100, calc.Add(1, 2));
    }

    [Fact]
    public void InterceptorReset_ResetsWhenChain()
    {
        var stub = new WhenCalculatorStub();

        #region when-reset-interceptor
        // Interceptor Reset() also resets When chain
        stub.Add
            .When(1, 2).Returns(100)
            .ThenCall((a, b) => 999);
        #endregion

        IWhenCalculator calc = stub;
        calc.Add(1, 2);  // Consume When matcher

        // Interceptor Reset() also resets When chain
        stub.Add.Reset();

        // When matcher is available again
        Assert.Equal(100, calc.Add(1, 2));
    }
}

// =============================================================================
// Async Methods
// =============================================================================

public class WhenAsyncTests
{
    [Fact]
    public async Task AsyncAutoWrap_ReturnsWrapsValue()
    {
        var stub = new WhenAsyncDataServiceStub();

        #region when-async-autowrap
        // Returns() auto-wraps with Task.FromResult()
        stub.GetAsync.When("hello").Returns("HELLO");
        #endregion

        IWhenAsyncDataService service = stub;

        // No Task.FromResult needed in configuration
        var result = await service.GetAsync("hello");
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task AsyncThenCall_RequiresExplicitTask()
    {
        var stub = new WhenAsyncDataServiceStub();

        #region when-async-thencall
        // Returns() auto-wraps, ThenCall uses full delegate
        stub.GetAsync
            .When("first").Returns("FIRST")
            .ThenCall(s => Task.FromResult(s.ToUpper()));
        #endregion

        IWhenAsyncDataService service = stub;

        Assert.Equal("FIRST", await service.GetAsync("first"));
        Assert.Equal("SECOND", await service.GetAsync("second"));
    }
}

// =============================================================================
// Use Cases
// =============================================================================

public class WhenUseCaseTests
{
    [Fact]
    public void UseCase_RetryLogic()
    {
        var stub = new WhenDataFetcherStub();

        #region when-usecase-retry
        // Simulate a service that fails once then succeeds
        stub.FetchData
            .When("user:123").Returns((string?)null)
            .ThenWhen("user:123").Returns("{ \"name\": \"Alice\" }");
        #endregion

        IWhenDataFetcher fetcher = stub;

        // First attempt fails
        var firstAttempt = fetcher.FetchData("user:123");
        Assert.Null(firstAttempt);

        // Retry succeeds
        var retry = fetcher.FetchData("user:123");
        Assert.Equal("{ \"name\": \"Alice\" }", retry);
    }

    [Fact]
    public void UseCase_StateTransitions()
    {
        var stub = new WhenStatusServiceStub();

        #region when-usecase-state-transitions
        // For parameterless methods, use OnCall().ThenCall() sequences
        stub.GetStatus
            .OnCall(() => "Pending")
            .ThenCall(() => "Processing")
            .ThenCall(() => "Complete");
        #endregion

        IWhenStatusService service = stub;

        // Status progresses with each call
        Assert.Equal("Pending", service.GetStatus());
        Assert.Equal("Processing", service.GetStatus());
        Assert.Equal("Complete", service.GetStatus());

        // After sequence exhausted: repeats last value in non-strict mode (NSubstitute behavior)
        Assert.Equal("Complete", service.GetStatus());
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public class WhenCompleteExampleTests
{
    [Fact]
    public void CompleteExample_PaymentService()
    {
        var stub = new WhenPaymentGatewayStub();

        #region when-complete-example
        // Payment gateway with different responses for specific amounts
        stub.ProcessPayment
            .When(100m).Returns(new WhenPaymentResult { Success = true, Message = "Payment processed" })
            .ThenWhen(0m).Returns(new WhenPaymentResult { Success = false, Message = "Invalid amount" })
            .ThenWhen((amt) => amt > 1000m).Returns(new WhenPaymentResult { RequiresApproval = true })
            .ThenCall((amt) => new WhenPaymentResult { Success = true, Message = $"Processed ${amt}" })
            .Verifiable();
        #endregion

        IWhenPaymentGateway gateway = stub;

        // Test specific scenarios
        var result100 = gateway.ProcessPayment(100m);
        Assert.True(result100.Success);

        var resultZero = gateway.ProcessPayment(0m);
        Assert.False(resultZero.Success);
        Assert.Equal("Invalid amount", resultZero.Message);

        var resultLarge = gateway.ProcessPayment(5000m);
        Assert.True(resultLarge.RequiresApproval);

        var resultGeneral = gateway.ProcessPayment(250m);
        Assert.True(resultGeneral.Success);
        Assert.Equal("Processed $250", resultGeneral.Message);

        // Verify the chain was fully exercised
        stub.Verify();
    }
}

// =============================================================================
// Interceptor API Reference - When Chain API
// =============================================================================

public class WhenChainApiTests
{
    [Fact]
    public void WhenChain_ReturnMethod()
    {
        var stub = new WhenCalculatorStub();

        #region when-chain-return-method-api
        // When() returns IWhenBuilder, which requires .Returns() to complete
        var chain = stub.Calculate.When(5, 10).Returns(50);

        // chain is IWhenChain - can continue chaining or verify
        chain.ThenWhen(1, 2).Returns(100);
        #endregion

        IWhenCalculator calc = stub;
        Assert.Equal(50, calc.Calculate(5, 10));
        Assert.Equal(100, calc.Calculate(1, 2));
    }

    [Fact]
    public void WhenChain_VoidMethod()
    {
        var stub = new WhenProcessorStub();

        var called = false;
        #region when-chain-void-method-api
        // Void methods: When() returns IVoidWhenChain directly
        var chain = stub.Process.When(1, 2);

        // .Call() is optional - adds callback for side effects
        chain.Call((a, b) => called = true);
        #endregion

        IWhenProcessor processor = stub;
        processor.Process(1, 2);

        Assert.True(called);

        // Verify this matcher was called once (parameter-specific count)
        chain.Verify(Times.Once);
    }
}
