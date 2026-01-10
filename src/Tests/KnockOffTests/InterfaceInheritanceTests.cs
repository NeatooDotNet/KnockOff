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

		Assert.Equal(1, knockOff.ModifiedAt.SetCount);
		Assert.Equal(1, knockOff.ModifiedBy.SetCount);
		Assert.Equal("TestUser", knockOff.ModifiedBy.LastSetValue);
	}

	[Fact]
	public void InterfaceInheritance_BasePropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.Id.GetCount);
		Assert.Equal(1, knockOff.CreatedAt.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_ImplicitConversion_Works()
	{
		var knockOff = new AuditableEntityKnockOff();

		IAuditableEntity auditable = knockOff;
		IBaseEntity baseEntity = knockOff;

		auditable.ModifiedBy = "Via cast";
		var id = baseEntity.Id;

		Assert.Equal(1, knockOff.ModifiedBy.SetCount);
		Assert.Equal(1, knockOff.Id.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_AccessBaseViaDerivied()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.Id.GetCount);
		Assert.Equal(1, knockOff.CreatedAt.GetCount);
	}
}
