namespace KnockOff.Tests;

/// <summary>
/// Tests for method overload support in KnockOff.
/// With the new design, each overload gets its own handler: Process1, Process2, Process3, etc. (1-based).
/// This allows clear tracking of which specific overload was called.
/// </summary>
public class OverloadedMethodTests
{
	[Fact]
	public void OverloadedMethod_EachOverload_HasOwnCallCount()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("data1");           // Process1 - Process(string)
		service.Process("data2", 5);         // Process2 - Process(string, int)
		service.Process("data3", 10, true);  // Process3 - Process(string, int, bool)

		// Each overload has its own separate tracking (1-based numbering)
		Assert.Equal(1, knockOff.IOverloadedService.Process1.CallCount);
		Assert.Equal(1, knockOff.IOverloadedService.Process2.CallCount);
		Assert.Equal(1, knockOff.IOverloadedService.Process3.CallCount);
	}

	[Fact]
	public void OverloadedMethod_Process1_TracksLastCallArg()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("first");
		service.Process("second");

		// Process1 takes single param - uses LastCallArg (not LastCallArgs)
		Assert.Equal("second", knockOff.IOverloadedService.Process1.LastCallArg);
		Assert.Equal(2, knockOff.IOverloadedService.Process1.CallCount);
	}

	[Fact]
	public void OverloadedMethod_Process2_TracksLastCallArgs_WithProperTypes()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("test", 42);

		// Process2 takes two params - uses LastCallArgs tuple with proper (non-nullable) types
		var lastArgs = knockOff.IOverloadedService.Process2.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal("test", lastArgs.Value.data);
		Assert.Equal(42, lastArgs.Value.priority); // int, not int?
	}

	[Fact]
	public void OverloadedMethod_Process3_TracksLastCallArgs_AllParams()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("full", 100, true);

		var lastArgs = knockOff.IOverloadedService.Process3.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal("full", lastArgs.Value.data);
		Assert.Equal(100, lastArgs.Value.priority);
		Assert.True(lastArgs.Value.async);
	}

	[Fact]
	public void OverloadedMethod_AllCalls_TracksPerOverload()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("a");
		service.Process("b");
		service.Process("c", 1);
		service.Process("d", 2, false);

		// Process1 - tracks Process(string) calls
		Assert.Equal(2, knockOff.IOverloadedService.Process1.CallCount);
		Assert.Equal("b", knockOff.IOverloadedService.Process1.LastCallArg); // Last call to this overload

		// Process2 - tracks Process(string, int) calls
		Assert.Equal(1, knockOff.IOverloadedService.Process2.CallCount);
		Assert.Equal(("c", 1), knockOff.IOverloadedService.Process2.LastCallArgs);

		// Process3 - tracks Process(string, int, bool) calls
		Assert.Equal(1, knockOff.IOverloadedService.Process3.CallCount);
		Assert.Equal(("d", 2, false), knockOff.IOverloadedService.Process3.LastCallArgs);
	}

	[Fact]
	public void OverloadedMethod_OnCall_SimpleCallback_NoDelegateCasting()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var overload1Called = false;
		var overload2Called = false;
		var overload3Called = false;

		// Simple property assignment - no delegate casting needed!
		knockOff.IOverloadedService.Process1.OnCall = (ko, data) =>
		{
			overload1Called = true;
		};

		knockOff.IOverloadedService.Process2.OnCall = (ko, data, priority) =>
		{
			overload2Called = true;
		};

		knockOff.IOverloadedService.Process3.OnCall = (ko, data, priority, async) =>
		{
			overload3Called = true;
		};

		// Call only the first overload
		service.Process("test");

		Assert.True(overload1Called);
		Assert.False(overload2Called);
		Assert.False(overload3Called);
	}

	[Fact]
	public void OverloadedMethod_WithReturn_OnCallReturnsValue()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Set callback for Calculate(int value) - overload 1
		knockOff.IOverloadedService.Calculate1.OnCall = (ko, value) => value * 2;

		// Set callback for Calculate(int a, int b) - overload 2
		knockOff.IOverloadedService.Calculate2.OnCall = (ko, a, b) => a + b;

		Assert.Equal(10, service.Calculate(5));      // 5 * 2 = 10
		Assert.Equal(8, service.Calculate(3, 5));    // 3 + 5 = 8
	}

#pragma warning disable xUnit1051 // Testing CancellationToken overload specifically
	[Fact]
	public async Task OverloadedAsyncMethod_TracksCorrectly()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var user = new User { Id = 42, Name = "Test" };

		// Set callback for GetByIdAsync(int id) - overload 1
		knockOff.IOverloadedService.GetByIdAsync1.OnCall = (ko, id) =>
			Task.FromResult<User?>(user);

		// Set callback for GetByIdAsync(int id, CancellationToken) - overload 2
		knockOff.IOverloadedService.GetByIdAsync2.OnCall = (ko, id, ct) =>
			Task.FromResult<User?>(new User { Id = id, Name = "FromCt" });

		// Call first overload
		var result1 = await service.GetByIdAsync(1);
		Assert.Equal(42, result1?.Id);

		// Call second overload
		using var cts = new CancellationTokenSource();
		var result2 = await service.GetByIdAsync(99, cts.Token);
		Assert.Equal(99, result2?.Id);
		Assert.Equal("FromCt", result2?.Name);

		// Verify tracking - each overload tracked separately
		Assert.Equal(1, knockOff.IOverloadedService.GetByIdAsync1.CallCount);
		Assert.Equal(1, knockOff.IOverloadedService.GetByIdAsync2.CallCount);

		// LastCallArg for GetByIdAsync1 (single param)
		Assert.Equal(1, knockOff.IOverloadedService.GetByIdAsync1.LastCallArg);

		// LastCallArgs for GetByIdAsync2 (two params)
		var lastArgs = knockOff.IOverloadedService.GetByIdAsync2.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal(99, lastArgs.Value.id);
	}
#pragma warning restore xUnit1051

	[Fact]
	public void OverloadedMethod_Reset_ClearsOnlyThatOverload()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		knockOff.IOverloadedService.Process1.OnCall = (ko, data) => { };
		service.Process("test");
		service.Process("test2", 1);

		Assert.Equal(1, knockOff.IOverloadedService.Process1.CallCount);
		Assert.Equal(1, knockOff.IOverloadedService.Process2.CallCount);

		// Reset only Process1
		knockOff.IOverloadedService.Process1.Reset();

		Assert.Equal(0, knockOff.IOverloadedService.Process1.CallCount);
		Assert.False(knockOff.IOverloadedService.Process1.WasCalled);
		Assert.Null(knockOff.IOverloadedService.Process1.LastCallArg);

		// Process2 is NOT affected
		Assert.Equal(1, knockOff.IOverloadedService.Process2.CallCount);
		Assert.True(knockOff.IOverloadedService.Process2.WasCalled);
	}

	[Fact]
	public void OverloadedMethod_DifferentParamNames_EachOverloadHasProperTypes()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Calculate has (int value) and (int a, int b) - completely separate handlers now
		knockOff.IOverloadedService.Calculate1.OnCall = (ko, value) => value;
		knockOff.IOverloadedService.Calculate2.OnCall = (ko, a, b) => a;

		service.Calculate(5);
		service.Calculate(3, 7);

		// Calculate1 - single param overload
		Assert.Equal(1, knockOff.IOverloadedService.Calculate1.CallCount);
		Assert.Equal(5, knockOff.IOverloadedService.Calculate1.LastCallArg);

		// Calculate2 - two param overload
		Assert.Equal(1, knockOff.IOverloadedService.Calculate2.CallCount);
		Assert.Equal((3, 7), knockOff.IOverloadedService.Calculate2.LastCallArgs);
	}

	[Fact]
	public void CanIdentifyWhichOverloadWasCalled()
	{
		// This test demonstrates the key benefit of the new design:
		// You can now tell exactly which overload was called!
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("data", 42);  // This is Process2 (string, int)

		// Clear identification of which overload was called
		Assert.False(knockOff.IOverloadedService.Process1.WasCalled);  // Process(string) - NOT called
		Assert.True(knockOff.IOverloadedService.Process2.WasCalled);   // Process(string, int) - CALLED!
		Assert.False(knockOff.IOverloadedService.Process3.WasCalled);  // Process(string, int, bool) - NOT called
	}
}
