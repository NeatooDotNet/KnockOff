namespace KnockOff.Tests;

/// <summary>
/// Tests for edge cases in value-based overloads.
/// Phase 5 of value-based overloads feature.
///
/// Edge cases covered:
/// - Delegate return types (method returns Func&lt;int&gt;)
/// - Nullable async types (Task&lt;string?&gt;)
/// - Overloaded methods (overload groups don't have value overloads yet)
///
/// Note: Generic methods with Of&lt;T&gt;().Return(value) is NOT implemented -
/// generic typed handlers only support callback syntax.
/// </summary>
public partial class EdgeCaseValueOverloadTests
{
	#region Delegate Return Type Tests

	/// <summary>
	/// Tests that OnCall(value) works when the return type is a delegate (Func&lt;int&gt;).
	/// The value is the delegate itself, not the result of invoking it.
	/// </summary>
	[Fact]
	public void DelegateReturnType_OnCallValue_ReturnsDelegateValue()
	{
		var knockOff = new DelegateReturnKnockOff();
		IDelegateReturnService service = knockOff;

		Func<int> factory = () => 42;
		knockOff.GetIntFactory.Return(factory);

		var result = service.GetIntFactory();
		Assert.Same(factory, result);
		Assert.Equal(42, result());
	}

	[Fact]
	public void DelegateReturnType_OnCallValue_DifferentDelegateValues()
	{
		var knockOff = new DelegateReturnKnockOff();
		IDelegateReturnService service = knockOff;

		Func<string> factory1 = () => "first";
		knockOff.GetStringFactory.Return(factory1);
		var result1 = service.GetStringFactory();

		Func<string> factory2 = () => "second";
		knockOff.GetStringFactory.Return(factory2);
		var result2 = service.GetStringFactory();

		Assert.Equal("first", result1());
		Assert.Equal("second", result2());
	}

	[Fact]
	public void DelegateReturnType_OnCallValue_NullDelegateAllowed()
	{
		var knockOff = new DelegateReturnKnockOff();
		IDelegateReturnService service = knockOff;

		knockOff.GetNullableFactory.Return((Func<int>?)null);

		var result = service.GetNullableFactory();
		Assert.Null(result);
	}

	[Fact]
	public void DelegateReturnType_OnCallCallback_VsOnCallValue_Distinction()
	{
		var knockOff = new DelegateReturnKnockOff();
		IDelegateReturnService service = knockOff;

		// OnCall with a delegate TYPE (using value overload)
		// The delegate is returned as-is when the method is called
		Func<int> valueToReturn = () => 99;
		knockOff.GetIntFactory.Return(valueToReturn);

		var result = service.GetIntFactory();
		Assert.Same(valueToReturn, result);
	}

	#endregion

	#region Nullable Async Type Tests

	[Fact]
	public async Task NullableAsyncReturnType_OnCallValue_AutoWrapsNonNull()
	{
		var knockOff = new NullableAsyncKnockOff();
		INullableAsyncService service = knockOff;

		// Value overload with non-null string auto-wraps in Task
		knockOff.GetNullableAsync.Return("hello");

		var result = await service.GetNullableAsync();
		Assert.Equal("hello", result);
	}

	[Fact]
	public async Task NullableAsyncReturnType_OnCallValue_AutoWrapsNull()
	{
		var knockOff = new NullableAsyncKnockOff();
		INullableAsyncService service = knockOff;

		// Value overload with null string auto-wraps in Task
		knockOff.GetNullableAsync.Return((string?)null);

		var result = await service.GetNullableAsync();
		Assert.Null(result);
	}

	[Fact]
	public async Task NullableAsyncReturnType_RepeatedCalls_ReturnsSameValue()
	{
		var knockOff = new NullableAsyncKnockOff();
		INullableAsyncService service = knockOff;

		knockOff.GetNullableAsync.Return("repeated");

		Assert.Equal("repeated", await service.GetNullableAsync());
		Assert.Equal("repeated", await service.GetNullableAsync());
		Assert.Equal("repeated", await service.GetNullableAsync());
	}

	[Fact]
	public async Task NullableValueTaskReturnType_OnCallValue_AutoWraps()
	{
		var knockOff = new NullableAsyncKnockOff();
		INullableAsyncService service = knockOff;

		knockOff.GetNullableValueTaskAsync.Return(42);

		var result = await service.GetNullableValueTaskAsync();
		Assert.Equal(42, result);
	}

	[Fact]
	public async Task NullableValueTaskReturnType_OnCallValue_NullableInt()
	{
		var knockOff = new NullableAsyncKnockOff();
		INullableAsyncService service = knockOff;

		knockOff.GetNullableValueTaskAsync.Return((int?)null);

		var result = await service.GetNullableValueTaskAsync();
		Assert.Null(result);
	}

	#endregion

	#region Overloaded Method Tests

	// Note: Overload groups (methods with the same name but different parameters)
	// use delegate type disambiguation. Each overload has its own delegate type
	// like CalculateDelegate_Int32_Void, and OnCall accepts these delegates.
	// Value overloads for overload groups are NOT yet implemented.

	[Fact]
	public void OverloadedMethods_CallbacksWork_EachOverloadIndependently()
	{
		var knockOff = new OverloadedMethodKnockOff();
		IOverloadedMethodService service = knockOff;

		// Each overload is distinguished by its delegate type
		knockOff.Calculate.Call((int x) => { });
		knockOff.Calculate.Call((int x, int y) => { });

		service.Calculate(1);
		service.Calculate(2, 3);

		// Verify uses the total call count across all overloads
		knockOff.Calculate.Verify(Called.Exactly(2));
	}

	[Fact]
	public void OverloadedMethods_ReturningValues_UseCallbackSyntax()
	{
		var knockOff = new OverloadedMethodKnockOff();
		IOverloadedMethodService service = knockOff;

		// Add is a single-signature method (not overloaded), so it uses a pre-compiled interceptor directly
		knockOff.Add.Return((a, b) => a + b);

		Assert.Equal(5, service.Add(2, 3));
		Assert.Equal(10, service.Add(4, 6));
	}

	#endregion

	// Note: C# doesn't allow a property and method with the same name in an interface,
	// so there's no name collision edge case to test.

	#region Edge Values Tests

	[Fact]
	public void OnCallValue_EmptyString_ReturnsEmptyString()
	{
		var knockOff = new SampleKnockOff();
		ISampleService service = knockOff;

		knockOff.GetOptional.Return("");

		Assert.Equal("", service.GetOptional());
	}

	[Fact]
	public void OnGetValue_DefaultValue_IntReturnsZero()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.Get(0);

		Assert.Equal(0, service.Count);
	}

	[Fact]
	public void OnGetValue_DefaultValue_BoolReturnsFalse()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.IsEnabled.Get(false);

		Assert.False(service.IsEnabled);
	}

	#endregion

	#region Test Interfaces and Stubs

	public interface IDelegateReturnService
	{
		Func<int> GetIntFactory();
		Func<string> GetStringFactory();
		Func<int>? GetNullableFactory();
	}

	[KnockOff]
	public partial class DelegateReturnKnockOff : IDelegateReturnService
	{
	}

	public interface INullableAsyncService
	{
		Task<string?> GetNullableAsync();
		ValueTask<int?> GetNullableValueTaskAsync();
	}

	[KnockOff]
	public partial class NullableAsyncKnockOff : INullableAsyncService
	{
	}

	public interface IOverloadedMethodService
	{
		void Calculate(int x);
		void Calculate(int x, int y);
		int Add(int a, int b);
	}

	[KnockOff]
	public partial class OverloadedMethodKnockOff : IOverloadedMethodService
	{
	}

	public interface IPropertyTest
	{
		string? Name { get; set; }
		int Count { get; set; }
		bool IsEnabled { get; set; }
	}

	[KnockOff]
	public partial class PropertyTestKnockOff : IPropertyTest
	{
	}

	#endregion
}
