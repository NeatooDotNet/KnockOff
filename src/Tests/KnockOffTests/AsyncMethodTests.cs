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
		var tracking = knockOff.DoWorkAsync.OnCall(() => Task.CompletedTask);
		IAsyncService service = knockOff;

		await service.DoWorkAsync();

		tracking.Verify(Times.Once);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfT_WithUserMethod_ReturnsUserResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueAsync(10);

		Assert.Equal(30, result); // User method multiplies by 3
		Assert.Equal(10, knockOff.GetValueAsync2.LastArg);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfNullableT_ReturnsDefault()
	{
		var knockOff = new AsyncServiceKnockOff();
		var tracking = knockOff.GetOptionalAsync.OnCall(() => Task.FromResult<string?>(null));
		IAsyncService service = knockOff;

		var result = await service.GetOptionalAsync();

		Assert.Null(result);
		tracking.Verify();
	}

	[Fact]
	public async Task AsyncMethod_ValueTask_ReturnsCompleted()
	{
		var knockOff = new AsyncServiceKnockOff();
		var tracking = knockOff.DoWorkValueTaskAsync.OnCall(() => default);
		IAsyncService service = knockOff;

		await service.DoWorkValueTaskAsync();

		tracking.Verify();
	}

	[Fact]
	public async Task AsyncMethod_ValueTaskOfT_WithUserMethod_ReturnsUserResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueValueTaskAsync(5);

		Assert.Equal(20, result); // User method multiplies by 4
		Assert.Equal(5, knockOff.GetValueValueTaskAsync2.LastArg);
	}
}
