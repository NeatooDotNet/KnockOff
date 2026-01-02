namespace KnockOff.Tests;

/// <summary>
/// Tests for out parameter support in KnockOff.
/// Verifies that methods with out parameters:
/// - Compile and implement the interface correctly
/// - Track only input parameters (not out params)
/// - Allow callbacks to set out parameter values
/// </summary>
public class OutParameterTests
{
	[Fact]
	public void OutParameter_MethodWithOutParam_CompilesAndImplementsInterface()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		// This test verifies that the generated code compiles
		// The method signature must include 'out' keyword
		Assert.NotNull(knockOff.Spy.TryGetValue);
		Assert.NotNull(knockOff.Spy.TryParse);
		Assert.NotNull(knockOff.Spy.GetData);
	}

	[Fact]
	public void OutParameter_TracksOnlyInputParams()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		// Set callback to provide return value
		knockOff.Spy.TryGetValue.OnCall((OutParameterServiceKnockOff ko, string key, out string? value) =>
		{
			value = "found";
			return true;
		});

		// Call the method
		service.TryGetValue("myKey", out var result);

		// Tracking should only show the input parameter 'key', not the out param 'value'
		Assert.Equal(1, knockOff.Spy.TryGetValue.CallCount);
		Assert.Equal("myKey", knockOff.Spy.TryGetValue.LastCallArg);
		Assert.Single(knockOff.Spy.TryGetValue.AllCalls);
		Assert.Equal("myKey", knockOff.Spy.TryGetValue.AllCalls[0]);
	}

	[Fact]
	public void OutParameter_CallbackCanSetOutValue()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.Spy.TryGetValue.OnCall((OutParameterServiceKnockOff ko, string key, out string? value) =>
		{
			if (key == "exists")
			{
				value = "theValue";
				return true;
			}
			value = null;
			return false;
		});

		// Test positive case
		var found = service.TryGetValue("exists", out var value1);
		Assert.True(found);
		Assert.Equal("theValue", value1);

		// Test negative case
		var notFound = service.TryGetValue("missing", out var value2);
		Assert.False(notFound);
		Assert.Null(value2);
	}

	[Fact]
	public void OutParameter_ValueTypeOutParam_CallbackSetsValue()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.Spy.TryParse.OnCall((OutParameterServiceKnockOff ko, string input, out int result) =>
		{
			if (int.TryParse(input, out result))
				return true;
			result = 0;
			return false;
		});

		// Test successful parse
		var success = service.TryParse("42", out var parsed);
		Assert.True(success);
		Assert.Equal(42, parsed);

		// Test failed parse
		var fail = service.TryParse("not a number", out var defaultVal);
		Assert.False(fail);
		Assert.Equal(0, defaultVal);

		// Verify tracking
		Assert.Equal(2, knockOff.Spy.TryParse.CallCount);
	}

	[Fact]
	public void OutParameter_VoidMethodWithOnlyOutParams_TracksCallCount()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.Spy.GetData.OnCall((OutParameterServiceKnockOff ko, out string name, out int count) =>
		{
			name = "TestName";
			count = 100;
		});

		// GetData has only out params, so there's nothing to track except call count
		service.GetData(out var name, out var count);

		Assert.Equal("TestName", name);
		Assert.Equal(100, count);
		Assert.Equal(1, knockOff.Spy.GetData.CallCount);
		Assert.True(knockOff.Spy.GetData.WasCalled);
	}

	[Fact]
	public void OutParameter_MultipleOutParams_AllSetByCallback()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.Spy.GetData.OnCall((OutParameterServiceKnockOff ko, out string name, out int count) =>
		{
			name = "Product";
			count = 42;
		});

		service.GetData(out var n, out var c);

		Assert.Equal("Product", n);
		Assert.Equal(42, c);
	}

	[Fact]
	public void OutParameter_NoCallback_OutParamsGetDefaultValues()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		// GetData is a void method, so no callback required - out params get defaults
		service.GetData(out var name, out var count);

		// Without a callback, out params should have their default values
		Assert.Null(name); // string defaults to null
		Assert.Equal(0, count); // int defaults to 0
	}

	[Fact]
	public void OutParameter_Reset_ClearsTrackingState()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.Spy.TryGetValue.OnCall((OutParameterServiceKnockOff ko, string key, out string? value) => { value = "x"; return true; });

		service.TryGetValue("key1", out _);
		service.TryGetValue("key2", out _);

		Assert.Equal(2, knockOff.Spy.TryGetValue.CallCount);

		knockOff.Spy.TryGetValue.Reset();

		Assert.Equal(0, knockOff.Spy.TryGetValue.CallCount);
		Assert.False(knockOff.Spy.TryGetValue.WasCalled);
		Assert.Null(knockOff.Spy.TryGetValue.LastCallArg);
		Assert.Empty(knockOff.Spy.TryGetValue.AllCalls);
	}

	[Fact]
	public void OutParameter_AllCalls_TracksInputParamsOnly()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.Spy.TryParse.OnCall((OutParameterServiceKnockOff ko, string input, out int result) => { result = 0; return false; });

		service.TryParse("first", out _);
		service.TryParse("second", out _);
		service.TryParse("third", out _);

		var allCalls = knockOff.Spy.TryParse.AllCalls;
		Assert.Equal(3, allCalls.Count);
		Assert.Equal("first", allCalls[0]);
		Assert.Equal("second", allCalls[1]);
		Assert.Equal("third", allCalls[2]);
	}
}
