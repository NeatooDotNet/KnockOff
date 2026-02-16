using KnockOff;
using Xunit;

namespace KnockOffTests;

// ===== Bug Reproduction: CS9035 required member not set =====
// When a method returns a type with required properties, the generator
// emits new T() as a smart default, which fails because required members
// must be initialized in the object initializer.

public class ResultWithRequired
{
	public required string Name { get; init; }
	public string? Description { get; set; }
}

public interface IServiceWithRequiredReturn
{
	ResultWithRequired GetResult();
	Task<ResultWithRequired> GetResultAsync();
}

[KnockOff<IServiceWithRequiredReturn>]
public partial class RequiredPropertyBugTests
{
	[Fact]
	public void CanCreateStub()
	{
		var stub = new Stubs.IServiceWithRequiredReturn();
		Assert.NotNull(stub);
	}
}
