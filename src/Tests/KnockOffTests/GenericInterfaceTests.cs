namespace KnockOff.Tests;

/// <summary>
/// Tests for generic interface support.
/// </summary>
public class GenericInterfaceTests
{
	[Fact]
	public void GenericInterface_MethodsWork()
	{
		var knockOff = new UserRepositoryKnockOff();
		var tracking = knockOff.Save.OnCall((ko, user) => { });
		IRepository<User> repo = knockOff;

		var user = new User { Id = 1, Name = "Test" };
		repo.Save(user);

		Assert.True(tracking.WasCalled);
		Assert.Same(user, tracking.LastArg);
	}

	[Fact]
	public void GenericInterface_NullableReturn_ReturnsDefault()
	{
		var knockOff = new UserRepositoryKnockOff();
		var tracking = knockOff.GetById.OnCall((ko, id) => null);
		IRepository<User> repo = knockOff;

		var result = repo.GetById(42);

		Assert.Null(result);
		Assert.Equal(42, tracking.LastArg);
	}

	[Fact]
	public async Task GenericInterface_AsyncMethod_ReturnsTaskWithDefault()
	{
		var knockOff = new UserRepositoryKnockOff();
		var tracking = knockOff.GetByIdAsync.OnCall((ko, id) => Task.FromResult<User?>(null));
		IRepository<User> repo = knockOff;

		var result = await repo.GetByIdAsync(100);

		Assert.Null(result);
		Assert.Equal(100, tracking.LastArg);
	}

	[Fact]
	public void GenericInterface_ImplicitConversion_Works()
	{
		var knockOff = new UserRepositoryKnockOff();
		var tracking = knockOff.Save.OnCall((ko, user) => { });

		IRepository<User> repo = knockOff;

		repo.Save(new User { Id = 1, Name = "Via cast" });
		Assert.True(tracking.WasCalled);
	}
}
