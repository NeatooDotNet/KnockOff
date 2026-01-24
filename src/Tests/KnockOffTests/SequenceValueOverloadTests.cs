namespace KnockOff.Tests;

/// <summary>
/// Tests for value-based sequence overloads.
/// Phase 5 of value-based overloads feature.
///
/// Note: Method sequence value overloads (OnCallSequence(value), ThenCall(value))
/// are not yet implemented - those are callback-only.
///
/// Property sequences have value overloads:
/// - OnGetSequence(value) - implemented and works
/// - ThenGet(value) - generated on concrete class but interface returns IPropertyGetSequence
///   which doesn't include the value overload, so chaining must use lambda syntax
/// </summary>
public partial class SequenceValueOverloadTests
{
	#region Property Sequence Value Tests

	[Fact]
	public void OnGetSequence_WithValue_ReturnsSingleValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence("first");

		Assert.Equal("first", service.Name);
	}

	[Fact]
	public void OnGetSequence_WithValue_ThenGet_WithCallback_ReturnsSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// Note: ThenGet(value) is on the concrete class but OnGetSequence returns
		// IPropertyGetSequence which doesn't have the value overload.
		// Use lambda syntax for chaining.
		knockOff.Name.OnGetSequence("first")
			.ThenGet(() => "second")
			.ThenGet(() => "third");

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
		Assert.Equal("third", service.Name);
	}

	[Fact]
	public void OnGetSequence_WithCallback_ThenGet_ReturnsSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence(() => "callback first")
			.ThenGet(() => "callback second")
			.ThenGet(() => "callback third");

		Assert.Equal("callback first", service.Name);
		Assert.Equal("callback second", service.Name);
		Assert.Equal("callback third", service.Name);
	}

	[Fact]
	public void OnGetSequence_WithValue_TracksCorrectly()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var sequence = knockOff.Name.OnGetSequence("a")
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
	public void OnGetSequence_WithValue_SupportsNullValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence((string?)null);

		Assert.Null(service.Name);
	}

	[Fact]
	public void OnGetSequence_WithIntValue_WorksWithValueTypes()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.OnGetSequence(42);

		Assert.Equal(42, service.Count);
	}

	[Fact]
	public void OnGetSequence_WithBoolValue_WorksWithBooleans()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.IsEnabled.OnGetSequence(true);

		Assert.True(service.IsEnabled);
	}

	#endregion

	#region Method Sequence Tests (Callback-Only)

	// Note: Method sequences only support callbacks, not direct values.
	// These tests demonstrate the existing callback-based sequences work correctly.

	[Fact]
	public void OnCallSequence_WithCallbacks_ReturnsSequenceValues()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.OnCallSequence(() => "first")
			.ThenCall(() => "second")
			.ThenCall(() => "third");

		Assert.Equal("first", service.GetOptional());
		Assert.Equal("second", service.GetOptional());
		Assert.Equal("third", service.GetOptional());
	}

	[Fact]
	public void OnCallSequence_WithCallbacks_TracksCorrectly()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var sequence = knockOff.GetOptional.OnCallSequence(() => "a")
			.ThenCall(() => "b")
			.ThenCall(() => "c");

		service.GetOptional();
		service.GetOptional();
		service.GetOptional();

		sequence.Verify();
	}

	[Fact]
	public void OnCallSequence_CanMixWithOnCallValue_AfterSequenceExhausted()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Set up sequence that will be exhausted
		knockOff.GetOptional.OnCallSequence(() => "seq1").ThenCall(() => "seq2");

		// Consume the sequence
		Assert.Equal("seq1", service.GetOptional());
		Assert.Equal("seq2", service.GetOptional());

		// Now configure with value - clears sequence
		knockOff.GetOptional.OnCall("after sequence");

		Assert.Equal("after sequence", service.GetOptional());
		Assert.Equal("after sequence", service.GetOptional());
	}

	#endregion

	#region Async Method Sequence Tests

	[Fact]
	public async Task AsyncMethod_OnCallSequence_ReturnsSequenceValues()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		knockOff.GetRequiredAsync.OnCallSequence(() => Task.FromResult("first"))
			.ThenCall(() => Task.FromResult("second"))
			.ThenCall(() => Task.FromResult("third"));

		Assert.Equal("first", await service.GetRequiredAsync());
		Assert.Equal("second", await service.GetRequiredAsync());
		Assert.Equal("third", await service.GetRequiredAsync());
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void OnGetSequence_Exhausted_ReturnsDefaultInNonStrictMode()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence("only one");

		Assert.Equal("only one", service.Name);
		// Second access - sequence exhausted, returns default in non-strict mode
		Assert.Null(service.Name);
	}

	[Fact]
	public void OnGetSequence_Exhausted_ThrowsInStrictMode()
	{
		var knockOff = new PropertyTestKnockOff();
		knockOff.Strict = true;
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence("only one");

		Assert.Equal("only one", service.Name);
		// Second access - sequence exhausted, throws in strict mode
		Assert.Throws<StubException>(() => service.Name);
	}

	[Fact]
	public void OnGetSequence_VerifyIncomplete_Throws()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var sequence = knockOff.Name.OnGetSequence("a")
			.ThenGet(() => "b")
			.ThenGet(() => "c");

		// Only consume two of three
		_ = service.Name;
		_ = service.Name;

		// Verify should throw - sequence incomplete
		Assert.Throws<VerificationException>(() => sequence.Verify());
	}

	[Fact]
	public void OnGetSequence_Reset_AllowsReplay()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence("a").ThenGet(() => "b");

		Assert.Equal("a", service.Name);
		Assert.Equal("b", service.Name);

		// Reset and replay
		knockOff.Name.Reset();

		Assert.Equal("a", service.Name);
		Assert.Equal("b", service.Name);
	}

	[Fact]
	public void MethodOnCallSequence_Exhausted_ReturnsDefaultInNonStrictMode()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.OnCallSequence(() => "only one");

		Assert.Equal("only one", service.GetOptional());
		// Second access - sequence exhausted, returns default in non-strict mode
		Assert.Null(service.GetOptional());
	}

	[Fact]
	public void MethodOnCallSequence_Exhausted_ThrowsInStrictMode()
	{
		var knockOff = new SampleKnockOff();
		knockOff.Strict = true;
		ISampleService service = knockOff;

		knockOff.GetOptional.OnCallSequence(() => "only one");

		Assert.Equal("only one", service.GetOptional());
		// Second access - sequence exhausted, throws in strict mode
		Assert.Throws<StubException>(() => service.GetOptional());
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

	#endregion
}
