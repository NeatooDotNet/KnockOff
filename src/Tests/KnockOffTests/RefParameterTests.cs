namespace KnockOff.Tests;

/// <summary>
/// Tests for ref parameter support in KnockOff.
/// Verifies that methods with ref parameters:
/// - Compile and implement the interface correctly
/// - Track the input value of ref parameters
/// - Allow callbacks to modify ref parameter values
/// </summary>
public class RefParameterTests
{
	[Fact]
	public void RefParameter_MethodWithRefParam_CompilesAndImplementsInterface()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		// This test verifies that the generated code compiles
		// The method signature must include 'ref' keyword
		Assert.NotNull(knockOff.Spy.Increment);
		Assert.NotNull(knockOff.Spy.TryUpdate);
	}

	[Fact]
	public void RefParameter_TracksInputValue()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		knockOff.Spy.Increment.OnCall((RefParameterServiceKnockOff ko, ref int value) =>
		{
			value++; // Modify the ref param
		});

		int myValue = 5;
		service.Increment(ref myValue);

		// The ref param should be tracked with its input value (5), not the modified value
		Assert.Equal(1, knockOff.Spy.Increment.CallCount);
		Assert.Equal(5, knockOff.Spy.Increment.LastCallArg);
	}

	[Fact]
	public void RefParameter_CallbackCanModifyValue()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		knockOff.Spy.Increment.OnCall((RefParameterServiceKnockOff ko, ref int value) =>
		{
			value = value * 2; // Double the value
		});

		int val = 10;
		service.Increment(ref val);

		Assert.Equal(20, val); // Value was modified by callback
	}

	[Fact]
	public void RefParameter_MixedParams_TracksAll()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		knockOff.Spy.TryUpdate.OnCall((RefParameterServiceKnockOff ko, string key, ref string value) =>
		{
			if (key == "valid")
			{
				value = value.ToUpper();
				return true;
			}
			return false;
		});

		string text = "hello";
		var success = service.TryUpdate("valid", ref text);

		Assert.True(success);
		Assert.Equal("HELLO", text);

		// Check tracking - should have both key and the original value
		Assert.Equal(1, knockOff.Spy.TryUpdate.CallCount);
		var args = knockOff.Spy.TryUpdate.LastCallArgs;
		Assert.NotNull(args);
		Assert.Equal("valid", args.Value.key);
		Assert.Equal("hello", args.Value.value); // Original value, before modification
	}

	[Fact]
	public void RefParameter_AllCalls_TracksAllInvocations()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		knockOff.Spy.Increment.OnCall((RefParameterServiceKnockOff ko, ref int value) => { value++; });

		int v1 = 1, v2 = 2, v3 = 3;
		service.Increment(ref v1);
		service.Increment(ref v2);
		service.Increment(ref v3);

		var allCalls = knockOff.Spy.Increment.AllCalls;
		Assert.Equal(3, allCalls.Count);
		Assert.Equal(1, allCalls[0]); // Original values
		Assert.Equal(2, allCalls[1]);
		Assert.Equal(3, allCalls[2]);

		// And the values were modified
		Assert.Equal(2, v1);
		Assert.Equal(3, v2);
		Assert.Equal(4, v3);
	}

	[Fact]
	public void RefParameter_NoCallback_VoidMethod_Succeeds()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		// Increment is a void method - no callback required
		int val = 100;
		service.Increment(ref val);

		// Value unchanged without callback
		Assert.Equal(100, val);

		// But call was still tracked
		Assert.Equal(1, knockOff.Spy.Increment.CallCount);
	}

	[Fact]
	public void RefParameter_Reset_ClearsTrackingState()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		knockOff.Spy.Increment.OnCall((RefParameterServiceKnockOff ko, ref int v) => { });

		int x = 1;
		service.Increment(ref x);
		service.Increment(ref x);

		Assert.Equal(2, knockOff.Spy.Increment.CallCount);

		knockOff.Spy.Increment.Reset();

		Assert.Equal(0, knockOff.Spy.Increment.CallCount);
		Assert.False(knockOff.Spy.Increment.WasCalled);
		Assert.Null(knockOff.Spy.Increment.LastCallArg);
		Assert.Empty(knockOff.Spy.Increment.AllCalls);
	}

	[Fact]
	public void RefParameter_WithReturnValue_CallbackProvidesReturn()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		knockOff.Spy.TryUpdate.OnCall((RefParameterServiceKnockOff ko, string key, ref string value) =>
		{
			if (key == "modify")
			{
				value = "modified-" + value;
				return true;
			}
			return false;
		});

		string text = "original";
		var result = service.TryUpdate("modify", ref text);

		Assert.True(result);
		Assert.Equal("modified-original", text);
	}

	[Fact]
	public void RefParameter_WasCalled_ReportsCorrectly()
	{
		var knockOff = new RefParameterServiceKnockOff();
		IRefParameterService service = knockOff;

		Assert.False(knockOff.Spy.Increment.WasCalled);

		knockOff.Spy.Increment.OnCall((RefParameterServiceKnockOff ko, ref int v) => { });
		int dummy = 0;
		service.Increment(ref dummy);

		Assert.True(knockOff.Spy.Increment.WasCalled);
	}
}
