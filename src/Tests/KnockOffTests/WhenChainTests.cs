using KnockOff;

namespace KnockOff.Tests;

/// <summary>
/// Comprehensive tests for the When API (Parameter-Specific Matching).
/// Phase 10-12 of the Parameter-Specific Matching feature.
///
/// Tests cover:
/// - When(value).Return(value) basic case
/// - When(predicate).Return(value) basic case
/// - ThenWhen fluent chaining (Phase 12 fix)
/// - ThenCall terminal behavior (repeats forever)
/// - ThenNone terminal behavior (exhausts, falls through)
/// - Fallback to OnCall/Returns when When doesn't match
/// - Verification of When chains
/// - Reset() clears HEAD and all matcher CallCounts
/// - Priority order (When > Sequence > Returns > OnCall)
/// - Async wrapping (Returns(value) wraps with Task.FromResult)
/// - All four patterns (Standalone, Inline Interface, Inline Class, Inline Delegate)
/// </summary>
public class WhenChainTests
{
	#region Pattern 1: Standalone - Basic When Tests

	[Fact]
	public void Standalone_When_Value_Returns_BasicCase()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);

		var result = service.Add(1, 2);

		Assert.Equal(100, result);
	}

	[Fact]
	public void Standalone_When_Predicate_Returns_BasicCase()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When((a, b) => a > 10).Return(999);

		var result = service.Add(15, 0);

		Assert.Equal(999, result);
	}

	[Fact]
	public void Standalone_When_ThenWhen_FluentChaining_NonVoid()
	{
		// Phase 12 fix: ThenWhen is now accessible via concrete return types
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Fluent chain: When().Return().ThenWhen().Return()
		stub.Add
			.When(1, 2).Return(100)
			.ThenWhen(3, 4).Return(200)
			.ThenWhen((a, b) => a > 100).Return(999);

		// First matcher (1, 2)
		Assert.Equal(100, service.Add(1, 2));

		// Second matcher (3, 4) - HEAD advances after first match
		Assert.Equal(200, service.Add(3, 4));

		// Third matcher (predicate) - HEAD advances after second match
		Assert.Equal(999, service.Add(150, 0));

		// Last matcher repeats when matched
		Assert.Equal(999, service.Add(200, 0));
	}

	[Fact]
	public void Standalone_When_SingleArgument_Works()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Transform.When("hello").Return("HELLO");

		var result = service.Transform("hello");

		Assert.Equal("HELLO", result);
	}

	[Fact]
	public void Standalone_When_SingleArgument_Predicate_Works()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Transform.When(s => s.StartsWith("h")).Return("STARTS_WITH_H");

		Assert.Equal("STARTS_WITH_H", service.Transform("hello"));
		Assert.Equal("STARTS_WITH_H", service.Transform("hi"));
	}

	[Fact]
	public void Standalone_When_SingleMatcher_Repeats()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);

		// Single When repeats when matched (it's both first and last)
		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(100, service.Add(1, 2));
	}

	[Fact]
	public void Standalone_When_MultipleMatchersViaMultipleWhens()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Multiple When calls create multiple matchers in the chain
		stub.Add.When(1, 2).Return(100);
		stub.Add.When(2, 3).Return(200);  // Adds to chain
		stub.Add.When(3, 4).Return(300);  // Adds to chain

		// Each matcher is consumed in order
		Assert.Equal(100, service.Add(1, 2));   // First matcher consumed
		Assert.Equal(200, service.Add(2, 3));   // Second matcher consumed
		Assert.Equal(300, service.Add(3, 4));   // Third matcher - last, repeats
		Assert.Equal(300, service.Add(3, 4));   // Still third
	}

	#endregion

	#region ThenCall Terminal Behavior Tests

	[Fact]
	public void Standalone_ThenCall_RepeatsForever()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a + b);

		// First matcher consumed
		Assert.Equal(100, service.Add(1, 2));
		// ThenCall repeats forever (always matches)
		Assert.Equal(5, service.Add(2, 3));
		Assert.Equal(10, service.Add(4, 6));
		Assert.Equal(0, service.Add(0, 0));
	}

	[Fact]
	public void Standalone_ThenCall_UsesArgumentsInCallback()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<(int a, int b)>();
		stub.Add
			.When(1, 1).Return(1)
			.ThenCall((a, b) =>
			{
				calls.Add((a, b));
				return a * b;
			});

		service.Add(1, 1);   // Consumed
		service.Add(2, 3);   // ThenCall
		service.Add(4, 5);   // ThenCall

		Assert.Equal(2, calls.Count);
		Assert.Equal((2, 3), calls[0]);
		Assert.Equal((4, 5), calls[1]);
	}

	[Fact]
	public void Standalone_ThenCall_IsTerminal()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a + b);

		service.Add(1, 2);   // Consume first
		service.Add(9, 9);   // ThenCall (terminal)

		// Verify succeeds - chain reached terminal
		chain.Verify();
	}

	#endregion

	#region ThenNone Terminal Behavior Tests

	[Fact]
	public void Standalone_ThenNone_ExhaustsChain()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100).ThenNone();
		stub.Add.Return(999);  // Fallback

		Assert.Equal(100, service.Add(1, 2));   // First matcher consumed
		// ThenNone reached - chain exhausted, falls through
		Assert.Equal(999, service.Add(1, 2));   // Falls through to Returns
		Assert.Equal(999, service.Add(9, 9));   // Still falls through
	}

	[Fact]
	public void Standalone_ThenNone_IsTerminal()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Add.When(1, 2).Return(100).ThenNone();
		stub.Add.Return(999);

		service.Add(1, 2);   // Consume first
		service.Add(9, 9);   // Falls through (ThenNone exhausted)

		// Verify succeeds - chain reached terminal
		chain.Verify();
	}

	#endregion

	#region Fallback Behavior Tests

	[Fact]
	public void Standalone_When_FallsThrough_ToReturns()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);
		stub.Add.Return(999);

		Assert.Equal(100, service.Add(1, 2));   // When matches
		Assert.Equal(999, service.Add(9, 9));   // Falls through to Returns
	}

	[Fact]
	public void Standalone_When_FallsThrough_ToOnCall()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);
		stub.Add.Return((a, b) => a * b);

		Assert.Equal(100, service.Add(1, 2));   // When matches
		Assert.Equal(27, service.Add(9, 3));    // Falls through to OnCall
	}

	[Fact]
	public void Standalone_When_Coexists_WithOnCall()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Configure When and Return(callback)
		// When() always takes priority when it matches; Return(callback) handles the rest
		stub.Add.When(1, 2).Return(100);
		stub.Add.Return((a, b) => 300);

		// When has priority when it matches
		Assert.Equal(100, service.Add(1, 2));

		// When doesn't match - falls through to OnCall
		Assert.Equal(300, service.Add(9, 9));
	}

	[Fact]
	public void Standalone_When_Coexists_WithReturns()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Configure When and Returns (configured last)
		stub.Add.When(1, 2).Return(100);
		stub.Add.Return(200);

		// When has priority when it matches
		Assert.Equal(100, service.Add(1, 2));

		// When doesn't match - falls through to Returns
		Assert.Equal(200, service.Add(9, 9));
	}

	#endregion

	#region Priority Order Tests (When > Sequence > Returns > OnCall)

	[Fact]
	public void Priority_When_OverSequence()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.Return((a, b) => 1).ThenReturn((a, b) => 2);
		stub.Add.When(1, 2).Return(100);

		// When takes priority when matching
		Assert.Equal(100, service.Add(1, 2));
		// When doesn't match - falls through to sequence
		Assert.Equal(1, service.Add(9, 9));
		Assert.Equal(2, service.Add(8, 8));
	}

	[Fact]
	public void Priority_When_OverReturns()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.Return(999);
		stub.Add.When(1, 2).Return(100);

		Assert.Equal(100, service.Add(1, 2));   // When matches
		Assert.Equal(999, service.Add(9, 9));   // Falls through to Returns
	}

	[Fact]
	public void Priority_When_OverOnCall()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.Return((a, b) => 999);
		stub.Add.When(1, 2).Return(100);

		Assert.Equal(100, service.Add(1, 2));   // When matches
		Assert.Equal(999, service.Add(9, 9));   // Falls through to OnCall
	}

	#endregion

	#region Verification Tests

	[Fact]
	public void Standalone_When_Verify_SucceedsWhenTerminalReached()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a + b);

		service.Add(1, 2);   // Consume first
		service.Add(3, 4);   // Use ThenCall (terminal)

		// Should not throw - chain reached terminal state
		chain.Verify();
	}

	[Fact]
	public void Standalone_When_Verify_ThrowsWhenIncomplete()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);
		stub.Add.When(2, 3).Return(200);
		var chain = stub.Add.When(3, 4).Return(300);

		service.Add(1, 2);   // Only consume first
		// Second and third matchers not consumed

		Assert.Throws<VerificationException>(() => chain.Verify());
	}

	[Fact]
	public void Standalone_When_Verifiable_RegistersWithStub()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => 999)
			.Verifiable();

		service.Add(1, 2);
		service.Add(9, 9);   // Terminal reached

		// Stub.Verify() should succeed (chain completed)
		stub.Verify();
	}

	[Fact]
	public void Standalone_When_Verifiable_StubVerifyThrows_WhenIncomplete()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);
		stub.Add.When(2, 3).Return(200).Verifiable();

		service.Add(1, 2);   // Only first matcher

		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	#endregion

	#region Reset Tests

	[Fact]
	public void Standalone_When_Reset_ClearsHEAD()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Add.When(1, 2).Return(100).ThenCall((a, b) => 999);

		service.Add(1, 2);   // Consume first
		service.Add(9, 9);   // Terminal

		chain.Reset();

		// After reset, HEAD is back at start
		Assert.Equal(100, service.Add(1, 2));
	}

	[Fact]
	public void Standalone_When_Reset_ClearsMatcherCallCounts()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a + b);

		service.Add(1, 2);
		service.Add(2, 3);
		service.Add(3, 4);

		chain.Reset();

		// Verify would throw if chain incomplete
		Assert.Throws<VerificationException>(() => chain.Verify());
	}

	[Fact]
	public void Standalone_InterceptorReset_ClearsWhenChain()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100).ThenCall((a, b) => 999);

		service.Add(1, 2);
		service.Add(9, 9);

		// Reset via interceptor
		stub.Add.Reset();

		// Chain should restart
		Assert.Equal(100, service.Add(1, 2));
	}

	#endregion

	#region Async Method Tests
	// Phase 14: For async methods, When().Return() now auto-wraps with Task.FromResult.
	// This matches the behavior of the regular interceptor Returns() method.
	// Users no longer need to manually wrap with Task.FromResult().

	[Fact]
	public async Task Standalone_When_Async_Returns_AutoWraps()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// When chain Returns() for async methods auto-wraps with Task.FromResult
		stub.GetAsync.When("hello").Return("HELLO");  // No Task.FromResult needed!

		var result = await service.GetAsync("hello");

		Assert.Equal("HELLO", result);
	}

	[Fact]
	public async Task Standalone_When_Async_Predicate_Works()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.GetAsync.When(s => s.Length > 5).Return("LONG");  // Auto-wrapped

		var result = await service.GetAsync("longstring");

		Assert.Equal("LONG", result);
	}

	[Fact]
	public async Task Standalone_When_Async_FallbackToReturns()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Both When chain Returns() and regular Returns() accept unwrapped type
		stub.GetAsync.When("special").Return("SPECIAL");
		stub.GetAsync.Return("default");

		Assert.Equal("SPECIAL", await service.GetAsync("special"));
		Assert.Equal("default", await service.GetAsync("other"));
	}

	[Fact]
	public async Task Standalone_When_Async_ThenCall_Works()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Returns auto-wraps, but ThenCall still uses the full delegate type (returns Task<T>)
		stub.GetAsync
			.When("first").Return("FIRST")  // Auto-wrapped
			.ThenCall(s => Task.FromResult(s.ToUpper()));  // ThenCall uses full delegate type

		Assert.Equal("FIRST", await service.GetAsync("first"));
		Assert.Equal("HELLO", await service.GetAsync("hello"));
	}

	[Fact]
	public async Task Standalone_When_Async_ThenWhen_ChainWorks()
	{
		// Test that ThenWhen also works with auto-wrapping
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.GetAsync
			.When("first").Return("FIRST")
			.ThenWhen("second").Return("SECOND")
			.ThenWhen(s => s.StartsWith("x")).Return("X_VALUE");

		Assert.Equal("FIRST", await service.GetAsync("first"));
		Assert.Equal("SECOND", await service.GetAsync("second"));
		Assert.Equal("X_VALUE", await service.GetAsync("xyz"));
		// Last matcher repeats
		Assert.Equal("X_VALUE", await service.GetAsync("xabc"));
	}

	#endregion

	#region Null Matching Tests

	[Fact]
	public void Standalone_When_NullMatching_ViaPredicate()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		// Match null via predicate (direct When(null) would throw)
		stub.Transform.When(s => s == null).Return("WAS_NULL");

		var result = service.Transform(null!);

		Assert.Equal("WAS_NULL", result);
	}

	[Fact]
	public void Standalone_When_EmptyString_DirectMatch()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Transform.When("").Return("EMPTY");

		Assert.Equal("EMPTY", service.Transform(""));
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void When_NoMatchFallsToDefault_InNonStrictMode()
	{
		var stub = new WhenChainTestStub();
		stub.Strict = false;
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);

		Assert.Equal(100, service.Add(1, 2));
		// No match, no fallback configured - returns default
		Assert.Equal(0, service.Add(9, 9));
	}

	[Fact]
	public void When_NoMatchThrows_InStrictMode()
	{
		var stub = new WhenChainTestStub();
		stub.Strict = true;
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);

		Assert.Equal(100, service.Add(1, 2));
		// No match, no fallback - strict mode throws
		Assert.Throws<StubException>(() => service.Add(9, 9));
	}

	[Fact]
	public void When_CanConfigureMultipleMethodsIndependently()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);
		stub.Transform.When("hello").Return("HELLO");

		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal("HELLO", service.Transform("hello"));
	}

	[Fact]
	public void When_DoesNotClearExistingConfiguration()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Add.Return(999);
		stub.Add.When(1, 2).Return(100);

		// When doesn't clear Returns
		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(999, service.Add(9, 9));  // Fallback still works
	}

	#endregion

	#region Pattern 2: Inline Interface Tests

	[Fact]
	public void InlineInterface_When_Value_Returns_Works()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);

		Assert.Equal(100, service.Add(1, 2));
	}

	[Fact]
	public void InlineInterface_When_Predicate_Returns_Works()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		stub.Add.When((a, b) => a > 10).Return(999);

		Assert.Equal(999, service.Add(15, 0));
	}

	[Fact]
	public void InlineInterface_When_ThenWhen_FluentChaining()
	{
		// Phase 12 fix: ThenWhen is now accessible via concrete return types
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		stub.Add
			.When(1, 2).Return(100)
			.ThenWhen(3, 4).Return(200)
			.ThenWhen((a, b) => a > 100).Return(999);

		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(200, service.Add(3, 4));
		Assert.Equal(999, service.Add(150, 0));
	}

	[Fact]
	public void InlineInterface_ThenCall_Terminal()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a * b);

		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(20, service.Add(4, 5));
	}

	[Fact]
	public void InlineInterface_ThenNone_Exhausts()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100).ThenNone();
		stub.Add.Return(999);

		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(999, service.Add(1, 2));
	}

	[Fact]
	public void InlineInterface_Verification_Works()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		var chain = stub.Add
			.When(1, 2).Return(100)
			.ThenCall((a, b) => 200);

		service.Add(1, 2);
		service.Add(9, 9);

		chain.Verify();
	}

	[Fact]
	public void InlineInterface_Fallback_Works()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		stub.Add.When(1, 2).Return(100);
		stub.Add.Return(999);

		Assert.Equal(100, service.Add(1, 2));
		Assert.Equal(999, service.Add(9, 9));
	}

	#endregion

	#region Pattern 3: Inline Class Tests

	[Fact]
	public void InlineClass_OnCall_Works()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual.Return((a, b) => a * b);

		Assert.Equal(6, instance.ComputeVirtual(2, 3));
	}

	[Fact]
	public void InlineClass_FallsBackToBase_WhenNotConfigured()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		// No OnCall configured, should use base class implementation
		Assert.Equal(3, instance.ComputeVirtual(1, 2));  // Base returns a + b
	}

	[Fact]
	public void InlineClass_TracksLastArgs()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		instance.ComputeVirtual(5, 7);

		Assert.Equal((5, 7), classStub.ComputeVirtual.LastArgs);
	}

	[Fact]
	public void InlineClass_Verification_Works()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		instance.ComputeVirtual(1, 2);

		classStub.ComputeVirtual.Verify();
	}

	[Fact]
	public void InlineClass_When_Value_Returns_Works()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual.When(1, 2).Return(100);

		Assert.Equal(100, instance.ComputeVirtual(1, 2));
	}

	[Fact]
	public void InlineClass_When_Predicate_Returns_Works()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual.When((a, b) => a > 10).Return(999);

		Assert.Equal(999, instance.ComputeVirtual(15, 0));
	}

	[Fact]
	public void InlineClass_When_ThenWhen_FluentChaining()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual
			.When(1, 2).Return(100)
			.ThenWhen(3, 4).Return(200)
			.ThenWhen((a, b) => a > 100).Return(999);

		Assert.Equal(100, instance.ComputeVirtual(1, 2));
		Assert.Equal(200, instance.ComputeVirtual(3, 4));
		Assert.Equal(999, instance.ComputeVirtual(150, 0));
	}

	[Fact]
	public void InlineClass_ThenCall_Terminal()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a * b);

		Assert.Equal(100, instance.ComputeVirtual(1, 2));
		Assert.Equal(20, instance.ComputeVirtual(4, 5));
	}

	[Fact]
	public void InlineClass_ThenNone_FallsBackToBase()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual.When(1, 2).Return(100).ThenNone();

		Assert.Equal(100, instance.ComputeVirtual(1, 2));
		// After ThenNone, falls back to base class implementation (a + b)
		Assert.Equal(5, instance.ComputeVirtual(2, 3));
	}

	[Fact]
	public void InlineClass_When_FallsBackToBase_WhenNotMatched()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual.When(1, 2).Return(100);

		Assert.Equal(100, instance.ComputeVirtual(1, 2));
		// Not matched, falls back to base class implementation (a + b)
		Assert.Equal(9, instance.ComputeVirtual(4, 5));
	}

	[Fact]
	public void InlineClass_When_Verification()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var chain = classStub.ComputeVirtual
			.When(1, 2).Return(100)
			.ThenCall((a, b) => 200);

		instance.ComputeVirtual(1, 2);
		instance.ComputeVirtual(9, 9);

		chain.Verify();
	}

	[Fact]
	public void InlineClass_Returns_Works()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		classStub.ComputeVirtual.Return(42);

		Assert.Equal(42, instance.ComputeVirtual(1, 2));
		Assert.Equal(42, instance.ComputeVirtual(9, 9));
	}

	#endregion

	#region Pattern 3: Inline Class Void Method Tests

	[Fact]
	public void InlineClass_VoidMethod_When_BasicCase()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var chain = classStub.ProcessVirtual.When(1, 2);
		instance.ProcessVirtual(1, 2);

		chain.Verify(Times.Once);
	}

	[Fact]
	public void InlineClass_VoidMethod_When_Call()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var calls = new List<(int a, int b)>();
		classStub.ProcessVirtual.When(1, 2).Call((a, b) => calls.Add((a, b)));

		instance.ProcessVirtual(1, 2);
		instance.ProcessVirtual(1, 2);

		Assert.Equal(2, calls.Count);
	}

	[Fact]
	public void InlineClass_VoidMethod_When_Predicate()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var calls = new List<(int a, int b)>();
		classStub.ProcessVirtual.When((a, b) => a > 10).Call((a, b) => calls.Add((a, b)));

		instance.ProcessVirtual(1, 2);    // Doesn't match
		instance.ProcessVirtual(15, 20);  // Matches
		instance.ProcessVirtual(25, 30);  // Matches

		Assert.Equal(2, calls.Count);
	}

	[Fact]
	public void InlineClass_VoidMethod_When_VerifyTimes()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var chain = classStub.ProcessVirtual.When(1, 2);

		instance.ProcessVirtual(1, 2);
		instance.ProcessVirtual(1, 2);
		instance.ProcessVirtual(3, 4);

		chain.Verify(Times.Exactly(2));
	}

	[Fact]
	public void InlineClass_VoidMethod_When_ThenWhen_Chaining()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var calls = new List<string>();
		classStub.ProcessVirtual
			.When(1, 2).Call((a, b) => calls.Add("first"))
			.ThenWhen(3, 4).Call((a, b) => calls.Add("second"))
			.ThenWhen((a, b) => a > 100).Call((a, b) => calls.Add("large"));

		instance.ProcessVirtual(1, 2);
		instance.ProcessVirtual(3, 4);
		instance.ProcessVirtual(200, 1);
		instance.ProcessVirtual(200, 1);  // Last matcher repeats

		Assert.Equal(new[] { "first", "second", "large", "large" }, calls);
	}

	[Fact]
	public void InlineClass_VoidMethod_When_ThenCall_Terminal()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var calls = new List<string>();
		classStub.ProcessVirtual
			.When(1, 2).Call((a, b) => calls.Add("specific"))
			.ThenCall((a, b) => calls.Add($"any:{a},{b}"));

		instance.ProcessVirtual(1, 2);
		instance.ProcessVirtual(9, 9);
		instance.ProcessVirtual(8, 8);

		Assert.Equal(new[] { "specific", "any:9,9", "any:8,8" }, calls);
	}

	[Fact]
	public void InlineClass_VoidMethod_When_ThenNone_Exhausts()
	{
		var classStub = new WhenChainInlineStubs.Stubs.WhenChainBaseClass();
		WhenChainBaseClass instance = classStub.Object;

		var calls = new List<string>();
		classStub.ProcessVirtual.When(1, 2).Call((a, b) => calls.Add("matched")).ThenNone();
		classStub.ProcessVirtual.Call((a, b) => calls.Add("fallback"));

		instance.ProcessVirtual(1, 2);
		instance.ProcessVirtual(1, 2);  // Falls through to OnCall
		instance.ProcessVirtual(9, 9);  // Falls through to OnCall

		Assert.Equal(new[] { "matched", "fallback", "fallback" }, calls);
	}

	#endregion

	#region Pattern 4: Inline Delegate Tests

	[Fact]
	public void InlineDelegate_When_Value_Returns_Works()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		stub.Interceptor.When("hello").Return("HELLO");

		Assert.Equal("HELLO", formatter("hello"));
	}

	[Fact]
	public void InlineDelegate_When_Predicate_Returns_Works()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		stub.Interceptor.When(s => s.Length > 5).Return("LONG");

		Assert.Equal("LONG", formatter("longstring"));
	}

	[Fact]
	public void InlineDelegate_When_ThenWhen_FluentChaining()
	{
		// Phase 12 fix: ThenWhen is now accessible via concrete return types
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		stub.Interceptor
			.When("one").Return("ONE")
			.ThenWhen("two").Return("TWO")
			.ThenWhen(s => s.StartsWith("x")).Return("X_PREFIX");

		Assert.Equal("ONE", formatter("one"));
		Assert.Equal("TWO", formatter("two"));
		Assert.Equal("X_PREFIX", formatter("xyz"));
	}

	[Fact]
	public void InlineDelegate_ThenCall_Terminal()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		stub.Interceptor
			.When("special").Return("SPECIAL")
			.ThenCall(s => s.ToUpper());

		Assert.Equal("SPECIAL", formatter("special"));
		Assert.Equal("HELLO", formatter("hello"));
	}

	[Fact]
	public void InlineDelegate_ThenNone_Exhausts()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		stub.Interceptor.When("one").Return("ONE").ThenNone();
		stub.Interceptor.Return("default");

		Assert.Equal("ONE", formatter("one"));
		Assert.Equal("default", formatter("one"));
	}

	[Fact]
	public void InlineDelegate_MultiParam_When_Works()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenCalculator();
		WhenCalculator calculator = stub;

		stub.Interceptor.When(1, 2).Return(100);

		Assert.Equal(100, calculator(1, 2));
	}

	[Fact]
	public void InlineDelegate_MultiParam_Predicate_Works()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenCalculator();
		WhenCalculator calculator = stub;

		stub.Interceptor.When((a, b) => a > 10).Return(999);

		Assert.Equal(999, calculator(20, 0));
	}

	[Fact]
	public void InlineDelegate_ThenCall_UsesArguments()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenCalculator();
		WhenCalculator calculator = stub;

		stub.Interceptor
			.When(1, 2).Return(100)
			.ThenCall((a, b) => a * b);

		Assert.Equal(100, calculator(1, 2));
		Assert.Equal(20, calculator(4, 5));
	}

	[Fact]
	public void InlineDelegate_Verification_Works()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		var chain = stub.Interceptor
			.When("one").Return("ONE")
			.ThenCall(s => s.ToUpper());

		formatter("one");
		formatter("two");

		chain.Verify();
	}

	[Fact]
	public void InlineDelegate_Reset_Works()
	{
		var stub = new WhenChainDelegateStubs.Stubs.WhenFormatter();
		WhenFormatter formatter = stub;

		var chain = stub.Interceptor
			.When("one").Return("ONE")
			.ThenCall(s => "TERMINAL");

		formatter("one");
		formatter("two");

		chain.Reset();

		Assert.Equal("ONE", formatter("one"));
	}

	#endregion

	#region Phase 11: Void Method Tests

	[Fact]
	public void Standalone_VoidMethod_When_BasicCase()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Process.When(1, 2);

		service.Process(1, 2);
		service.Process(1, 2);

		// Verify parameter-specific call tracking works via chain
		chain.Verify(Times.Exactly(2));
	}

	[Fact]
	public void Standalone_VoidMethod_When_Call_WithCallback()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<(int a, int b)>();
		stub.Process.When(1, 2).Call((a, b) => calls.Add((a, b)));

		service.Process(1, 2);
		service.Process(1, 2);

		Assert.Equal(2, calls.Count);
		Assert.All(calls, c => Assert.Equal((1, 2), c));
	}

	[Fact]
	public void Standalone_VoidMethod_When_Predicate()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var largeCalls = new List<(int a, int b)>();
		stub.Process.When((a, b) => a > 10).Call((a, b) => largeCalls.Add((a, b)));

		service.Process(1, 2);    // Doesn't match
		service.Process(15, 20);  // Matches
		service.Process(25, 30);  // Matches

		Assert.Equal(2, largeCalls.Count);
	}

	[Fact]
	public void Standalone_VoidMethod_When_VerifyTimes()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Process.When(1, 2);

		service.Process(1, 2);
		service.Process(1, 2);
		service.Process(3, 4);  // Doesn't match When

		// Verify specific parameter combination was called exactly twice
		chain.Verify(Times.Exactly(2));
	}

	[Fact]
	public void Standalone_VoidMethod_When_VerifyTimes_Fails()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var chain = stub.Process.When(1, 2);

		service.Process(1, 2);

		// Should fail - expected 3 calls but only 1
		Assert.Throws<VerificationException>(() => chain.Verify(Times.Exactly(3)));
	}

	[Fact]
	public void Standalone_VoidMethod_When_ThenWhen_Chaining()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<string>();
		stub.Process
			.When(1, 2).Call((a, b) => calls.Add("first"))
			.ThenWhen(3, 4).Call((a, b) => calls.Add("second"))
			.ThenWhen((a, b) => a > 100).Call((a, b) => calls.Add("large"));

		service.Process(1, 2);    // First matcher
		service.Process(3, 4);    // Second matcher
		service.Process(200, 1);  // Third matcher (large)
		service.Process(200, 1);  // Third matcher repeats

		Assert.Equal(new[] { "first", "second", "large", "large" }, calls);
	}

	[Fact]
	public void Standalone_VoidMethod_When_ThenCall_Terminal()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<string>();
		stub.Process
			.When(1, 2).Call((a, b) => calls.Add("specific"))
			.ThenCall((a, b) => calls.Add($"any:{a},{b}"));

		service.Process(1, 2);    // First matcher
		service.Process(9, 9);    // ThenCall
		service.Process(8, 8);    // ThenCall repeats

		Assert.Equal(new[] { "specific", "any:9,9", "any:8,8" }, calls);
	}

	[Fact]
	public void Standalone_VoidMethod_When_ThenNone_Exhausts()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<string>();
		stub.Process.When(1, 2).Call((a, b) => calls.Add("matched")).ThenNone();
		stub.Process.Call((a, b) => calls.Add("fallback"));

		service.Process(1, 2);    // First matcher
		service.Process(1, 2);    // Falls through to OnCall (ThenNone exhausted)
		service.Process(9, 9);    // Falls through to OnCall

		Assert.Equal(new[] { "matched", "fallback", "fallback" }, calls);
	}

	[Fact]
	public void Standalone_VoidMethod_When_FallsThrough_ToOnCall()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<string>();
		stub.Process.When(1, 2).Call((a, b) => calls.Add("when"));
		stub.Process.Call((a, b) => calls.Add("oncall"));

		service.Process(1, 2);    // When matches
		service.Process(9, 9);    // Falls through to OnCall

		Assert.Equal(new[] { "when", "oncall" }, calls);
	}

	[Fact]
	public void Standalone_VoidMethod_When_Verifiable()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		stub.Process
			.When(1, 2)
			.ThenCall((a, b) => { })
			.Verifiable();

		service.Process(1, 2);  // Consume first
		service.Process(9, 9);  // Terminal reached

		// Should not throw - chain completed
		stub.Verify();
	}

	[Fact]
	public void Standalone_VoidMethod_When_Reset()
	{
		var stub = new WhenChainTestStub();
		IWhenChainTestService service = stub;

		var calls = new List<string>();
		var chain = stub.Process
			.When(1, 2).Call((a, b) => calls.Add("first"))
			.ThenCall((a, b) => calls.Add("terminal"));

		service.Process(1, 2);    // First
		service.Process(9, 9);    // Terminal

		calls.Clear();
		chain.Reset();

		service.Process(1, 2);    // First again after reset

		Assert.Equal(new[] { "first" }, calls);
	}

	#endregion

	#region Void Method - Inline Interface Tests

	[Fact]
	public void InlineInterface_VoidMethod_When_BasicCase()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		var chain = stub.Process.When(1, 2);
		service.Process(1, 2);

		chain.Verify(Times.Once);
	}

	[Fact]
	public void InlineInterface_VoidMethod_When_Call()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		var calls = new List<(int a, int b)>();
		stub.Process.When(1, 2).Call((a, b) => calls.Add((a, b)));

		service.Process(1, 2);

		Assert.Single(calls);
	}

	[Fact]
	public void InlineInterface_VoidMethod_When_VerifyTimes()
	{
		var stub = new WhenChainInlineStubs.Stubs.IWhenChainTestService();
		IWhenChainTestService service = stub;

		var chain = stub.Process.When(1, 2);

		service.Process(1, 2);
		service.Process(1, 2);

		chain.Verify(Times.Exactly(2));
	}

	#endregion

	// Note: Void Method - Inline Class Tests are in the "Pattern 3: Inline Class Void Method Tests" region.

	#region Void Delegate Tests

	[Fact]
	public void VoidDelegate_When_BasicCase()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var chain = stub.Interceptor.When(1, 2);
		processor(1, 2);

		chain.Verify(Times.Once);
	}

	[Fact]
	public void VoidDelegate_When_Call()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var calls = new List<(int a, int b)>();
		stub.Interceptor.When(1, 2).Call((a, b) => calls.Add((a, b)));

		processor(1, 2);
		processor(1, 2);

		Assert.Equal(2, calls.Count);
	}

	[Fact]
	public void VoidDelegate_When_Predicate()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var calls = new List<(int a, int b)>();
		stub.Interceptor.When((a, b) => a > 10).Call((a, b) => calls.Add((a, b)));

		processor(1, 2);    // Doesn't match
		processor(15, 20);  // Matches
		processor(25, 30);  // Matches

		Assert.Equal(2, calls.Count);
	}

	[Fact]
	public void VoidDelegate_When_VerifyTimes()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var chain = stub.Interceptor.When(1, 2);

		processor(1, 2);
		processor(1, 2);
		processor(3, 4);

		chain.Verify(Times.Exactly(2));
	}

	[Fact]
	public void VoidDelegate_When_ThenWhen_Chaining()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var calls = new List<string>();
		stub.Interceptor
			.When(1, 2).Call((a, b) => calls.Add("first"))
			.ThenWhen(3, 4).Call((a, b) => calls.Add("second"));

		processor(1, 2);
		processor(3, 4);
		processor(3, 4);  // Last matcher repeats

		Assert.Equal(new[] { "first", "second", "second" }, calls);
	}

	[Fact]
	public void VoidDelegate_When_ThenCall_Terminal()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var calls = new List<string>();
		stub.Interceptor
			.When(1, 2).Call((a, b) => calls.Add("specific"))
			.ThenCall((a, b) => calls.Add("any"));

		processor(1, 2);
		processor(9, 9);
		processor(8, 8);

		Assert.Equal(new[] { "specific", "any", "any" }, calls);
	}

	[Fact]
	public void VoidDelegate_When_ThenNone_Exhausts()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var calls = new List<string>();
		stub.Interceptor.When(1, 2).Call((a, b) => calls.Add("matched")).ThenNone();
		stub.Interceptor.Call((a, b) => calls.Add("fallback"));

		processor(1, 2);
		processor(1, 2);  // Falls through

		Assert.Equal(new[] { "matched", "fallback" }, calls);
	}

	[Fact]
	public void VoidDelegate_When_Reset()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidProcessor();
		VoidProcessor processor = stub;

		var calls = new List<string>();
		var chain = stub.Interceptor
			.When(1, 2).Call((a, b) => calls.Add("first"))
			.ThenCall((a, b) => calls.Add("terminal"));

		processor(1, 2);
		processor(9, 9);

		calls.Clear();
		chain.Reset();

		processor(1, 2);

		Assert.Equal(new[] { "first" }, calls);
	}

	[Fact]
	public void VoidDelegate_SingleParam_When()
	{
		var stub = new WhenChainDelegateStubs.Stubs.VoidFormatter();
		VoidFormatter formatter = stub;

		var calls = new List<string>();
		stub.Interceptor.When("hello").Call(s => calls.Add(s));

		formatter("hello");
		formatter("world");  // Doesn't match, falls through

		Assert.Single(calls);
		Assert.Equal("hello", calls[0]);
	}

	#endregion
}

#region Test Interface and Types

public interface IWhenChainTestService
{
	int Add(int a, int b);
	string Transform(string input);
	Task<string> GetAsync(string input);
	void Process(int a, int b);  // Void method for Phase 11 tests
}

[KnockOff]
public partial class WhenChainTestStub : IWhenChainTestService
{
}

/// <summary>
/// Base class for inline class pattern testing with When support.
/// </summary>
public class WhenChainBaseClass
{
	public virtual int ComputeVirtual(int a, int b) => a + b;
	public virtual string? GetVirtualName() => "base";
	public virtual void ProcessVirtual(int a, int b) { }  // Void method for Phase 11 tests
}

/// <summary>Delegate for single-param When testing.</summary>
public delegate string WhenFormatter(string input);

/// <summary>Delegate for multi-param When testing.</summary>
public delegate int WhenCalculator(int a, int b);

/// <summary>Void delegate for Phase 11 testing.</summary>
public delegate void VoidProcessor(int a, int b);

/// <summary>Void delegate with single param for Phase 11 testing.</summary>
public delegate void VoidFormatter(string input);

/// <summary>Inline stubs container for interface and class patterns.</summary>
[KnockOff<IWhenChainTestService>]
[KnockOff<WhenChainBaseClass>]
public partial class WhenChainInlineStubs
{
}

/// <summary>Inline stubs container for delegate patterns.</summary>
[KnockOff<WhenFormatter>]
[KnockOff<WhenCalculator>]
[KnockOff<VoidProcessor>]
[KnockOff<VoidFormatter>]
public partial class WhenChainDelegateStubs
{
}

#endregion
