namespace KnockOff.Tests;

/// <summary>
/// Tests for core KnockOff functionality: properties, methods, and tracking.
/// </summary>
public class BasicTests
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
		Assert.NotNull(knockOff.Name);
	}

	[Fact]
	public void Property_Setter_TracksInvocation_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";

		Assert.Equal(1, knockOff.Name.SetCount);
		string? lastValue = knockOff.Name.LastSetValue;
		Assert.Equal("Test", lastValue);
	}

	[Fact]
	public void Property_Getter_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		var _ = service.Name;

		Assert.Equal(1, knockOff.Name.GetCount);
	}

	[Fact]
	public void Method_VoidNoParams_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.DoSomething();

		Assert.True(knockOff.DoSomething.WasCalled);
		Assert.Equal(1, knockOff.DoSomething.CallCount);
	}

	[Fact]
	public void Method_WithSingleParam_TracksArg_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetValue(42);

		Assert.Equal(84, result);
		Assert.Equal(1, knockOff.GetValue2.CallCount);

		int? lastArg = knockOff.GetValue2.LastCallArg;
		Assert.Equal(42, lastArg);
	}

	[Fact]
	public void Method_WithMultipleParams_TracksArgs_AsNamedTuple()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 100, true);

		var args = knockOff.Calculate.LastCallArgs;
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

		Assert.Equal(3, knockOff.GetValue2.CallCount);
		Assert.Equal(3, knockOff.GetValue2.LastCallArg); // Last call was GetValue(3)
	}

	[Fact]
	public void Method_WithNullableReturn_NoUserMethod_ReturnsDefault()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetOptional();

		Assert.Null(result);
		Assert.True(knockOff.GetOptional.WasCalled);
	}

	[Fact]
	public void ImplicitConversion_ReturnsTypedInterface()
	{
		var knockOff = new SampleKnockOff();

		ISampleService service = knockOff;

		service.Name = "Test";
		Assert.Equal(1, knockOff.Name.SetCount);
	}

	[Fact]
	public void Reset_ClearsTrackingState()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		service.GetValue(42);
		service.DoSomething();

		knockOff.Name.Reset();
		knockOff.GetValue2.Reset();
		knockOff.DoSomething.Reset();

		Assert.Equal(0, knockOff.Name.SetCount);
		Assert.Equal(0, knockOff.GetValue2.CallCount);
		Assert.Equal(0, knockOff.DoSomething.CallCount);
		Assert.False(knockOff.DoSomething.WasCalled);
	}

	[Fact]
	public void TupleDestructuring_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 42, true);

		if (knockOff.Calculate.LastCallArgs is var (name, value, flag))
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
