namespace KnockOff.Tests;

/// <summary>
/// Tests for known bugs in generic method handling.
/// These tests are skipped until the bugs are fixed.
/// See: docs/todos/bug-generic-methods-edge-cases.md
/// </summary>
public class GenericMethodBugTests
{
	#region Bug 1: User Method Detection for Generic Methods

	/// <summary>
	/// Tests that user-defined protected generic methods are detected and called by the generator.
	/// </summary>
	[Fact]
	public void GenericMethod_UserMethod_ShouldBeCalledInsteadOfDefault()
	{
		var knockOff = new GenericMethodWithUserMethodKnockOff();
		IGenericMethodWithUserMethod service = knockOff;

		var result = service.Create<TestEntity>();

		// User method sets Id = 999
		Assert.Equal(999, result.Id);
		Assert.True(knockOff.Create2.Of<TestEntity>().WasCalled);
	}

	[Fact]
	public void GenericMethod_UserMethod_WithParameter_ShouldTransformValue()
	{
		var knockOff = new GenericMethodWithUserMethodKnockOff();
		IGenericMethodWithUserMethod service = knockOff;

		// User method doubles integers
		var result = service.Transform(21);
		Assert.Equal(42, result);

		// User method appends "_transformed" to strings
		var stringResult = service.Transform("hello");
		Assert.Equal("hello_transformed", stringResult);
	}

	[Fact]
	public void GenericMethod_UserMethod_OnCallTakesPriority()
	{
		// OnCall should still take priority over user method
		var knockOff = new GenericMethodWithUserMethodKnockOff();
		IGenericMethodWithUserMethod service = knockOff;

		knockOff.Create2.Of<TestEntity>().OnCall = (ko) => new TestEntity { Id = 123 };

		var result = service.Create<TestEntity>();

		// OnCall value, not user method value (999)
		Assert.Equal(123, result.Id);
	}

	#endregion

	#region Bug 2: Mixed Generic/Non-Generic Overloads

	/// <summary>
	/// Tests that non-generic overloads are tracked separately from generic overloads.
	/// </summary>
	[Fact]
	public void MixedOverloads_NonGeneric_TrackedSeparately()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		IMixedOverloadService service = knockOff;

		service.Process("hello");
		service.Process(42);

		// Non-generic overloads should be tracked
		Assert.Equal(1, knockOff.Process1.CallCount); // Process(string)
		Assert.Equal(1, knockOff.Process2.CallCount); // Process(int)
	}

	[Fact]
	public void MixedOverloads_Generic_TrackedWithOf()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		IMixedOverloadService service = knockOff;

		service.Process(3.14);
		service.Process(true);

		// Generic overload should use Of<T>() pattern
		Assert.Equal(1, knockOff.ProcessGeneric.Of<double>().CallCount);
		Assert.Equal(1, knockOff.ProcessGeneric.Of<bool>().CallCount);
	}

	[Fact]
	public void MixedOverloads_AllOverloads_IndependentTracking()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		IMixedOverloadService service = knockOff;

		// Call all overloads
		service.Process("text");           // Non-generic (string)
		service.Process(100);              // Non-generic (int)
		service.Process<string>("generic"); // Generic with T=string
		service.Process<int>(200);          // Generic with T=int

		// All should be tracked independently
		Assert.Equal(1, knockOff.Process1.CallCount);  // Process(string)
		Assert.Equal(1, knockOff.Process2.CallCount);  // Process(int)
		Assert.Equal(1, knockOff.ProcessGeneric.Of<string>().CallCount);
		Assert.Equal(1, knockOff.ProcessGeneric.Of<int>().CallCount);

		// Note: Process(string) and Process<string>() are DIFFERENT overloads
		// even though T happens to be string
	}

	[Fact]
	public void MixedOverloads_WithReturnType_BothWork()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		IMixedOverloadService service = knockOff;

		knockOff.Format.OnCall = (ko, value) => $"int:{value}";
		knockOff.FormatGeneric.Of<double>().OnCall = (ko, value) => $"double:{value}";

		var intResult = service.Format(42);
		var doubleResult = service.Format(3.14);

		Assert.Equal("int:42", intResult);
		Assert.Equal("double:3.14", doubleResult);
	}

	#endregion

	#region Bug 3: Generic Method Constraints Not Preserved (Fixed)

	/// <summary>
	/// Tests that generic methods with type constraints returning T? compile correctly.
	/// The generator must emit "where T : class" to make T? a nullable reference type.
	/// </summary>
	[Fact]
	public void ConstrainedGeneric_WithTypeConstraint_CompilesAndWorks()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		// Configure to return a test attribute
		var testAttr = new TestAttribute();
		knockOff.GetAttribute.Of<TestAttribute>().OnCall = (ko) => testAttr;

		var result = service.GetAttribute<TestAttribute>();

		Assert.Same(testAttr, result);
		Assert.True(knockOff.GetAttribute.Of<TestAttribute>().WasCalled);
	}

	[Fact]
	public void ConstrainedGeneric_WithTypeConstraint_CanReturnNull()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		knockOff.GetAttribute.Of<TestAttribute>().OnCall = (ko) => null;

		var result = service.GetAttribute<TestAttribute>();

		Assert.Null(result);
	}

	[Fact]
	public void ConstrainedGeneric_WithClassConstraint_Works()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		knockOff.GetOrDefault.Of<string>().OnCall = (ko) => "test";

		var result = service.GetOrDefault<string>();

		Assert.Equal("test", result);
	}

	[Fact]
	public void ConstrainedGeneric_MultipleTypeParams_Works()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		knockOff.Transform.Of<int, string>().OnCall = (ko, input) => $"value:{input}";

		var result = service.Transform<int, string>(42);

		Assert.Equal("value:42", result);
	}

	[Fact]
	public void ConstrainedGeneric_InterfaceConstraint_Works()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		var stream = new MemoryStream();
		knockOff.FindService.Of<MemoryStream>().OnCall = (ko) => stream;

		var result = service.FindService<MemoryStream>();

		Assert.Same(stream, result);
	}

	#endregion
}

/// <summary>
/// Test attribute for constrained generic method tests.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class TestAttribute : Attribute
{
	public string? Value { get; set; }
}

#region Bug Test Types

/// <summary>
/// Interface for testing user method detection with generic methods.
/// </summary>
public interface IGenericMethodWithUserMethod
{
	T Create<T>() where T : new();
	T Transform<T>(T value);
	TOut Convert<TIn, TOut>(TIn input) where TOut : new();
}

/// <summary>
/// KnockOff with user-defined generic methods that should take priority over defaults.
/// </summary>
[KnockOff]
public partial class GenericMethodWithUserMethodKnockOff : IGenericMethodWithUserMethod
{
	// User method - should be called instead of smart default
	protected T Create<T>() where T : new()
	{
		var instance = new T();
		// Mark it somehow - for TestEntity, set Id to 999
		if (instance is TestEntity entity)
			entity.Id = 999;
		return instance;
	}

	// User method for transform - doubles integers, appends to strings
	protected T Transform<T>(T value)
	{
		if (value is int i)
			return (T)(object)(i * 2);
		if (value is string s)
			return (T)(object)(s + "_transformed");
		return value;
	}

	// User method for convert - special handling
	protected TOut Convert<TIn, TOut>(TIn input) where TOut : new()
	{
		var result = new TOut();
		if (result is TestEntity entity && input is int id)
			entity.Id = id * 10;
		return result;
	}
}

/// <summary>
/// Interface with both generic and non-generic overloads of the same method name.
/// Bug 2: Generator crashes with KeyNotFoundException.
/// </summary>
public interface IMixedOverloadService
{
	// Non-generic overloads
	void Process(string value);
	void Process(int value);

	// Generic overload - same name, but with type parameter
	void Process<T>(T value);

	// Non-generic with return
	string Format(int value);

	// Generic with return
	string Format<T>(T value);
}

[KnockOff]
public partial class MixedOverloadServiceKnockOff : IMixedOverloadService
{
}

/// <summary>
/// Interface with constrained generic methods returning nullable type parameter.
/// Bug 3: Generator didn't emit class constraint for T? returns, causing CS0453/CS0539.
/// Fixed: Now emits "where T : class" when return type is T? and T has any constraint that implies class.
/// </summary>
public interface IConstrainedGenericMethod
{
	// Type constraint (Attribute) implies T is a reference type
	// Return type T? should be nullable reference, not Nullable<T>
	T? GetAttribute<T>() where T : Attribute;

	// Explicit class constraint
	T? GetOrDefault<T>() where T : class;

	// Multiple type parameters with different constraints
	TResult? Transform<TInput, TResult>(TInput input)
		where TInput : struct
		where TResult : class;

	// No constraint - should NOT emit class (T? interpreted as Nullable<T>)
	T GetValue<T>(int index);

	// Interface constraint with class - this is the proper way
	T? FindService<T>() where T : class, IDisposable;
}

[KnockOff]
public partial class ConstrainedGenericMethodKnockOff : IConstrainedGenericMethod
{
}

#endregion
