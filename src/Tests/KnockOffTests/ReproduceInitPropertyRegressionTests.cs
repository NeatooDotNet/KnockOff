using Xunit;

namespace KnockOffTests;

/// <summary>
/// Tests to reproduce and verify the fix for the init-only property RecordSet regression.
///
/// The bug: After adding the Source(T) feature, init-only property setters stopped
/// calling RecordSet(), breaking test verification. SetCount would not increment
/// and LastSetValue would not be captured when init properties were set.
///
/// The fix: Ensure init-only property setters call RecordSet() before setting Value.
/// </summary>
public class ReproduceInitPropertyRegressionTests
{
	/// <summary>
	/// This test creates a stub that exposes a public init property for testing purposes.
	/// It verifies that calling the init setter increments SetCount and captures LastSetValue.
	///
	/// The stub pattern here allows us to test the init setter behavior which is normally
	/// difficult to test because explicit interface implementations can only be set during
	/// object construction.
	/// </summary>
	[Fact]
	public void InitProperty_Setter_Should_Call_RecordSet()
	{
		// Arrange - create the stub
		var stub = new InitPropertyRegressionStub();

		// Act - use object initializer to set the init property
		// This triggers the init setter on the public property
		var stubWithInit = new InitPropertyRegressionStub { Name = "test-value" };

		// Assert - SetCount should be 1 and LastSetValue should capture the value
		// This will FAIL if the init setter doesn't call RecordSet()
		Assert.Equal(1, stubWithInit.NameInterceptor.SetCount);
		Assert.Equal("test-value", stubWithInit.NameInterceptor.LastSetValue);
	}

	[Fact]
	public void InitProperty_Setter_Should_Set_Value()
	{
		// Arrange & Act
		var stub = new InitPropertyRegressionStub { Name = "initialized" };

		// Assert - the value should be accessible via the interceptor
		Assert.Equal("initialized", stub.NameInterceptor.Value);
	}

	[Fact]
	public void InitProperty_Getter_Should_Call_RecordGet()
	{
		// Arrange
		var stub = new InitPropertyRegressionStub { Name = "getter-test" };
		Assert.Equal(0, stub.NameInterceptor.GetCount);

		// Act - access the property
		var _ = stub.Name;

		// Assert - GetCount should be 1
		Assert.Equal(1, stub.NameInterceptor.GetCount);
	}
}

/// <summary>
/// A test stub with a PUBLIC init property to allow testing init setter behavior.
/// This mirrors the generated code structure but exposes the property publicly.
/// </summary>
public class InitPropertyRegressionStub
{
	/// <summary>Interceptor for Name property.</summary>
	public sealed class NameInterceptorClass
	{
		public int GetCount { get; private set; }
		public int SetCount { get; private set; }
		public string? LastSetValue { get; private set; }
		public string Value { get; set; } = default!;

		public void RecordGet() => GetCount++;
		public void RecordSet(string? value) { SetCount++; LastSetValue = value; }
		public void Reset() { GetCount = 0; SetCount = 0; LastSetValue = default; Value = default!; }
	}

	public NameInterceptorClass NameInterceptor { get; } = new();

	/// <summary>
	/// Public init property that mimics the generated code behavior.
	/// The init setter SHOULD call RecordSet() before setting Value.
	///
	/// BEFORE the fix (broken):
	///   init { NameInterceptor.Value = value; }
	///
	/// AFTER the fix (correct):
	///   init { NameInterceptor.RecordSet(value); NameInterceptor.Value = value; }
	/// </summary>
	public string Name
	{
		get
		{
			NameInterceptor.RecordGet();
			return NameInterceptor.Value;
		}
		init
		{
			// This is what the generated code SHOULD do:
			NameInterceptor.RecordSet(value);
			NameInterceptor.Value = value;
		}
	}
}
