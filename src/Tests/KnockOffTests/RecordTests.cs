using KnockOff;
using Xunit;

namespace KnockOffTests;

/// <summary>
/// Tests whether KnockOff can stub record types.
/// Records have compiler-synthesized members that must be filtered,
/// and the Impl class must use `sealed record` instead of `sealed class`.
/// </summary>

// ===== Record Types =====

/// <summary>Record with a virtual method.</summary>
public record MyRecord
{
	public virtual void Foo() { }
}

/// <summary>Record with positional properties and a virtual method.</summary>
public record PositionalRecordWithMethod(string Name, int Age)
{
	public virtual string Greet() => $"Hello, {Name}!";
}

/// <summary>Abstract record with abstract method.</summary>
public abstract record AbstractRecord
{
	public abstract string GetValue();
}

/// <summary>Record with an explicitly declared virtual property.</summary>
public record RecordWithVirtualProperty
{
	public virtual string Label { get; set; } = "";
	public virtual int Compute(int x) => x * 2;
}

// ===== Inline Stubs =====

[KnockOff<MyRecord>]
[KnockOff<PositionalRecordWithMethod>]
[KnockOff<AbstractRecord>]
[KnockOff<RecordWithVirtualProperty>]
public partial class RecordTests { }

// ===== Standalone Class Stub =====

/// <summary>
/// Standalone class stub for MyRecord (Pattern 3).
/// Verifies the StandaloneClassRenderer also emits 'sealed record Impl'.
/// </summary>
[KnockOffBase<MyRecord>]
public partial class MyRecordStandaloneStub
{
}

/// <summary>
/// Tests for record type stubbing.
/// </summary>
public class RecordStubTests
{
	[Fact]
	public void Record_VirtualMethod_CanBeStubbed()
	{
		// Arrange
		var stub = new RecordTests.Stubs.MyRecord();

		// Act - call the virtual method
		stub.Object.Foo();

		// Assert - method was called
		stub.Foo.Verify(Called.Once);
	}

	[Fact]
	public void Record_PositionalWithVirtualMethod_CanBeStubbed()
	{
		// Arrange - positional record with a virtual method
		var stub = new RecordTests.Stubs.PositionalRecordWithMethod("Alice", 30);
		stub.Greet.Return("Mocked greeting");

		// Act
		var result = stub.Object.Greet();

		// Assert
		Assert.Equal("Mocked greeting", result);
	}

	[Fact]
	public void Record_PositionalProperties_PreservedOnObject()
	{
		// Arrange - positional record properties are not interceptable via inheritance
		// but their values should be preserved from the constructor
		var stub = new RecordTests.Stubs.PositionalRecordWithMethod("Alice", 30);

		// Assert - the positional properties retain their constructor values
		Assert.Equal("Alice", stub.Object.Name);
		Assert.Equal(30, stub.Object.Age);
	}

	[Fact]
	public void Record_AbstractMethod_CanBeStubbed()
	{
		// Arrange
		var stub = new RecordTests.Stubs.AbstractRecord();
		stub.GetValue.Return("abstract-value");

		// Act
		var result = stub.Object.GetValue();

		// Assert
		Assert.Equal("abstract-value", result);
	}

	[Fact]
	public void Record_ExplicitVirtualProperty_CanBeStubbed()
	{
		// Arrange - explicitly declared virtual property IS interceptable
		var stub = new RecordTests.Stubs.RecordWithVirtualProperty();
		stub.Label.Get(() => "intercepted");

		// Assert
		Assert.Equal("intercepted", stub.Object.Label);
	}

	[Fact]
	public void Record_ExplicitVirtualMethod_CanBeStubbed()
	{
		// Arrange
		var stub = new RecordTests.Stubs.RecordWithVirtualProperty();
		stub.Compute.Return(999);

		// Act
		var result = stub.Object.Compute(5);

		// Assert
		Assert.Equal(999, result);
	}

	[Fact]
	public void Record_StandaloneClassStub_CanBeCreated()
	{
		// Arrange - standalone class stub for a record (Pattern 3)
		var stub = new MyRecordStandaloneStub();

		// Act
		stub.Object.Foo();

		// Assert - the Foo method was called
		stub.Foo.Verify(Called.Once);
	}

	[Fact]
	public void Record_ObjectIsRecord()
	{
		// Arrange
		var stub = new RecordTests.Stubs.MyRecord();

		// Assert - the Object is an instance of the record type
		Assert.IsAssignableFrom<MyRecord>(stub.Object);
	}
}
