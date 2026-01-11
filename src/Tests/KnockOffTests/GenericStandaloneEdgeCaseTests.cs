namespace KnockOff.Tests;

/// <summary>
/// Edge case tests for generic standalone stubs.
/// Tests nested type parameters, variance, and constraint handling.
/// </summary>
public class GenericStandaloneEdgeCaseTests
{
	#region Nested Type Parameters

	[Fact]
	public void NestedTypeParams_ListOfT_Works()
	{
		// Arrange - interface returns List<T> where T is the class type param
		var stub = new NestedTypeParamStub<string>();
		INestedTypeParamService<string> service = stub;
		var expected = new List<string> { "a", "b", "c" };
		stub.GetItems.OnCall = (ko) => expected;

		// Act
		var result = service.GetItems();

		// Assert
		Assert.Same(expected, result);
	}

	[Fact]
	public void NestedTypeParams_DictionaryOfTKey_Works()
	{
		// Arrange
		var stub = new NestedDictStub<int, string>();
		INestedDictService<int, string> service = stub;
		var expected = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
		stub.GetMapping.OnCall = (ko) => expected;

		// Act
		var result = service.GetMapping();

		// Assert
		Assert.Same(expected, result);
	}

	[Fact]
	public async Task NestedTypeParams_TaskOfT_Works()
	{
		// Arrange
		var stub = new AsyncNestedStub<User>();
		IAsyncNestedService<User> service = stub;
		var expected = new User { Id = 1, Name = "Async" };
		stub.GetAsync.OnCall = (ko) => Task.FromResult<User?>(expected);

		// Act
		var result = await service.GetAsync();

		// Assert
		Assert.Same(expected, result);
	}

	#endregion

	#region Variance (in/out modifiers)

	[Fact]
	public void Covariance_OutModifier_Works()
	{
		// Arrange - ICovariantService<out T> allows T to be returned
		var stub = new CovariantStub<string>();
		ICovariantService<string> service = stub;
		stub.Get.OnCall = (ko) => "covariant result";

		// Act
		var result = service.Get();

		// Assert
		Assert.Equal("covariant result", result);
	}

	[Fact]
	public void Covariance_CanAssignToBaseType()
	{
		// Arrange - covariance allows ICovariantService<string> to be assigned to ICovariantService<object>
		var stub = new CovariantStub<string>();
		stub.Get.OnCall = (ko) => "test";

		// Act - this should compile due to covariance
		ICovariantService<object> baseService = stub;
		var result = baseService.Get();

		// Assert
		Assert.Equal("test", result);
	}

	[Fact]
	public void Contravariance_InModifier_Works()
	{
		// Arrange - IContravariantService<in T> allows T to be consumed
		var stub = new ContravariantStub<object>();
		IContravariantService<object> service = stub;
		object? captured = null;
		stub.Process.OnCall = (ko, item) => captured = item;

		// Act
		service.Process("test string");

		// Assert
		Assert.Equal("test string", captured);
	}

	[Fact]
	public void Contravariance_CanAssignToDerivedType()
	{
		// Arrange - contravariance allows IContravariantService<object> to be assigned to IContravariantService<string>
		var stub = new ContravariantStub<object>();
		object? captured = null;
		stub.Process.OnCall = (ko, item) => captured = item;

		// Act - this should compile due to contravariance
		IContravariantService<string> derivedService = stub;
		derivedService.Process("contravariant test");

		// Assert
		Assert.Equal("contravariant test", captured);
	}

	#endregion

	#region Multiple Constraints

	[Fact]
	public void MultipleConstraints_ClassAndInterface_Works()
	{
		// Arrange
		var stub = new MultiConstraintStub<TestEntityWithInterface>();
		IMultiConstraintService<TestEntityWithInterface> service = stub;
		var entity = new TestEntityWithInterface { Id = 42, Name = "Multi" };
		stub.Save.OnCall = (ko, e) => { };

		// Act
		service.Save(entity);

		// Assert
		Assert.Equal(1, stub.Save.CallCount);
		Assert.Same(entity, stub.Save.LastCallArg);
	}

	[Fact]
	public void MultipleConstraints_StructConstraint_Works()
	{
		// Arrange
		var stub = new StructConstraintStub<int>();
		IStructConstraintService<int> service = stub;
		stub.GetDefault.OnCall = (ko) => 42;

		// Act
		var result = service.GetDefault();

		// Assert
		Assert.Equal(42, result);
	}

	[Fact]
	public void MultipleConstraints_NewConstraint_Works()
	{
		// Arrange
		var stub = new NewConstraintStub<TestEntity>();
		INewConstraintService<TestEntity> service = stub;
		var expected = new TestEntity { Id = 99 };
		stub.Create.OnCall = (ko) => expected;

		// Act
		var result = service.Create();

		// Assert
		Assert.Same(expected, result);
	}

	[Fact]
	public void MultipleConstraints_NewConstraint_PreservesConstraint()
	{
		// This test verifies the constraint is preserved on the generated class
		// The stub should compile and work with types that have parameterless constructors
		var stub = new NewConstraintStub<TestEntity>();
		INewConstraintService<TestEntity> service = stub;

		// Act
		stub.Create.OnCall = (ko) => new TestEntity(); // We can use new() here because of constraint
		var result = service.Create();

		// Assert
		Assert.NotNull(result);
	}

	#endregion

	#region Nullable Reference Types

	[Fact]
	public void NullableRefTypes_NullableReturn_AllowsNull()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;
		stub.GetById.OnCall = (ko, id) => null;

		// Act
		var result = repo.GetById(999);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void NullableRefTypes_NonNullableParam_AcceptsNonNull()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;
		var user = new User { Id = 1 };

		// Act
		repo.Save(user);

		// Assert
		Assert.Same(user, stub.Save.LastCallArg);
	}

	#endregion
}

#region Test Interfaces and Stubs for Edge Cases

// --- Nested Type Parameters ---

public interface INestedTypeParamService<T>
{
	List<T> GetItems();
	void AddItems(IEnumerable<T> items);
}

[KnockOff]
public partial class NestedTypeParamStub<T> : INestedTypeParamService<T>
{
}

public interface INestedDictService<TKey, TValue> where TKey : notnull
{
	Dictionary<TKey, TValue> GetMapping();
}

[KnockOff]
public partial class NestedDictStub<TKey, TValue> : INestedDictService<TKey, TValue> where TKey : notnull
{
}

public interface IAsyncNestedService<T> where T : class
{
	Task<T?> GetAsync();
}

[KnockOff]
public partial class AsyncNestedStub<T> : IAsyncNestedService<T> where T : class
{
}

// --- Variance ---

public interface ICovariantService<out T>
{
	T Get();
}

[KnockOff]
public partial class CovariantStub<T> : ICovariantService<T>
{
}

public interface IContravariantService<in T>
{
	void Process(T item);
}

[KnockOff]
public partial class ContravariantStub<T> : IContravariantService<T>
{
}

// --- Multiple Constraints ---

public interface IMultiConstraintService<T> where T : class, IEntity
{
	void Save(T entity);
	T? Find(int id);
}

[KnockOff]
public partial class MultiConstraintStub<T> : IMultiConstraintService<T> where T : class, IEntity
{
}

public interface IStructConstraintService<T> where T : struct
{
	T GetDefault();
	void Set(T value);
}

[KnockOff]
public partial class StructConstraintStub<T> : IStructConstraintService<T> where T : struct
{
}

public interface INewConstraintService<T> where T : new()
{
	T Create();
}

[KnockOff]
public partial class NewConstraintStub<T> : INewConstraintService<T> where T : new()
{
}

#endregion
