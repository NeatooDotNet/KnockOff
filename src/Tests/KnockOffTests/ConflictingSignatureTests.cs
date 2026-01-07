namespace KnockOff.Tests;

/// <summary>
/// Tests for the original bug: conflicting method signatures across interfaces.
/// Before the fix, having two interfaces with methods of the same name but different
/// signatures would fail because the handler types would conflict.
///
/// The fix: Interface-scoped handlers. Each interface gets its own KO class
/// with its own handlers, so IDataProvider.GetData and IKeyLookup.GetData can have
/// completely different signatures.
/// </summary>
public class ConflictingSignatureTests
{
	[Fact]
	public void ConflictingSignatures_BothMethodsWork()
	{
		var knockOff = new ConflictingSignatureKnockOff();
		IDataProvider provider = knockOff;
		IKeyLookup lookup = knockOff;

		// Call IDataProvider.GetData(int) - returns string
		var stringResult = provider.GetData(42);

		// Call IKeyLookup.GetData(string) - returns int
		var intResult = lookup.GetData("hello");

		// Verify both work correctly using user methods
		Assert.Equal("Data-42", stringResult);
		Assert.Equal(5, intResult); // "hello".Length
	}

	[Fact]
	public void ConflictingSignatures_TrackingSeparate()
	{
		var knockOff = new ConflictingSignatureKnockOff();
		IDataProvider provider = knockOff;
		IKeyLookup lookup = knockOff;

		provider.GetData(1);
		provider.GetData(2);
		lookup.GetData("test");

		// Each interface has its own tracking
		Assert.Equal(2, knockOff.IDataProvider.GetData.CallCount);
		Assert.Equal(1, knockOff.IKeyLookup.GetData.CallCount);

		Assert.Equal(2, knockOff.IDataProvider.GetData.LastCallArg);
		Assert.Equal("test", knockOff.IKeyLookup.GetData.LastCallArg);
	}

	[Fact]
	public void ConflictingSignatures_CallbacksIndependent()
	{
		var knockOff = new ConflictingSignatureKnockOff();
		IDataProvider provider = knockOff;
		IKeyLookup lookup = knockOff;

		// Configure callbacks for each independently
		knockOff.IDataProvider.GetData.OnCall = (ko, id) => $"Override-{id}";
		knockOff.IKeyLookup.GetData.OnCall = (ko, key) => 100;

		var stringResult = provider.GetData(5);
		var intResult = lookup.GetData("ignored");

		Assert.Equal("Override-5", stringResult);
		Assert.Equal(100, intResult);
	}

	[Fact]
	public void ConflictingSignatures_SameNameProperty_SeparateBacking()
	{
		// Both interfaces have Count property but with different signatures
		var knockOff = new ConflictingSignatureKnockOff();
		IDataProvider provider = knockOff;
		IKeyLookup lookup = knockOff;

		// IDataProvider.Count is get-only
		// IKeyLookup.Count is get/set

		lookup.Count = 42;

		Assert.Equal(42, lookup.Count);
		Assert.Equal(0, provider.Count); // Separate backing, still default

		Assert.Equal(1, knockOff.IKeyLookup.Count.SetCount);
		// After accessing provider.Count above, GetCount is 1
		Assert.Equal(1, knockOff.IDataProvider.Count.GetCount);
	}

	[Fact]
	public void ConflictingSignatures_AllCalls_CorrectType()
	{
		var knockOff = new ConflictingSignatureKnockOff();
		IDataProvider provider = knockOff;
		IKeyLookup lookup = knockOff;

		provider.GetData(1);
		provider.GetData(2);
		provider.GetData(3);
		lookup.GetData("a");
		lookup.GetData("bb");

		// IDataProvider.GetData tracks int arguments
		Assert.Equal(3, knockOff.IDataProvider.GetData.CallCount);
		Assert.Equal(3, knockOff.IDataProvider.GetData.LastCallArg); // Last call was GetData(3)

		// IKeyLookup.GetData tracks string arguments
		Assert.Equal(2, knockOff.IKeyLookup.GetData.CallCount);
		Assert.Equal("bb", knockOff.IKeyLookup.GetData.LastCallArg); // Last call was GetData("bb")
	}
}
