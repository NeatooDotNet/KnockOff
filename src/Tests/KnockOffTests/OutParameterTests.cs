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
		Assert.NotNull(knockOff.TryGetValue);
		Assert.NotNull(knockOff.TryParse);
		Assert.NotNull(knockOff.GetData);
	}

	[Fact]
	public void OutParameter_TracksOnlyInputParams()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		// Set callback to provide return value
		var tracking = knockOff.TryGetValue.Call((string key, out string? value) =>
		{
			value = "found";
			return true;
		});

		// Call the method
		service.TryGetValue("myKey", out var result);

		// Tracking should only show the input parameter 'key', not the out param 'value'
		tracking.Verify(Called.Once);
		Assert.Equal("myKey", tracking.LastArg);
	}

	[Fact]
	public void OutParameter_CallbackCanSetOutValue()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.TryGetValue.Call((string key, out string? value) =>
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

		var tracking = knockOff.TryParse.Call((string input, out int result) =>
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
		tracking.Verify(Called.Exactly(2));
	}

	[Fact]
	public void OutParameter_VoidMethodWithOnlyOutParams_TracksCallCount()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		var tracking = knockOff.GetData.Call((out string name, out int count) =>
		{
			name = "TestName";
			count = 100;
		});

		// GetData has only out params, so there's nothing to track except call count
		service.GetData(out var name, out var count);

		Assert.Equal("TestName", name);
		Assert.Equal(100, count);
		tracking.Verify(Called.Once);
	}

	[Fact]
	public void OutParameter_MultipleOutParams_AllSetByCallback()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.GetData.Call((out string name, out int count) =>
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

		// GetData is a void method - out params get defaults when no callback configured
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

		var tracking = knockOff.TryGetValue.Call((string key, out string? value) => { value = "x"; return true; });

		service.TryGetValue("key1", out _);
		service.TryGetValue("key2", out _);

		tracking.Verify(Called.Exactly(2));

		knockOff.TryGetValue.Reset();

		tracking.Verify(Called.Never);
	}

	[Fact]
	public void OutParameter_LastArg_TracksInputParamsOnly()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		var tracking = knockOff.TryParse.Call((string input, out int result) => { result = 0; return false; });

		service.TryParse("first", out _);
		service.TryParse("second", out _);
		service.TryParse("third", out _);

		tracking.Verify(Called.Exactly(3));
		Assert.Equal("third", tracking.LastArg); // Last call
	}

	[Fact]
	public void OutParameter_Unconfigured_ReturnValueAndOutGetDefaults()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		var found = service.TryGetValue("key", out string? value);

		Assert.False(found);  // default(bool)
		Assert.Null(value);   // default(string)
	}

	[Fact]
	public void OutParameter_Sequence_ThenReturn()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.TryGetValue
			.Call((string key, out string? value) => { value = "first"; return true; })
			.ThenReturn((string key, out string? value) => { value = "second"; return true; });

		service.TryGetValue("a", out var v1);
		service.TryGetValue("b", out var v2);
		service.TryGetValue("c", out var v3); // repeats last

		Assert.Equal("first", v1);
		Assert.Equal("second", v2);
		Assert.Equal("second", v3);
	}

	[Fact]
	public void OutParameter_Verify_ThrowsWhenNotSatisfied()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;

		knockOff.TryGetValue.Call((string key, out string? value) =>
		{
			value = default;
			return false;
		});

		service.TryGetValue("key1", out _);

		Assert.Throws<VerificationException>(() =>
			knockOff.TryGetValue.Verify(Called.Exactly(2)));
	}

	[Fact]
	public void OutParameter_BackedByDictionary()
	{
		var knockOff = new OutParameterServiceKnockOff();
		IOutParameterService service = knockOff;
		var data = new Dictionary<string, string> { ["key1"] = "value1" };

		knockOff.TryGetValue.Call((string key, out string? value) =>
		{
			if (data.TryGetValue(key, out var v))
			{
				value = v;
				return true;
			}
			value = null;
			return false;
		});

		Assert.True(service.TryGetValue("key1", out var v1));
		Assert.Equal("value1", v1);

		Assert.False(service.TryGetValue("key2", out var v2));
		Assert.Null(v2);
	}
}
