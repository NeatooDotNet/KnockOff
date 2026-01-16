namespace KnockOff.Tests;

/// <summary>
/// Tests for namespace collision scenarios where a type name shadows a namespace.
/// Bug: Without global:: prefix, the compiler resolves "Person" to the class
/// instead of the Person.Ef namespace.
/// </summary>
public class NamespaceCollisionTests
{
	[Fact]
	public void Interface_InCollidingNamespace_GeneratesCompilableCode()
	{
		// Arrange: Create a stub for an interface in a namespace that collides with a type name
		// The stub is in DomainModel.Tests namespace where DomainModel.Person shadows the Person namespace
		var stub = new DomainModel.Tests.PersonDbContextKnockOff();
		var tracking = stub.SavePerson.OnCall((ko, person) => { });
		global::Person.Ef.IPersonDbContext service = stub;

		// Act
		service.SavePerson(new DomainModel.Person { Name = "Test" });

		// Assert
		Assert.True(tracking.WasCalled);
	}

	[Fact]
	public void InlineStub_InCollidingNamespace_GeneratesCompilableCode()
	{
		// Arrange: Test inline stub pattern with namespace collision
		var stub = new DomainModel.Tests.NamespaceCollisionInlineTests.Stubs.IPersonDbContext();
		// Inline stubs use OnCall as a property
		stub.SavePerson.OnCall = (ko, person) => { };
		global::Person.Ef.IPersonDbContext service = stub;

		// Act
		service.SavePerson(new DomainModel.Person { Name = "Test" });

		// Assert - inline stubs have WasCalled on the interceptor directly
		Assert.True(stub.SavePerson.WasCalled);
	}
}
