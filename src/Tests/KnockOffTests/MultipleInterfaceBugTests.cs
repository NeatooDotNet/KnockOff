using KnockOff;
using Xunit;

namespace KnockOffTests;

// ===== Bug Reproduction: CS0102 duplicate delegate types =====
// When multiple interfaces share a common base interface with OVERLOADED
// methods, and both are stubbed inline in the same test class, the generator
// emits overload compositor delegate types at the Stubs level (outside the
// individual stub classes). These delegate names collide because they don't
// include the stub class name prefix.

public interface IEntityBase
{
	Task WaitForTasks();
	Task WaitForTasks(CancellationToken token);
	Task RunRules(string propertyName);
	Task RunRules(string propertyName, CancellationToken token);
}

public interface IEmployee : IEntityBase
{
	string Name { get; set; }
}

public interface IDepartment : IEntityBase
{
	string Title { get; set; }
}

[KnockOff<IEmployee>]
[KnockOff<IDepartment>]
public partial class MultipleInterfaceBugTests
{
	[Fact]
	public void Employee_CanCreate()
	{
		var employee = new Stubs.IEmployee();
		Assert.NotNull(employee);
	}

	[Fact]
	public void Department_CanCreate()
	{
		var department = new Stubs.IDepartment();
		Assert.NotNull(department);
	}
}
