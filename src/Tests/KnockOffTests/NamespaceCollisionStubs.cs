using KnockOff;

// Stubs in DomainModel.Tests namespace - this triggers the collision because
// when C# resolves "Person.Ef.IPersonDbContext", it looks up the namespace
// hierarchy and finds DomainModel.Person (the class) before the Person namespace.
namespace DomainModel.Tests
{
	/// <summary>
	/// Inline stub test class for namespace collision scenario.
	/// </summary>
	[KnockOff<global::Person.Ef.IPersonDbContext>]
	public partial class NamespaceCollisionInlineTests
	{
	}

	/// <summary>
	/// Stand-alone stub for interface in colliding namespace.
	/// </summary>
	[KnockOff]
	public partial class PersonDbContextKnockOff : global::Person.Ef.IPersonDbContext
	{
	}
}
