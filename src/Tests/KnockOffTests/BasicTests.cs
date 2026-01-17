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
		var tracking = knockOff.DoSomething.OnCall(ko => { });
		ISampleService service = knockOff;

		service.DoSomething();

		Assert.True(tracking.WasCalled);
		Assert.Equal(1, tracking.CallCount);
	}

	[Fact]
	public void Method_WithSingleParam_TracksArg_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetValue(42);

		Assert.Equal(84, result);
		Assert.Equal(1, knockOff.GetValue2.CallCount);

		int lastArg = knockOff.GetValue2.LastArg;
		Assert.Equal(42, lastArg);
	}

	[Fact]
	public void Method_WithMultipleParams_TracksArgs_AsNamedTuple()
	{
		var knockOff = new SampleKnockOff();
		var tracking = knockOff.Calculate.OnCall((ko, name, value, flag) => { });
		ISampleService service = knockOff;

		service.Calculate("test", 100, true);

		var args = tracking.LastArgs;
		Assert.Equal("test", args.name);
		Assert.Equal(100, args.value);
		Assert.True(args.flag);
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
		Assert.Equal(3, knockOff.GetValue2.LastArg); // Last call was GetValue(3)
	}

	[Fact]
	public void Method_WithNullableReturn_NoUserMethod_ReturnsDefault()
	{
		var knockOff = new SampleKnockOff();
		var tracking = knockOff.GetOptional.OnCall(ko => null);
		ISampleService service = knockOff;

		var result = service.GetOptional();

		Assert.Null(result);
		Assert.True(tracking.WasCalled);
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
		var doSomethingTracking = knockOff.DoSomething.OnCall(ko => { });
		ISampleService service = knockOff;

		service.Name = "Test";
		service.GetValue(42);
		service.DoSomething();

		knockOff.Name.Reset();
		knockOff.GetValue2.Reset();
		knockOff.DoSomething.Reset();

		Assert.Equal(0, knockOff.Name.SetCount);
		Assert.Equal(0, knockOff.GetValue2.CallCount);
		// After reset, the tracking object is also reset
		Assert.False(doSomethingTracking.WasCalled);
	}

	[Fact]
	public void TupleDestructuring_Works()
	{
		var knockOff = new SampleKnockOff();
		var tracking = knockOff.Calculate.OnCall((ko, name, value, flag) => { });
		ISampleService service = knockOff;

		service.Calculate("test", 42, true);

		var (name, value, flag) = tracking.LastArgs;
		Assert.Equal("test", name);
		Assert.Equal(42, value);
		Assert.True(flag);
	}
}
