namespace KnockOff.Tests;

/// <summary>
/// Tests for KO property naming collision detection.
///
/// When an interface has a member with the same name as the interface itself
/// (e.g., ICollision interface with a property named ICollision), the generator
/// must detect this and rename the KO accessor to avoid a compile error.
/// </summary>
public class KOPropertyCollisionTests
{
	[Fact]
	public void Collision_KnockOffCompiles()
	{
		// The mere fact this compiles proves collision detection works
		var knockOff = new CollisionKnockOff();
		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Collision_KOAccessorWorks()
	{
		var knockOff = new CollisionKnockOff();

		// Flat API: ICollision is the property interceptor, DoWork is the method interceptor
		Assert.NotNull(knockOff.ICollision);
		Assert.NotNull(knockOff.DoWork);
	}

	[Fact]
	public void Collision_PropertyWorksViaInterface()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;

		collision.ICollision = "test";
		var value = collision.ICollision;

		Assert.Equal("test", value);
		knockOff.ICollision.VerifySet(Called.Once);
		knockOff.ICollision.VerifyGet(Called.Once);
	}

	[Fact]
	public void Collision_MethodWorksViaInterface()
	{
		var knockOff = new CollisionKnockOff();
		var tracking = knockOff.DoWork.Call(() => { });
		ICollision collision = knockOff;

		collision.DoWork();

		tracking.Verify();
	}

	[Fact]
	public void Collision_OnCallbackWorks()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;
		var callbackInvoked = false;

		knockOff.DoWork.Call(() =>
		{
			callbackInvoked = true;
		});

		collision.DoWork();

		Assert.True(callbackInvoked);
	}

	[Fact]
	public void Collision_OnGetWorks()
	{
		var knockOff = new CollisionKnockOff();
		ICollision collision = knockOff;

		knockOff.ICollision.Get(() => "from callback");

		var result = collision.ICollision;

		Assert.Equal("from callback", result);
	}
}
