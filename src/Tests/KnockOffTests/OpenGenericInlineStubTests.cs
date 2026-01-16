namespace KnockOff.Tests;

/// <summary>
/// Tests for open generic inline stubs using [KnockOff(typeof(T&lt;&gt;))] syntax.
/// This feature allows creating generic stubs like Stubs.Repository&lt;T&gt; from [KnockOff(typeof(IOGRepository&lt;&gt;))].
/// </summary>
public class OpenGenericInlineStubTests
{
	#region Interface Tests

	[Fact]
	public void OpenGenericInterface_GeneratesGenericStubClass()
	{
		// Verify the generic stub class exists and can be instantiated with type argument
		var stringStub = new OpenGenericInterfaceTest.Stubs.IOGRepository<string>();
		var intStub = new OpenGenericInterfaceTest.Stubs.IOGRepository<int>();

		Assert.NotNull(stringStub);
		Assert.NotNull(intStub);
	}

	[Fact]
	public void OpenGenericInterface_ImplementsCorrectInterface()
	{
		var stub = new OpenGenericInterfaceTest.Stubs.IOGRepository<string>();

		// Verify it implements IOGRepository<string>
		IOGRepository<string> repo = stub;
		Assert.NotNull(repo);
	}

	[Fact]
	public void OpenGenericInterface_MethodTracking_Works()
	{
		var stub = new OpenGenericInterfaceTest.Stubs.IOGRepository<string>();
		IOGRepository<string> repo = stub;

		repo.Add("test");
		repo.Add("another");

		Assert.Equal(2, stub.Add.CallCount);
		Assert.True(stub.Add.WasCalled);
		Assert.Equal("another", stub.Add.LastCallArg);
	}

	[Fact]
	public void OpenGenericInterface_PropertyTracking_Works()
	{
		var stub = new OpenGenericInterfaceTest.Stubs.IOGRepository<int>();
		IOGRepository<int> repo = stub;

		stub.Count.Value = 42;
		var count = repo.Count;

		Assert.Equal(1, stub.Count.GetCount);
		Assert.Equal(42, count);
	}

	[Fact]
	public void OpenGenericInterface_OnCall_Works()
	{
		var stub = new OpenGenericInterfaceTest.Stubs.IOGRepository<string>();
		stub.GetById.OnCall = (ko, id) => $"Item-{id}";

		IOGRepository<string> repo = stub;
		var result = repo.GetById(123);

		Assert.Equal("Item-123", result);
	}

	[Fact]
	public void OpenGenericInterface_DifferentTypeArgs_AreSeparate()
	{
		var stringStub = new OpenGenericInterfaceTest.Stubs.IOGRepository<string>();
		var intStub = new OpenGenericInterfaceTest.Stubs.IOGRepository<int>();

		IOGRepository<string> stringRepo = stringStub;
		IOGRepository<int> intRepo = intStub;

		stringRepo.Add("test");
		intRepo.Add(42);

		Assert.Equal(1, stringStub.Add.CallCount);
		Assert.Equal(1, intStub.Add.CallCount);
		Assert.Equal("test", stringStub.Add.LastCallArg);
		Assert.Equal(42, intStub.Add.LastCallArg);
	}

	#endregion

	// TODO: Open generic class stubs are not yet implemented
	// #region Class Tests
	// ...
	// #endregion

	#region Constraint Tests

	[Fact]
	public void OpenGenericWithConstraint_Interface_Works()
	{
		// Verify class constraint is enforced at compile time
		// This test verifying it compiles is sufficient
		var stub = new ConstrainedGenericInterfaceTest.Stubs.IClassRepository<OGTestEntity>();
		Assert.NotNull(stub);
	}

	// TODO: Open generic class stubs are not yet implemented
	// [Fact]
	// public void OpenGenericWithConstraint_Class_Works()
	// {
	// 	var stub = new ConstrainedGenericClassTest.Stubs.ConstrainedService<OGTestEntity>();
	// 	Assert.NotNull(stub);
	// }

	#endregion

	#region Multi-Parameter Tests

	[Fact]
	public void OpenGenericMultiParam_GeneratesCorrectly()
	{
		var stub = new MultiParamGenericTest.Stubs.IKeyValueStore<string, int>();
		Assert.NotNull(stub);

		IKeyValueStore<string, int> store = stub;
		store.Set("key", 42);

		Assert.True(stub.Set.WasCalled);
		Assert.Equal(("key", 42), stub.Set.LastCallArgs);
	}

	#endregion
}

#region Test Types for Open Generic Stubs

/// <summary>
/// Generic repository interface for testing open generic interface stubs.
/// </summary>
public interface IOGRepository<T>
{
	int Count { get; }
	void Add(T item);
	T? GetById(int id);
	IEnumerable<T> GetAll();
}

/// <summary>
/// Generic factory delegate for testing open generic delegate stubs.
/// </summary>
public delegate T GenericFactory<T>();

/// <summary>
/// Generic converter delegate with input parameter.
/// </summary>
public delegate TResult GenericConverter<TInput, TResult>(TInput input);

// ============================================================================
// Open Generic Delegate Tests
// ============================================================================

public delegate T OGFactory<T>();
public delegate TResult OGConverter<TIn, TOut, TResult>(TIn input) where TResult : class;

[KnockOff(typeof(OGFactory<>))]
[KnockOff(typeof(OGConverter<,,>))]
public partial class OpenGenericDelegateTests
{
	[Fact]
	public void SingleTypeParam_CanInstantiateWithDifferentTypes()
	{
		var stringFactory = new Stubs.OGFactory<string>();
		var intFactory = new Stubs.OGFactory<int>();

		Assert.NotNull(stringFactory);
		Assert.NotNull(intFactory);
	}

	[Fact]
	public void SingleTypeParam_InterceptorTracksInvocations()
	{
		var stub = new Stubs.OGFactory<string>();
		stub.Interceptor.OnCall = (ko) => "test-value";

		OGFactory<string> factory = stub;
		var result = factory();

		Assert.Equal("test-value", result);
		Assert.Equal(1, stub.Interceptor.CallCount);
	}

	[Fact]
	public void MultipleTypeParams_PreservesConstraints()
	{
		// TResult has 'class' constraint - string satisfies it
		var stub = new Stubs.OGConverter<int, bool, string>();
		stub.Interceptor.OnCall = (ko, input) => input.ToString();

		OGConverter<int, bool, string> converter = stub;
		var result = converter(42);

		Assert.Equal("42", result);
	}
}

/// <summary>
/// Generic service class for testing open generic class stubs.
/// </summary>
public class GenericService<T>
{
	public virtual T? Value { get; set; }
	public virtual void Process(T item) { }
	public virtual T? GetValue() => Value;
}

/// <summary>
/// Interface with class constraint for testing constraint preservation.
/// </summary>
public interface IClassRepository<T> where T : class
{
	void Add(T item);
	T? Find(int id);
}

/// <summary>
/// Class with constraint for testing constraint preservation.
/// </summary>
public class ConstrainedService<T> where T : class, new()
{
	public virtual T Create() => new T();
	public virtual void Process(T item) { }
}

/// <summary>
/// Multi-parameter generic interface.
/// </summary>
public interface IKeyValueStore<TKey, TValue>
{
	void Set(TKey key, TValue value);
	TValue? Get(TKey key);
}

/// <summary>
/// Test entity for constraint tests.
/// </summary>
public class OGTestEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

#endregion

#region Test Host Classes

/// <summary>
/// Test class using open generic interface stub syntax.
/// </summary>
[KnockOff(typeof(IOGRepository<>))]
public partial class OpenGenericInterfaceTest
{
}

// TODO: Open generic class stubs are not yet implemented
// [KnockOff(typeof(GenericService<>))]
// public partial class OpenGenericClassTest
// {
// }

/// <summary>
/// Test class with constrained generic interface.
/// </summary>
[KnockOff(typeof(IClassRepository<>))]
public partial class ConstrainedGenericInterfaceTest
{
}

// TODO: Open generic class stubs are not yet implemented
// [KnockOff(typeof(ConstrainedService<>))]
// public partial class ConstrainedGenericClassTest
// {
// }

/// <summary>
/// Test class with multi-parameter generic.
/// </summary>
[KnockOff(typeof(IKeyValueStore<,>))]
public partial class MultiParamGenericTest
{
}

#endregion
