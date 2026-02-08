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
		var tracking = knockOff.DoWorkAsync.Return(() => Task.CompletedTask);
		IAsyncService service = knockOff;

		await service.DoWorkAsync();

		tracking.Verify(Called.Once);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfT_WithStubOverride_ReturnsStubResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueAsync(10);

		Assert.Equal(30, result); // Stub override multiplies by 3
		Assert.Equal(10, knockOff.GetValueAsync.LastArg);
	}

	[Fact]
	public async Task AsyncMethod_TaskOfNullableT_ReturnsDefault()
	{
		var knockOff = new AsyncServiceKnockOff();
		var tracking = knockOff.GetOptionalAsync.Return(() => Task.FromResult<string?>(null));
		IAsyncService service = knockOff;

		var result = await service.GetOptionalAsync();

		Assert.Null(result);
		tracking.Verify();
	}

	[Fact]
	public async Task AsyncMethod_ValueTask_ReturnsCompleted()
	{
		var knockOff = new AsyncServiceKnockOff();
		var tracking = knockOff.DoWorkValueTaskAsync.Return(() => default);
		IAsyncService service = knockOff;

		await service.DoWorkValueTaskAsync();

		tracking.Verify();
	}

	[Fact]
	public async Task AsyncMethod_ValueTaskOfT_WithStubOverride_ReturnsStubResult()
	{
		var knockOff = new AsyncServiceKnockOff();
		IAsyncService service = knockOff;

		var result = await service.GetValueValueTaskAsync(5);

		Assert.Equal(20, result); // Stub override multiplies by 4
		Assert.Equal(5, knockOff.GetValueValueTaskAsync.LastArg);
	}
}
