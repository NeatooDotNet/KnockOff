// Domain model namespace - has a class named "Person"
namespace DomainModel
{
	/// <summary>
	/// Domain entity class. Its name "Person" will shadow the "Person" namespace
	/// when both are in scope.
	/// </summary>
	public class Person
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
	}
}

// Interface namespace that starts with "Person" - this causes the collision
namespace Person.Ef
{
	/// <summary>
	/// Interface in a namespace starting with "Person".
	/// When DomainModel.Person is in scope, "Person.Ef.IPersonDbContext"
	/// resolves "Person" to the class instead of the namespace.
	/// </summary>
	public interface IPersonDbContext
	{
		void SavePerson(DomainModel.Person person);
		DomainModel.Person? GetPerson(int id);
	}
}
