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

		Assert.Equal(1, knockOff.IAuditableEntity.ModifiedAt.SetCount);
		Assert.Equal(1, knockOff.IAuditableEntity.ModifiedBy.SetCount);
		Assert.Equal("TestUser", knockOff.IAuditableEntity.ModifiedBy.LastSetValue);
	}

	[Fact]
	public void InterfaceInheritance_BasePropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.IBaseEntity.Id.GetCount);
		Assert.Equal(1, knockOff.IBaseEntity.CreatedAt.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_BothAsMethodsWork()
	{
		var knockOff = new AuditableEntityKnockOff();

		IAuditableEntity auditable = knockOff.AsAuditableEntity();
		IBaseEntity baseEntity = knockOff.AsBaseEntity();

		auditable.ModifiedBy = "Via AsAuditableEntity";
		var id = baseEntity.Id;

		Assert.Equal(1, knockOff.IAuditableEntity.ModifiedBy.SetCount);
		Assert.Equal(1, knockOff.IBaseEntity.Id.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_AccessBaseViaDerivied()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.IBaseEntity.Id.GetCount);
		Assert.Equal(1, knockOff.IBaseEntity.CreatedAt.GetCount);
	}
}
