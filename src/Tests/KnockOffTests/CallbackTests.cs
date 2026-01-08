namespace KnockOff.Tests;

/// <summary>
/// Tests for OnCall/OnGet/OnSet callback functionality.
/// </summary>
public class CallbackTests
{
	[Fact]
	public void OnCall_VoidMethod_CallbackInvoked()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var callbackInvoked = false;
		knockOff.DoSomething.OnCall = (ko) =>
		{
			callbackInvoked = true;
			Assert.Same(knockOff, ko);
		};

		service.DoSomething();

		Assert.True(callbackInvoked);
		Assert.True(knockOff.DoSomething.WasCalled);
	}

	[Fact]
	public void OnCall_ReturnMethod_CallbackReturnsValue()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetValue2.OnCall = (ko, input) => input * 10;

		var result = service.GetValue(5);

		// Callback overrides user method (which would return 5*2=10)
		Assert.Equal(50, result);
		Assert.Equal(5, knockOff.GetValue2.LastCallArg);
	}

	[Fact]
	public void OnCall_WithMultipleParams_ReceivesAllParams()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		string? capturedName = null;
		int? capturedValue = null;
		bool? capturedFlag = null;
		knockOff.Calculate.OnCall = (ko, name, value, flag) =>
		{
			capturedName = name;
			capturedValue = value;
			capturedFlag = flag;
		};

		service.Calculate("test", 42, true);

		Assert.Equal("test", capturedName);
		Assert.Equal(42, capturedValue);
		Assert.True(capturedFlag);
	}

	[Fact]
	public void OnCall_CanAccessOtherHandler()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.DoSomething();

		knockOff.GetValue2.OnCall = (ko, input) =>
		{
			Assert.True(ko.DoSomething.WasCalled);
			return input * 100;
		};

		var result = service.GetValue(3);

		Assert.Equal(300, result);
	}

	[Fact]
	public void OnGet_PropertyGetter_CallbackReturnsValue()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.Name.OnGet = (ko) => "FromCallback";

		var result = service.Name;

		Assert.Equal("FromCallback", result);
		Assert.Equal(1, knockOff.Name.GetCount);
	}

	[Fact]
	public void OnSet_PropertySetter_CallbackInvoked()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		string? capturedValue = null;
		knockOff.Name.OnSet = (ko, value) =>
		{
			capturedValue = value;
		};

		service.Name = "TestValue";

		Assert.Equal("TestValue", capturedValue);
		Assert.Equal(1, knockOff.Name.SetCount);

		// Since OnSet was used, backing was not updated
		knockOff.Name.OnGet = null;
		var storedValue = service.Name;
		Assert.Equal("", storedValue);
	}

	[Fact]
	public void Callback_Reset_ClearsCallback()
	{
		// Note: When a user method exists, the generated code calls it directly without checking OnCall.
		// Use GetOptional (no user method) to test OnCall behavior.
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.OnCall = (ko) => "callback value";

		var resultBefore = service.GetOptional();
		Assert.Equal("callback value", resultBefore);

		knockOff.GetOptional.Reset();

		var resultAfter = service.GetOptional();
		// Now falls back to default (null)
		Assert.Null(resultAfter);
	}

	[Fact]
	public async Task OnCall_AsyncMethod_CallbackReturnsTask()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		knockOff.GetValueAsync2.OnCall = (ko, input) =>
			Task.FromResult(input * 100);

		var result = await service.GetValueAsync(7);

		// Callback overrides user method (which would return 7*3=21)
		Assert.Equal(700, result);
	}

	[Fact]
	public void OnCall_GenericInterface_CallbackWorks()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		var mockUser = new User { Id = 42, Name = "MockUser" };
		knockOff.GetById.OnCall = (ko, id) =>
		{
			if (id == 42) return mockUser;
			return null;
		};

		var result = repo.GetById(42);

		Assert.Same(mockUser, result);
		Assert.Equal(42, knockOff.GetById.LastCallArg);
	}

	[Fact]
	public void OnGet_InheritedProperty_CallbackWorks()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		knockOff.Id.OnGet = (ko) => 999;

		var result = entity.Id;

		Assert.Equal(999, result);
		Assert.Equal(1, knockOff.Id.GetCount);
	}
}
