namespace KnockOff.Tests;

/// <summary>
/// Tests for generic standalone class stubs using [KnockOffBase(typeof(T&lt;&gt;))] attribute.
/// Pattern 9: Open Generic Standalone Class Stubs.
/// These allow creating reusable, file-based stubs for generic base classes.
/// </summary>
public class GenericStandaloneClassStubTests
{
	#region Basic Existence Tests

	[Fact]
	public void GenericStandaloneClassStub_Exists()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();
		Assert.NotNull(stub);
	}

	[Fact]
	public void GenericStandaloneClassStub_HasObjectProperty()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		Assert.NotNull(stub.Object);
	}

	[Fact]
	public void GenericStandaloneClassStub_ObjectIsTargetType()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		// .Object should be assignable to ClassRepositoryBase<T>
		ClassRepositoryBase<ClassStubUser> repo = stub.Object;
		Assert.NotNull(repo);
	}

	[Fact]
	public void GenericStandaloneClassStub_ImplementsIKnockOffStub()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		Assert.IsAssignableFrom<IKnockOffStub>(stub);
	}

	#endregion

	#region Multiple Type Arguments

	[Fact]
	public void GenericStandaloneClassStub_CanInstantiate_WithDifferentTypeArgs()
	{
		var userRepo = new GenericClassRepoStub<ClassStubUser>();
		var productRepo = new GenericClassRepoStub<ClassStubProduct>();

		Assert.NotNull(userRepo);
		Assert.NotNull(productRepo);
	}

	[Fact]
	public void GenericStandaloneClassStub_DifferentTypeArgs_IndependentState()
	{
		var userRepo = new GenericClassRepoStub<ClassStubUser>();
		var productRepo = new GenericClassRepoStub<ClassStubProduct>();

		userRepo.GetById.OnCall((id) => new ClassStubUser { Id = id, Name = "User" });
		productRepo.GetById.OnCall((id) => new ClassStubProduct { Id = id, Price = 9.99m });

		var user = userRepo.Object.GetById(1);
		var product = productRepo.Object.GetById(2);

		Assert.Equal("User", user?.Name);
		Assert.Equal(9.99m, product?.Price);
	}

	#endregion

	#region Abstract Method Tests

	[Fact]
	public void GenericStandaloneClassStub_AbstractMethod_TracksCallCount()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		stub.Object.GetById(1);
		stub.Object.GetById(2);

		stub.GetById.Verify(Times.Exactly(2));
	}

	[Fact]
	public void GenericStandaloneClassStub_AbstractMethod_TracksLastArg()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		stub.Object.GetById(42);

		Assert.Equal(42, stub.GetById.LastCallArg);
	}

	[Fact]
	public void GenericStandaloneClassStub_AbstractMethod_OnCall_ReturnsConfiguredValue()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();
		var expected = new ClassStubUser { Id = 123, Name = "Test User" };
		stub.GetById.OnCall((id) => expected);

		var result = stub.Object.GetById(123);

		Assert.Same(expected, result);
	}

	[Fact]
	public void GenericStandaloneClassStub_AbstractMethod_NoCallback_ReturnsDefault()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		var result = stub.Object.GetById(999);

		// Nullable reference type returns null by default
		Assert.Null(result);
	}

	[Fact]
	public void GenericStandaloneClassStub_VoidMethod_TracksCall()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();
		var saved = new List<ClassStubUser>();
		stub.Save.OnCall((entity) => saved.Add(entity));

		var user = new ClassStubUser { Id = 1, Name = "Saved" };
		stub.Object.Save(user);

		stub.Save.Verify(Times.Once);
		Assert.Same(user, stub.Save.LastCallArg);
		Assert.Single(saved);
	}

	#endregion

	#region Constraint Preservation Tests

	[Fact]
	public void GenericStandaloneClassStub_PreservesConstraints()
	{
		// ConstrainedClassRepositoryBase<T> has "where T : class"
		var stub = new ConstrainedClassRepoStub<ClassStubUser>();

		Assert.NotNull(stub);
		Assert.NotNull(stub.Object);
	}

	#endregion

	#region Reset Tests

	[Fact]
	public void GenericStandaloneClassStub_Reset_ClearsInterceptorState()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();
		stub.GetById.OnCall((id) => new ClassStubUser { Id = id });

		stub.Object.GetById(1);
		stub.Object.GetById(2);

		stub.GetById.Reset();

		stub.GetById.Verify(Times.Never);
		Assert.Null(stub.GetById.LastCallArg);
	}

	[Fact]
	public void GenericStandaloneClassStub_ResetInterceptors_ClearsAllState()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		stub.Object.GetById(1);
		stub.Object.Save(new ClassStubUser());

		stub.ResetInterceptors();

		stub.GetById.Verify(Times.Never);
		stub.Save.Verify(Times.Never);
	}

	#endregion

	#region Substitutability Tests

	[Fact]
	public void GenericStandaloneClassStub_Substitutability_PassToMethod()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();
		stub.GetById.OnCall((id) => new ClassStubUser { Id = id, Name = $"User_{id}" });

		var result = ProcessRepository(stub.Object);

		Assert.Equal("User_42", result);
	}

	private static string ProcessRepository(ClassRepositoryBase<ClassStubUser> repo)
	{
		var user = repo.GetById(42);
		return user?.Name ?? "Not Found";
	}

	#endregion

	#region Strict Mode Tests

	[Fact]
	public void GenericStandaloneClassStub_Strict_ThrowsOnUnconfiguredMethod()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();
		stub.Strict = true;

		Assert.Throws<StubException>(() => stub.Object.GetById(1));
	}

	#endregion

	#region User Custom Methods Tests

	[Fact]
	public void GenericStandaloneClassStub_UserCanAddCustomMethods()
	{
		var stub = new GenericClassRepoStub<ClassStubUser>();

		stub.TrackSave(new ClassStubUser { Id = 1, Name = "Tracked" });

		Assert.Single(stub.SavedEntities);
		Assert.Equal("Tracked", stub.SavedEntities[0].Name);
	}

	#endregion
}

#region Test Entity Classes

/// <summary>
/// Simple user entity for testing standalone class stubs.
/// Named uniquely to avoid conflicts with other test files.
/// </summary>
public class ClassStubUser
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

/// <summary>
/// Simple product entity for testing standalone class stubs.
/// Named uniquely to avoid conflicts with other test files.
/// </summary>
public class ClassStubProduct
{
	public int Id { get; set; }
	public decimal Price { get; set; }
}

#endregion

#region Target Classes

/// <summary>
/// Generic abstract repository base class for testing Pattern 9.
/// Named uniquely to avoid conflicts.
/// </summary>
public abstract class ClassRepositoryBase<T> where T : class
{
	public abstract T? GetById(int id);
	public abstract void Save(T entity);
}

/// <summary>
/// Generic abstract repository with constraints for testing constraint preservation.
/// Named uniquely to avoid conflicts.
/// </summary>
public abstract class ConstrainedClassRepositoryBase<T> where T : class, new()
{
	public abstract T? GetById(int id);
	public abstract void Save(T entity);
}

#endregion

#region Test Stubs

/// <summary>
/// Generic standalone class stub for ClassRepositoryBase using [KnockOffBase(typeof(T&lt;&gt;))] pattern.
/// Named uniquely to avoid conflicts.
/// </summary>
[KnockOffBase(typeof(ClassRepositoryBase<>))]
public partial class GenericClassRepoStub<T> where T : class
{
	// Users can add custom tracking or state
	private readonly List<T> _saved = [];
	public IReadOnlyList<T> SavedEntities => _saved;

	public void TrackSave(T entity)
	{
		_saved.Add(entity);
	}
}

/// <summary>
/// Generic standalone class stub with constraints.
/// Named uniquely to avoid conflicts.
/// </summary>
[KnockOffBase(typeof(ConstrainedClassRepositoryBase<>))]
public partial class ConstrainedClassRepoStub<T> where T : class, new()
{
}

#endregion
