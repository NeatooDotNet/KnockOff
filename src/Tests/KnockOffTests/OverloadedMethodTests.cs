namespace KnockOff.Tests;

/// <summary>
/// Tests for method overload support in KnockOff.
/// </summary>
public class OverloadedMethodTests
{
	[Fact]
	public void OverloadedMethod_TracksAllCalls_InSingleList()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("data1");
		service.Process("data2", 5);
		service.Process("data3", 10, true);

		Assert.Equal(3, knockOff.Spy.Process.CallCount);
		Assert.True(knockOff.Spy.Process.WasCalled);
	}

	[Fact]
	public void OverloadedMethod_LastCallArgs_TracksNullableParams()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Call simplest overload - priority and async should be null
		service.Process("first");
		var lastArgs = knockOff.Spy.Process.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal("first", lastArgs.Value.data);
		Assert.Null(lastArgs.Value.priority);
		Assert.Null(lastArgs.Value.async);

		// Call middle overload - async should be null
		service.Process("second", 5);
		lastArgs = knockOff.Spy.Process.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal("second", lastArgs.Value.data);
		Assert.Equal(5, lastArgs.Value.priority);
		Assert.Null(lastArgs.Value.async);

		// Call full overload - all params present
		service.Process("third", 10, true);
		lastArgs = knockOff.Spy.Process.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal("third", lastArgs.Value.data);
		Assert.Equal(10, lastArgs.Value.priority);
		Assert.True(lastArgs.Value.async);
	}

	[Fact]
	public void OverloadedMethod_AllCalls_TracksAllOverloads()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		service.Process("a");
		service.Process("b", 1);
		service.Process("c", 2, false);

		var allCalls = knockOff.Spy.Process.AllCalls;
		Assert.Equal(3, allCalls.Count);

		Assert.Equal("a", allCalls[0].data);
		Assert.Null(allCalls[0].priority);

		Assert.Equal("b", allCalls[1].data);
		Assert.Equal(1, allCalls[1].priority);

		Assert.Equal("c", allCalls[2].data);
		Assert.Equal(2, allCalls[2].priority);
		Assert.False(allCalls[2].async);
	}

	[Fact]
	public void OverloadedMethod_OnCall_SetsCallbackForSpecificOverload()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		var overload0Called = false;
		var overload1Called = false;
		var overload2Called = false;

		knockOff.Spy.Process.OnCall((OverloadedServiceKnockOff.ProcessHandler.ProcessDelegate0)((ko, data) =>
		{
			overload0Called = true;
		}));

		knockOff.Spy.Process.OnCall((OverloadedServiceKnockOff.ProcessHandler.ProcessDelegate1)((ko, data, priority) =>
		{
			overload1Called = true;
		}));

		knockOff.Spy.Process.OnCall((OverloadedServiceKnockOff.ProcessHandler.ProcessDelegate2)((ko, data, priority, async) =>
		{
			overload2Called = true;
		}));

		// Call only the first overload
		service.Process("test");

		Assert.True(overload0Called);
		Assert.False(overload1Called);
		Assert.False(overload2Called);
	}

	[Fact]
	public void OverloadedMethod_WithReturn_OnCallReturnsValue()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Set callback for Calculate(int value)
		knockOff.Spy.Calculate.OnCall((OverloadedServiceKnockOff.CalculateHandler.CalculateDelegate0)((ko, value) => value * 2));

		// Set callback for Calculate(int a, int b)
		knockOff.Spy.Calculate.OnCall((OverloadedServiceKnockOff.CalculateHandler.CalculateDelegate1)((ko, a, b) => a + b));

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

		// Set callback for both overloads
		knockOff.Spy.GetByIdAsync.OnCall((OverloadedServiceKnockOff.GetByIdAsyncHandler.GetByIdAsyncDelegate0)(
			(ko, id) => Task.FromResult<User?>(user)));

		knockOff.Spy.GetByIdAsync.OnCall((OverloadedServiceKnockOff.GetByIdAsyncHandler.GetByIdAsyncDelegate1)(
			(ko, id, ct) => Task.FromResult<User?>(new User { Id = id, Name = "FromCt" })));

		// Call first overload
		var result1 = await service.GetByIdAsync(1);
		Assert.Equal(42, result1?.Id);

		// Call second overload
		using var cts = new CancellationTokenSource();
		var result2 = await service.GetByIdAsync(99, cts.Token);
		Assert.Equal(99, result2?.Id);
		Assert.Equal("FromCt", result2?.Name);

		// Verify tracking
		Assert.Equal(2, knockOff.Spy.GetByIdAsync.CallCount);
		var lastArgs = knockOff.Spy.GetByIdAsync.LastCallArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal(99, lastArgs.Value.id);
		Assert.NotNull(lastArgs.Value.cancellationToken); // Was provided
	}
#pragma warning restore xUnit1051

	[Fact]
	public void OverloadedMethod_Reset_ClearsAllState()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		knockOff.Spy.Process.OnCall((OverloadedServiceKnockOff.ProcessHandler.ProcessDelegate0)((ko, data) => { }));
		service.Process("test");
		service.Process("test2", 1);

		Assert.Equal(2, knockOff.Spy.Process.CallCount);

		knockOff.Spy.Process.Reset();

		Assert.Equal(0, knockOff.Spy.Process.CallCount);
		Assert.False(knockOff.Spy.Process.WasCalled);
		Assert.Null(knockOff.Spy.Process.LastCallArgs);
	}

	[Fact]
	public void OverloadedMethod_DifferentParamNames_TracksCorrectly()
	{
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		// Calculate has (int value) and (int a, int b) - different param names
		knockOff.Spy.Calculate.OnCall((OverloadedServiceKnockOff.CalculateHandler.CalculateDelegate0)((ko, v) => v));
		knockOff.Spy.Calculate.OnCall((OverloadedServiceKnockOff.CalculateHandler.CalculateDelegate1)((ko, a, b) => a));

		service.Calculate(5);
		service.Calculate(3, 7);

		var allCalls = knockOff.Spy.Calculate.AllCalls;
		Assert.Equal(2, allCalls.Count);

		// First call: value=5, a=null, b=null
		Assert.Equal(5, allCalls[0].value);
		Assert.Null(allCalls[0].a);
		Assert.Null(allCalls[0].b);

		// Second call: value=null, a=3, b=7
		Assert.Null(allCalls[1].value);
		Assert.Equal(3, allCalls[1].a);
		Assert.Equal(7, allCalls[1].b);
	}
}
