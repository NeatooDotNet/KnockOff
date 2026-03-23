// ============================================================================
// AsynchronousTests: Edge case tests for async method handling
// Inspired by Rocks.Analysis.IntegrationTests.AsynchronousTests
//
// Tests async edge cases across applicable patterns:
// 1. Interface IAmAsynchronous with Task, ValueTask, Task<T>, ValueTask<T>
//    - Pattern 1 (Standalone Interface), Pattern 5 (Inline Interface)
// 2. Class AsyncEnumeration with virtual IAsyncEnumerable<string>
//    - Pattern 3 (Standalone Class), Pattern 6 (Inline Class)
//
// Scenarios:
// - Task void: configure callback, await, verify called
// - Task<int> return: configure return value, await, assert value
// - ValueTask void: same as Task but ValueTask
// - ValueTask<int> return: same as Task<int> but ValueTask
// - Unconfigured async methods: smart defaults (Task.CompletedTask, default
//   ValueTask, Task.FromResult(default), default ValueTask<T>)
// - Async callbacks: configure with actual async delegate (Task and ValueTask)
// - IAsyncEnumerable: configure return, iterate with await foreach
// - Virtual fallback: unconfigured class stub falls back to base implementation
// ============================================================================

using System.Runtime.CompilerServices;
using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AsynchronousTestTypes
{
	// ========================================================================
	// Edge Case 1: Interface with async methods (Task, ValueTask, Task<T>,
	// ValueTask<T>)
	// ========================================================================

	public interface IAmAsynchronous
	{
		Task FooAsync();
		ValueTask ValueFooAsync();
		Task<int> FooReturnAsync();
		ValueTask<int> ValueFooReturnAsync();
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class AsyncInterfaceStandaloneKnockOff : IAmAsynchronous
	{
	}

	// ========================================================================
	// Edge Case 2: Class with virtual IAsyncEnumerable<string> method
	// ========================================================================

	public class AsyncEnumeration
	{
		public virtual async IAsyncEnumerable<string> GetRecordsAsync(
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			yield return "y";
		}
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<AsyncEnumeration>]
	public partial class AsyncEnumerationStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AsynchronousTestTypes;

	// Inline stubs for applicable patterns
	[KnockOff<IAmAsynchronous>]          // Pattern 5: Inline Interface
	[KnockOff<AsyncEnumeration>]         // Pattern 6: Inline Class
	public partial class AsynchronousInlineTests
	{
	}

	public class AsynchronousTests
	{
		// ====================================================================
		// Edge Case 1: Task void method (FooAsync)
		// ====================================================================

		#region Pattern 1 (Standalone): Task void

		[Fact]
		public async Task TaskVoid_Standalone_CallbackExecutes()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			bool called = false;
			knockOff.FooAsync.Call(() =>
			{
				called = true;
			});

			await service.FooAsync();

			Assert.True(called);
		}

		[Fact]
		public async Task TaskVoid_Standalone_ReturnCompletedTask()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.FooAsync.Return(Task.CompletedTask);

			await service.FooAsync();

			knockOff.FooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task TaskVoid_Standalone_VerifyCallCount()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.FooAsync.Call(() => { });

			await service.FooAsync();
			await service.FooAsync();
			await service.FooAsync();

			knockOff.FooAsync.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 5 (Inline): Task void

		[Fact]
		public async Task TaskVoid_Inline_CallbackExecutes()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			bool called = false;
			stub.FooAsync.Call(() =>
			{
				called = true;
			});

			await service.FooAsync();

			Assert.True(called);
		}

		[Fact]
		public async Task TaskVoid_Inline_ReturnCompletedTask()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooAsync.Return(Task.CompletedTask);

			await service.FooAsync();

			stub.FooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task TaskVoid_Inline_VerifyCallCount()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooAsync.Call(() => { });

			await service.FooAsync();
			await service.FooAsync();

			stub.FooAsync.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Task<int> return method (FooReturnAsync)
		// ====================================================================

		#region Pattern 1 (Standalone): Task<int> return

		[Fact]
		public async Task TaskReturn_Standalone_ReturnsConfiguredValue()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Return(int) auto-wraps in Task.FromResult
			knockOff.FooReturnAsync.Return(42);

			var result = await service.FooReturnAsync();

			Assert.Equal(42, result);
		}

		[Fact]
		public async Task TaskReturn_Standalone_CallbackReturnsValue()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Simplified callback: () => int, auto-wrapped in Task.FromResult
			knockOff.FooReturnAsync.Call(() => 99);

			var result = await service.FooReturnAsync();

			Assert.Equal(99, result);
		}

		[Fact]
		public async Task TaskReturn_Standalone_FullDelegateCallback()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Full delegate: () => Task<int>
			knockOff.FooReturnAsync.Call(() => Task.FromResult(77));

			var result = await service.FooReturnAsync();

			Assert.Equal(77, result);
		}

		[Fact]
		public async Task TaskReturn_Standalone_VerifyCallTracking()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.FooReturnAsync.Return(1);

			await service.FooReturnAsync();
			await service.FooReturnAsync();

			knockOff.FooReturnAsync.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 5 (Inline): Task<int> return

		[Fact]
		public async Task TaskReturn_Inline_ReturnsConfiguredValue()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooReturnAsync.Return(42);

			var result = await service.FooReturnAsync();

			Assert.Equal(42, result);
		}

		[Fact]
		public async Task TaskReturn_Inline_CallbackReturnsValue()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooReturnAsync.Call(() => 99);

			var result = await service.FooReturnAsync();

			Assert.Equal(99, result);
		}

		[Fact]
		public async Task TaskReturn_Inline_FullDelegateCallback()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooReturnAsync.Call(() => Task.FromResult(77));

			var result = await service.FooReturnAsync();

			Assert.Equal(77, result);
		}

		[Fact]
		public async Task TaskReturn_Inline_VerifyCallTracking()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooReturnAsync.Return(1);

			await service.FooReturnAsync();
			await service.FooReturnAsync();

			stub.FooReturnAsync.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 3: ValueTask void method (ValueFooAsync)
		// ====================================================================

		#region Pattern 1 (Standalone): ValueTask void

		[Fact]
		public async Task ValueTaskVoid_Standalone_CallbackExecutes()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			bool called = false;
			knockOff.ValueFooAsync.Call(() =>
			{
				called = true;
			});

			await service.ValueFooAsync();

			Assert.True(called);
		}

		[Fact]
		public async Task ValueTaskVoid_Standalone_ReturnCompletedValueTask()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.ValueFooAsync.Return(new ValueTask());

			await service.ValueFooAsync();

			knockOff.ValueFooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task ValueTaskVoid_Standalone_VerifyCallCount()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.ValueFooAsync.Call(() => { });

			await service.ValueFooAsync();
			await service.ValueFooAsync();

			knockOff.ValueFooAsync.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 5 (Inline): ValueTask void

		[Fact]
		public async Task ValueTaskVoid_Inline_CallbackExecutes()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			bool called = false;
			stub.ValueFooAsync.Call(() =>
			{
				called = true;
			});

			await service.ValueFooAsync();

			Assert.True(called);
		}

		[Fact]
		public async Task ValueTaskVoid_Inline_ReturnCompletedValueTask()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooAsync.Return(new ValueTask());

			await service.ValueFooAsync();

			stub.ValueFooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task ValueTaskVoid_Inline_VerifyCallCount()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooAsync.Call(() => { });

			await service.ValueFooAsync();
			await service.ValueFooAsync();

			stub.ValueFooAsync.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 4: ValueTask<int> return method (ValueFooReturnAsync)
		// ====================================================================

		#region Pattern 1 (Standalone): ValueTask<int> return

		[Fact]
		public async Task ValueTaskReturn_Standalone_ReturnsConfiguredValue()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Return(int) auto-wraps in new ValueTask<int>(value)
			knockOff.ValueFooReturnAsync.Return(42);

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(42, result);
		}

		[Fact]
		public async Task ValueTaskReturn_Standalone_CallbackReturnsValue()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Simplified callback: () => int, auto-wrapped
			knockOff.ValueFooReturnAsync.Call(() => 99);

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(99, result);
		}

		[Fact]
		public async Task ValueTaskReturn_Standalone_FullDelegateCallback()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Full delegate: () => ValueTask<int>
			knockOff.ValueFooReturnAsync.Call(() => new ValueTask<int>(77));

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(77, result);
		}

		[Fact]
		public async Task ValueTaskReturn_Standalone_VerifyCallTracking()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.ValueFooReturnAsync.Return(1);

			await service.ValueFooReturnAsync();
			await service.ValueFooReturnAsync();

			knockOff.ValueFooReturnAsync.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 5 (Inline): ValueTask<int> return

		[Fact]
		public async Task ValueTaskReturn_Inline_ReturnsConfiguredValue()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooReturnAsync.Return(42);

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(42, result);
		}

		[Fact]
		public async Task ValueTaskReturn_Inline_CallbackReturnsValue()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooReturnAsync.Call(() => 99);

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(99, result);
		}

		[Fact]
		public async Task ValueTaskReturn_Inline_FullDelegateCallback()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooReturnAsync.Call(() => new ValueTask<int>(77));

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(77, result);
		}

		[Fact]
		public async Task ValueTaskReturn_Inline_VerifyCallTracking()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooReturnAsync.Return(1);

			await service.ValueFooReturnAsync();
			await service.ValueFooReturnAsync();

			stub.ValueFooReturnAsync.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 5: Async callbacks (delegate is actually async)
		// ====================================================================

		#region Pattern 1 (Standalone): Async callbacks

		[Fact]
		public async Task AsyncCallback_Standalone_TaskVoidWithAwait()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Full async delegate for Task void method
			knockOff.FooAsync.Call(async () => await Task.Delay(10));

			await service.FooAsync();

			knockOff.FooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task AsyncCallback_Standalone_TaskReturnWithAwait()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Full async delegate for Task<int> method
			knockOff.FooReturnAsync.Call(async () =>
			{
				await Task.Delay(10);
				return 3;
			});

			var value = await service.FooReturnAsync();

			Assert.Equal(3, value);
		}

		[Fact]
		public async Task AsyncCallback_Standalone_ValueTaskVoidWithAwait()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			bool called = false;
			knockOff.ValueFooAsync.Call(async () =>
			{
				await Task.Delay(10);
				called = true;
			});

			await service.ValueFooAsync();

			Assert.True(called);
		}

		[Fact]
		public async Task AsyncCallback_Standalone_ValueTaskReturnWithAwait()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.ValueFooReturnAsync.Call(async () =>
			{
				await Task.Delay(10);
				return 5;
			});

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(5, result);
		}

		#endregion

		#region Pattern 5 (Inline): Async callbacks

		[Fact]
		public async Task AsyncCallback_Inline_TaskVoidWithAwait()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooAsync.Call(async () => await Task.Delay(10));

			await service.FooAsync();

			stub.FooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task AsyncCallback_Inline_TaskReturnWithAwait()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooReturnAsync.Call(async () =>
			{
				await Task.Delay(10);
				return 3;
			});

			var value = await service.FooReturnAsync();

			Assert.Equal(3, value);
		}

		[Fact]
		public async Task AsyncCallback_Inline_ValueTaskVoidWithAwait()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			bool called = false;
			stub.ValueFooAsync.Call(async () =>
			{
				await Task.Delay(10);
				called = true;
			});

			await service.ValueFooAsync();

			Assert.True(called);
		}

		[Fact]
		public async Task AsyncCallback_Inline_ValueTaskReturnWithAwait()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.ValueFooReturnAsync.Call(async () =>
			{
				await Task.Delay(10);
				return 5;
			});

			var result = await service.ValueFooReturnAsync();

			Assert.Equal(5, result);
		}

		#endregion

		// ====================================================================
		// Edge Case 6: IAsyncEnumerable — configure return, iterate
		// ====================================================================

		#region Pattern 3 (Standalone Class): IAsyncEnumerable

		[Fact]
		public async Task AsyncEnumerable_StandaloneClass_ConfiguredReturn()
		{
			var knockOff = new AsyncEnumerationStandaloneKnockOff();
			AsyncEnumeration obj = knockOff.Object;

			// Configure to return a custom async enumerable
			knockOff.GetRecordsAsync.Return(CreateAsyncEnumerable("x"));

			var values = new List<string>();
			await foreach (var value in obj.GetRecordsAsync())
			{
				values.Add(value);
			}

			Assert.Single(values);
			Assert.Equal("x", values[0]);
		}

		[Fact]
		public async Task AsyncEnumerable_StandaloneClass_MultipleValues()
		{
			var knockOff = new AsyncEnumerationStandaloneKnockOff();
			AsyncEnumeration obj = knockOff.Object;

			knockOff.GetRecordsAsync.Return(CreateAsyncEnumerable("a", "b", "c"));

			var values = new List<string>();
			await foreach (var value in obj.GetRecordsAsync())
			{
				values.Add(value);
			}

			Assert.Equal(3, values.Count);
			Assert.Equal("a", values[0]);
			Assert.Equal("b", values[1]);
			Assert.Equal("c", values[2]);
		}

		[Fact]
		public async Task AsyncEnumerable_StandaloneClass_VirtualFallback()
		{
			var knockOff = new AsyncEnumerationStandaloneKnockOff();
			AsyncEnumeration obj = knockOff.Object;

			// No configuration — class stubs fall back to base implementation
			// Base implementation yields "y"
			var values = new List<string>();
			await foreach (var value in obj.GetRecordsAsync())
			{
				values.Add(value);
			}

			Assert.Single(values);
			Assert.Equal("y", values[0]);
		}

		[Fact]
		public async Task AsyncEnumerable_StandaloneClass_VerifyCallTracking()
		{
			var knockOff = new AsyncEnumerationStandaloneKnockOff();
			AsyncEnumeration obj = knockOff.Object;

			knockOff.GetRecordsAsync.Return(CreateAsyncEnumerable("x"));

			// Consume the enumerable to trigger the call
			await foreach (var _ in obj.GetRecordsAsync()) { }

			knockOff.GetRecordsAsync.Verify(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): IAsyncEnumerable

		[Fact]
		public async Task AsyncEnumerable_InlineClass_ConfiguredReturn()
		{
			var stub = new AsynchronousInlineTests.Stubs.AsyncEnumeration();
			AsyncEnumeration obj = stub.Object;

			stub.GetRecordsAsync.Return(CreateAsyncEnumerable("x"));

			var values = new List<string>();
			await foreach (var value in obj.GetRecordsAsync())
			{
				values.Add(value);
			}

			Assert.Single(values);
			Assert.Equal("x", values[0]);
		}

		[Fact]
		public async Task AsyncEnumerable_InlineClass_MultipleValues()
		{
			var stub = new AsynchronousInlineTests.Stubs.AsyncEnumeration();
			AsyncEnumeration obj = stub.Object;

			stub.GetRecordsAsync.Return(CreateAsyncEnumerable("a", "b", "c"));

			var values = new List<string>();
			await foreach (var value in obj.GetRecordsAsync())
			{
				values.Add(value);
			}

			Assert.Equal(3, values.Count);
			Assert.Equal("a", values[0]);
			Assert.Equal("b", values[1]);
			Assert.Equal("c", values[2]);
		}

		[Fact]
		public async Task AsyncEnumerable_InlineClass_VirtualFallback()
		{
			var stub = new AsynchronousInlineTests.Stubs.AsyncEnumeration();
			AsyncEnumeration obj = stub.Object;

			// No configuration — falls back to base virtual implementation
			// Base yields "y"
			var values = new List<string>();
			await foreach (var value in obj.GetRecordsAsync())
			{
				values.Add(value);
			}

			Assert.Single(values);
			Assert.Equal("y", values[0]);
		}

		[Fact]
		public async Task AsyncEnumerable_InlineClass_VerifyCallTracking()
		{
			var stub = new AsynchronousInlineTests.Stubs.AsyncEnumeration();
			AsyncEnumeration obj = stub.Object;

			stub.GetRecordsAsync.Return(CreateAsyncEnumerable("x"));

			await foreach (var _ in obj.GetRecordsAsync()) { }

			stub.GetRecordsAsync.Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 7: All four async methods configured and awaited together
		// (Rocks CreateAsynchronousMethodsAsync equivalent)
		// ====================================================================

		#region Pattern 1 (Standalone): All four async methods together

		[Fact]
		public async Task AllFourAsync_Standalone_ConfigureAndAwait()
		{
			const int returnValue = 3;
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			knockOff.FooAsync.Return(Task.CompletedTask);
			knockOff.FooReturnAsync.Return(returnValue);
			knockOff.ValueFooAsync.Return(new ValueTask());
			knockOff.ValueFooReturnAsync.Return(returnValue);

			await service.FooAsync();
			var value = await service.FooReturnAsync();
			await service.ValueFooAsync();
			var valueValue = await service.ValueFooReturnAsync();

			Assert.Equal(returnValue, value);
			Assert.Equal(returnValue, valueValue);
		}

		#endregion

		#region Pattern 5 (Inline): All four async methods together

		[Fact]
		public async Task AllFourAsync_Inline_ConfigureAndAwait()
		{
			const int returnValue = 3;
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			stub.FooAsync.Return(Task.CompletedTask);
			stub.FooReturnAsync.Return(returnValue);
			stub.ValueFooAsync.Return(new ValueTask());
			stub.ValueFooReturnAsync.Return(returnValue);

			await service.FooAsync();
			var value = await service.FooReturnAsync();
			await service.ValueFooAsync();
			var valueValue = await service.ValueFooReturnAsync();

			Assert.Equal(returnValue, value);
			Assert.Equal(returnValue, valueValue);
		}

		#endregion

		// ====================================================================
		// Edge Case 8: Unconfigured async methods return smart defaults
		// (Task.CompletedTask, default ValueTask, Task.FromResult(default),
		// default ValueTask<T>)
		// ====================================================================

		#region Pattern 1 (Standalone): Unconfigured defaults

		[Fact]
		public async Task UnconfiguredDefaults_Standalone_TaskVoidCompletesWithoutError()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Unconfigured Task void method returns Task.CompletedTask
			await service.FooAsync();

			// Should complete without throwing
			knockOff.FooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task UnconfiguredDefaults_Standalone_ValueTaskVoidCompletesWithoutError()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Unconfigured ValueTask void returns default(ValueTask) — completed
			await service.ValueFooAsync();

			knockOff.ValueFooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task UnconfiguredDefaults_Standalone_TaskReturnReturnsDefault()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Unconfigured Task<int> returns Task.FromResult(default(int)) = 0
			var value = await service.FooReturnAsync();

			Assert.Equal(0, value);
		}

		[Fact]
		public async Task UnconfiguredDefaults_Standalone_ValueTaskReturnReturnsDefault()
		{
			var knockOff = new AsyncInterfaceStandaloneKnockOff();
			IAmAsynchronous service = knockOff;

			// Unconfigured ValueTask<int> returns default = 0
			var value = await service.ValueFooReturnAsync();

			Assert.Equal(0, value);
		}

		#endregion

		#region Pattern 5 (Inline): Unconfigured defaults

		[Fact]
		public async Task UnconfiguredDefaults_Inline_TaskVoidCompletesWithoutError()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			await service.FooAsync();

			stub.FooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task UnconfiguredDefaults_Inline_ValueTaskVoidCompletesWithoutError()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			await service.ValueFooAsync();

			stub.ValueFooAsync.Verify(Called.Once);
		}

		[Fact]
		public async Task UnconfiguredDefaults_Inline_TaskReturnReturnsDefault()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			var value = await service.FooReturnAsync();

			Assert.Equal(0, value);
		}

		[Fact]
		public async Task UnconfiguredDefaults_Inline_ValueTaskReturnReturnsDefault()
		{
			var stub = new AsynchronousInlineTests.Stubs.IAmAsynchronous();
			IAmAsynchronous service = stub;

			var value = await service.ValueFooReturnAsync();

			Assert.Equal(0, value);
		}

		#endregion

		// ====================================================================
		// Helper Methods
		// ====================================================================

		private static async IAsyncEnumerable<string> CreateAsyncEnumerable(
			params string[] items)
		{
			foreach (var item in items)
			{
				await Task.CompletedTask;
				yield return item;
			}
		}
	}
}
