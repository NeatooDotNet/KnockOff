namespace KnockOff.Tests;

/// <summary>
/// Tests for known bugs in generic method handling.
/// These tests are skipped until the bugs are fixed.
/// See: docs/todos/bug-generic-methods-edge-cases.md
/// </summary>
public class GenericMethodBugTests
{
	#region Generic Methods with OnCall

	/// <summary>
	/// Tests that generic methods use OnCall for configuration.
	/// NOTE: Generic methods do NOT support user overrides by design. Use OnCall instead.
	/// </summary>
	[Fact]
	public void GenericMethod_OnCall_ReturnsConfiguredValue()
	{
		var knockOff = new GenericMethodWithUserMethodKnockOff();
		IGenericMethodWithUserMethod service = knockOff;

		knockOff.Create.Of<TestEntity>().Return(() => new TestEntity { Id = 999 });

		var result = service.Create<TestEntity>();

		Assert.Equal(999, result.Id);
		knockOff.Create.Of<TestEntity>().Verify();
	}

	[Fact]
	public void GenericMethod_OnCall_WithParameter_TransformsValue()
	{
		var knockOff = new GenericMethodWithUserMethodKnockOff();
		IGenericMethodWithUserMethod service = knockOff;

		// OnCall doubles integers
		knockOff.Transform.Of<int>().Return((value) => value * 2);
		knockOff.Transform.Of<string>().Return((value) => value + "_transformed");

		var result = service.Transform(21);
		Assert.Equal(42, result);

		var stringResult = service.Transform("hello");
		Assert.Equal("hello_transformed", stringResult);
	}

	[Fact]
	public void GenericMethod_MultipleTypeParams_OnCall()
	{
		var knockOff = new GenericMethodWithUserMethodKnockOff();
		IGenericMethodWithUserMethod service = knockOff;

		knockOff.Convert.Of<int, TestEntity>().Return((input) => new TestEntity { Id = input * 10 });

		var result = service.Convert<int, TestEntity>(5);

		Assert.Equal(50, result.Id);
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
		// Configure callbacks for non-generic overloads via overloaded Execute
		var stringTracking = knockOff.Process.Call((string value) => { });
		var intTracking = knockOff.Process.Call((int value) => { });
		IMixedOverloadService service = knockOff;

		service.Process("hello");
		service.Process(42);

		// Non-generic overloads should be tracked
		stringTracking.Verify(Called.Once); // Process(string)
		intTracking.Verify(Called.Once); // Process(int)
	}

	[Fact]
	public void MixedOverloads_Generic_TrackedWithOf()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		IMixedOverloadService service = knockOff;

		service.Process(3.14);
		service.Process(true);

		// Generic overload should use Of<T>() pattern
		knockOff.ProcessGeneric.Of<double>().Verify(Called.Once);
		knockOff.ProcessGeneric.Of<bool>().Verify(Called.Once);
	}

	[Fact]
	public void MixedOverloads_AllOverloads_IndependentTracking()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		// Configure callbacks for non-generic overloads
		var stringTracking = knockOff.Process.Call((string value) => { });
		var intTracking = knockOff.Process.Call((int value) => { });
		IMixedOverloadService service = knockOff;

		// Call all overloads
		service.Process("text");           // Non-generic (string)
		service.Process(100);              // Non-generic (int)
		service.Process<string>("generic"); // Generic with T=string
		service.Process<int>(200);          // Generic with T=int

		// All should be tracked independently
		stringTracking.Verify(Called.Once);  // Process(string)
		intTracking.Verify(Called.Once);  // Process(int)
		knockOff.ProcessGeneric.Of<string>().Verify(Called.Once);
		knockOff.ProcessGeneric.Of<int>().Verify(Called.Once);

		// Note: Process(string) and Process<string>() are DIFFERENT overloads
		// even though T happens to be string
	}

	[Fact]
	public void MixedOverloads_WithReturnType_BothWork()
	{
		var knockOff = new MixedOverloadServiceKnockOff();
		IMixedOverloadService service = knockOff;

		knockOff.Format.Return((int value) => $"int:{value}");
		knockOff.FormatGeneric.Of<double>().Return((value) => $"double:{value}");

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
		knockOff.GetAttribute.Of<TestAttribute>().Return(() => testAttr);

		var result = service.GetAttribute<TestAttribute>();

		Assert.Same(testAttr, result);
		knockOff.GetAttribute.Of<TestAttribute>().Verify();
	}

	[Fact]
	public void ConstrainedGeneric_WithTypeConstraint_CanReturnNull()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		knockOff.GetAttribute.Of<TestAttribute>().Return(() => null);

		var result = service.GetAttribute<TestAttribute>();

		Assert.Null(result);
	}

	[Fact]
	public void ConstrainedGeneric_WithClassConstraint_Works()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		knockOff.GetOrDefault.Of<string>().Return(() => "test");

		var result = service.GetOrDefault<string>();

		Assert.Equal("test", result);
	}

	[Fact]
	public void ConstrainedGeneric_MultipleTypeParams_Works()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		knockOff.Transform.Of<int, string>().Return((input) => $"value:{input}");

		var result = service.Transform<int, string>(42);

		Assert.Equal("value:42", result);
	}

	[Fact]
	public void ConstrainedGeneric_InterfaceConstraint_Works()
	{
		var knockOff = new ConstrainedGenericMethodKnockOff();
		IConstrainedGenericMethod service = knockOff;

		var stream = new MemoryStream();
		knockOff.FindService.Of<MemoryStream>().Return(() => stream);

		var result = service.FindService<MemoryStream>();

		Assert.Same(stream, result);
	}

	#endregion

	#region Bug 4: Spurious where T : class on Nullable Unconstrained Generics (Fixed)

	/// <summary>
	/// Tests that unconstrained generic methods with T? return type compile and work correctly.
	/// Bug: Generator emitted "where T : class" on explicit implementation even though the
	/// original method has no such constraint. This caused CS8665.
	/// Fix: Generator now checks IsKnownReferenceType and emits #nullable disable instead.
	/// </summary>
	[Fact]
	public void UnconstrainedNullable_ReturnType_CompilesAndWorks()
	{
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		knockOff.NullableReturn.Of<string>().Return(() => "hello");

		var result = service.NullableReturn<string>();

		Assert.Equal("hello", result);
	}

	[Fact]
	public void UnconstrainedNullable_ReturnType_CanReturnNull()
	{
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		knockOff.NullableReturn.Of<string>().Return(() => null!);

		var result = service.NullableReturn<string>();

		Assert.Null(result);
	}

	[Fact]
	public void UnconstrainedNullable_WithParameter_CompilesAndWorks()
	{
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		knockOff.NullableValues.Of<int>().Return((data) => data);

		var result = service.NullableValues(42);

		Assert.Equal(42, result);
	}

	[Fact]
	public void UnconstrainedNullable_WithValueType_Works()
	{
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		knockOff.NullableReturn.Of<int>().Return(() => 99);

		var result = service.NullableReturn<int>();

		Assert.Equal(99, result);
	}

	[Fact]
	public void InterfaceConstrainedNullable_NoSpuriousClassConstraint()
	{
		// Interface-only constraints (e.g., where T : IDisposable) should NOT
		// get "where T : class" because structs can implement interfaces.
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		var stream = new MemoryStream();
		knockOff.InterfaceConstrainedReturn.Of<MemoryStream>().Return(() => stream);

		var result = service.InterfaceConstrainedReturn<MemoryStream>();

		Assert.Same(stream, result);
	}

	[Fact]
	public void ClassConstrainedNullable_RegressionStillWorks()
	{
		// Methods with class-implying constraints (e.g., where T : Attribute)
		// should STILL get "where T : class" in the explicit implementation.
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		var attr = new TestAttribute { Value = "test" };
		knockOff.ConstrainedNullableReturn.Of<TestAttribute>().Return(() => attr);

		var result = service.ConstrainedNullableReturn<TestAttribute>();

		Assert.Same(attr, result);
	}

	[Fact]
	public void ClassConstrainedNullable_CanReturnNull()
	{
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		knockOff.ConstrainedNullableReturn.Of<TestAttribute>().Return(() => null);

		var result = service.ConstrainedNullableReturn<TestAttribute>();

		Assert.Null(result);
	}

	[Fact]
	public void UnconstrainedNullable_Verification_Works()
	{
		var knockOff = new NullableGenericServiceKnockOff();
		INullableGenericServiceForTests service = knockOff;

		service.NullableReturn<string>();
		service.NullableReturn<int>();

		knockOff.NullableReturn.Of<string>().Verify(Called.Once);
		knockOff.NullableReturn.Of<int>().Verify(Called.Once);
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
/// KnockOff for testing generic methods.
/// NOTE: Generic methods do NOT support user overrides by design.
/// Use OnCall to configure behavior instead.
/// </summary>
[KnockOff]
public partial class GenericMethodWithUserMethodKnockOff : IGenericMethodWithUserMethod
{
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

/// <summary>
/// Interface with unconstrained nullable generic type parameters.
/// Bug 4: Generator incorrectly added "where T : class" to explicit implementations
/// for methods with T? when T was unconstrained or had only interface constraints.
/// </summary>
public interface INullableGenericServiceForTests
{
	/// <summary>Nullable unconstrained return and parameter.</summary>
	TData? NullableValues<TData>(TData? data);

	/// <summary>Nullable unconstrained return only.</summary>
	T? NullableReturn<T>();

	/// <summary>Interface constraint only - should NOT emit where T : class.</summary>
	T? InterfaceConstrainedReturn<T>() where T : IDisposable;

	/// <summary>Class-implying constraint (Attribute) - SHOULD emit where T : class.</summary>
	T? ConstrainedNullableReturn<T>() where T : Attribute;
}

[KnockOff]
public partial class NullableGenericServiceKnockOff : INullableGenericServiceForTests
{
}

#endregion
