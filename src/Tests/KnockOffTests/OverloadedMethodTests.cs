namespace KnockOff.Tests;

/// <summary>
/// Tests for method overload support in KnockOff.
/// With the new design, overloaded methods share a single interceptor with multiple OnCall overloads.
/// The compiler resolves the correct delegate type based on the lambda parameter types.
/// </summary>
public class OverloadedMethodTests
{
	[Fact]
	public void OverloadedMethod_EachOverload_HasOwnTracking()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Each OnCall returns a separate tracking object for that overload
		var tracking1 = knockOff.Process.Call((data) => { });
		var tracking2 = knockOff.Process.Call((data, priority) => { });
		var tracking3 = knockOff.Process.Call((data, priority, async) => { });

		service.Process("data1");           // Process(string)
		service.Process("data2", 5);         // Process(string, int)
		service.Process("data3", 10, true);  // Process(string, int, bool)

		// Each overload has its own separate tracking
		tracking1.Verify(Called.Once);
		tracking2.Verify(Called.Once);
		tracking3.Verify(Called.Once);
	}

	[Fact]
	public void OverloadedMethod_SingleParam_TracksLastArg()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking = knockOff.Process.Call((data) => { });

		service.Process("first");
		service.Process("second");

		// Single param uses LastArg
		Assert.Equal("second", tracking.LastArg);
		tracking.Verify(Called.Exactly(2));
	}

	[Fact]
	public void OverloadedMethod_TwoParams_TracksLastArgs_WithProperTypes()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking = knockOff.Process.Call((data, priority) => { });

		service.Process("test", 42);

		// Two params uses LastArgs tuple with named fields
		var lastArgs = tracking.LastArgs!.Value;
		Assert.Equal("test", lastArgs.data);
		Assert.Equal(42, lastArgs.priority);
	}

	[Fact]
	public void OverloadedMethod_ThreeParams_TracksLastArgs_AllParams()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking = knockOff.Process.Call((data, priority, async) => { });

		service.Process("full", 100, true);

		var lastArgs = tracking.LastArgs!.Value;
		Assert.Equal("full", lastArgs.data);
		Assert.Equal(100, lastArgs.priority);
		Assert.True(lastArgs.@async);
	}

	[Fact]
	public void OverloadedMethod_AllCalls_TracksPerOverload()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking1 = knockOff.Process.Call((data) => { });
		var tracking2 = knockOff.Process.Call((data, priority) => { });
		var tracking3 = knockOff.Process.Call((data, priority, async) => { });

		service.Process("a");
		service.Process("b");
		service.Process("c", 1);
		service.Process("d", 2, false);

		// Each tracking object tracks its own overload
		tracking1.Verify(Called.Exactly(2));
		Assert.Equal("b", tracking1.LastArg); // Last call to this overload

		tracking2.Verify(Called.Once);
		Assert.Equal(("c", 1), tracking2.LastArgs);

		tracking3.Verify(Called.Once);
		Assert.Equal(("d", 2, false), tracking3.LastArgs);
	}

	[Fact]
	public void OverloadedMethod_OnCall_SimpleCallback()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Compiler resolves correct delegate type based on lambda signature
		var tracking1 = knockOff.Process.Call((data) => { });
		var tracking2 = knockOff.Process.Call((data, priority) => { });
		var tracking3 = knockOff.Process.Call((data, priority, async) => { });

		// Call only the first overload
		service.Process("test");

		// Verify only the first overload was called
		tracking1.Verify(Called.Once);
		tracking2.Verify(Called.Never);
		tracking3.Verify(Called.Never);
	}

	[Fact]
	public void OverloadedMethod_WithReturn_OnCallReturnsValue()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Set callback for Calculate(int value) - single param overload
		knockOff.Calculate.Return((value) => value * 2);

		// Set callback for Calculate(int a, int b) - two param overload
		knockOff.Calculate.Return((a, b) => a + b);

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

		// Set callback for GetByIdAsync(int id) - single param overload
		var tracking1 = knockOff.GetByIdAsync.Return((id) =>
			Task.FromResult<User?>(user));

		// Set callback for GetByIdAsync(int id, CancellationToken) - two param overload
		var tracking2 = knockOff.GetByIdAsync.Return((id, ct) =>
			Task.FromResult<User?>(new User { Id = id, Name = "FromCt" }));

		// Call first overload
		var result1 = await service.GetByIdAsync(1);
		Assert.Equal(42, result1?.Id);

		// Call second overload
		using var cts = new CancellationTokenSource();
		var result2 = await service.GetByIdAsync(99, cts.Token);
		Assert.Equal(99, result2?.Id);
		Assert.Equal("FromCt", result2?.Name);

		// Verify tracking - each overload tracked separately
		tracking1.Verify(Called.Once);
		tracking2.Verify(Called.Once);

		// LastArg for single param overload
		Assert.Equal(1, tracking1.LastArg);

		// LastArgs for two param overload
		var lastArgs = tracking2.LastArgs!.Value;
		Assert.Equal(99, lastArgs.id);
	}
#pragma warning restore xUnit1051

	[Fact]
	public void OverloadedMethod_Reset_ClearsAllOverloads()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking1 = knockOff.Process.Call((data) => { });
		var tracking2 = knockOff.Process.Call((data, priority) => { });

		service.Process("test");
		service.Process("test2", 1);

		tracking1.Verify(Called.Once);
		tracking2.Verify(Called.Once);

		// Reset clears all overloads
		knockOff.Process.Reset();

		tracking1.Verify(Called.Never);

		tracking2.Verify(Called.Never);
	}

	[Fact]
	public void OverloadedMethod_DifferentParamNames_CompilerResolvesCorrectly()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Calculate has (int value) and (int a, int b) - compiler resolves by param count
		var tracking1 = knockOff.Calculate.Return((value) => value);
		var tracking2 = knockOff.Calculate.Return((a, b) => a);

		service.Calculate(5);
		service.Calculate(3, 7);

		// Single param overload
		tracking1.Verify(Called.Once);
		Assert.Equal(5, tracking1.LastArg);

		// Two param overload
		tracking2.Verify(Called.Once);
		Assert.Equal((3, 7), tracking2.LastArgs);
	}

	[Fact]
	public void CanIdentifyWhichOverloadWasCalled()
	{
		// This test demonstrates tracking which overload was called
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking1 = knockOff.Process.Call((data) => { });
		var tracking2 = knockOff.Process.Call((data, priority) => { });
		var tracking3 = knockOff.Process.Call((data, priority, async) => { });

		service.Process("data", 42);  // This is the two-param overload

		// Clear identification of which overload was called
		tracking1.Verify(Called.Never);  // Process(string) - NOT called
		tracking2.Verify(Called.Once);   // Process(string, int) - CALLED!
		tracking3.Verify(Called.Never);  // Process(string, int, bool) - NOT called
	}
}
