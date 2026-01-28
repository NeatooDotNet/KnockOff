namespace KnockOff.Tests;

/// <summary>
/// Tests for Returns(value) method value overloads.
/// Phase 1 of value-based overloads feature.
/// </summary>
public class MethodValueOverloadTests
{
	#region Basic Value Return Tests

	[Fact]
	public void Returns_WithValue_ReturnsConfiguredValue()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var tracking = knockOff.GetOptional.Returns("configured value");

		var result = service.GetOptional();

		Assert.Equal("configured value", result);
		tracking.Verify(Times.Once);
	}

	[Fact]
	public void Returns_WithValue_ReturnsNullWhenConfiguredNull()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var tracking = knockOff.GetOptional.Returns((string?)null);

		var result = service.GetOptional();

		Assert.Null(result);
		tracking.Verify(Times.Once);
	}

	[Fact]
	public void Returns_WithValue_RepeatsIndefinitely()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Returns("repeated");

		Assert.Equal("repeated", service.GetOptional());
		Assert.Equal("repeated", service.GetOptional());
		Assert.Equal("repeated", service.GetOptional());
	}

	[Fact]
	public void Returns_WithValue_TracksCallCount()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var tracking = knockOff.GetOptional.Returns("test");

		service.GetOptional();
		service.GetOptional();
		service.GetOptional();

		tracking.Verify(Times.Exactly(3));
	}

	[Fact]
	public void Returns_WithValue_ReturnsValueType()
	{
		// Use generic interface with value type return
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var tracking = knockOff.GetInt.Returns(42);

		var result = service.GetInt();

		Assert.Equal(42, result);
		tracking.Verify(Times.Once);
	}

	[Fact]
	public void Returns_WithValue_ReturnsZeroForValueType()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var tracking = knockOff.GetInt.Returns(0);

		var result = service.GetInt();

		Assert.Equal(0, result);
		tracking.Verify(Times.Once);
	}

	[Fact]
	public void Returns_WithValue_ReturnsBoolTrue()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		knockOff.GetBool.Returns(true);

		Assert.True(service.GetBool());
	}

	[Fact]
	public void Returns_WithValue_ReturnsBoolFalse()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		knockOff.GetBool.Returns(false);

		Assert.False(service.GetBool());
	}

	#endregion

	#region Async Auto-Wrapping Tests

	[Fact]
	public async Task Returns_WithValue_AsyncTaskOfT_AutoWraps()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		// GetRequiredAsync returns Task<string> - no user method, so we can test value overload
		knockOff.GetRequiredAsync.Returns("async result");

		var result = await service.GetRequiredAsync();

		Assert.Equal("async result", result);
	}

	[Fact]
	public async Task Returns_WithValue_AsyncTaskOfNullableT_AutoWrapsNull()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		knockOff.GetOptionalAsync.Returns((string?)null);

		var result = await service.GetOptionalAsync();

		Assert.Null(result);
	}

	[Fact]
	public async Task Returns_WithValue_AsyncTaskOfT_TracksInvocations()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var tracking = knockOff.GetRequiredAsync.Returns("test");

		await service.GetRequiredAsync();
		await service.GetRequiredAsync();

		tracking.Verify(Times.Exactly(2));
	}

	#endregion

	#region Mutual Exclusivity Tests

	[Fact]
	public void OnCall_Callback_ClearsValueConfiguration()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// First configure with value
		knockOff.GetOptional.Returns("value");
		Assert.Equal("value", service.GetOptional());

		// Then configure with callback - should clear value
		knockOff.GetOptional.OnCall(() => "callback");
		Assert.Equal("callback", service.GetOptional());
	}

	[Fact]
	public void Returns_Value_ClearsCallbackConfiguration()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// First configure with callback
		knockOff.GetOptional.OnCall(() => "callback");
		Assert.Equal("callback", service.GetOptional());

		// Then configure with value - should clear callback
		knockOff.GetOptional.Returns("value");
		Assert.Equal("value", service.GetOptional());
	}

	[Fact]
	public void OnCallSequence_ClearsValueConfiguration()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// First configure with value
		knockOff.GetOptional.Returns("value");
		Assert.Equal("value", service.GetOptional());

		// Then configure with sequence - should clear value
		knockOff.GetOptional.OnCallSequence(() => "seq1").ThenCall(() => "seq2");
		Assert.Equal("seq1", service.GetOptional());
		Assert.Equal("seq2", service.GetOptional());
	}

	#endregion

	#region Tracking and Verification Tests

	[Fact]
	public void Returns_WithValue_Verifiable_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Returns("test").Verifiable();

		service.GetOptional();

		knockOff.Verify();
	}

	[Fact]
	public void Returns_WithValue_VerifiableWithTimes_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Returns("test").Verifiable(Times.Exactly(2));

		service.GetOptional();
		service.GetOptional();

		knockOff.Verify();
	}

	[Fact]
	public void Returns_WithValue_IsConfigured_ReturnsTrue()
	{
		var knockOff = new SampleKnockOff();

		knockOff.GetOptional.Returns("test");

		// VerifyAll should not fail because IsConfigured is true
		Assert.Throws<VerificationException>(() => knockOff.VerifyAll());

		// After calling, VerifyAll passes
		ISampleService service = knockOff;
		service.GetOptional();
		knockOff.VerifyAll();
	}

	#endregion

	#region Generic Interface Tests

	[Fact]
	public void Returns_WithValue_GenericInterface_Works()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		var mockUser = new User { Id = 1, Name = "Test" };
		var tracking = knockOff.GetById.Returns(mockUser);

		var result = repo.GetById(42);

		Assert.Same(mockUser, result);
		Assert.Equal(42, tracking.LastArg);
	}

	[Fact]
	public void Returns_WithValue_GenericInterface_ReturnsNull()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		knockOff.GetById.Returns((User?)null);

		var result = repo.GetById(42);

		Assert.Null(result);
	}

	#endregion

	#region Complex Object Tests

	[Fact]
	public void Returns_WithValue_ReturnsComplexObject()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var entity = new TestEntity { Id = 99, Name = "TestName" };
		knockOff.GetEntity.Returns(entity);

		var result = service.GetEntity();

		Assert.Same(entity, result);
	}

	[Fact]
	public void Returns_WithValue_ReturnsCollection()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var list = new List<string> { "a", "b", "c" };
		knockOff.GetList.Returns(list);

		var result = service.GetList();

		Assert.Same(list, result);
	}

	#endregion
}
