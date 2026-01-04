namespace KnockOff.Tests;

/// <summary>
/// Tests for spy property naming collision detection.
///
/// When an interface has a member with the same name as the interface itself
/// (e.g., ICollision interface with a property named ICollision), the generator
/// must detect this and rename the spy accessor to avoid a compile error.
/// </summary>
public class SpyPropertyCollisionTests
{
	[Fact]
	public void Collision_KnockOffCompiles()
	{
		// The mere fact this compiles proves collision detection works
		var knockOff = new CollisionKnockOff();
		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Collision_SpyAccessorWorks()
	{
		var knockOff = new CollisionKnockOff();

		// The spy accessor is renamed to ICollision_ due to collision
		Assert.NotNull(knockOff.ICollision_);
		Assert.NotNull(knockOff.ICollision_.ICollision);
		Assert.NotNull(knockOff.ICollision_.DoWork);
	}

	[Fact]
	public void Collision_PropertyWorksViaInterface()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;

		collision.ICollision = "test";
		var value = collision.ICollision;

		Assert.Equal("test", value);
		Assert.Equal(1, knockOff.ICollision_.ICollision.SetCount);
		Assert.Equal(1, knockOff.ICollision_.ICollision.GetCount);
	}

	[Fact]
	public void Collision_MethodWorksViaInterface()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;

		collision.DoWork();

		Assert.True(knockOff.ICollision_.DoWork.WasCalled);
	}

	[Fact]
	public void Collision_OnCallbackWorks()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;
		var callbackInvoked = false;

		knockOff.ICollision_.DoWork.OnCall = (ko) =>
		{
			callbackInvoked = true;
		};

		collision.DoWork();

		Assert.True(callbackInvoked);
	}

	[Fact]
	public void Collision_OnGetWorks()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;

		knockOff.ICollision_.ICollision.OnGet = (ko) => "from callback";

		var result = collision.ICollision;

		Assert.Equal("from callback", result);
	}
}
