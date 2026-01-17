namespace KnockOff.Tests;

/// <summary>
/// Tests for generic standalone stubs.
/// Generic standalone stubs are classes like:
///   [KnockOff]
///   public partial class RepositoryStub&lt;T&gt; : IRepository&lt;T&gt; where T : class { }
/// </summary>
public class GenericStandaloneStubTests
{
	#region Basic Generic Standalone Stub

	[Fact]
	public void GenericStandaloneStub_CanInstantiate_WithDifferentTypeArgs()
	{
		// Arrange & Act
		var userRepo = new GenericRepositoryStub<User>();
		var entityRepo = new GenericRepositoryStub<TestEntity>();

		// Assert - just verify they can be created
		Assert.NotNull(userRepo);
		Assert.NotNull(entityRepo);
	}

	[Fact]
	public void GenericStandaloneStub_ImplementsInterface()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();

		// Assert
		Assert.IsAssignableFrom<IGenericRepository<User>>(stub);
	}

	#endregion

	#region Method Tracking

	[Fact]
	public void GenericStandaloneStub_Method_TracksCallCount()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		var tracking = stub.Save.OnCall((ko, entity) => { });
		IGenericRepository<User> repo = stub;

		// Act
		repo.Save(new User { Id = 1 });
		repo.Save(new User { Id = 2 });

		// Assert
		Assert.Equal(2, tracking.CallCount);
		Assert.True(tracking.WasCalled);
	}

	[Fact]
	public void GenericStandaloneStub_Method_TracksLastCallArg()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		var tracking = stub.Save.OnCall((ko, entity) => { });
		IGenericRepository<User> repo = stub;
		var user = new User { Id = 42, Name = "Test" };

		// Act
		repo.Save(user);

		// Assert
		Assert.Same(user, tracking.LastArg);
	}

	#endregion

	#region OnCall Callbacks

	[Fact]
	public void GenericStandaloneStub_OnCall_ReturnsConfiguredValue()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;
		var expected = new User { Id = 123, Name = "Found" };
		stub.GetById.OnCall((ko, id) => expected);

		// Act
		var result = repo.GetById(123);

		// Assert
		Assert.Same(expected, result);
	}

	[Fact]
	public void GenericStandaloneStub_OnCall_ReceivesKnockOffInstance()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;
		GenericRepositoryStub<User>? capturedKo = null;
		stub.GetById.OnCall((ko, id) =>
		{
			capturedKo = ko;
			return null;
		});

		// Act
		repo.GetById(1);

		// Assert
		Assert.Same(stub, capturedKo);
	}

	#endregion

	#region Property Tracking

	[Fact]
	public void GenericStandaloneStub_Property_TracksGetAndSet()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;
		stub.Count.Value = 5;

		// Act
		var value = repo.Count;
		repo.Count = 10;

		// Assert
		Assert.Equal(5, value);
		Assert.Equal(1, stub.Count.GetCount);
		Assert.Equal(1, stub.Count.SetCount);
		Assert.Equal(10, stub.Count.LastSetValue);
	}

	#endregion

	#region Collection Return Types

	[Fact]
	public void GenericStandaloneStub_GetAll_ReturnsConfiguredList()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;
		var users = new List<User>
		{
			new() { Id = 1, Name = "User1" },
			new() { Id = 2, Name = "User2" }
		};
		stub.GetAll.OnCall((ko) => users);

		// Act
		var result = repo.GetAll();

		// Assert
		Assert.Same(users, result);
	}

	[Fact]
	public void GenericStandaloneStub_GetAll_DefaultReturnsEmptyList()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		IGenericRepository<User> repo = stub;

		// Act
		var result = repo.GetAll();

		// Assert - should return empty list (smart default for IEnumerable<T>)
		Assert.NotNull(result);
		Assert.Empty(result);
	}

	#endregion

	#region Multiple Type Parameters

	[Fact]
	public void GenericStandaloneStub_MultipleTypeParams_Works()
	{
		// Arrange
		var stub = new GenericKeyValueStoreStub<string, int>();
		IGenericKeyValueStore<string, int> store = stub;
		var tracking = stub.Get.OnCall((ko, key) => 42);

		// Act
		var result = store.Get("answer");

		// Assert
		Assert.Equal(42, result);
		Assert.Equal(1, tracking.CallCount);
		Assert.Equal("answer", tracking.LastArg);
	}

	[Fact]
	public void GenericStandaloneStub_MultipleTypeParams_DifferentTypeArgs()
	{
		// Arrange
		var intStringStore = new GenericKeyValueStoreStub<int, string>();
		var stringBoolStore = new GenericKeyValueStoreStub<string, bool>();

		// Assert - both implement their respective interface
		Assert.IsAssignableFrom<IGenericKeyValueStore<int, string>>(intStringStore);
		Assert.IsAssignableFrom<IGenericKeyValueStore<string, bool>>(stringBoolStore);
	}

	#endregion

	#region Constrained Generics

	[Fact]
	public void GenericStandaloneStub_WithConstraints_PreservesConstraints()
	{
		// Arrange - ConstrainedRepositoryStub<T> has "where T : class"
		var stub = new ConstrainedRepositoryStub<User>();
		IConstrainedRepository<User> repo = stub;
		var user = new User { Id = 1 };
		var tracking = stub.Save.OnCall((ko, entity) => { });

		// Act
		repo.Save(user);

		// Assert
		Assert.Equal(1, tracking.CallCount);
		Assert.Same(user, tracking.LastArg);
	}

	[Fact]
	public void GenericStandaloneStub_WithConstraints_GetByIdReturnsNull()
	{
		// Arrange
		var stub = new ConstrainedRepositoryStub<User>();
		IConstrainedRepository<User> repo = stub;

		// Act
		var result = repo.GetById(999);

		// Assert - nullable reference type returns null by default
		Assert.Null(result);
	}

	#endregion

	#region Reset

	[Fact]
	public void GenericStandaloneStub_Reset_ClearsCallTracking()
	{
		// Arrange
		var stub = new GenericRepositoryStub<User>();
		var tracking = stub.GetById.OnCall((ko, id) => null);
		IGenericRepository<User> repo = stub;
		repo.GetById(1);
		repo.GetById(2);

		// Act
		stub.GetById.Reset();

		// Assert - tracking object is also reset
		Assert.Equal(0, tracking.CallCount);
		Assert.False(tracking.WasCalled);
	}

	#endregion
}

#region Test Interfaces and Stubs

/// <summary>
/// Generic repository interface for testing.
/// </summary>
public interface IGenericRepository<T> where T : class
{
	T? GetById(int id);
	void Save(T entity);
	IEnumerable<T> GetAll();
	int Count { get; set; }
}

/// <summary>
/// Generic standalone stub for IGenericRepository.
/// This tests the core feature: generic class implementing generic interface.
/// </summary>
[KnockOff]
public partial class GenericRepositoryStub<T> : IGenericRepository<T> where T : class
{
}

/// <summary>
/// Generic key-value store interface with multiple type parameters.
/// </summary>
public interface IGenericKeyValueStore<TKey, TValue>
{
	TValue Get(TKey key);
	void Set(TKey key, TValue value);
}

/// <summary>
/// Generic standalone stub with multiple type parameters.
/// </summary>
[KnockOff]
public partial class GenericKeyValueStoreStub<TKey, TValue> : IGenericKeyValueStore<TKey, TValue>
{
}

/// <summary>
/// Generic interface with class constraint.
/// </summary>
public interface IConstrainedRepository<T> where T : class
{
	T? GetById(int id);
	void Save(T entity);
}

/// <summary>
/// Generic standalone stub that preserves the class constraint.
/// </summary>
[KnockOff]
public partial class ConstrainedRepositoryStub<T> : IConstrainedRepository<T> where T : class
{
}

#endregion
