namespace KnockOff.Tests;

/// <summary>
/// Tests for simplified async callback overloads:
/// - OnCall(Func&lt;TParams..., TInnerType&gt;) for Task&lt;T&gt;/ValueTask&lt;T&gt; methods
/// - OnCall(Action&lt;TParams...&gt;) for Task/ValueTask methods
/// </summary>
public partial class AsyncCallbackSimplificationTests
{
	#region Test Interfaces and Stubs

	public interface ISimplifiedAsyncService
	{
		// Task<T> methods for simplified callback testing
		Task<string> GetStringAsync(int id);
		Task<int?> GetNullableIntAsync(string key);
		Task<User?> GetUserAsync(int id, string name);

		// ValueTask<T> methods for simplified callback testing
		ValueTask<string> GetCachedStringAsync(int id);
		ValueTask<double> GetDoubleAsync(int a, int b, int c);

		// Void async methods for Action callback testing
		Task SaveAsync(string data);
		Task ProcessAsync(int id, string name);
		ValueTask LogAsync(string message);
		ValueTask RecordAsync(int id, int count);

		// Zero-parameter async methods
		Task<string> GetDefaultStringAsync();
		Task RunAsync();
		ValueTask<int> GetDefaultIntAsync();
		ValueTask PingAsync();
	}

	[KnockOff]
	public partial class SimplifiedAsyncServiceKnockOff : ISimplifiedAsyncService { }

	#endregion

	#region Task<T> Simplified Callback Tests

	[Fact]
	public async Task TaskT_SimplifiedCallback_ReturnsCorrectValue()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Use simplified callback - returns unwrapped value, auto-wrapped in Task.FromResult
		stub.GetStringAsync.Return((id) => $"Result-{id}");

		var result = await service.GetStringAsync(42);

		Assert.Equal("Result-42", result);
	}

	[Fact]
	public async Task TaskT_SimplifiedCallback_ReturnsNull()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.GetNullableIntAsync.Return((key) => (int?)null);

		var result = await service.GetNullableIntAsync("test");

		Assert.Null(result);
	}

	[Fact]
	public async Task TaskT_SimplifiedCallback_MultipleParameters()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.GetUserAsync.Return((id, name) => new User { Id = id, Name = name });

		var result = await service.GetUserAsync(99, "Alice");

		Assert.NotNull(result);
		Assert.Equal(99, result.Id);
		Assert.Equal("Alice", result.Name);
	}

	[Fact]
	public async Task TaskT_SimplifiedCallback_ZeroParameters_ComputesEachCall()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Func<T> is useful when value must be computed each call
		// For static values, use OnCall(value) instead
		var callCount = 0;
		stub.GetDefaultStringAsync.Return(() => $"call-{++callCount}");

		var result1 = await service.GetDefaultStringAsync();
		var result2 = await service.GetDefaultStringAsync();
		var result3 = await service.GetDefaultStringAsync();

		Assert.Equal("call-1", result1);
		Assert.Equal("call-2", result2);
		Assert.Equal("call-3", result3);
	}

	[Fact]
	public async Task TaskT_SimplifiedCallback_RepeatsIndefinitely()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.GetStringAsync.Return((id) => "repeated");

		Assert.Equal("repeated", await service.GetStringAsync(1));
		Assert.Equal("repeated", await service.GetStringAsync(2));
		Assert.Equal("repeated", await service.GetStringAsync(3));
	}

	#endregion

	#region ValueTask<T> Simplified Callback Tests

	[Fact]
	public async Task ValueTaskT_SimplifiedCallback_ReturnsCorrectValue()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Use simplified callback - returns unwrapped value, auto-wrapped in new ValueTask<T>()
		stub.GetCachedStringAsync.Return((id) => $"Cached-{id}");

		var result = await service.GetCachedStringAsync(7);

		Assert.Equal("Cached-7", result);
	}

	[Fact]
	public async Task ValueTaskT_SimplifiedCallback_MultipleParameters()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.GetDoubleAsync.Return((a, b, c) => (double)(a + b + c));

		var result = await service.GetDoubleAsync(1, 2, 3);

		Assert.Equal(6.0, result);
	}

	[Fact]
	public async Task ValueTaskT_SimplifiedCallback_ZeroParameters_ComputesEachCall()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Func<T> is useful when value must be computed each call
		// For static values, use OnCall(value) instead
		var callCount = 0;
		stub.GetDefaultIntAsync.Return(() => ++callCount * 10);

		var result1 = await service.GetDefaultIntAsync();
		var result2 = await service.GetDefaultIntAsync();
		var result3 = await service.GetDefaultIntAsync();

		Assert.Equal(10, result1);
		Assert.Equal(20, result2);
		Assert.Equal(30, result3);
	}

	#endregion

	#region Void Async (Task) Simplified Callback Tests

	[Fact]
	public async Task Task_SimplifiedVoidCallback_ExecutesAction()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		string? capturedData = null;
		stub.SaveAsync.Call((data) => capturedData = data);

		await service.SaveAsync("test data");

		Assert.Equal("test data", capturedData);
	}

	[Fact]
	public async Task Task_SimplifiedVoidCallback_MultipleParameters()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		int capturedId = 0;
		string? capturedName = null;
		stub.ProcessAsync.Call((id, name) =>
		{
			capturedId = id;
			capturedName = name;
		});

		await service.ProcessAsync(42, "Alice");

		Assert.Equal(42, capturedId);
		Assert.Equal("Alice", capturedName);
	}

	[Fact]
	public async Task Task_SimplifiedVoidCallback_ZeroParameters()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		bool wasInvoked = false;
		stub.RunAsync.Call(() => wasInvoked = true);

		await service.RunAsync();

		Assert.True(wasInvoked);
	}

	[Fact]
	public async Task Task_SimplifiedVoidCallback_ReturnsCompletedTask()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.SaveAsync.Call((data) => { /* no-op */ });

		// Should not throw and should complete
		var task = service.SaveAsync("test");
		await task;

		Assert.True(task.IsCompleted);
	}

	#endregion

	#region Void Async (ValueTask) Simplified Callback Tests

	[Fact]
	public async Task ValueTask_SimplifiedVoidCallback_ExecutesAction()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		string? capturedMessage = null;
		stub.LogAsync.Call((message) => capturedMessage = message);

		await service.LogAsync("log entry");

		Assert.Equal("log entry", capturedMessage);
	}

	[Fact]
	public async Task ValueTask_SimplifiedVoidCallback_MultipleParameters()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		int capturedId = 0;
		int capturedCount = 0;
		stub.RecordAsync.Call((id, count) =>
		{
			capturedId = id;
			capturedCount = count;
		});

		await service.RecordAsync(5, 10);

		Assert.Equal(5, capturedId);
		Assert.Equal(10, capturedCount);
	}

	[Fact]
	public async Task ValueTask_SimplifiedVoidCallback_ZeroParameters()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		bool wasInvoked = false;
		stub.PingAsync.Call(() => wasInvoked = true);

		await service.PingAsync();

		Assert.True(wasInvoked);
	}

	#endregion

	#region Tracking Tests

	[Fact]
	public async Task SimplifiedCallback_TracksCallCount()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		var tracking = stub.GetStringAsync.Return((id) => "test");

		await service.GetStringAsync(1);
		await service.GetStringAsync(2);
		await service.GetStringAsync(3);

		tracking.Verify(Times.Exactly(3));
	}

	[Fact]
	public async Task SimplifiedCallback_TracksLastArg()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		var tracking = stub.GetStringAsync.Return((id) => "test");

		await service.GetStringAsync(1);
		await service.GetStringAsync(42);
		await service.GetStringAsync(99);

		Assert.Equal(99, tracking.LastArg);
	}

	[Fact]
	public async Task SimplifiedCallback_TracksLastArgs()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		var tracking = stub.GetUserAsync.Return((id, name) => new User { Id = id, Name = name });

		await service.GetUserAsync(1, "A");
		await service.GetUserAsync(2, "B");
		await service.GetUserAsync(3, "C");

		Assert.Equal((3, "C"), tracking.LastArgs);
	}

	[Fact]
	public async Task SimplifiedVoidCallback_TracksCallCount()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		var tracking = stub.SaveAsync.Call((data) => { });

		await service.SaveAsync("a");
		await service.SaveAsync("b");

		tracking.Verify(Times.Exactly(2));
	}

	[Fact]
	public async Task SimplifiedVoidCallback_TracksLastArg()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		var tracking = stub.SaveAsync.Call((data) => { });

		await service.SaveAsync("first");
		await service.SaveAsync("last");

		Assert.Equal("last", tracking.LastArg);
	}

	[Fact]
	public async Task SimplifiedCallback_VerifyWorks()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.GetStringAsync.Return((id) => "test").Verifiable();

		await service.GetStringAsync(1);

		stub.Verify();
	}

	[Fact]
	public async Task SimplifiedVoidCallback_VerifyWorks()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		stub.SaveAsync.Call((data) => { }).Verifiable();

		await service.SaveAsync("test");

		stub.Verify();
	}

	#endregion

	#region Mutual Exclusivity Tests

	[Fact]
	public async Task SimplifiedCallback_ClearsAsyncCallback()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Configure with async callback first
		stub.GetStringAsync.Return((id) => Task.FromResult("async"));
		Assert.Equal("async", await service.GetStringAsync(1));

		// Configure with simplified callback - should clear async callback
		stub.GetStringAsync.Return((id) => "simplified");
		Assert.Equal("simplified", await service.GetStringAsync(2));
	}

	[Fact]
	public async Task AsyncCallback_ClearsSimplifiedCallback()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Configure with simplified callback first
		stub.GetStringAsync.Return((id) => "simplified");
		Assert.Equal("simplified", await service.GetStringAsync(1));

		// Configure with async callback - should clear simplified callback
		stub.GetStringAsync.Return((id) => Task.FromResult("async"));
		Assert.Equal("async", await service.GetStringAsync(2));
	}

	[Fact]
	public async Task ValueOverload_ClearsSimplifiedCallback()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Configure with simplified callback first
		stub.GetStringAsync.Return((id) => "simplified");
		Assert.Equal("simplified", await service.GetStringAsync(1));

		// Configure with value - should clear simplified callback
		stub.GetStringAsync.Return("value");
		Assert.Equal("value", await service.GetStringAsync(2));
	}

	[Fact]
	public async Task SimplifiedCallback_ClearsValueOverload()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Configure with value first
		stub.GetStringAsync.Return("value");
		Assert.Equal("value", await service.GetStringAsync(1));

		// Configure with simplified callback - should clear value
		stub.GetStringAsync.Return((id) => "simplified");
		Assert.Equal("simplified", await service.GetStringAsync(2));
	}

	[Fact]
	public async Task Sequence_ClearsSimplifiedCallback()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Configure with simplified callback first
		stub.GetStringAsync.Return((id) => "simplified");
		Assert.Equal("simplified", await service.GetStringAsync(1));

		// Configure with OnCall().ThenReturn() sequence - should clear simplified callback
		stub.GetStringAsync.Return((id) => Task.FromResult("seq1"))
			.ThenReturn((id) => Task.FromResult("seq2"));
		Assert.Equal("seq1", await service.GetStringAsync(2));
		Assert.Equal("seq2", await service.GetStringAsync(3));
	}

	[Fact]
	public async Task SimplifiedVoidCallback_ClearsAsyncCallback()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		bool asyncCalled = false;
		bool simplifiedCalled = false;

		// Configure with async callback first
		stub.SaveAsync.Return((data) => { asyncCalled = true; return Task.CompletedTask; });
		await service.SaveAsync("test1");
		Assert.True(asyncCalled);

		asyncCalled = false;

		// Configure with simplified void callback - should clear async callback
		stub.SaveAsync.Call((data) => simplifiedCalled = true);
		await service.SaveAsync("test2");
		Assert.True(simplifiedCalled);
		Assert.False(asyncCalled); // async callback should not be called
	}

	[Fact]
	public async Task AsyncCallback_ClearsSimplifiedVoidCallback()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		bool asyncCalled = false;
		bool simplifiedCalled = false;

		// Configure with simplified void callback first
		stub.SaveAsync.Call((data) => simplifiedCalled = true);
		await service.SaveAsync("test1");
		Assert.True(simplifiedCalled);

		simplifiedCalled = false;

		// Configure with async callback - should clear simplified void callback
		stub.SaveAsync.Return((data) => { asyncCalled = true; return Task.CompletedTask; });
		await service.SaveAsync("test2");
		Assert.True(asyncCalled);
		Assert.False(simplifiedCalled); // simplified callback should not be called
	}

	#endregion

	#region Source Delegation Tests

	[Fact]
	public async Task SourceDelegation_WorksWhenSimplifiedNotConfigured()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Set up source
		var source = new FakeAsyncService();
		stub.Source(source);

		// No callback configured - should delegate to source
		var result = await service.GetStringAsync(42);

		Assert.Equal("Source-42", result);
	}

	[Fact]
	public async Task SimplifiedCallback_TakesPriorityOverSource()
	{
		var stub = new SimplifiedAsyncServiceKnockOff();
		ISimplifiedAsyncService service = stub;

		// Set up source
		var source = new FakeAsyncService();
		stub.Source(source);

		// Configure simplified callback
		stub.GetStringAsync.Return((id) => "Callback");

		// Callback should take priority over source
		var result = await service.GetStringAsync(42);

		Assert.Equal("Callback", result);
	}

	private class FakeAsyncService : ISimplifiedAsyncService
	{
		public Task<string> GetStringAsync(int id) => Task.FromResult($"Source-{id}");
		public Task<int?> GetNullableIntAsync(string key) => Task.FromResult<int?>(null);
		public Task<User?> GetUserAsync(int id, string name) => Task.FromResult<User?>(null);
		public ValueTask<string> GetCachedStringAsync(int id) => new($"Source-{id}");
		public ValueTask<double> GetDoubleAsync(int a, int b, int c) => new(0.0);
		public Task SaveAsync(string data) => Task.CompletedTask;
		public Task ProcessAsync(int id, string name) => Task.CompletedTask;
		public ValueTask LogAsync(string message) => default;
		public ValueTask RecordAsync(int id, int count) => default;
		public Task<string> GetDefaultStringAsync() => Task.FromResult("default");
		public Task RunAsync() => Task.CompletedTask;
		public ValueTask<int> GetDefaultIntAsync() => new(0);
		public ValueTask PingAsync() => default;
	}

	#endregion

	#region Three Patterns Tests

	// Standalone pattern is tested above via SimplifiedAsyncServiceKnockOff

	[KnockOff<ISimplifiedAsyncService>]
	public partial class InlineInterfaceAsyncStub { }

	[Fact]
	public async Task InlineInterface_SimplifiedCallback_Works()
	{
		var stub = new InlineInterfaceAsyncStub.Stubs.ISimplifiedAsyncService();

		stub.GetStringAsync.Return((id) => $"Inline-{id}");

		ISimplifiedAsyncService service = stub;
		var result = await service.GetStringAsync(5);

		Assert.Equal("Inline-5", result);
	}

	[Fact]
	public async Task InlineInterface_SimplifiedVoidCallback_Works()
	{
		var stub = new InlineInterfaceAsyncStub.Stubs.ISimplifiedAsyncService();

		string? captured = null;
		stub.SaveAsync.Call((data) => captured = data);

		ISimplifiedAsyncService service = stub;
		await service.SaveAsync("test");

		Assert.Equal("test", captured);
	}

	// Note: Inline class pattern tests are covered in separate integration tests.
	// The simplified async callback feature works identically across all three patterns
	// since the renderer logic is shared.

	#endregion
}
