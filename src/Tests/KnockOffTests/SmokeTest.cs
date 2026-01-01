namespace KnockOff.Tests;

/// <summary>
/// Tests verifying the generator runs and produces correct output.
/// </summary>
public class SmokeTest
{
	[Fact]
	public void KnockOffAttribute_Exists()
	{
		var attr = new KnockOffAttribute();
		Assert.NotNull(attr);
	}

	[Fact]
	public void Generator_ProducesOutput_ForKnockOffClass()
	{
		var knockOff = new SampleKnockOff();
		Assert.NotNull(knockOff);
		Assert.NotNull(knockOff.ExecutionInfo);
	}

	[Fact]
	public void Property_Setter_TracksInvocation_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";

		Assert.Equal(1, knockOff.ExecutionInfo.Name.SetCount);
		// Strongly typed - no cast needed!
		string? lastValue = knockOff.ExecutionInfo.Name.LastSetValue;
		Assert.Equal("Test", lastValue);
	}

	[Fact]
	public void Property_Getter_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		var _ = service.Name;

		Assert.Equal(1, knockOff.ExecutionInfo.Name.GetCount);
	}

	[Fact]
	public void Method_VoidNoParams_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.DoSomething();

		Assert.True(knockOff.ExecutionInfo.DoSomething.WasCalled);
		Assert.Equal(1, knockOff.ExecutionInfo.DoSomething.CallCount);
	}

	[Fact]
	public void Method_WithSingleParam_TracksArg_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetValue(42);

		Assert.Equal(84, result);
		Assert.Equal(1, knockOff.ExecutionInfo.GetValue.CallCount);

		// Single param uses LastCallArg (not tuple)
		int? lastArg = knockOff.ExecutionInfo.GetValue.LastCallArg;
		Assert.Equal(42, lastArg);
	}

	[Fact]
	public void Method_WithMultipleParams_TracksArgs_AsNamedTuple()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 100, true);

		var args = knockOff.ExecutionInfo.Calculate.LastCallArgs;
		Assert.NotNull(args);
		Assert.Equal("test", args.Value.name);
		Assert.Equal(100, args.Value.value);
		Assert.True(args.Value.flag);
	}

	[Fact]
	public void Method_AllCalls_TracksHistory()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.GetValue(1);
		service.GetValue(2);
		service.GetValue(3);

		Assert.Equal(3, knockOff.ExecutionInfo.GetValue.CallCount);
		Assert.Equal(3, knockOff.ExecutionInfo.GetValue.AllCalls.Count);
		// Single param - AllCalls is List<int>
		Assert.Equal(1, knockOff.ExecutionInfo.GetValue.AllCalls[0]);
		Assert.Equal(2, knockOff.ExecutionInfo.GetValue.AllCalls[1]);
		Assert.Equal(3, knockOff.ExecutionInfo.GetValue.AllCalls[2]);
	}

	[Fact]
	public void Method_WithNullableReturn_NoUserMethod_ReturnsDefault()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetOptional();

		Assert.Null(result);
		Assert.True(knockOff.ExecutionInfo.GetOptional.WasCalled);
	}

	[Fact]
	public void AsInterface_ReturnsTypedInterface()
	{
		var knockOff = new SampleKnockOff();

		// Use the generated AsXYZ() method
		ISampleService service = knockOff.AsSampleService();

		service.Name = "Test";
		Assert.Equal(1, knockOff.ExecutionInfo.Name.SetCount);
	}

	[Fact]
	public void Reset_ClearsTrackingState()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		service.GetValue(42);
		service.DoSomething();

		// Reset all
		knockOff.ExecutionInfo.Name.Reset();
		knockOff.ExecutionInfo.GetValue.Reset();
		knockOff.ExecutionInfo.DoSomething.Reset();

		Assert.Equal(0, knockOff.ExecutionInfo.Name.SetCount);
		Assert.Equal(0, knockOff.ExecutionInfo.GetValue.CallCount);
		Assert.Equal(0, knockOff.ExecutionInfo.DoSomething.CallCount);
		Assert.False(knockOff.ExecutionInfo.DoSomething.WasCalled);
	}

	[Fact]
	public void TupleDestructuring_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 42, true);

		// Destructure the tuple
		if (knockOff.ExecutionInfo.Calculate.LastCallArgs is var (name, value, flag))
		{
			Assert.Equal("test", name);
			Assert.Equal(42, value);
			Assert.True(flag);
		}
		else
		{
			Assert.Fail("LastCallArgs should not be null");
		}
	}
}

// Test interface
public interface ISampleService
{
	string Name { get; set; }
	void DoSomething();
	int GetValue(int input);
	void Calculate(string name, int value, bool flag);
	string? GetOptional();
}

// Test KnockOff class - generator produces the explicit interface implementations
[KnockOff]
public partial class SampleKnockOff : ISampleService
{
	// User-defined protected method - generator calls this for GetValue
	protected int GetValue(int input) => input * 2;
}
