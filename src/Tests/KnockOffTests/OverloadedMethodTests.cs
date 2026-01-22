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
		var tracking1 = knockOff.Process.OnCall((data) => { });
		var tracking2 = knockOff.Process.OnCall((data, priority) => { });
		var tracking3 = knockOff.Process.OnCall((data, priority, async) => { });

		service.Process("data1");           // Process(string)
		service.Process("data2", 5);         // Process(string, int)
		service.Process("data3", 10, true);  // Process(string, int, bool)

		// Each overload has its own separate tracking
		tracking1.Verify(Times.Once);
		tracking2.Verify(Times.Once);
		tracking3.Verify(Times.Once);
	}

	[Fact]
	public void OverloadedMethod_SingleParam_TracksLastArg()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking = knockOff.Process.OnCall((data) => { });

		service.Process("first");
		service.Process("second");

		// Single param uses LastArg
		Assert.Equal("second", tracking.LastArg);
		tracking.Verify(Times.Exactly(2));
	}

	[Fact]
	public void OverloadedMethod_TwoParams_TracksLastArgs_WithProperTypes()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking = knockOff.Process.OnCall((data, priority) => { });

		service.Process("test", 42);

		// Two params uses LastArgs tuple with proper types
		var lastArgs = tracking.LastArgs;
		Assert.Equal("test", lastArgs.data);
		Assert.Equal(42, lastArgs.priority);
	}

	[Fact]
	public void OverloadedMethod_ThreeParams_TracksLastArgs_AllParams()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking = knockOff.Process.OnCall((data, priority, async) => { });

		service.Process("full", 100, true);

		var lastArgs = tracking.LastArgs;
		Assert.Equal("full", lastArgs.data);
		Assert.Equal(100, lastArgs.priority);
		Assert.True(lastArgs.async);
	}

	[Fact]
	public void OverloadedMethod_AllCalls_TracksPerOverload()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking1 = knockOff.Process.OnCall((data) => { });
		var tracking2 = knockOff.Process.OnCall((data, priority) => { });
		var tracking3 = knockOff.Process.OnCall((data, priority, async) => { });

		service.Process("a");
		service.Process("b");
		service.Process("c", 1);
		service.Process("d", 2, false);

		// Each tracking object tracks its own overload
		tracking1.Verify(Times.Exactly(2));
		Assert.Equal("b", tracking1.LastArg); // Last call to this overload

		tracking2.Verify(Times.Once);
		Assert.Equal(("c", 1), tracking2.LastArgs);

		tracking3.Verify(Times.Once);
		Assert.Equal(("d", 2, false), tracking3.LastArgs);
	}

	[Fact]
	public void OverloadedMethod_OnCall_SimpleCallback()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var overload1Called = false;
		var overload2Called = false;
		var overload3Called = false;

		// Compiler resolves correct delegate type based on lambda signature
		knockOff.Process.OnCall((data) =>
		{
			overload1Called = true;
		});

		knockOff.Process.OnCall((data, priority) =>
		{
			overload2Called = true;
		});

		knockOff.Process.OnCall((data, priority, async) =>
		{
			overload3Called = true;
		});

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

		// Set callback for Calculate(int value) - single param overload
		knockOff.Calculate.OnCall((value) => value * 2);

		// Set callback for Calculate(int a, int b) - two param overload
		knockOff.Calculate.OnCall((a, b) => a + b);

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
		var tracking1 = knockOff.GetByIdAsync.OnCall((id) =>
			Task.FromResult<User?>(user));

		// Set callback for GetByIdAsync(int id, CancellationToken) - two param overload
		var tracking2 = knockOff.GetByIdAsync.OnCall((id, ct) =>
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
		tracking1.Verify(Times.Once);
		tracking2.Verify(Times.Once);

		// LastArg for single param overload
		Assert.Equal(1, tracking1.LastArg);

		// LastArgs for two param overload
		var lastArgs = tracking2.LastArgs;
		Assert.Equal(99, lastArgs.id);
	}
#pragma warning restore xUnit1051

	[Fact]
	public void OverloadedMethod_Reset_ClearsAllOverloads()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking1 = knockOff.Process.OnCall((data) => { });
		var tracking2 = knockOff.Process.OnCall((data, priority) => { });

		service.Process("test");
		service.Process("test2", 1);

		tracking1.Verify(Times.Once);
		tracking2.Verify(Times.Once);

		// Reset clears all overloads
		knockOff.Process.Reset();

		tracking1.Verify(Times.Never);

		tracking2.Verify(Times.Never);
	}

	[Fact]
	public void OverloadedMethod_DifferentParamNames_CompilerResolvesCorrectly()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Calculate has (int value) and (int a, int b) - compiler resolves by param count
		var tracking1 = knockOff.Calculate.OnCall((value) => value);
		var tracking2 = knockOff.Calculate.OnCall((a, b) => a);

		service.Calculate(5);
		service.Calculate(3, 7);

		// Single param overload
		tracking1.Verify(Times.Once);
		Assert.Equal(5, tracking1.LastArg);

		// Two param overload
		tracking2.Verify(Times.Once);
		Assert.Equal((3, 7), tracking2.LastArgs);
	}

	[Fact]
	public void CanIdentifyWhichOverloadWasCalled()
	{
		// This test demonstrates tracking which overload was called
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var tracking1 = knockOff.Process.OnCall((data) => { });
		var tracking2 = knockOff.Process.OnCall((data, priority) => { });
		var tracking3 = knockOff.Process.OnCall((data, priority, async) => { });

		service.Process("data", 42);  // This is the two-param overload

		// Clear identification of which overload was called
		tracking1.Verify(Times.Never);  // Process(string) - NOT called
		tracking2.Verify(Times.Once);   // Process(string, int) - CALLED!
		tracking3.Verify(Times.Never);  // Process(string, int, bool) - NOT called
	}
}
