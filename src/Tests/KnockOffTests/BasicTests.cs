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
		Assert.NotNull(knockOff.Spy);
	}

	[Fact]
	public void Property_Setter_TracksInvocation_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";

		Assert.Equal(1, knockOff.Spy.Name.SetCount);
		string? lastValue = knockOff.Spy.Name.LastSetValue;
		Assert.Equal("Test", lastValue);
	}

	[Fact]
	public void Property_Getter_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		var _ = service.Name;

		Assert.Equal(1, knockOff.Spy.Name.GetCount);
	}

	[Fact]
	public void Method_VoidNoParams_TracksInvocation()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.DoSomething();

		Assert.True(knockOff.Spy.DoSomething.WasCalled);
		Assert.Equal(1, knockOff.Spy.DoSomething.CallCount);
	}

	[Fact]
	public void Method_WithSingleParam_TracksArg_Typed()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetValue(42);

		Assert.Equal(84, result);
		Assert.Equal(1, knockOff.Spy.GetValue.CallCount);

		int? lastArg = knockOff.Spy.GetValue.LastCallArg;
		Assert.Equal(42, lastArg);
	}

	[Fact]
	public void Method_WithMultipleParams_TracksArgs_AsNamedTuple()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 100, true);

		var args = knockOff.Spy.Calculate.LastCallArgs;
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

		Assert.Equal(3, knockOff.Spy.GetValue.CallCount);
		Assert.Equal(3, knockOff.Spy.GetValue.AllCalls.Count);
		Assert.Equal(1, knockOff.Spy.GetValue.AllCalls[0]);
		Assert.Equal(2, knockOff.Spy.GetValue.AllCalls[1]);
		Assert.Equal(3, knockOff.Spy.GetValue.AllCalls[2]);
	}

	[Fact]
	public void Method_WithNullableReturn_NoUserMethod_ReturnsDefault()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		var result = service.GetOptional();

		Assert.Null(result);
		Assert.True(knockOff.Spy.GetOptional.WasCalled);
	}

	[Fact]
	public void AsInterface_ReturnsTypedInterface()
	{
		var knockOff = new SampleKnockOff();

		ISampleService service = knockOff.AsSampleService();

		service.Name = "Test";
		Assert.Equal(1, knockOff.Spy.Name.SetCount);
	}

	[Fact]
	public void Reset_ClearsTrackingState()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Name = "Test";
		service.GetValue(42);
		service.DoSomething();

		knockOff.Spy.Name.Reset();
		knockOff.Spy.GetValue.Reset();
		knockOff.Spy.DoSomething.Reset();

		Assert.Equal(0, knockOff.Spy.Name.SetCount);
		Assert.Equal(0, knockOff.Spy.GetValue.CallCount);
		Assert.Equal(0, knockOff.Spy.DoSomething.CallCount);
		Assert.False(knockOff.Spy.DoSomething.WasCalled);
	}

	[Fact]
	public void TupleDestructuring_Works()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		service.Calculate("test", 42, true);

		if (knockOff.Spy.Calculate.LastCallArgs is var (name, value, flag))
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
