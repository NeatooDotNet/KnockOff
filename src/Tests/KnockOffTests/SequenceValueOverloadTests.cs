namespace KnockOff.Tests;

/// <summary>
/// Tests for value-based sequence overloads.
/// Phase 5 of value-based overloads feature.
///
/// Property Get/OnCall support both values and callbacks, with ThenGet/ThenCall for sequencing:
/// - Get(value) - configures repeating value return
/// - Get(callback) - configures repeating callback
/// - Get(...).ThenGet(...) - elevates to sequence mode
/// </summary>
public partial class SequenceValueOverloadTests
{
	#region Property Sequence Value Tests

	[Fact]
	public void OnGet_WithValue_ReturnsSingleValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get("first");

		Assert.Equal("first", service.Name);
	}

	[Fact]
	public void OnGet_WithValue_ThenGet_WithCallback_ReturnsSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// Get returns builder interface, ThenGet elevates to sequence mode
		knockOff.Name.Get("first")
			.ThenGet(() => "second")
			.ThenGet(() => "third");

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
		Assert.Equal("third", service.Name);
	}

	[Fact]
	public void OnGet_WithCallback_ThenGet_ReturnsSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get(() => "callback first")
			.ThenGet(() => "callback second")
			.ThenGet(() => "callback third");

		Assert.Equal("callback first", service.Name);
		Assert.Equal("callback second", service.Name);
		Assert.Equal("callback third", service.Name);
	}

	[Fact]
	public void OnGet_WithValue_TracksCorrectly()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var sequence = knockOff.Name.Get("a")
			.ThenGet(() => "b")
			.ThenGet(() => "c");

		_ = service.Name;
		_ = service.Name;
		_ = service.Name;

		// Verify sequence was fully consumed
		sequence.Verify();
		knockOff.Name.VerifyGet(Times.Exactly(3));
	}

	[Fact]
	public void OnGet_WithValue_SupportsNullValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get((string?)null);

		Assert.Null(service.Name);
	}

	[Fact]
	public void OnGet_WithIntValue_WorksWithValueTypes()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.Get(42);

		Assert.Equal(42, service.Count);
	}

	[Fact]
	public void OnGet_WithBoolValue_WorksWithBooleans()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.IsEnabled.Get(true);

		Assert.True(service.IsEnabled);
	}

	#endregion

	#region Method Sequence Tests (Callback-Only)

	// Note: Method sequences only support callbacks, not direct values.
	// These tests demonstrate the existing callback-based sequences work correctly.

	[Fact]
	public void OnCall_WithCallbacks_ReturnsSequenceValues()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Return(() => "first")
			.ThenReturn(() => "second")
			.ThenReturn(() => "third");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void OnCall_WithCallbacks_TracksCorrectly()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var sequence = knockOff.GetOptional.Return(() => "a")
			.ThenReturn(() => "b")
			.ThenReturn(() => "c");

		service.GetOptional();
		service.GetOptional();
		service.GetOptional();

		sequence.Verify();
	}

	[Fact]
	public void OnCall_CanMixWithOnCallValue_AfterSequenceExhausted()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Set up sequence that will be exhausted
		knockOff.GetOptional.Return(() => "seq1").ThenReturn(() => "seq2");

		// Consume the sequence
		Assert.Equal("seq1", service.GetOptional());
		Assert.Equal("seq2", service.GetOptional());

		// Now configure with value - clears sequence
		knockOff.GetOptional.Return("after sequence");

		Assert.Equal("after sequence", service.GetOptional());
		Assert.Equal("after sequence", service.GetOptional());
	}

	#endregion

	#region Async Method Sequence Tests

	[Fact]
	public async Task AsyncMethod_OnCall_ReturnsSequenceValues()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		knockOff.GetRequiredAsync.Return(() => Task.FromResult("first"))
			.ThenReturn(() => Task.FromResult("second"))
			.ThenReturn(() => Task.FromResult("third"));

		Assert.Equal("first", await service.GetRequiredAsync());
		Assert.Equal("second", await service.GetRequiredAsync());
		Assert.Equal("third", await service.GetRequiredAsync());
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void OnGet_WithoutThenGet_RepeatsForever()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get("repeating");

		// Get without ThenGet repeats the same value forever
		Assert.Equal("repeating", service.Name);
		Assert.Equal("repeating", service.Name);
		Assert.Equal("repeating", service.Name);
	}

	[Fact]
	public void OnGet_ThenGet_Exhausted_RepeatsLastValueInNonStrictMode()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// Create sequence with ThenGet to enable exhaustion
		knockOff.Name.Get("first").ThenGet("second");

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
		// Third access - sequence exhausted, repeats last value in non-strict mode (NSubstitute behavior)
		Assert.Equal("second", service.Name);
		Assert.Equal("second", service.Name);
	}

	[Fact]
	public void OnGet_ThenGet_WithThenDefault_ReturnsDefaultInNonStrictMode()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// Create sequence with ThenDefault() to return default after exhaustion
		knockOff.Name.Get("first").ThenGet("second").ThenDefault();

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
		// Third access - sequence exhausted, returns default due to ThenDefault()
		Assert.Null(service.Name);
	}

	[Fact]
	public void OnGet_ThenGet_Exhausted_ThrowsInStrictMode()
	{
		var knockOff = new PropertyTestKnockOff();
		knockOff.Strict = true;
		IPropertyTest service = knockOff;

		// Create sequence with ThenGet to enable exhaustion
		knockOff.Name.Get("first").ThenGet("second");

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
		// Third access - sequence exhausted, throws in strict mode
		Assert.Throws<StubException>(() => service.Name);
	}

	[Fact]
	public void OnGet_VerifyIncomplete_Throws()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var sequence = knockOff.Name.Get("a")
			.ThenGet(() => "b")
			.ThenGet(() => "c");

		// Only consume two of three
		_ = service.Name;
		_ = service.Name;

		// Verify should throw - sequence incomplete
		Assert.Throws<VerificationException>(() => sequence.Verify());
	}

	[Fact]
	public void OnGet_Reset_AllowsReplay()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get("a").ThenGet(() => "b");

		Assert.Equal("a", service.Name);
		Assert.Equal("b", service.Name);

		// Reset and replay
		knockOff.Name.Reset();

		Assert.Equal("a", service.Name);
		Assert.Equal("b", service.Name);
	}

	[Fact]
	public void MethodOnCall_WithoutThenCall_RepeatsForever()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Return(() => "repeating");

		// OnCall without ThenCall repeats the same callback forever
		Assert.Equal("repeating", service.GetOptional());
		Assert.Equal("repeating", service.GetOptional());
		Assert.Equal("repeating", service.GetOptional());
	}

	[Fact]
	public void MethodOnCall_ThenCall_Exhausted_RepeatsLastValueInNonStrictMode()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Create sequence with ThenCall to enable exhaustion
		knockOff.GetOptional.Return(() => "first").ThenReturn(() => "second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, repeats last value in non-strict mode (NSubstitute behavior)
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
	}

	[Fact]
	public void MethodOnCall_ThenCall_WithThenDefault_ReturnsDefaultInNonStrictMode()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Create sequence with ThenDefault() to return default after exhaustion
		knockOff.GetOptional.Return(() => "first").ThenReturn(() => "second").ThenDefault();

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, returns default due to ThenDefault()
		Assert.Null(service.GetOptional());
	}

	[Fact]
	public void MethodOnCall_ThenCall_Exhausted_ThrowsInStrictMode()
	{
		var knockOff = new SampleKnockOff();
		knockOff.Strict = true;
		ISampleService service = knockOff;

		// Create sequence with ThenCall to enable exhaustion
		knockOff.GetOptional.Return(() => "first").ThenReturn(() => "second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, throws in strict mode
		Assert.Throws<StubException>(() => service.GetOptional());
	}

	#endregion

	#region Method Sequence Value Overload Tests

	/// <summary>
	/// Tests for method sequence value overloads (ThenReturns).
	/// Phase 2 of method-sequence-value-overloads feature.
	///
	/// DESIGN NOTE: Returns().ThenReturn() is NOT supported by design.
	/// ThenReturns is available on sequences started with OnCall().
	/// Use OnCall(() => value) to start a sequence with a constant value.
	/// </summary>

	[Fact]
	public void OnCall_ThenReturns_ReturnsSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// OnCall starts the sequence, ThenReturns adds values to it
		knockOff.GetOptional.Return(() => "first")
			.ThenReturn("second")
			.ThenReturn("third");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void OnCall_ThenReturns_MixedSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Mix callbacks and values in the same sequence
		knockOff.GetOptional.Return(() => "callback first")
			.ThenReturn("value second")
			.ThenReturn(() => "callback third")
			.ThenReturn("value fourth");

		Assert.Equal("callback first", service.GetOptional());
		Assert.Equal("value second", service.GetOptional());
		Assert.Equal("callback third", service.GetOptional());
		Assert.Equal("value fourth", service.GetOptional());
	}

	[Fact]
	public async Task AsyncMethod_ThenReturns_AutoWraps()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		// ThenReturns for Task<T> methods auto-wraps with Task.FromResult
		knockOff.GetRequiredAsync.Return(() => Task.FromResult("first"))
			.ThenReturn("second")
			.ThenReturn("third");

		Assert.Equal("first", await service.GetRequiredAsync());
		Assert.Equal("second", await service.GetRequiredAsync());
		Assert.Equal("third", await service.GetRequiredAsync());
	}

	[Fact]
	public async Task ValueTaskMethod_ThenReturns_AutoWraps()
	{
		var knockOff = new ValueTaskMethodKnockOff();
		IValueTaskMethodService service = knockOff;

		// ThenReturns for ValueTask<T> methods auto-wraps with new ValueTask<T>(value)
		knockOff.GetValueAsync.Return(() => new ValueTask<string>("first"))
			.ThenReturn("second")
			.ThenReturn("third");

		Assert.Equal("first", await service.GetValueAsync());
		Assert.Equal("second", await service.GetValueAsync());
		Assert.Equal("third", await service.GetValueAsync());
	}

	[Fact]
	public void ThenReturns_TracksCorrectly()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var sequence = knockOff.GetOptional.Return(() => "a")
			.ThenReturn("b")
			.ThenReturn("c");

		service.GetOptional();
		service.GetOptional();
		service.GetOptional();

		// Verify sequence was fully consumed
		sequence.Verify();
		knockOff.GetOptional.Verify(Times.Exactly(3));
	}

	[Fact]
	public void ThenReturns_SequenceExhaustion_RepeatsLastValueInNonStrictMode()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Create sequence with ThenReturns to enable exhaustion
		knockOff.GetOptional.Return(() => "first").ThenReturn("second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, repeats last value in non-strict mode (NSubstitute behavior)
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
	}

	[Fact]
	public void ThenReturns_WithThenDefault_ReturnsDefaultInNonStrictMode()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Create sequence with ThenDefault() to return default after exhaustion
		knockOff.GetOptional.Return(() => "first").ThenReturn("second").ThenDefault();

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, returns default due to ThenDefault()
		Assert.Null(service.GetOptional());
	}

	[Fact]
	public void ThenReturns_SequenceExhaustion_ThrowsInStrictMode()
	{
		var knockOff = new SampleKnockOff();
		knockOff.Strict = true;
		ISampleService service = knockOff;

		// Create sequence with ThenReturns to enable exhaustion
		knockOff.GetOptional.Return(() => "first").ThenReturn("second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, throws in strict mode
		Assert.Throws<StubException>(() => service.GetOptional());
	}

	[Fact]
	public void ThenReturns_NullValue_WorksCorrectly()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// ThenReturns with null should work (may need cast for nullable types)
		knockOff.GetOptional.Return(() => "first")
			.ThenReturn((string?)null)
			.ThenReturn("third");

		Assert.Equal("first", service.GetOptional());
		Assert.Null(service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	#endregion

	#region Params Sequence Tests

	/// <summary>
	/// Tests for params overloads on Returns and ThenReturns.
	/// Phase 3 of params-sequence-values feature.
	/// </summary>

	[Fact]
	public void Returns_Params_CreatesSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Returns with params creates a sequence: first value, then rest values
		knockOff.GetOptional.Return("first", "second", "third");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
		// After exhaustion, repeats last value (NSubstitute behavior)
		Assert.Equal("third", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void Returns_Params_SingleValue_UsesNonParamsOverload()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Single value uses non-params overload which returns MethodCallBuilderImpl
		// We can verify this by checking that the value repeats forever (not exhausted)
		knockOff.GetOptional.Return("single");

		Assert.Equal("single", service.GetOptional());
		Assert.Equal("single", service.GetOptional());
		Assert.Equal("single", service.GetOptional());
		Assert.Equal("single", service.GetOptional());
		// Repeats indefinitely without strict mode issues
	}

	[Fact]
	public void Returns_Params_TwoValues_CreatesSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Two values uses params overload, creating a sequence
		knockOff.GetOptional.Return("first", "second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Exhausted, repeats last
		Assert.Equal("second", service.GetOptional());
	}

	[Fact]
	public async Task Returns_Params_AsyncMethod_AutoWraps()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		// For Task<T> methods, params Returns auto-wraps with Task.FromResult
		knockOff.GetRequiredAsync.Return("first", "second", "third");

		Assert.Equal("first", await service.GetRequiredAsync());
		Assert.Equal("second", await service.GetRequiredAsync());
		Assert.Equal("third", await service.GetRequiredAsync());
		// Repeats last after exhaustion
		Assert.Equal("third", await service.GetRequiredAsync());
	}

	[Fact]
	public async Task Returns_Params_ValueTaskMethod_AutoWraps()
	{
		var knockOff = new ValueTaskMethodKnockOff();
		IValueTaskMethodService service = knockOff;

		// For ValueTask<T> methods, params Returns auto-wraps with new ValueTask<T>(value)
		knockOff.GetValueAsync.Return("first", "second", "third");

		Assert.Equal("first", await service.GetValueAsync());
		Assert.Equal("second", await service.GetValueAsync());
		Assert.Equal("third", await service.GetValueAsync());
		// Repeats last after exhaustion
		Assert.Equal("third", await service.GetValueAsync());
	}

	[Fact]
	public void ThenReturns_Params_AddsMultipleValues()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// OnCall starts the sequence, ThenReturns(params) adds multiple values
		knockOff.GetOptional.Return(() => "callback first")
			.ThenReturn("second", "third", "fourth");

		Assert.Equal("callback first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
		Assert.Equal("fourth", service.GetOptional());
		// Repeats last after exhaustion
		Assert.Equal("fourth", service.GetOptional());
	}

	[Fact]
	public void ThenReturns_Params_EmptyArray_NoOp()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Empty params array elevates to sequence mode but adds no values
		knockOff.GetOptional.Return(() => "only")
			.ThenReturn(new string?[] { });

		Assert.Equal("only", service.GetOptional());
		// No additional sequence items, repeats the only value
		Assert.Equal("only", service.GetOptional());
	}

	[Fact]
	public void ThenReturns_Params_MixWithSingleValue()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Mix single value calls with params calls
		knockOff.GetOptional.Return(() => "first")
			.ThenReturn("second")  // single value
			.ThenReturn("third", "fourth", "fifth"); // params

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
		Assert.Equal("fourth", service.GetOptional());
		Assert.Equal("fifth", service.GetOptional());
		// Repeats last after exhaustion
		Assert.Equal("fifth", service.GetOptional());
	}

	[Fact]
	public void ThenGet_Params_AddsMultipleValues()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// Property API returns interfaces, so params ThenGet is not accessible through fluent API.
		// However, the generated PropertyGetSequenceImpl has the params method.
		// This test verifies it works when chaining single-value ThenGet calls.
		// For params access, users would need to use the direct generated type.
		knockOff.Name.Get("first")
			.ThenGet("second")
			.ThenGet("third")
			.ThenGet("fourth");

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
		Assert.Equal("third", service.Name);
		Assert.Equal("fourth", service.Name);
		// Repeats last after exhaustion
		Assert.Equal("fourth", service.Name);
	}

	[Fact]
	public void Params_Sequence_ExhaustionRepeatsLast()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Verify NSubstitute-like behavior: after exhaustion, repeats last value
		knockOff.GetOptional.Return("a", "b", "c");

		Assert.Equal("a", service.GetOptional());
		Assert.Equal("b", service.GetOptional());
		Assert.Equal("c", service.GetOptional());
		// Exhausted - verify repeats
		Assert.Equal("c", service.GetOptional());
		Assert.Equal("c", service.GetOptional());
		Assert.Equal("c", service.GetOptional());
	}

	[Fact]
	public void Params_Sequence_StrictModeThrows()
	{
		var knockOff = new SampleKnockOff();
		knockOff.Strict = true;
		ISampleService service = knockOff;

		// In strict mode, sequence exhaustion throws
		knockOff.GetOptional.Return("first", "second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Third access - sequence exhausted, throws in strict mode
		Assert.Throws<StubException>(() => service.GetOptional());
	}

	[Fact]
	public void Params_Sequence_NullValues()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Returns(null, "a", null) works for nullable types
		knockOff.GetOptional.Return((string?)null, "middle", (string?)null);

		Assert.Null(service.GetOptional());
		Assert.Equal("middle", service.GetOptional());
		Assert.Null(service.GetOptional());
		// Repeats last (null)
		Assert.Null(service.GetOptional());
	}

	[Fact]
	public void Returns_Params_IAsyncEnumerable_NoUnwrap()
	{
		var knockOff = new AsyncEnumerableServiceKnockOff();
		IAsyncEnumerableService service = knockOff;

		// IAsyncEnumerable<T> methods get params of the full enumerable type, not the inner type
		// The sequence is of IAsyncEnumerable<string> instances, not strings
		var enumerable1 = CreateAsyncEnumerable("a1", "a2");
		var enumerable2 = CreateAsyncEnumerable("b1", "b2");
		var enumerable3 = CreateAsyncEnumerable("c1", "c2");

		knockOff.GetItemsAsync.Return(enumerable1, enumerable2, enumerable3);

		// Each call returns a different IAsyncEnumerable<string> instance
		Assert.Same(enumerable1, service.GetItemsAsync());
		Assert.Same(enumerable2, service.GetItemsAsync());
		Assert.Same(enumerable3, service.GetItemsAsync());
		// Repeats last after exhaustion
		Assert.Same(enumerable3, service.GetItemsAsync());
	}

	[Fact]
	public void ThenGet_MixWithCallbacksAndValues()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// Mix callbacks and values in the same sequence
		// (Params not accessible through interface - using single value calls)
		knockOff.Name.Get(() => "callback first")
			.ThenGet(() => "value second")
			.ThenGet("third")
			.ThenGet("fourth");

		Assert.Equal("callback first", service.Name);
		Assert.Equal("value second", service.Name);
		Assert.Equal("third", service.Name);
		Assert.Equal("fourth", service.Name);
	}

	[Fact]
	public void Returns_ChainedWithThenReturns_UsesOnCall()
	{
		// Design note: Returns(value).ThenReturn() works when using OnCall pattern.
		// For sequence after Returns(value), use OnCall(() => value).ThenReturn()
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Start with OnCall for the first value, then chain ThenReturns
		knockOff.GetOptional.Return(() => "value")
			.ThenReturn("second", "third");

		Assert.Equal("value", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void Returns_Params_Verification_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Params Returns returns MethodSequenceImpl which has Verify()
		var seq = knockOff.GetOptional.Return("first", "second", "third");

		service.GetOptional();
		service.GetOptional();
		service.GetOptional();

		// Verify sequence was fully consumed
		seq.Verify();
	}

	// Helper method to create IAsyncEnumerable for testing
	private static async IAsyncEnumerable<string> CreateAsyncEnumerable(params string[] items)
	{
		foreach (var item in items)
		{
			yield return item;
		}
		await Task.CompletedTask;
	}

	#endregion

	#region Return(value).ThenReturn(value) Sequence Bug Fix (NRE Regression Tests)

	/// <summary>
	/// Regression tests for the NRE bug where Return(value).ThenReturn(value) caused
	/// a NullReferenceException because _call was null during sequence elevation.
	/// </summary>

	[Fact]
	public void ReturnValue_ThenReturnValue_ReturnsSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Value-based Return followed by value-based ThenReturn -- previously an NRE
		knockOff.GetOptional.Return("first")
			.ThenReturn("second")
			.ThenReturn("third");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void ReturnValue_ThenReturnCallback_ReturnsSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Value-based start, callback continuation
		knockOff.GetOptional.Return("first")
			.ThenReturn(() => "second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
	}

	[Fact]
	public void ReturnValue_ThenReturnValue_RepeatsLastAfterExhaustion()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Return("first").ThenReturn("second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Exhausted -- repeats last value in non-strict mode
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
	}

	[Fact]
	public void ReturnValue_ThenReturnValue_StrictModeThrows()
	{
		var knockOff = new SampleKnockOff();
		knockOff.Strict = true;
		ISampleService service = knockOff;

		knockOff.GetOptional.Return("first").ThenReturn("second");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// Strict mode -- throws on exhaustion
		Assert.Throws<StubException>(() => service.GetOptional());
	}

	[Fact]
	public void ReturnValue_ThenReturnValue_Verification()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var sequence = knockOff.GetOptional.Return("first")
			.ThenReturn("second")
			.ThenReturn("third");

		service.GetOptional();
		service.GetOptional();
		service.GetOptional();

		// Verify sequence was fully consumed
		sequence.Verify();
		knockOff.GetOptional.Verify(Times.Exactly(3));
	}

	[Fact]
	public void ReturnValue_ThenReturnValue_ThenDefault()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Return("first").ThenReturn("second").ThenDefault();

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		// ThenDefault returns default after exhaustion
		Assert.Null(service.GetOptional());
	}

	[Fact]
	public async Task AsyncReturnValue_ThenReturnValue_AutoWraps()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		// For Task<T> methods, Return(value) auto-wraps with Task.FromResult
		knockOff.GetRequiredAsync.Return("first")
			.ThenReturn("second")
			.ThenReturn("third");

		Assert.Equal("first", await service.GetRequiredAsync());
		Assert.Equal("second", await service.GetRequiredAsync());
		Assert.Equal("third", await service.GetRequiredAsync());
	}

	[Fact]
	public async Task ValueTaskReturnValue_ThenReturnValue_AutoWraps()
	{
		var knockOff = new ValueTaskMethodKnockOff();
		IValueTaskMethodService service = knockOff;

		// For ValueTask<T> methods, Return(value) auto-wraps with new ValueTask<T>(value)
		knockOff.GetValueAsync.Return("first")
			.ThenReturn("second")
			.ThenReturn("third");

		Assert.Equal("first", await service.GetValueAsync());
		Assert.Equal("second", await service.GetValueAsync());
		Assert.Equal("third", await service.GetValueAsync());
	}

	[Fact]
	public async Task AsyncSimplified_ThenReturnCallback_ReturnsSequence()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		// Return(simplifiedCallback) followed by ThenReturn(fullCallback) -- previously an NRE
		// The simplified callback returns string, auto-wrapped in Task.FromResult
		knockOff.GetRequiredAsync.Return(() => "first")
			.ThenReturn(() => Task.FromResult("second"));

		Assert.Equal("first", await service.GetRequiredAsync());
		Assert.Equal("second", await service.GetRequiredAsync());
	}

	[Fact]
	public async Task VoidTaskSimplifiedCall_ThenReturn_ReturnsSequence()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var callLog = new List<string>();

		// Call(simplifiedVoidCallback) followed by ThenReturn(fullCallback) -- previously an NRE
		// The simplified callback is Action (void), auto-wrapped to return Task.CompletedTask
		// Note: Task-returning methods use Return/ThenReturn (not Call/ThenCall) since IsVoid=false
		knockOff.DoWorkAsync.Call(() => { callLog.Add("first"); })
			.ThenReturn(() => { callLog.Add("second"); return Task.CompletedTask; });

		await service.DoWorkAsync();
		await service.DoWorkAsync();

		Assert.Equal(new[] { "first", "second" }, callLog);
	}

	[Fact]
	public async Task VoidValueTaskSimplifiedCall_ThenReturn_ReturnsSequence()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var callLog = new List<string>();

		// Call(simplifiedVoidCallback) followed by ThenReturn(fullCallback) -- previously an NRE
		// The simplified callback is Action (void), auto-wrapped to return default(ValueTask)
		// Note: ValueTask-returning methods use Return/ThenReturn (not Call/ThenCall) since IsVoid=false
		knockOff.DoWorkValueTaskAsync.Call(() => { callLog.Add("first"); })
			.ThenReturn(() => { callLog.Add("second"); return default(ValueTask); });

		await service.DoWorkValueTaskAsync();
		await service.DoWorkValueTaskAsync();

		Assert.Equal(new[] { "first", "second" }, callLog);
	}

	#endregion

	#region Test Stubs

	public interface IPropertyTest
	{
		string? Name { get; set; }
		int Count { get; set; }
		bool IsEnabled { get; set; }
	}

	[KnockOff]
	public partial class PropertyTestKnockOff : IPropertyTest
	{
	}

	/// <summary>
	/// Interface with ValueTask&lt;T&gt; returning method for testing auto-wrapping.
	/// </summary>
	public interface IValueTaskMethodService
	{
		ValueTask<string> GetValueAsync();
	}

	[KnockOff]
	public partial class ValueTaskMethodKnockOff : IValueTaskMethodService
	{
	}

	/// <summary>
	/// Interface with IAsyncEnumerable&lt;T&gt; returning method for testing no-unwrap behavior.
	/// </summary>
	public interface IAsyncEnumerableService
	{
		IAsyncEnumerable<string> GetItemsAsync();
	}

	[KnockOff]
	public partial class AsyncEnumerableServiceKnockOff : IAsyncEnumerableService
	{
	}

	#endregion
}
