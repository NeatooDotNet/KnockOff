namespace KnockOff.Tests;

/// <summary>
/// Tests for async method support (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;).
/// </summary>
public class AsyncMethodTests
{
	[Fact]
	public async Task AsyncMethod_Task_ReturnsCompletedTask()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		await service.DoWorkAsync();

		Assert.True(knockOff.Spy.DoWorkAsync.WasCalled);
		Assert.Equal(1, knockOff.Spy.DoWorkAsync.CallCount);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfT_WithUserMethod_ReturnsUserResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueAsync(10);

		Assert.Equal(30, result); // User method multiplies by 3
		Assert.Equal(10, knockOff.Spy.GetValueAsync.LastCallArg);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfNullableT_ReturnsDefault()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetOptionalAsync();

		Assert.Null(result);
		Assert.True(knockOff.Spy.GetOptionalAsync.WasCalled);
	}

	[Fact]
	public async Task AsyncMethod_ValueTask_ReturnsCompleted()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		await service.DoWorkValueTaskAsync();

		Assert.True(knockOff.Spy.DoWorkValueTaskAsync.WasCalled);
	}

	[Fact]
	public async Task AsyncMethod_ValueTaskOfT_WithUserMethod_ReturnsUserResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueValueTaskAsync(5);

		Assert.Equal(20, result); // User method multiplies by 4
		Assert.Equal(5, knockOff.Spy.GetValueValueTaskAsync.LastCallArg);
	}
}
