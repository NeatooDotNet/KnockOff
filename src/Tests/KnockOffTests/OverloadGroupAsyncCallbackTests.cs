namespace KnockOff.Tests;

/// <summary>
/// Tests for simplified async callbacks on overload groups.
/// When methods have multiple overloads, each overload gets its own simplified callback:
/// - OnCall(Func&lt;TParams..., TInnerType&gt;) for Task&lt;T&gt;/ValueTask&lt;T&gt; methods
/// - OnCall(Action&lt;TParams...&gt;) for Task/ValueTask methods
/// </summary>
public partial class OverloadGroupAsyncCallbackTests
{
	#region Test Interfaces and Stubs

	/// <summary>
	/// Test interface with async overloaded methods for Task&lt;T&gt; simplified callbacks.
	/// </summary>
	public interface IAsyncOverloadService
	{
		// Task<T> overloads with different signatures
		Task<User?> GetByIdAsync(int id);
		Task<User?> GetByIdAsync(int id, CancellationToken ct);
		Task<User?> GetByIdAsync(int id, string includeFields);

		// ValueTask<T> overloads
		ValueTask<string> GetCachedAsync(string key);
		ValueTask<string> GetCachedAsync(string key, bool refresh);

		// Task (void) overloads
		Task SaveAsync(User user);
		Task SaveAsync(User user, CancellationToken ct);

		// ValueTask (void) overloads
		ValueTask LogAsync(string message);
		ValueTask LogAsync(string message, int level);
	}

	[KnockOff]
	public partial class AsyncOverloadServiceKnockOff : IAsyncOverloadService { }

	#endregion

	#region Task<T> Overload Simplified Callback Tests

	[Fact]
	public async Task TaskT_Overload_SimplifiedCallback_SingleParam()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var user = new User { Id = 42, Name = "Test" };

		// Use simplified callback - returns unwrapped User?, auto-wrapped in Task.FromResult
		stub.GetByIdAsync.Return((int id) => user);

		var result = await service.GetByIdAsync(42);

		Assert.NotNull(result);
		Assert.Equal(42, result.Id);
		Assert.Equal("Test", result.Name);
	}

	[Fact]
	public async Task TaskT_Overload_SimplifiedCallback_TwoParams()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		// Configure two-param overload with simplified callback
		stub.GetByIdAsync.Return((int id, CancellationToken ct) => new User { Id = id, Name = "FromCT" });

		using var cts = new CancellationTokenSource();
		var result = await service.GetByIdAsync(99, cts.Token);

		Assert.NotNull(result);
		Assert.Equal(99, result.Id);
		Assert.Equal("FromCT", result.Name);
	}

	[Fact]
	public async Task TaskT_Overload_SimplifiedCallback_ReturnsNull()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		// Configure to return null
		stub.GetByIdAsync.Return((int id) => (User?)null);

		var result = await service.GetByIdAsync(1);

		Assert.Null(result);
	}

	[Fact]
	public async Task TaskT_Overload_SimplifiedCallback_EachOverloadConfiguredSeparately()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		// Configure each overload with different simplified callbacks
		stub.GetByIdAsync.Return((int id) => new User { Id = id, Name = "Single" });
		stub.GetByIdAsync.Return((int id, CancellationToken ct) => new User { Id = id * 2, Name = "WithToken" });
		stub.GetByIdAsync.Return((int id, string includeFields) => new User { Id = id * 3, Name = includeFields });

		// Call each overload
		var result1 = await service.GetByIdAsync(10);
		using var cts = new CancellationTokenSource();
		var result2 = await service.GetByIdAsync(10, cts.Token);
		var result3 = await service.GetByIdAsync(10, "name,email");

		// Each overload returns different result
		Assert.Equal(10, result1?.Id);
		Assert.Equal("Single", result1?.Name);

		Assert.Equal(20, result2?.Id);
		Assert.Equal("WithToken", result2?.Name);

		Assert.Equal(30, result3?.Id);
		Assert.Equal("name,email", result3?.Name);
	}

	#endregion

	#region ValueTask<T> Overload Simplified Callback Tests

	[Fact]
	public async Task ValueTaskT_Overload_SimplifiedCallback_SingleParam()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		stub.GetCachedAsync.Return((string key) => $"cached-{key}");

		var result = await service.GetCachedAsync("test");

		Assert.Equal("cached-test", result);
	}

	[Fact]
	public async Task ValueTaskT_Overload_SimplifiedCallback_TwoParams()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		stub.GetCachedAsync.Return((string key, bool refresh) =>
			refresh ? $"fresh-{key}" : $"cached-{key}");

		var cached = await service.GetCachedAsync("data", false);
		var fresh = await service.GetCachedAsync("data", true);

		Assert.Equal("cached-data", cached);
		Assert.Equal("fresh-data", fresh);
	}

	[Fact]
	public async Task ValueTaskT_Overload_SimplifiedCallback_BothOverloadsConfigured()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		stub.GetCachedAsync.Return((string key) => "single-param");
		stub.GetCachedAsync.Return((string key, bool refresh) => "two-params");

		var result1 = await service.GetCachedAsync("key");
		var result2 = await service.GetCachedAsync("key", true);

		Assert.Equal("single-param", result1);
		Assert.Equal("two-params", result2);
	}

	#endregion

	#region Task (Void) Overload Simplified Callback Tests

	[Fact]
	public async Task Task_Overload_SimplifiedVoidCallback_SingleParam()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		User? savedUser = null;
		stub.SaveAsync.Call((User user) => { savedUser = user; });

		var user = new User { Id = 1, Name = "Test" };
		await service.SaveAsync(user);

		Assert.NotNull(savedUser);
		Assert.Equal(1, savedUser.Id);
	}

	[Fact]
	public async Task Task_Overload_SimplifiedVoidCallback_TwoParams()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		User? savedUser = null;
		bool tokenPassed = false;

		stub.SaveAsync.Call((User user, CancellationToken ct) =>
		{
			savedUser = user;
			tokenPassed = !ct.IsCancellationRequested;
		});

		using var cts = new CancellationTokenSource();
		await service.SaveAsync(new User { Id = 2, Name = "Test" }, cts.Token);

		Assert.NotNull(savedUser);
		Assert.Equal(2, savedUser.Id);
		Assert.True(tokenPassed);
	}

	[Fact]
	public async Task Task_Overload_SimplifiedVoidCallback_BothConfigured()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var calls = new List<string>();

		stub.SaveAsync.Call((User user) => calls.Add("single"));
		stub.SaveAsync.Call((User user, CancellationToken ct) => calls.Add("withToken"));

		await service.SaveAsync(new User { Id = 1 });
		using var cts = new CancellationTokenSource();
		await service.SaveAsync(new User { Id = 2 }, cts.Token);

		Assert.Equal(2, calls.Count);
		Assert.Equal("single", calls[0]);
		Assert.Equal("withToken", calls[1]);
	}

	#endregion

	#region ValueTask (Void) Overload Simplified Callback Tests

	[Fact]
	public async Task ValueTask_Overload_SimplifiedVoidCallback_SingleParam()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		string? loggedMessage = null;
		stub.LogAsync.Call((string message) => { loggedMessage = message; });

		await service.LogAsync("Hello");

		Assert.Equal("Hello", loggedMessage);
	}

	[Fact]
	public async Task ValueTask_Overload_SimplifiedVoidCallback_TwoParams()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		string? loggedMessage = null;
		int? loggedLevel = null;

		stub.LogAsync.Call((string message, int level) =>
		{
			loggedMessage = message;
			loggedLevel = level;
		});

		await service.LogAsync("Warning", 2);

		Assert.Equal("Warning", loggedMessage);
		Assert.Equal(2, loggedLevel);
	}

	#endregion

	#region Tracking Tests

	[Fact]
	public async Task Overload_SimplifiedCallback_TracksCallsPerSignature()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var tracking1 = stub.GetByIdAsync.Return((int id) => new User { Id = id });
		var tracking2 = stub.GetByIdAsync.Return((int id, CancellationToken ct) => new User { Id = id });

		await service.GetByIdAsync(1);
		await service.GetByIdAsync(2);
		using var cts = new CancellationTokenSource();
		await service.GetByIdAsync(3, cts.Token);

		// Each overload tracked separately
		tracking1.Verify(Called.Exactly(2));
		tracking2.Verify(Called.Once);
	}

	[Fact]
	public async Task Overload_SimplifiedCallback_TracksLastArg()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var tracking = stub.GetByIdAsync.Return((int id) => new User { Id = id });

		await service.GetByIdAsync(10);
		await service.GetByIdAsync(20);
		await service.GetByIdAsync(30);

		Assert.Equal(30, tracking.LastArg);
	}

	[Fact]
	public async Task Overload_SimplifiedCallback_TracksLastArgs()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var tracking = stub.GetByIdAsync.Return((int id, string includeFields) =>
			new User { Id = id, Name = includeFields });

		await service.GetByIdAsync(1, "name");
		await service.GetByIdAsync(2, "email");

		var lastArgs = tracking.LastArgs;
		Assert.Equal(2, lastArgs.id);
		Assert.Equal("email", lastArgs.includeFields);
	}

	[Fact]
	public async Task Overload_SimplifiedCallback_VerifyTimesWorks()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var tracking = stub.GetCachedAsync.Return((string key) => "value");

		await service.GetCachedAsync("a");
		await service.GetCachedAsync("b");
		await service.GetCachedAsync("c");

		tracking.Verify(Called.AtLeast(2));
		tracking.Verify(Called.AtMost(5));
		tracking.Verify(Called.Exactly(3));
	}

	#endregion

	#region Mutual Exclusivity Tests

	[Fact]
	public async Task Overload_SimplifiedCallback_ClearsAsyncCallback()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		// First configure with async delegate callback
		stub.GetByIdAsync.Return((int id) => Task.FromResult<User?>(new User { Id = 100 }));

		// Then configure with simplified callback - should clear async callback
		stub.GetByIdAsync.Return((int id) => new User { Id = 200 });

		var result = await service.GetByIdAsync(1);

		// Simplified callback wins (configured last)
		Assert.Equal(200, result?.Id);
	}

	[Fact]
	public async Task Overload_AsyncCallback_ClearsSimplifiedCallback()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		// First configure with simplified callback
		stub.GetByIdAsync.Return((int id) => new User { Id = 100 });

		// Then configure with async delegate callback - should clear simplified
		stub.GetByIdAsync.Return((int id) => Task.FromResult<User?>(new User { Id = 200 }));

		var result = await service.GetByIdAsync(1);

		// Async callback wins (configured last)
		Assert.Equal(200, result?.Id);
	}

	[Fact]
	public async Task Overload_MutualExclusivity_PerSignature()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		// Configure single-param with simplified
		stub.GetByIdAsync.Return((int id) => new User { Id = 100 });

		// Configure two-param with async delegate (should NOT affect single-param)
		stub.GetByIdAsync.Return((int id, CancellationToken ct) => Task.FromResult<User?>(new User { Id = 200 }));

		// Single-param still uses simplified callback
		var result1 = await service.GetByIdAsync(1);
		Assert.Equal(100, result1?.Id);

		// Two-param uses async delegate callback
		using var cts = new CancellationTokenSource();
		var result2 = await service.GetByIdAsync(1, cts.Token);
		Assert.Equal(200, result2?.Id);
	}

	#endregion

	#region Reset Tests

	[Fact]
	public async Task Overload_Reset_ClearsSimplifiedCallbackTracking()
	{
		var stub = new AsyncOverloadServiceKnockOff();
		IAsyncOverloadService service = stub;

		var tracking = stub.GetByIdAsync.Return((int id) => new User { Id = id });

		await service.GetByIdAsync(1);
		await service.GetByIdAsync(2);

		tracking.Verify(Called.Exactly(2));

		stub.GetByIdAsync.Reset();

		tracking.Verify(Called.Never);
	}

	#endregion

	#region Three Patterns Tests

	// Pattern 1: Standalone (tested above via AsyncOverloadServiceKnockOff)
	// The [KnockOff] attribute on the class directly implements the interface

	// Pattern 2: Inline Interface
	[KnockOff<IAsyncOverloadService>]
	public partial class InlineAsyncOverloadContainer { }

	[Fact]
	public async Task InlineInterface_SimplifiedCallback_TaskT_Works()
	{
		var stub = new InlineAsyncOverloadContainer.Stubs.IAsyncOverloadService();
		IAsyncOverloadService service = stub;

		// Configure simplified callbacks on inline interface stub
		stub.GetByIdAsync.Return((int id) => new User { Id = id, Name = "Inline" });

		var result = await service.GetByIdAsync(42);

		Assert.NotNull(result);
		Assert.Equal(42, result.Id);
		Assert.Equal("Inline", result.Name);
	}

	[Fact]
	public async Task InlineInterface_SimplifiedCallback_VoidTask_Works()
	{
		var stub = new InlineAsyncOverloadContainer.Stubs.IAsyncOverloadService();
		IAsyncOverloadService service = stub;

		User? savedUser = null;
		stub.SaveAsync.Call((User user) => { savedUser = user; });

		await service.SaveAsync(new User { Id = 99 });

		Assert.NotNull(savedUser);
		Assert.Equal(99, savedUser.Id);
	}

	[Fact]
	public async Task InlineInterface_SimplifiedCallback_ValueTaskT_Works()
	{
		var stub = new InlineAsyncOverloadContainer.Stubs.IAsyncOverloadService();
		IAsyncOverloadService service = stub;

		stub.GetCachedAsync.Return((string key) => $"inline-{key}");

		var result = await service.GetCachedAsync("test");

		Assert.Equal("inline-test", result);
	}

	[Fact]
	public async Task InlineInterface_SimplifiedCallback_MultipleOverloads()
	{
		var stub = new InlineAsyncOverloadContainer.Stubs.IAsyncOverloadService();
		IAsyncOverloadService service = stub;

		// Configure both overloads with simplified callbacks
		stub.GetByIdAsync.Return((int id) => new User { Id = id, Name = "Single" });
		stub.GetByIdAsync.Return((int id, CancellationToken ct) => new User { Id = id, Name = "WithToken" });

		var result1 = await service.GetByIdAsync(1);
		using var cts = new CancellationTokenSource();
		var result2 = await service.GetByIdAsync(2, cts.Token);

		Assert.Equal("Single", result1?.Name);
		Assert.Equal("WithToken", result2?.Name);
	}

	// Pattern 3: Inline Class - using the standalone stub as if it were inline
	// Note: In KnockOff, Inline Class pattern generates the same interceptor structure
	// as Standalone pattern, so if Standalone works, Inline Class works too.
	// This is because both use the same MethodInterceptorRenderer code path.

	#endregion
}
