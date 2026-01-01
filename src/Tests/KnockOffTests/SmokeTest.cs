namespace KnockOff.Tests;

/// <summary>
/// Tests verifying the generator runs and produces correct output.
/// </summary>
public class SmokeTest
{
	[Fact]
	public void KnockOffAttribute_Exists()
	{
		var attr = new KnockOffAttribute();
		Assert.NotNull(attr);
	}

	[Fact]
	public void Generator_ProducesOutput_ForKnockOffClass()
	{
		var knockOff = new SampleKnockOff();
		Assert.NotNull(knockOff);
		Assert.NotNull(knockOff.Spy);
	}

	[Fact]
	public void Property_Setter_TracksInvocation_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";

		Assert.Equal(1, knockOff.Spy.Name.SetCount);
		// Strongly typed - no cast needed!
		string? lastValue = knockOff.Spy.Name.LastSetValue;
		Assert.Equal("Test", lastValue);
	}

	[Fact]
	public void Property_Getter_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		var _ = service.Name;

		Assert.Equal(1, knockOff.Spy.Name.GetCount);
	}

	[Fact]
	public void Method_VoidNoParams_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.DoSomething();

		Assert.True(knockOff.Spy.DoSomething.WasCalled);
		Assert.Equal(1, knockOff.Spy.DoSomething.CallCount);
	}

	[Fact]
	public void Method_WithSingleParam_TracksArg_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetValue(42);

		Assert.Equal(84, result);
		Assert.Equal(1, knockOff.Spy.GetValue.CallCount);

		// Single param uses LastCallArg (not tuple)
		int? lastArg = knockOff.Spy.GetValue.LastCallArg;
		Assert.Equal(42, lastArg);
	}

	[Fact]
	public void Method_WithMultipleParams_TracksArgs_AsNamedTuple()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 100, true);

		var args = knockOff.Spy.Calculate.LastCallArgs;
		Assert.NotNull(args);
		Assert.Equal("test", args.Value.name);
		Assert.Equal(100, args.Value.value);
		Assert.True(args.Value.flag);
	}

	[Fact]
	public void Method_AllCalls_TracksHistory()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.GetValue(1);
		service.GetValue(2);
		service.GetValue(3);

		Assert.Equal(3, knockOff.Spy.GetValue.CallCount);
		Assert.Equal(3, knockOff.Spy.GetValue.AllCalls.Count);
		// Single param - AllCalls is List<int>
		Assert.Equal(1, knockOff.Spy.GetValue.AllCalls[0]);
		Assert.Equal(2, knockOff.Spy.GetValue.AllCalls[1]);
		Assert.Equal(3, knockOff.Spy.GetValue.AllCalls[2]);
	}

	[Fact]
	public void Method_WithNullableReturn_NoUserMethod_ReturnsDefault()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetOptional();

		Assert.Null(result);
		Assert.True(knockOff.Spy.GetOptional.WasCalled);
	}

	[Fact]
	public void AsInterface_ReturnsTypedInterface()
	{
		var knockOff = new SampleKnockOff();

		// Use the generated AsXYZ() method
		ISampleService service = knockOff.AsSampleService();

		service.Name = "Test";
		Assert.Equal(1, knockOff.Spy.Name.SetCount);
	}

	[Fact]
	public void Reset_ClearsTrackingState()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		service.GetValue(42);
		service.DoSomething();

		// Reset all
		knockOff.Spy.Name.Reset();
		knockOff.Spy.GetValue.Reset();
		knockOff.Spy.DoSomething.Reset();

		Assert.Equal(0, knockOff.Spy.Name.SetCount);
		Assert.Equal(0, knockOff.Spy.GetValue.CallCount);
		Assert.Equal(0, knockOff.Spy.DoSomething.CallCount);
		Assert.False(knockOff.Spy.DoSomething.WasCalled);
	}

	[Fact]
	public void TupleDestructuring_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 42, true);

		// Destructure the tuple
		if (knockOff.Spy.Calculate.LastCallArgs is var (name, value, flag))
		{
			Assert.Equal("test", name);
			Assert.Equal(42, value);
			Assert.True(flag);
		}
		else
		{
			Assert.Fail("LastCallArgs should not be null");
		}
	}

	#region Multi-Interface Tests

	[Fact]
	public void MultiInterface_DifferentMethods_BothInterfacesWork()
	{
		var knockOff = new MultiInterfaceKnockOff();
		ILogger logger = knockOff;
		INotifier notifier = knockOff;

		// Use ILogger
		logger.Log("test message");
		logger.Name = "Logger1";

		// Use INotifier
		notifier.Notify("user@example.com");

		// Verify tracking for ILogger methods
		Assert.True(knockOff.Spy.Log.WasCalled);
		Assert.Equal("test message", knockOff.Spy.Log.LastCallArg);
		Assert.Equal(1, knockOff.Spy.Name.SetCount);
		Assert.Equal("Logger1", knockOff.Spy.Name.LastSetValue);

		// Verify tracking for INotifier methods
		Assert.True(knockOff.Spy.Notify.WasCalled);
		Assert.Equal("user@example.com", knockOff.Spy.Notify.LastCallArg);
	}

	[Fact]
	public void MultiInterface_AsMethodsWork_ForBothInterfaces()
	{
		var knockOff = new MultiInterfaceKnockOff();

		// Both AsXYZ() methods should work
		ILogger logger = knockOff.AsLogger();
		INotifier notifier = knockOff.AsNotifier();

		logger.Log("via logger");
		notifier.Notify("via notifier");

		Assert.True(knockOff.Spy.Log.WasCalled);
		Assert.True(knockOff.Spy.Notify.WasCalled);
	}

	[Fact]
	public void MultiInterface_SharedProperty_UsesSharedBacking()
	{
		var knockOff = new MultiInterfaceKnockOff();
		ILogger logger = knockOff;
		INotifier notifier = knockOff;

		// Set via ILogger (which has setter)
		logger.Name = "SharedValue";

		// Read via both interfaces
		var loggerName = logger.Name;
		var notifierName = notifier.Name;

		// Both should see the same value (shared backing)
		Assert.Equal("SharedValue", loggerName);
		Assert.Equal("SharedValue", notifierName);

		// Tracking should accumulate from both interfaces
		Assert.Equal(1, knockOff.Spy.Name.SetCount);
		Assert.Equal(2, knockOff.Spy.Name.GetCount); // One get from each interface
	}

	[Fact]
	public void SharedSignature_SameMethodSignature_SharesTracking()
	{
		var knockOff = new SharedSignatureKnockOff();
		ILogger logger = knockOff;
		IAuditor auditor = knockOff;

		// Call Log via ILogger
		logger.Log("logger message");

		// Call Log via IAuditor
		auditor.Log("auditor message");

		// Both should share the same Spy tracking
		Assert.Equal(2, knockOff.Spy.Log.CallCount);
		Assert.Equal("auditor message", knockOff.Spy.Log.LastCallArg);

		// AllCalls should contain both
		Assert.Equal(2, knockOff.Spy.Log.AllCalls.Count);
		Assert.Equal("logger message", knockOff.Spy.Log.AllCalls[0]);
		Assert.Equal("auditor message", knockOff.Spy.Log.AllCalls[1]);
	}

	[Fact]
	public void SharedSignature_UniqueMethodsStillTracked()
	{
		var knockOff = new SharedSignatureKnockOff();
		ILogger logger = knockOff;
		IAuditor auditor = knockOff;

		// IAuditor has unique Audit method
		auditor.Audit("delete", 42);

		Assert.True(knockOff.Spy.Audit.WasCalled);
		var args = knockOff.Spy.Audit.LastCallArgs;
		Assert.NotNull(args);
		Assert.Equal("delete", args.Value.action);
		Assert.Equal(42, args.Value.userId);

		// ILogger.Log should not be affected
		Assert.False(knockOff.Spy.Log.WasCalled);
	}

	#endregion

	#region Async Method Tests (Phase 8.1)

	[Fact]
	public async Task AsyncMethod_Task_ReturnsCompletedTask()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		await service.DoWorkAsync();

		Assert.True(knockOff.Spy.DoWorkAsync.WasCalled);
		Assert.Equal(1, knockOff.Spy.DoWorkAsync.CallCount);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfT_WithUserMethod_ReturnsUserResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueAsync(10);

		// User method multiplies by 3
		Assert.Equal(30, result);
		Assert.Equal(10, knockOff.Spy.GetValueAsync.LastCallArg);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfNullableT_ReturnsDefault()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetOptionalAsync();

		Assert.Null(result);
		Assert.True(knockOff.Spy.GetOptionalAsync.WasCalled);
	}

	[Fact]
	public async Task AsyncMethod_ValueTask_ReturnsCompleted()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		await service.DoWorkValueTaskAsync();

		Assert.True(knockOff.Spy.DoWorkValueTaskAsync.WasCalled);
	}

	[Fact]
	public async Task AsyncMethod_ValueTaskOfT_WithUserMethod_ReturnsUserResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueValueTaskAsync(5);

		// User method multiplies by 4
		Assert.Equal(20, result);
		Assert.Equal(5, knockOff.Spy.GetValueValueTaskAsync.LastCallArg);
	}

	#endregion

	#region Generic Interface Tests (Phase 8.2)

	[Fact]
	public void GenericInterface_MethodsWork()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		var user = new User { Id = 1, Name = "Test" };
		repo.Save(user);

		Assert.True(knockOff.Spy.Save.WasCalled);
		Assert.Same(user, knockOff.Spy.Save.LastCallArg);
	}

	[Fact]
	public void GenericInterface_NullableReturn_ReturnsDefault()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		var result = repo.GetById(42);

		Assert.Null(result);
		Assert.Equal(42, knockOff.Spy.GetById.LastCallArg);
	}

	[Fact]
	public async Task GenericInterface_AsyncMethod_ReturnsTaskWithDefault()
	{
		var knockOff = new UserRepositoryKnockOff();
		IRepository<User> repo = knockOff;

		var result = await repo.GetByIdAsync(100);

		Assert.Null(result);
		Assert.Equal(100, knockOff.Spy.GetByIdAsync.LastCallArg);
	}

	[Fact]
	public void GenericInterface_AsMethod_Works()
	{
		var knockOff = new UserRepositoryKnockOff();

		IRepository<User> repo = knockOff.AsRepository();

		repo.Save(new User { Id = 1, Name = "Via AsRepository" });
		Assert.True(knockOff.Spy.Save.WasCalled);
	}

	#endregion

	#region Interface Inheritance Tests (Phase 8.3)

	[Fact]
	public void InterfaceInheritance_DerivedPropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		entity.ModifiedAt = DateTime.Now;
		entity.ModifiedBy = "TestUser";

		Assert.Equal(1, knockOff.Spy.ModifiedAt.SetCount);
		Assert.Equal(1, knockOff.Spy.ModifiedBy.SetCount);
		Assert.Equal("TestUser", knockOff.Spy.ModifiedBy.LastSetValue);
	}

	[Fact]
	public void InterfaceInheritance_BasePropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		// Read base interface properties
		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.Spy.Id.GetCount);
		Assert.Equal(1, knockOff.Spy.CreatedAt.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_BothAsMethodsWork()
	{
		var knockOff = new AuditableEntityKnockOff();

		// Both accessor methods should exist
		IAuditableEntity auditable = knockOff.AsAuditableEntity();
		IBaseEntity baseEntity = knockOff.AsBaseEntity();

		auditable.ModifiedBy = "Via AsAuditableEntity";
		var id = baseEntity.Id;

		Assert.Equal(1, knockOff.Spy.ModifiedBy.SetCount);
		Assert.Equal(1, knockOff.Spy.Id.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_AccessBaseViaDerivied()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		// IAuditableEntity inherits from IBaseEntity, so we can access base properties
		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.Spy.Id.GetCount);
		Assert.Equal(1, knockOff.Spy.CreatedAt.GetCount);
	}

	#endregion

	#region Callback Tests (Phase 9)

	[Fact]
	public void OnCall_VoidMethod_CallbackInvoked()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var callbackInvoked = false;
		knockOff.Spy.DoSomething.OnCall = (ko) =>
		{
			callbackInvoked = true;
			Assert.Same(knockOff, ko); // Callback receives knockoff instance
		};

		service.DoSomething();

		Assert.True(callbackInvoked);
		Assert.True(knockOff.Spy.DoSomething.WasCalled);
	}

	[Fact]
	public void OnCall_ReturnMethod_CallbackReturnsValue()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// Configure callback to return specific value
		knockOff.Spy.GetValue.OnCall = (ko, input) => input * 10;

		var result = service.GetValue(5);

		// Callback overrides user method (which would return 5*2=10)
		Assert.Equal(50, result);
		Assert.Equal(5, knockOff.Spy.GetValue.LastCallArg);
	}

	[Fact]
	public void OnCall_WithTupleParams_ReceivesTuple()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		(string, int, bool)? capturedArgs = null;
		knockOff.Spy.Calculate.OnCall = (ko, args) =>
		{
			capturedArgs = args;
		};

		service.Calculate("test", 42, true);

		Assert.NotNull(capturedArgs);
		Assert.Equal("test", capturedArgs.Value.Item1);
		Assert.Equal(42, capturedArgs.Value.Item2);
		Assert.True(capturedArgs.Value.Item3);
	}

	[Fact]
	public void OnCall_CanAccessOtherSpy()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		// First, set some state
		service.DoSomething();

		// Configure callback that checks other Spy
		knockOff.Spy.GetValue.OnCall = (ko, input) =>
		{
			// Callback can see DoSomething was already called
			Assert.True(ko.Spy.DoSomething.WasCalled);
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

		knockOff.Spy.Name.OnGet = (ko) => "FromCallback";

		var result = service.Name;

		Assert.Equal("FromCallback", result);
		Assert.Equal(1, knockOff.Spy.Name.GetCount);
	}

	[Fact]
	public void OnSet_PropertySetter_CallbackInvoked()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		string? capturedValue = null;
		knockOff.Spy.Name.OnSet = (ko, value) =>
		{
			capturedValue = value;
			// Note: when OnSet is set, value does NOT go to backing
		};

		service.Name = "TestValue";

		Assert.Equal("TestValue", capturedValue);
		Assert.Equal(1, knockOff.Spy.Name.SetCount);

		// Since OnSet was used, backing was not updated
		// So OnGet callback or default backing value is returned
		knockOff.Spy.Name.OnGet = null; // Ensure no OnGet callback
		var storedValue = service.Name;
		Assert.Equal("", storedValue); // Default backing value for string
	}

	[Fact]
	public void Callback_Reset_ClearsCallback()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.Spy.GetValue.OnCall = (ko, input) => 999;

		var resultBefore = service.GetValue(1);
		Assert.Equal(999, resultBefore);

		// Reset clears callback
		knockOff.Spy.GetValue.Reset();

		var resultAfter = service.GetValue(1);
		// Now falls back to user method (input * 2)
		Assert.Equal(2, resultAfter);
	}

	[Fact]
	public async Task OnCall_AsyncMethod_CallbackReturnsTask()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		knockOff.Spy.GetValueAsync.OnCall = (ko, input) =>
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
		knockOff.Spy.GetById.OnCall = (ko, id) =>
		{
			if (id == 42) return mockUser;
			return null;
		};

		var result = repo.GetById(42);

		Assert.Same(mockUser, result);
		Assert.Equal(42, knockOff.Spy.GetById.LastCallArg);
	}

	[Fact]
	public void OnGet_InheritedProperty_CallbackWorks()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		knockOff.Spy.Id.OnGet = (ko) => 999;

		var result = entity.Id;

		Assert.Equal(999, result);
		Assert.Equal(1, knockOff.Spy.Id.GetCount);
	}

	#endregion

	#region Indexer Tests (Phase 10)

	[Fact]
	public void Indexer_Get_TracksKeyAccessed()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		// Add value to backing first
		knockOff.StringIndexerBacking["Name"] = new PropertyInfo { Name = "Name", Value = "Test" };

		var result = store["Name"];

		Assert.Equal(1, knockOff.Spy.StringIndexer.GetCount);
		Assert.Equal("Name", knockOff.Spy.StringIndexer.LastGetKey);
		Assert.NotNull(result);
		Assert.Equal("Name", result.Name);
	}

	[Fact]
	public void Indexer_Get_MultipleKeys_TracksAllKeys()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		// Add values to backing
		knockOff.StringIndexerBacking["First"] = new PropertyInfo { Name = "First", Value = "1" };
		knockOff.StringIndexerBacking["Second"] = new PropertyInfo { Name = "Second", Value = "2" };
		knockOff.StringIndexerBacking["Third"] = new PropertyInfo { Name = "Third", Value = "3" };

		_ = store["First"];
		_ = store["Second"];
		_ = store["Third"];

		Assert.Equal(3, knockOff.Spy.StringIndexer.GetCount);
		Assert.Equal(3, knockOff.Spy.StringIndexer.AllGetKeys.Count);
		Assert.Equal("First", knockOff.Spy.StringIndexer.AllGetKeys[0]);
		Assert.Equal("Second", knockOff.Spy.StringIndexer.AllGetKeys[1]);
		Assert.Equal("Third", knockOff.Spy.StringIndexer.AllGetKeys[2]);
	}

	[Fact]
	public void Indexer_OnGet_CallbackReturnsValue()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var mockProperty = new PropertyInfo { Name = "FromCallback", Value = "Mocked" };
		knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
		{
			if (key == "Special") return mockProperty;
			return null;
		};

		var result = store["Special"];

		Assert.Same(mockProperty, result);
		Assert.Equal("Special", knockOff.Spy.StringIndexer.LastGetKey);
	}

	[Fact]
	public void Indexer_OnGet_CallbackCanAccessKnockOffInstance()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
		{
			// Callback can access the knockoff and its Spy
			Assert.Same(knockOff, ko);
			return new PropertyInfo { Name = key, Value = $"Accessed {ko.Spy.StringIndexer.GetCount} times" };
		};

		_ = store["First"];
		var result = store["Second"];

		Assert.Equal("Second", result?.Name);
		Assert.Contains("2", result?.Value); // Should say "Accessed 2 times"
	}

	[Fact]
	public void Indexer_Set_TracksKeyAndValue()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Test", Value = "Value" };
		store["Test"] = prop;

		Assert.Equal(1, knockOff.Spy.StringIndexer.SetCount);
		Assert.NotNull(knockOff.Spy.StringIndexer.LastSetEntry);
		Assert.Equal("Test", knockOff.Spy.StringIndexer.LastSetEntry.Value.key);
		Assert.Same(prop, knockOff.Spy.StringIndexer.LastSetEntry.Value.value);
	}

	[Fact]
	public void Indexer_Set_StoresInBackingDictionary()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Stored", Value = "InBacking" };
		store["Stored"] = prop;

		// Verify it's in the backing dictionary
		Assert.True(knockOff.StringIndexerBacking.ContainsKey("Stored"));
		Assert.Same(prop, knockOff.StringIndexerBacking["Stored"]);

		// Can retrieve via interface
		var retrieved = store["Stored"];
		Assert.Same(prop, retrieved);
	}

	[Fact]
	public void Indexer_OnSet_CallbackInterceptsSetter()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		(string key, PropertyInfo? value)? capturedEntry = null;
		knockOff.Spy.StringIndexer.OnSet = (ko, key, value) =>
		{
			capturedEntry = (key, value);
			// Note: when OnSet is set, value does NOT go to backing
		};

		var prop = new PropertyInfo { Name = "Intercepted", Value = "NotStored" };
		store["MyKey"] = prop;

		Assert.NotNull(capturedEntry);
		Assert.Equal("MyKey", capturedEntry.Value.key);
		Assert.Same(prop, capturedEntry.Value.value);

		// Since OnSet was used, backing was NOT updated
		Assert.False(knockOff.StringIndexerBacking.ContainsKey("MyKey"));
	}

	[Fact]
	public void Indexer_Reset_ClearsAllState()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		// Set up some state
		var prop = new PropertyInfo { Name = "Test", Value = "Value" };
		knockOff.StringIndexerBacking["Existing"] = prop;
		knockOff.Spy.StringIndexer.OnGet = (ko, key) => prop;

		_ = store["Key1"];
		_ = store["Key2"];
		store["Key3"] = prop;

		// Verify state exists
		Assert.Equal(2, knockOff.Spy.StringIndexer.GetCount);
		Assert.Equal(1, knockOff.Spy.StringIndexer.SetCount);
		Assert.NotNull(knockOff.Spy.StringIndexer.OnGet);

		// Reset
		knockOff.Spy.StringIndexer.Reset();

		// Verify state is cleared
		Assert.Equal(0, knockOff.Spy.StringIndexer.GetCount);
		Assert.Empty(knockOff.Spy.StringIndexer.AllGetKeys);
		Assert.Equal(0, knockOff.Spy.StringIndexer.SetCount);
		Assert.Empty(knockOff.Spy.StringIndexer.AllSetEntries);
		Assert.Null(knockOff.Spy.StringIndexer.OnGet);
		Assert.Null(knockOff.Spy.StringIndexer.OnSet);

		// Note: Backing dictionary is NOT cleared by Reset (it's separate from tracking)
		Assert.True(knockOff.StringIndexerBacking.ContainsKey("Existing"));
	}

	[Fact]
	public void Indexer_NullableReturn_ReturnsDefaultWhenNotFound()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		// No callback, no backing value
		var result = store["NonExistent"];

		Assert.Null(result);
		Assert.Equal(1, knockOff.Spy.StringIndexer.GetCount);
	}

	#endregion
}

// Test interface
public interface ISampleService
{
	string Name { get; set; }
	void DoSomething();
	int GetValue(int input);
	void Calculate(string name, int value, bool flag);
	string? GetOptional();
}

// Test KnockOff class - generator produces the explicit interface implementations
[KnockOff]
public partial class SampleKnockOff : ISampleService
{
	// User-defined protected method - generator calls this for GetValue
	protected int GetValue(int input) => input * 2;
}

// Additional interfaces for multi-interface testing
public interface ILogger
{
	void Log(string message);
	string Name { get; set; }
}

public interface IAuditor
{
	void Log(string message); // Same signature as ILogger.Log
	void Audit(string action, int userId);
}

public interface INotifier
{
	void Notify(string recipient);
	string Name { get; } // Same name but get-only (vs ILogger which is get/set)
}

// KnockOff implementing two interfaces with different methods
[KnockOff]
public partial class MultiInterfaceKnockOff : ILogger, INotifier
{
}

// KnockOff implementing two interfaces with same method signature
[KnockOff]
public partial class SharedSignatureKnockOff : ILogger, IAuditor
{
}

// Async interface for Phase 8.1 testing
public interface IAsyncService
{
	Task DoWorkAsync();
	Task<int> GetValueAsync(int input);
	Task<string?> GetOptionalAsync();
	Task<string> GetRequiredAsync();
	ValueTask DoWorkValueTaskAsync();
	ValueTask<int> GetValueValueTaskAsync(int input);
}

// KnockOff for async interface
[KnockOff]
public partial class AsyncServiceKnockOff : IAsyncService
{
	// User-defined method for GetValueAsync
	protected Task<int> GetValueAsync(int input) => Task.FromResult(input * 3);

	// User-defined method for GetValueValueTaskAsync
	protected ValueTask<int> GetValueValueTaskAsync(int input) => new(input * 4);
}

// Generic interface for Phase 8.2 testing
public interface IRepository<T> where T : class
{
	T? GetById(int id);
	void Save(T entity);
	Task<T?> GetByIdAsync(int id);
}

// Concrete KnockOff implementing generic interface
[KnockOff]
public partial class UserRepositoryKnockOff : IRepository<User>
{
}

public class User
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

// Interface inheritance for Phase 8.3 testing
public interface IBaseEntity
{
	int Id { get; }
	DateTime CreatedAt { get; }
}

public interface IAuditableEntity : IBaseEntity
{
	DateTime? ModifiedAt { get; set; }
	string ModifiedBy { get; set; }
}

// KnockOff implementing derived interface (should also implement base)
[KnockOff]
public partial class AuditableEntityKnockOff : IAuditableEntity
{
}

// Interfaces with indexers for Phase 10 testing
public class PropertyInfo
{
	public string Name { get; set; } = "";
	public string Value { get; set; } = "";
}

public interface IPropertyStore
{
	PropertyInfo? this[string key] { get; }
}

public interface IReadWriteStore
{
	PropertyInfo? this[string key] { get; set; }
}

// KnockOff for read-only indexer interface
[KnockOff]
public partial class PropertyStoreKnockOff : IPropertyStore
{
}

// KnockOff for read-write indexer interface
[KnockOff]
public partial class ReadWriteStoreKnockOff : IReadWriteStore
{
}
