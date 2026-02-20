namespace KnockOff.Tests;

// =============================================================================
// Generic Type Validation Tests
// =============================================================================
// Systematic validation that all generic type features (A-I) and where clause
// combinations work correctly across all applicable patterns.
//
// BUG DISCOVERED: The `unmanaged` constraint causes generator to emit
// `where TData : struct, unmanaged` which is CS0449 (cannot combine struct and
// unmanaged). The unmanaged constraint implies struct, so it should emit
// `where TData : unmanaged` only. This bug blocks unmanaged constraint testing
// across ALL patterns. Tests for unmanaged constraint are excluded from this file.
//
// See: docs/plans/generic-type-validation.md
// See: docs/todos/generic-type-gaps.md
// =============================================================================

#region Test Interfaces and Types

/// <summary>
/// Multi-type-param interface with comprehensive constraints for pattern validation.
/// Exercises Features C, D, E, H and where clause combinations 5, 6.
///
/// NOTE: unmanaged constraint (combination 8) excluded due to generator bug
/// (CS0449: struct + unmanaged cannot be combined). See file header.
/// </summary>
public interface IGenericValidation<TKey, TValue>
	where TKey : notnull
	where TValue : class, new()
{
	// Feature C: Methods using class type params
	TValue Get(TKey key);
	void Set(TKey key, TValue value);

	// Feature D: Method with single own type param + constraint
	TResult Convert<TResult>(TValue input) where TResult : new();

	// Feature E: Method with multiple own type params
	TOut Transform<TIn, TOut>(TIn input, TKey context);

	// Feature H: Multiple where clauses on methods
	TOut MapConstrained<TIn, TOut>(TIn input) where TIn : struct where TOut : class, new();

	// Combination 6: Cross-referencing constraint
	void Register<THandler>(THandler handler) where THandler : IComparable<TKey>;
}

/// <summary>
/// Multi-type-param abstract class with method-level generics for class stub validation.
/// Exercises Features C, D, E, H for class patterns (P3, P4, P6, P9).
/// </summary>
public abstract class ValidationBase<TKey, TValue>
	where TKey : notnull
	where TValue : class, new()
{
	// Feature C: Methods using class type params
	public abstract TValue Get(TKey key);
	public abstract void Set(TKey key, TValue value);

	// Feature D: Method with single own type param + constraint
	public virtual TResult Convert<TResult>(TValue input) where TResult : new() => new TResult();

	// Feature E: Method with multiple own type params
	public abstract TOut Transform<TIn, TOut>(TIn input, TKey context);

	// Feature H: Multiple where clauses on methods
	public abstract TOut MapConstrained<TIn, TOut>(TIn input) where TIn : struct where TOut : class, new();
}

/// <summary>
/// Mixed-arity generic method interface for non-generic patterns.
/// Tests Feature E-2 across P1, P3, P5, P6.
/// </summary>
public interface IMixedArityGenericService
{
	TResult Execute<TResult>() where TResult : new();
	TResult Execute<TInput, TResult>(TInput input) where TResult : new();
	void Log<T>(T item);
	void Log<T1, T2>(T1 item1, T2 item2);
}

/// <summary>
/// Abstract class with mixed-arity generic methods for P3/P6 class stub validation.
/// Has Convert&lt;T&gt; (1 type param) and Transform&lt;TInput,TResult&gt; (2 type params).
/// Also Register&lt;T&gt; (void, 0 non-generic params) and Process&lt;T&gt; (void, 2 params).
/// </summary>
public abstract class MixedArityClassBase
{
	/// <summary>Single type param generic method.</summary>
	public virtual T Convert<T>(object value) => default!;

	/// <summary>Abstract void generic method with no params.</summary>
	public abstract void Register<T>();

	/// <summary>Multi type param generic method with constraints.</summary>
	public virtual TResult Transform<TInput, TResult>(TInput input)
		where TInput : class
		where TResult : new()
		=> new TResult();

	/// <summary>Non-generic method alongside generic methods.</summary>
	public virtual string GetName() => "default";

	/// <summary>Generic void method with params.</summary>
	public virtual void Process<T>(T item, string label) { }

	/// <summary>Non-generic overload of Process.</summary>
	public virtual void Process(string label) { }
}

/// <summary>
/// Simple test entity for use in generic type validation tests.
/// Satisfies class + new() constraints.
/// </summary>
public class ValidationEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

#endregion

#region Stub Declarations -- Pattern 1: Standalone (closed generic)

// P1 standalone uses closed generics, so IGenericValidation<int, ValidationEntity>
[KnockOff]
public partial class GenericValidationStandaloneKnockOff : IGenericValidation<int, ValidationEntity>
{
}

// P1 standalone for mixed-arity (non-generic interface)
[KnockOff]
public partial class MixedArityStandaloneKnockOff : IMixedArityGenericService
{
}

#endregion

#region Stub Declarations -- Pattern 2: Generic Standalone

// P2 generic standalone for IGenericValidation
[KnockOff]
public partial class GenericValidationP2Stub<TKey, TValue> : IGenericValidation<TKey, TValue>
	where TKey : notnull
	where TValue : class, new()
{
}

#endregion

#region Stub Declarations -- Pattern 3: Standalone Class

// P3 standalone class for MixedArityClassBase
// MixedArityClassBase has Convert<T> (1 type param) and Transform<TInput,TResult> (2 type params)
[KnockOffBase<MixedArityClassBase>]
public partial class MixedArityClassStub
{
}

#endregion

#region Stub Declarations -- Pattern 4: Generic Standalone Class

// P4 for ValidationBase<TKey, TValue>
[KnockOffBase(typeof(ValidationBase<,>))]
public partial class ValidationBaseP4Stub<TKey, TValue>
	where TKey : notnull
	where TValue : class, new()
{
}

#endregion

#region Stub Declarations -- Pattern 5: Inline Interface (closed generic)

[KnockOff<IGenericValidation<int, ValidationEntity>>]
[KnockOff<IMixedArityGenericService>]
public partial class GenericValidationInlineTest
{
}

#endregion

#region Stub Declarations -- Pattern 6: Inline Class

// P6 inline class for MixedArityClassBase
[KnockOff<MixedArityClassBase>]
public partial class MixedArityInlineClassTest
{
}

#endregion

#region Stub Declarations -- Pattern 8: Open Generic Interface

// P8 for IGenericValidation
[KnockOff(typeof(IGenericValidation<,>))]
public partial class GenericValidationP8Test
{
}

#endregion

#region Stub Declarations -- Pattern 9: Open Generic Class

// P9 for ValidationBase
[KnockOff(typeof(ValidationBase<,>))]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class ValidationBaseP9Test
#pragma warning restore CA1052
{
}

// P9 for MixedArityClassBase -- non-generic class, no open generic needed
// MixedArityClassBase is not generic, so P9 doesn't apply to it directly.
// P9 open generic class stubs require generic base classes.

#endregion

#region P1 Standalone Tests

public class GenericValidationP1Tests
{
	[Fact]
	public void P1_CrossReferencingConstraint_Works()
	{
		var knockOff = new GenericValidationStandaloneKnockOff();
		IGenericValidation<int, ValidationEntity> service = knockOff;

		knockOff.Register.Of<int>().Call((handler) => { });

		service.Register(42);

		knockOff.Register.Of<int>().Verify(Called.Once);
	}

	[Fact]
	public void P1_MultipleWhereClausesOnMethod_Works()
	{
		var knockOff = new GenericValidationStandaloneKnockOff();
		IGenericValidation<int, ValidationEntity> service = knockOff;

		knockOff.MapConstrained.Of<int, ValidationEntity>().Call((input) =>
			new ValidationEntity { Id = input });

		var result = service.MapConstrained<int, ValidationEntity>(42);

		Assert.Equal(42, result.Id);
	}

	[Fact]
	public void P1_MethodLevelGeneric_Convert_ReturnAndVerify()
	{
		var knockOff = new GenericValidationStandaloneKnockOff();
		IGenericValidation<int, ValidationEntity> service = knockOff;

		knockOff.Convert.Of<List<string>>().Call((v) => new List<string> { v.Name });

		var result = service.Convert<List<string>>(new ValidationEntity { Name = "test" });

		Assert.Single(result);
		Assert.Equal("test", result[0]);
		knockOff.Convert.Of<List<string>>().Verify(Called.Once);
	}

	[Fact]
	public void P1_MultiTypeParamMethod_Transform_ReturnAndVerify()
	{
		var knockOff = new GenericValidationStandaloneKnockOff();
		IGenericValidation<int, ValidationEntity> service = knockOff;

		knockOff.Transform.Of<string, int>().Call((input, context) => input.Length + context);

		var result = service.Transform<string, int>("hello", 100);

		Assert.Equal(105, result);
	}

	[Fact]
	public void P1_MixedArity_Execute_BothAritiesWork()
	{
		var knockOff = new MixedArityStandaloneKnockOff();
		IMixedArityGenericService service = knockOff;

		knockOff.Execute.Of<ValidationEntity>().Call(() => new ValidationEntity { Id = 1 });
		knockOff.Execute.Of<string, ValidationEntity>().Call((input) => new ValidationEntity { Name = input });

		var result1 = service.Execute<ValidationEntity>();
		var result2 = service.Execute<string, ValidationEntity>("test");

		Assert.Equal(1, result1.Id);
		Assert.Equal("test", result2.Name);
	}

	[Fact]
	public void P1_MixedArity_Log_BothAritiesWork()
	{
		var knockOff = new MixedArityStandaloneKnockOff();
		IMixedArityGenericService service = knockOff;

		knockOff.Log.Of<string>().Call((item) => { });
		knockOff.Log.Of<int, string>().Call((item1, item2) => { });

		service.Log("hello");
		service.Log(42, "world");

		knockOff.Log.Of<string>().Verify(Called.Once);
		knockOff.Log.Of<int, string>().Verify(Called.Once);
	}
}

#endregion

#region P2 Generic Standalone Tests

public class GenericValidationP2Tests
{
	[Fact]
	public void P2_MethodLevelGeneric_Convert_ReturnAndVerify()
	{
		var stub = new GenericValidationP2Stub<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		var input = new ValidationEntity { Id = 1 };
		stub.Convert.Of<List<string>>().Call((v) => new List<string> { v.Name });

		var result = service.Convert<List<string>>(input);

		Assert.Single(result);
		stub.Convert.Of<List<string>>().Verify(Called.Once);
	}

	[Fact]
	public void P2_MultiTypeParamMethod_Transform_ReturnAndVerify()
	{
		var stub = new GenericValidationP2Stub<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Transform.Of<string, int>().Call((input, context) => input.Length + context);

		var result = service.Transform<string, int>("hello", 100);

		Assert.Equal(105, result);
		stub.Transform.Of<string, int>().Verify(Called.Once);
	}

	[Fact]
	public void P2_MultipleWhereClauses_MapConstrained_Works()
	{
		var stub = new GenericValidationP2Stub<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.MapConstrained.Of<int, ValidationEntity>().Call((input) =>
			new ValidationEntity { Id = input * 2 });

		var result = service.MapConstrained<int, ValidationEntity>(21);

		Assert.Equal(42, result.Id);
	}

	[Fact]
	public void P2_CrossReferencingConstraint_Register_Works()
	{
		var stub = new GenericValidationP2Stub<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		int? captured = null;
		stub.Register.Of<int>().Call((handler) => captured = handler);

		service.Register(99);

		Assert.Equal(99, captured);
		stub.Register.Of<int>().Verify(Called.Once);
	}

	[Fact]
	public void P2_MixedArity_ConvertAndTransformAreDifferentArities()
	{
		// IGenericValidation has Convert<TResult> (1 type param) and Transform<TIn,TOut> (2 type params)
		// This validates mixed-arity on a generic standalone stub
		var stub = new GenericValidationP2Stub<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Convert.Of<int>().Call((v) => v.Id);
		stub.Transform.Of<string, int>().Call((input, context) => input.Length);

		var convertResult = service.Convert<int>(new ValidationEntity { Id = 42 });
		var transformResult = service.Transform<string, int>("hello", 0);

		Assert.Equal(42, convertResult);
		Assert.Equal(5, transformResult);
	}

	[Fact]
	public void P2_ClassTypeParamMethods_GetAndSet_Work()
	{
		var stub = new GenericValidationP2Stub<string, ValidationEntity>();
		IGenericValidation<string, ValidationEntity> service = stub;

		var entity = new ValidationEntity { Id = 1, Name = "Test" };
		stub.Get.Call((key) => entity);
		var setTracking = stub.Set.Call((string key, ValidationEntity value) => { });

		var result = service.Get("key1");
		service.Set("key2", entity);

		Assert.Same(entity, result);
		setTracking.Verify(Called.Once);
	}
}

#endregion

#region P3 Standalone Class Tests

public class GenericValidationP3Tests
{
	[Fact]
	public void P3_MultiTypeParamMethod_Transform_ReturnAndVerify()
	{
		var stub = new MixedArityClassStub();
		MixedArityClassBase service = stub.Object;

		stub.Transform.Of<string, ValidationEntity>().Call((input) =>
			new ValidationEntity { Name = input });

		var result = service.Transform<string, ValidationEntity>("test-input");

		Assert.Equal("test-input", result.Name);
		stub.Transform.Of<string, ValidationEntity>().Verify(Called.Once);
	}

	[Fact]
	public void P3_MixedArity_ConvertAndTransform_BothWork()
	{
		var stub = new MixedArityClassStub();
		MixedArityClassBase service = stub.Object;

		stub.Convert.Of<int>().Call((value) => 42);
		stub.Transform.Of<string, ValidationEntity>().Call((input) =>
			new ValidationEntity { Name = input });

		var convertResult = service.Convert<int>("anything");
		var transformResult = service.Transform<string, ValidationEntity>("hello");

		Assert.Equal(42, convertResult);
		Assert.Equal("hello", transformResult.Name);
	}

	[Fact]
	public void P3_MixedArity_RegisterAndProcessGeneric_VoidMethods()
	{
		var stub = new MixedArityClassStub();
		MixedArityClassBase service = stub.Object;

		stub.Register.Of<int>().Call(() => { });
		// Process has both non-generic Process(string) and generic Process<T>(T, string).
		// The generator splits these: Process (non-generic) and ProcessGeneric (generic).
		stub.ProcessGeneric.Of<string>().Call((item, label) => { });

		service.Register<int>();
		service.Process("hello", "label1");

		stub.Register.Of<int>().Verify(Called.Once);
		stub.ProcessGeneric.Of<string>().Verify(Called.Once);
	}
}

#endregion

#region P4 Generic Standalone Class Tests

public class GenericValidationP4Tests
{
	[Fact]
	public void P4_MultipleWhereClausesOnMethod_ValidationBase()
	{
		var stub = new ValidationBaseP4Stub<int, ValidationEntity>();
		ValidationBase<int, ValidationEntity> service = stub.Object;

		stub.MapConstrained.Of<int, ValidationEntity>().Call((input) =>
			new ValidationEntity { Id = input * 3 });

		var result = service.MapConstrained<int, ValidationEntity>(14);

		Assert.Equal(42, result.Id);
		stub.MapConstrained.Of<int, ValidationEntity>().Verify(Called.Once);
	}

	[Fact]
	public void P4_MethodLevelGenerics_Convert_OnValidationBase()
	{
		var stub = new ValidationBaseP4Stub<int, ValidationEntity>();
		ValidationBase<int, ValidationEntity> service = stub.Object;

		stub.Convert.Of<List<int>>().Call((value) => new List<int> { value.Id });

		var result = service.Convert<List<int>>(new ValidationEntity { Id = 7 });

		Assert.Single(result);
		Assert.Equal(7, result[0]);
	}

	[Fact]
	public void P4_MultiTypeParamMethod_Transform_OnValidationBase()
	{
		var stub = new ValidationBaseP4Stub<int, ValidationEntity>();
		ValidationBase<int, ValidationEntity> service = stub.Object;

		stub.Transform.Of<string, int>().Call((input, context) => input.Length + context);

		var result = service.Transform<string, int>("hello", 10);

		Assert.Equal(15, result);
	}
}

#endregion

#region P5 Inline Interface Tests

public class GenericValidationP5Tests
{
	[Fact]
	public void P5_CrossReferencingConstraint_Works()
	{
		var stub = new GenericValidationInlineTest.Stubs.IGenericValidation();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Register.Of<int>().Call((handler) => { });

		service.Register(42);

		stub.Register.Of<int>().Verify(Called.Once);
	}

	[Fact]
	public void P5_MultipleWhereClauses_MapConstrained_Works()
	{
		var stub = new GenericValidationInlineTest.Stubs.IGenericValidation();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.MapConstrained.Of<int, ValidationEntity>().Call((input) =>
			new ValidationEntity { Id = input });

		var result = service.MapConstrained<int, ValidationEntity>(42);

		Assert.Equal(42, result.Id);
	}

	[Fact]
	public void P5_MethodLevelGeneric_Convert_ReturnAndVerify()
	{
		var stub = new GenericValidationInlineTest.Stubs.IGenericValidation();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Convert.Of<List<string>>().Call((v) => new List<string> { v.Name });

		var result = service.Convert<List<string>>(new ValidationEntity { Name = "inline" });

		Assert.Single(result);
		Assert.Equal("inline", result[0]);
	}

	[Fact]
	public void P5_MultiTypeParamMethod_Transform_ReturnAndVerify()
	{
		var stub = new GenericValidationInlineTest.Stubs.IGenericValidation();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Transform.Of<string, int>().Call((input, context) => input.Length + context);

		var result = service.Transform<string, int>("hello", 10);

		Assert.Equal(15, result);
	}

	[Fact]
	public void P5_MixedArity_Execute_BothAritiesWork()
	{
		var stub = new GenericValidationInlineTest.Stubs.IMixedArityGenericService();
		IMixedArityGenericService service = stub;

		stub.Execute.Of<ValidationEntity>().Call(() => new ValidationEntity { Id = 1 });
		stub.Execute.Of<string, ValidationEntity>().Call((input) => new ValidationEntity { Name = input });

		var result1 = service.Execute<ValidationEntity>();
		var result2 = service.Execute<string, ValidationEntity>("inline-test");

		Assert.Equal(1, result1.Id);
		Assert.Equal("inline-test", result2.Name);
	}

	[Fact]
	public void P5_MixedArity_Log_BothAritiesWork()
	{
		var stub = new GenericValidationInlineTest.Stubs.IMixedArityGenericService();
		IMixedArityGenericService service = stub;

		stub.Log.Of<string>().Call((item) => { });
		stub.Log.Of<int, string>().Call((item1, item2) => { });

		service.Log("test");
		service.Log(1, "two");

		stub.Log.Of<string>().Verify(Called.Once);
		stub.Log.Of<int, string>().Verify(Called.Once);
	}
}

#endregion

#region P6 Inline Class Tests

public class GenericValidationP6Tests
{
	[Fact]
	public void P6_MultiTypeParamMethod_Transform_Works()
	{
		var stub = new MixedArityInlineClassTest.Stubs.MixedArityClassBase();
		MixedArityClassBase service = stub.Object;

		stub.Transform.Of<string, List<int>>().Call((input) => new List<int> { input.Length });

		var result = service.Transform<string, List<int>>("hello");

		Assert.Single(result);
		Assert.Equal(5, result[0]);
	}

	[Fact]
	public void P6_MixedArity_ConvertAndTransform_BothWork()
	{
		var stub = new MixedArityInlineClassTest.Stubs.MixedArityClassBase();
		MixedArityClassBase service = stub.Object;

		stub.Convert.Of<int>().Call((value) => 42);
		stub.Transform.Of<string, ValidationEntity>().Call((input) =>
			new ValidationEntity { Name = input });

		var convertResult = service.Convert<int>("anything");
		var transformResult = service.Transform<string, ValidationEntity>("hello");

		Assert.Equal(42, convertResult);
		Assert.Equal("hello", transformResult.Name);
	}

	[Fact]
	public void P6_MixedArity_RegisterAndProcessGeneric_VoidMethods()
	{
		var stub = new MixedArityInlineClassTest.Stubs.MixedArityClassBase();
		MixedArityClassBase service = stub.Object;

		stub.Register.Of<int>().Call(() => { });
		// Process has both non-generic Process(string) and generic Process<T>(T, string).
		// The generator splits these: Process (non-generic) and ProcessGeneric (generic).
		stub.ProcessGeneric.Of<string>().Call((item, label) => { });

		service.Register<int>();
		service.Process("hello", "label1");

		stub.Register.Of<int>().Verify(Called.Once);
		stub.ProcessGeneric.Of<string>().Verify(Called.Once);
	}
}

#endregion

#region P8 Open Generic Interface Tests

public class GenericValidationP8Tests
{
	[Fact]
	public void P8_MethodLevelGeneric_Convert_ReturnAndVerify()
	{
		var stub = new GenericValidationP8Test.Stubs.IGenericValidation<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Convert.Of<List<string>>().Call((v) => new List<string> { v.Name });

		var result = service.Convert<List<string>>(new ValidationEntity { Name = "test" });

		Assert.Single(result);
		Assert.Equal("test", result[0]);
		stub.Convert.Of<List<string>>().Verify(Called.Once);
	}

	[Fact]
	public void P8_MultiTypeParamMethod_Transform_ReturnAndVerify()
	{
		var stub = new GenericValidationP8Test.Stubs.IGenericValidation<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Transform.Of<string, int>().Call((input, context) => input.Length + context);

		var result = service.Transform<string, int>("hello", 10);

		Assert.Equal(15, result);
		stub.Transform.Of<string, int>().Verify(Called.Once);
	}

	[Fact]
	public void P8_CrossReferencingConstraint_Works()
	{
		var stub = new GenericValidationP8Test.Stubs.IGenericValidation<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		int? captured = null;
		stub.Register.Of<int>().Call((handler) => captured = handler);

		service.Register(42);

		Assert.Equal(42, captured);
		stub.Register.Of<int>().Verify(Called.Once);
	}

	[Fact]
	public void P8_MixedArity_ConvertAndTransform()
	{
		var stub = new GenericValidationP8Test.Stubs.IGenericValidation<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.Convert.Of<int>().Call((v) => v.Id);
		stub.Transform.Of<string, int>().Call((input, context) => input.Length);

		var convertResult = service.Convert<int>(new ValidationEntity { Id = 99 });
		var transformResult = service.Transform<string, int>("abc", 0);

		Assert.Equal(99, convertResult);
		Assert.Equal(3, transformResult);
	}

	[Fact]
	public void P8_MultipleWhereClauses_MapConstrained_Works()
	{
		var stub = new GenericValidationP8Test.Stubs.IGenericValidation<int, ValidationEntity>();
		IGenericValidation<int, ValidationEntity> service = stub;

		stub.MapConstrained.Of<int, ValidationEntity>().Call((input) =>
			new ValidationEntity { Id = input });

		var result = service.MapConstrained<int, ValidationEntity>(42);

		Assert.Equal(42, result.Id);
	}
}

#endregion

#region P9 Open Generic Class Tests

public class GenericValidationP9Tests
{
	[Fact]
	public void P9_MethodLevelGenerics_Transform_OnValidationBase()
	{
		var stub = new ValidationBaseP9Test.Stubs.ValidationBase<int, ValidationEntity>();
		ValidationBase<int, ValidationEntity> service = stub.Object;

		stub.Transform.Of<string, int>().Call((input, context) => input.Length + context);

		var result = service.Transform<string, int>("hello", 10);

		Assert.Equal(15, result);
		stub.Transform.Of<string, int>().Verify(Called.Once);
	}

	[Fact]
	public void P9_MultiTypeParamMethod_Convert_ReturnAndVerify()
	{
		var stub = new ValidationBaseP9Test.Stubs.ValidationBase<int, ValidationEntity>();
		ValidationBase<int, ValidationEntity> service = stub.Object;

		stub.Convert.Of<List<int>>().Call((value) => new List<int> { value.Id });

		var result = service.Convert<List<int>>(new ValidationEntity { Id = 7 });

		Assert.Single(result);
		Assert.Equal(7, result[0]);
	}

	[Fact]
	public void P9_MultipleWhereClauses_MapConstrained_Works()
	{
		var stub = new ValidationBaseP9Test.Stubs.ValidationBase<int, ValidationEntity>();
		ValidationBase<int, ValidationEntity> service = stub.Object;

		stub.MapConstrained.Of<int, ValidationEntity>().Call((input) =>
			new ValidationEntity { Id = input * 2 });

		var result = service.MapConstrained<int, ValidationEntity>(21);

		Assert.Equal(42, result.Id);
	}

	[Fact]
	public void P9_ClassTypeParamMethods_GetAndSet_Work()
	{
		var stub = new ValidationBaseP9Test.Stubs.ValidationBase<string, ValidationEntity>();
		ValidationBase<string, ValidationEntity> service = stub.Object;

		var entity = new ValidationEntity { Id = 1, Name = "Test" };
		stub.Get.Call((key) => entity);
		var setTracking = stub.Set.Call((string key, ValidationEntity value) => { });

		var result = service.Get("key1");
		service.Set("key2", entity);

		Assert.Same(entity, result);
		setTracking.Verify(Called.Once);
	}
}

#endregion
