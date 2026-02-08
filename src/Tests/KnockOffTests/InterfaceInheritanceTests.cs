namespace KnockOff.Tests;

/// <summary>
/// Tests for interface inheritance support.
/// </summary>
public class InterfaceInheritanceTests
{
	[Fact]
	public void InterfaceInheritance_DerivedPropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		entity.ModifiedAt = DateTime.Now;
		entity.ModifiedBy = "TestUser";

		knockOff.ModifiedAt.VerifySet(Called.Once);
		knockOff.ModifiedBy.VerifySet(Called.Once);
		Assert.Equal("TestUser", knockOff.ModifiedBy.LastSetValue);
	}

	[Fact]
	public void InterfaceInheritance_BasePropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		knockOff.Id.VerifyGet(Called.Once);
		knockOff.CreatedAt.VerifyGet(Called.Once);
	}

	[Fact]
	public void InterfaceInheritance_ImplicitConversion_Works()
	{
		var knockOff = new AuditableEntityKnockOff();

		IAuditableEntity auditable = knockOff;
		IBaseEntity baseEntity = knockOff;

		auditable.ModifiedBy = "Via cast";
		var id = baseEntity.Id;

		knockOff.ModifiedBy.VerifySet(Called.Once);
		knockOff.Id.VerifyGet(Called.Once);
	}

	[Fact]
	public void InterfaceInheritance_AccessBaseViaDerivied()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		knockOff.Id.VerifyGet(Called.Once);
		knockOff.CreatedAt.VerifyGet(Called.Once);
	}
}
