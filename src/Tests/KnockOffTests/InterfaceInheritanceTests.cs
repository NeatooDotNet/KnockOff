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

		Assert.Equal(1, knockOff.Spy.ModifiedAt.SetCount);
		Assert.Equal(1, knockOff.Spy.ModifiedBy.SetCount);
		Assert.Equal("TestUser", knockOff.Spy.ModifiedBy.LastSetValue);
	}

	[Fact]
	public void InterfaceInheritance_BasePropertiesWork()
	{
		var knockOff = new AuditableEntityKnockOff();
		IBaseEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.Spy.Id.GetCount);
		Assert.Equal(1, knockOff.Spy.CreatedAt.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_BothAsMethodsWork()
	{
		var knockOff = new AuditableEntityKnockOff();

		IAuditableEntity auditable = knockOff.AsAuditableEntity();
		IBaseEntity baseEntity = knockOff.AsBaseEntity();

		auditable.ModifiedBy = "Via AsAuditableEntity";
		var id = baseEntity.Id;

		Assert.Equal(1, knockOff.Spy.ModifiedBy.SetCount);
		Assert.Equal(1, knockOff.Spy.Id.GetCount);
	}

	[Fact]
	public void InterfaceInheritance_AccessBaseViaDerivied()
	{
		var knockOff = new AuditableEntityKnockOff();
		IAuditableEntity entity = knockOff;

		var id = entity.Id;
		var createdAt = entity.CreatedAt;

		Assert.Equal(1, knockOff.Spy.Id.GetCount);
		Assert.Equal(1, knockOff.Spy.CreatedAt.GetCount);
	}
}
