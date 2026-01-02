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
}
