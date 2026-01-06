namespace KnockOff.Tests;

/// <summary>
/// Tests for generic method support using the Of&lt;T&gt;() pattern.
/// </summary>
public class GenericMethodTests
{
	#region Basic Tracking - Single Type Parameter

	[Fact]
	public void GenericMethod_NoParams_TracksCallCount()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Create<TestEntity>();
		service.Create<TestEntity>();

		Assert.Equal(2, knockOff.IGenericMethodService.Create.Of<TestEntity>().CallCount);
		Assert.True(knockOff.IGenericMethodService.Create.Of<TestEntity>().WasCalled);
	}

	[Fact]
	public void GenericMethod_DifferentTypeArgs_TrackedSeparately()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Create<TestEntity>();
		service.Create<List<int>>();
		service.Create<List<int>>();

		Assert.Equal(1, knockOff.IGenericMethodService.Create.Of<TestEntity>().CallCount);
		Assert.Equal(2, knockOff.IGenericMethodService.Create.Of<List<int>>().CallCount);
	}

	[Fact]
	public void GenericMethod_VoidWithParam_TracksCallCount()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Process("hello");
		service.Process(42);
		service.Process(42);

		Assert.Equal(1, knockOff.IGenericMethodService.Process.Of<string>().CallCount);
		Assert.Equal(2, knockOff.IGenericMethodService.Process.Of<int>().CallCount);
	}

	#endregion

	#region Aggregate Tracking

	[Fact]
	public void GenericMethod_TotalCallCount_AcrossAllTypeArgs()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Create<TestEntity>();
		service.Create<List<int>>();
		service.Create<List<string>>();

		Assert.Equal(3, knockOff.IGenericMethodService.Create.TotalCallCount);
	}

	[Fact]
	public void GenericMethod_WasCalled_TrueIfAnyTypeArgCalled()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		Assert.False(knockOff.IGenericMethodService.Create.WasCalled);

		service.Create<TestEntity>();

		Assert.True(knockOff.IGenericMethodService.Create.WasCalled);
	}

	[Fact]
	public void GenericMethod_CalledTypeArguments_TracksAllTypes()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Create<TestEntity>();
		service.Create<List<int>>();

		var calledTypes = knockOff.IGenericMethodService.Create.CalledTypeArguments;
		Assert.Equal(2, calledTypes.Count);
		Assert.Contains(typeof(TestEntity), calledTypes);
		Assert.Contains(typeof(List<int>), calledTypes);
	}

	#endregion

	#region OnCall Callbacks

	[Fact]
	public void GenericMethod_OnCall_ReturnsConfiguredValue()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		var expected = new TestEntity { Id = 42, Name = "Test" };
		knockOff.IGenericMethodService.Create.Of<TestEntity>().OnCall = (ko) => expected;

		var result = service.Create<TestEntity>();

		Assert.Same(expected, result);
	}

	[Fact]
	public void GenericMethod_OnCall_DifferentCallbacksPerTypeArg()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		knockOff.IGenericMethodService.Create.Of<TestEntity>().OnCall = (ko) => new TestEntity { Id = 1 };
		knockOff.IGenericMethodService.Create.Of<List<int>>().OnCall = (ko) => new List<int> { 100 };

		var entity = service.Create<TestEntity>();
		var list = service.Create<List<int>>();

		Assert.Equal(1, entity.Id);
		Assert.Single(list);
		Assert.Equal(100, list[0]);
	}

	[Fact]
	public void GenericMethod_OnCallVoid_InvokesCallback()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		string? captured = null;
		knockOff.IGenericMethodService.Process.Of<string>().OnCall = (ko, value) => captured = value;

		service.Process("hello world");

		Assert.Equal("hello world", captured);
	}

	[Fact]
	public void GenericMethod_OnCallWithParam_ReturnsValue()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		knockOff.IGenericMethodService.Deserialize.Of<TestEntity>().OnCall = (ko, json) =>
			new TestEntity { Id = 99, Name = json };

		var result = service.Deserialize<TestEntity>("{\"Name\":\"test\"}");

		Assert.Equal(99, result.Id);
		Assert.Equal("{\"Name\":\"test\"}", result.Name);
	}

	#endregion

	#region Parameter Tracking

	[Fact]
	public void GenericMethod_WithNonGenericParam_TracksLastArg()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Deserialize<TestEntity>("{\"first\":1}");
		service.Deserialize<TestEntity>("{\"second\":2}");

		Assert.Equal("{\"second\":2}", knockOff.IGenericMethodService.Deserialize.Of<TestEntity>().LastCallArg);
	}

	[Fact]
	public void GenericMethod_WithNonGenericParam_TracksAllCalls()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Find<User>(1);
		service.Find<User>(2);
		service.Find<User>(3);

		Assert.Equal(3, knockOff.IGenericMethodService.Find.Of<User>().CallCount);
		Assert.Equal(3, knockOff.IGenericMethodService.Find.Of<User>().LastCallArg); // Last call was Find<User>(3)
	}

	#endregion

	#region Smart Defaults

	[Fact]
	public void GenericMethod_SmartDefault_NewConstraint_ReturnsNewInstance()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		var result = service.Create<TestEntity>();

		Assert.NotNull(result);
		Assert.IsType<TestEntity>(result);
	}

	[Fact]
	public void GenericMethod_SmartDefault_ValueType_ReturnsDefault()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		// Process is void, but let's test with a method that returns generic
		// Use Convert<TIn, TOut> where TOut is value type
		var result = service.Convert<string, int>("test");

		Assert.Equal(0, result);
	}

	[Fact]
	public void GenericMethod_SmartDefault_NoParameterlessCtor_Throws()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		// string doesn't have a parameterless constructor
		var ex = Assert.Throws<InvalidOperationException>(() => service.Deserialize<string>("{}"));
		Assert.Contains("No implementation provided", ex.Message);
		Assert.Contains("Deserialize", ex.Message);
	}

	[Fact]
	public void GenericMethod_NullableReturn_ReturnsNull()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		var result = service.Find<User>(999);

		Assert.Null(result);
	}

	#endregion

	#region Multiple Type Parameters

	[Fact]
	public void GenericMethod_MultipleTypeParams_TracksCorrectly()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		// Need to set OnCall for int->string since string has no parameterless ctor
		knockOff.IGenericMethodService.Convert.Of<int, string>().OnCall = (ko, i) => i.ToString();

		service.Convert<string, int>("hello");
		service.Convert<int, string>(42);

		Assert.Equal(1, knockOff.IGenericMethodService.Convert.Of<string, int>().CallCount);
		Assert.Equal(1, knockOff.IGenericMethodService.Convert.Of<int, string>().CallCount);
	}

	[Fact]
	public void GenericMethod_MultipleTypeParams_OnCallWorks()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		knockOff.IGenericMethodService.Convert.Of<string, int>().OnCall = (ko, input) => input.Length;

		var result = service.Convert<string, int>("hello");

		Assert.Equal(5, result);
	}

	[Fact]
	public void GenericMethod_MultipleTypeParams_TotalCallCount()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		// Need to set OnCall for int->string since string has no parameterless ctor
		knockOff.IGenericMethodService.Convert.Of<int, string>().OnCall = (ko, i) => i.ToString();

		service.Convert<string, int>("a");
		service.Convert<string, int>("b");
		service.Convert<int, string>(1);

		Assert.Equal(3, knockOff.IGenericMethodService.Convert.TotalCallCount);
	}

	[Fact]
	public void GenericMethod_MultipleTypeParams_CalledTypeArguments()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Convert<string, int>("test");
		service.Convert<double, bool>(1.5);

		var calledTypes = knockOff.IGenericMethodService.Convert.CalledTypeArguments;
		Assert.Equal(2, calledTypes.Count);
		Assert.Contains((typeof(string), typeof(int)), calledTypes);
		Assert.Contains((typeof(double), typeof(bool)), calledTypes);
	}

	[Fact]
	public void GenericMethod_VoidMultipleTypeParams_OnCallWorks()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		string? capturedSource = null;
		int capturedDest = 0;
		knockOff.IGenericMethodService.Transfer.Of<string, int>().OnCall = (ko, source, dest) =>
		{
			capturedSource = source;
			capturedDest = dest;
		};

		service.Transfer("hello", 42);

		Assert.Equal("hello", capturedSource);
		Assert.Equal(42, capturedDest);
	}

	#endregion

	#region Constrained Generics

	[Fact]
	public void ConstrainedGeneric_ClassAndInterfaceAndNew_Works()
	{
		var knockOff = new ConstrainedGenericServiceKnockOff();
		IConstrainedGenericService service = knockOff;

		service.CreateEntity<TestEntityWithInterface>();

		Assert.Equal(1, knockOff.IConstrainedGenericService.CreateEntity.Of<TestEntityWithInterface>().CallCount);
	}

	[Fact]
	public void ConstrainedGeneric_OnCallWithConstraints_Works()
	{
		var knockOff = new ConstrainedGenericServiceKnockOff();
		IConstrainedGenericService service = knockOff;

		var expected = new TestEntityWithInterface { Id = 123, Name = "Constrained" };
		knockOff.IConstrainedGenericService.CreateEntity.Of<TestEntityWithInterface>().OnCall = (ko) => expected;

		var result = service.CreateEntity<TestEntityWithInterface>();

		Assert.Same(expected, result);
	}

	[Fact]
	public void ConstrainedGeneric_VoidMethod_TracksCorrectly()
	{
		var knockOff = new ConstrainedGenericServiceKnockOff();
		IConstrainedGenericService service = knockOff;

		var entity = new TestEntityWithInterface { Id = 1 };
		service.SaveEntity(entity);

		Assert.Equal(1, knockOff.IConstrainedGenericService.SaveEntity.Of<TestEntityWithInterface>().CallCount);
	}

	[Fact]
	public void ConstrainedGeneric_VoidMethod_OnCallCaptures()
	{
		var knockOff = new ConstrainedGenericServiceKnockOff();
		IConstrainedGenericService service = knockOff;

		TestEntityWithInterface? captured = null;
		knockOff.IConstrainedGenericService.SaveEntity.Of<TestEntityWithInterface>().OnCall = (ko, e) => captured = e;

		var entity = new TestEntityWithInterface { Id = 42 };
		service.SaveEntity(entity);

		Assert.NotNull(captured);
		Assert.Equal(42, captured.Id);
	}

	[Fact]
	public void ConstrainedGeneric_SmartDefault_UsesNewConstraint()
	{
		var knockOff = new ConstrainedGenericServiceKnockOff();
		IConstrainedGenericService service = knockOff;

		// TestEntityWithInterface has new() constraint and parameterless ctor
		var result = service.CreateEntity<TestEntityWithInterface>();

		Assert.NotNull(result);
		Assert.IsType<TestEntityWithInterface>(result);
		Assert.Equal(0, result.Id); // Default values
	}

	#endregion

	#region Reset

	[Fact]
	public void GenericMethod_Reset_ClearsTypedHandler()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		knockOff.IGenericMethodService.Create.Of<TestEntity>().OnCall = (ko) => new TestEntity { Id = 1 };
		service.Create<TestEntity>();

		knockOff.IGenericMethodService.Create.Of<TestEntity>().Reset();

		Assert.Equal(0, knockOff.IGenericMethodService.Create.Of<TestEntity>().CallCount);
		Assert.False(knockOff.IGenericMethodService.Create.Of<TestEntity>().WasCalled);
		Assert.Null(knockOff.IGenericMethodService.Create.Of<TestEntity>().OnCall);
	}

	[Fact]
	public void GenericMethod_Reset_ClearsAllTypedHandlers()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Create<TestEntity>();
		service.Create<List<int>>();

		knockOff.IGenericMethodService.Create.Reset();

		Assert.Equal(0, knockOff.IGenericMethodService.Create.TotalCallCount);
		Assert.False(knockOff.IGenericMethodService.Create.WasCalled);
		Assert.Empty(knockOff.IGenericMethodService.Create.CalledTypeArguments);
	}

	[Fact]
	public void GenericMethod_ResetWithParams_ClearsAllCalls()
	{
		var knockOff = new GenericMethodServiceKnockOff();
		IGenericMethodService service = knockOff;

		service.Deserialize<TestEntity>("{\"a\":1}");
		service.Deserialize<TestEntity>("{\"b\":2}");

		knockOff.IGenericMethodService.Deserialize.Of<TestEntity>().Reset();

		Assert.Equal(0, knockOff.IGenericMethodService.Deserialize.Of<TestEntity>().CallCount);
		Assert.Null(knockOff.IGenericMethodService.Deserialize.Of<TestEntity>().LastCallArg);
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void GenericMethod_NotCalled_HasZeroCallCount()
	{
		var knockOff = new GenericMethodServiceKnockOff();

		// Accessing Of<T>() without calling should have zero count
		Assert.Equal(0, knockOff.IGenericMethodService.Create.Of<TestEntity>().CallCount);
		Assert.False(knockOff.IGenericMethodService.Create.Of<TestEntity>().WasCalled);
	}

	[Fact]
	public void GenericMethod_CalledTypeArguments_InitiallyEmpty()
	{
		var knockOff = new GenericMethodServiceKnockOff();

		Assert.Empty(knockOff.IGenericMethodService.Create.CalledTypeArguments);
	}

	[Fact]
	public void GenericMethod_TotalCallCount_InitiallyZero()
	{
		var knockOff = new GenericMethodServiceKnockOff();

		Assert.Equal(0, knockOff.IGenericMethodService.Create.TotalCallCount);
		Assert.False(knockOff.IGenericMethodService.Create.WasCalled);
	}

	#endregion
}
