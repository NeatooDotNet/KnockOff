namespace KnockOff.Tests;

/// <summary>
/// Tests for value-based sequence overloads.
/// Phase 5 of value-based overloads feature.
///
/// Property OnGet/OnCall support both values and callbacks, with ThenGet/ThenCall for sequencing:
/// - OnGet(value) - configures repeating value return
/// - OnGet(callback) - configures repeating callback
/// - OnGet(...).ThenGet(...) - elevates to sequence mode
/// </summary>
public partial class SequenceValueOverloadTests
{
	#region Property Sequence Value Tests

	[Fact]
	public void OnGet_WithValue_ReturnsSingleValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGet("first");

		Assert.Equal("first", service.Name);
	}

	[Fact]
	public void OnGet_WithValue_ThenGet_WithCallback_ReturnsSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// OnGet returns builder interface, ThenGet elevates to sequence mode
		knockOff.Name.OnGet("first")
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

		knockOff.Name.OnGet(() => "callback first")
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

		var sequence = knockOff.Name.OnGet("a")
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

		knockOff.Name.OnGet((string?)null);

		Assert.Null(service.Name);
	}

	[Fact]
	public void OnGet_WithIntValue_WorksWithValueTypes()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.OnGet(42);

		Assert.Equal(42, service.Count);
	}

	[Fact]
	public void OnGet_WithBoolValue_WorksWithBooleans()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.IsEnabled.OnGet(true);

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

		knockOff.GetOptional.OnCall(() => "first")
			.ThenCall(() => "second")
			.ThenCall(() => "third");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void OnCall_WithCallbacks_TracksCorrectly()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var sequence = knockOff.GetOptional.OnCall(() => "a")
			.ThenCall(() => "b")
			.ThenCall(() => "c");

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
		knockOff.GetOptional.OnCall(() => "seq1").ThenCall(() => "seq2");

		// Consume the sequence
		Assert.Equal("seq1", service.GetOptional());
		Assert.Equal("seq2", service.GetOptional());

		// Now configure with value - clears sequence
		knockOff.GetOptional.Returns("after sequence");

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

		knockOff.GetRequiredAsync.OnCall(() => Task.FromResult("first"))
			.ThenCall(() => Task.FromResult("second"))
			.ThenCall(() => Task.FromResult("third"));

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

		knockOff.Name.OnGet("repeating");

		// OnGet without ThenGet repeats the same value forever
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
		knockOff.Name.OnGet("first").ThenGet("second");

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
		knockOff.Name.OnGet("first").ThenGet("second").ThenDefault();

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
		knockOff.Name.OnGet("first").ThenGet("second");

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

		var sequence = knockOff.Name.OnGet("a")
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

		knockOff.Name.OnGet("a").ThenGet(() => "b");

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

		knockOff.GetOptional.OnCall(() => "repeating");

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
		knockOff.GetOptional.OnCall(() => "first").ThenCall(() => "second");

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
		knockOff.GetOptional.OnCall(() => "first").ThenCall(() => "second").ThenDefault();

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
		knockOff.GetOptional.OnCall(() => "first").ThenCall(() => "second");

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
	/// DESIGN NOTE: Returns().ThenReturns() is NOT supported by design.
	/// ThenReturns is available on sequences started with OnCall().
	/// Use OnCall(() => value) to start a sequence with a constant value.
	/// </summary>

	[Fact]
	public void OnCall_ThenReturns_ReturnsSequence()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// OnCall starts the sequence, ThenReturns adds values to it
		knockOff.GetOptional.OnCall(() => "first")
			.ThenReturns("second")
			.ThenReturns("third");

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
		knockOff.GetOptional.OnCall(() => "callback first")
			.ThenReturns("value second")
			.ThenCall(() => "callback third")
			.ThenReturns("value fourth");

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
		knockOff.GetRequiredAsync.OnCall(() => Task.FromResult("first"))
			.ThenReturns("second")
			.ThenReturns("third");

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
		knockOff.GetValueAsync.OnCall(() => new ValueTask<string>("first"))
			.ThenReturns("second")
			.ThenReturns("third");

		Assert.Equal("first", await service.GetValueAsync());
		Assert.Equal("second", await service.GetValueAsync());
		Assert.Equal("third", await service.GetValueAsync());
	}

	[Fact]
	public void ThenReturns_TracksCorrectly()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var sequence = knockOff.GetOptional.OnCall(() => "a")
			.ThenReturns("b")
			.ThenReturns("c");

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
		knockOff.GetOptional.OnCall(() => "first").ThenReturns("second");

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
		knockOff.GetOptional.OnCall(() => "first").ThenReturns("second").ThenDefault();

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
		knockOff.GetOptional.OnCall(() => "first").ThenReturns("second");

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
		knockOff.GetOptional.OnCall(() => "first")
			.ThenReturns((string?)null)
			.ThenReturns("third");

		Assert.Equal("first", service.GetOptional());
		Assert.Null(service.GetOptional());
		Assert.Equal("third", service.GetOptional());
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

	#endregion
}
