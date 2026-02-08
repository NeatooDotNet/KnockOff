namespace KnockOff.Tests;

/// <summary>
/// Tests for OnCall/Get/Set callback functionality.
/// </summary>
public class CallbackTests
{
	[Fact]
	public void OnCall_VoidMethod_CallbackInvoked()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var callbackInvoked = false;
		var tracking = knockOff.DoSomething.Call(() =>
		{
			callbackInvoked = true;
		});

		service.DoSomething();

		Assert.True(callbackInvoked);
		tracking.Verify();
	}

	[Fact]
	public void OnCall_MethodWithUserImplementation_TracksInvocation()
	{
		// GetValue has a user-defined implementation that returns input * 2.
		// The interceptor only tracks calls, it doesn't allow overriding the implementation.
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetValue(5);

		// User method returns input * 2
		Assert.Equal(10, result);
		Assert.Equal(5, knockOff.GetValue.LastArg);
		knockOff.GetValue.Verify(Called.Once);
	}

	[Fact]
	public void OnCall_WithMultipleParams_ReceivesAllParams()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		string? capturedName = null;
		int? capturedValue = null;
		bool? capturedFlag = null;
		var tracking = knockOff.Calculate.Call((name, value, flag) =>
		{
			capturedName = name;
			capturedValue = value;
			capturedFlag = flag;
		});

		service.Calculate("test", 42, true);

		Assert.Equal("test", capturedName);
		Assert.Equal(42, capturedValue);
		Assert.True(capturedFlag);
	}

	[Fact]
	public void OnCall_CanAccessOtherInterceptorState()
	{
		var knockOff = new SampleKnockOff();
		var doSomethingTracking = knockOff.DoSomething.Call(() => { });
		ISampleService service = knockOff;

		service.DoSomething();

		// GetValue has user implementation - we just verify tracking works
		var result = service.GetValue(3);

		doSomethingTracking.Verify();
		Assert.Equal(6, result); // User method returns input * 2
	}

	[Fact]
	public void OnGet_PropertyGetter_CallbackReturnsValue()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.Name.Get(() => "FromCallback");

		var result = service.Name;

		Assert.Equal("FromCallback", result);
		knockOff.Name.VerifyGet(Called.Once);
	}

	[Fact]
	public void OnSet_PropertySetter_CallbackInvoked()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		string? capturedValue = null;
		knockOff.Name.Set((value) =>
		{
			capturedValue = value;
		});

		service.Name = "TestValue";

		Assert.Equal("TestValue", capturedValue);
		knockOff.Name.VerifySet(Called.Once);

		// Since Set was used (without Get), getter returns default
		var storedValue = service.Name;
		Assert.Equal(default, storedValue);
	}

	[Fact]
	public void Callback_Reset_ClearsTracking()
	{
		// Note: Reset() only clears tracking state, not the configured callback.
		// Use GetOptional (no user method) to test OnCall behavior.
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var tracking = knockOff.GetOptional.Return(() => "callback value");

		var resultBefore = service.GetOptional();
		Assert.Equal("callback value", resultBefore);
		tracking.Verify(Called.Once);

		knockOff.GetOptional.Reset();

		// After reset, tracking state is cleared but callback still works
		tracking.Verify(Called.Never);

		var resultAfter = service.GetOptional();
		Assert.Equal("callback value", resultAfter);
		tracking.Verify(Called.Once); // Called once more after reset
	}

	[Fact]
	public async Task OnCall_AsyncMethod_WithUserImplementation()
	{
		// GetValueAsync has a user-defined implementation that returns input * 3.
		// The interceptor only tracks calls.
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueAsync(7);

		// User method returns input * 3
		Assert.Equal(21, result);
		Assert.Equal(7, knockOff.GetValueAsync.LastArg);
	}

	[Fact]
	public void OnCall_GenericInterface_CallbackWorks()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		var mockUser = new User { Id = 42, Name = "MockUser" };
		var tracking = knockOff.GetById.Return((id) =>
		{
			if (id == 42) return mockUser;
			return null;
		});

		var result = repo.GetById(42);

		Assert.Same(mockUser, result);
		Assert.Equal(42, tracking.LastArg);
	}

	[Fact]
	public void OnGet_InheritedProperty_CallbackWorks()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		knockOff.Id.Get(() => 999);

		var result = entity.Id;

		Assert.Equal(999, result);
		knockOff.Id.VerifyGet(Called.Once);
	}
}
