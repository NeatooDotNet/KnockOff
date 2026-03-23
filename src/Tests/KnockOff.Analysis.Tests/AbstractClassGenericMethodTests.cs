// ============================================================================
// AbstractClassGenericMethodTests: Edge case tests for a generic abstract class
// with both class-level and method-level type parameters.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassGenericMethodTests
//
// Target class:
//   public abstract class AbstractClassGenericMethod<T>
//   {
//       public abstract void Foo(List<string> values);
//       public abstract void Quux(T value);
//       public abstract void Bar<TParam>(TParam value);
//       public abstract List<string> FooReturn();
//       public abstract T QuuxReturn();
//       public abstract TReturn BarReturn<TReturn>();
//       public abstract TData? NullableValues<TData>(TData? data);
//   }
//
// Key difference from ClassGenericMethodTests: members are ABSTRACT, not virtual.
// Abstract members have no base implementation, so unconfigured calls return
// default(T) or no-op.
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassGenericMethod<int>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(AbstractClassGenericMethod<>))]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassGenericMethod<int>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(AbstractClassGenericMethod<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassGenericMethodTestTypes
{
	public abstract class AbstractClassGenericMethod<T>
	{
		public abstract void Foo(List<string> values);
		public abstract void Quux(T value);
		public abstract void Bar<TParam>(TParam value);
		public abstract List<string> FooReturn();
		public abstract T QuuxReturn();
		public abstract TReturn BarReturn<TReturn>();
		public abstract TData? NullableValues<TData>(TData? data);
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<AbstractClassGenericMethod<int>>]
	public partial class AbstractClassGenericMethodStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(AbstractClassGenericMethod<>))]
	public partial class AbstractClassGenericMethodGenericStandaloneKnockOff<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassGenericMethodTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<AbstractClassGenericMethod<int>>]
	public partial class AbstractClassGenericMethodInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(AbstractClassGenericMethod<>))]
	public partial class AbstractClassGenericMethodOpenGenericTests
	{
	}

	public class AbstractClassGenericMethodTests
	{
		// ====================================================================
		// Scenario 1: Concrete param (Foo) — call with List<string>, verify
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete param (Foo)

		[Fact]
		public void Foo_StandaloneClass_CallbackCapturesArg()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "a", "b" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		[Fact]
		public void Foo_StandaloneClass_VerifyCallCount()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.Foo([]);
			obj.Foo([]);

			stub.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Concrete param (Foo)

		[Fact]
		public void Foo_GenericStandaloneClass_CallbackCapturesArg()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "x", "y" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		#endregion

		#region Pattern 6 (Inline Class): Concrete param (Foo)

		[Fact]
		public void Foo_InlineClass_CallbackCapturesArg()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "c", "d" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		[Fact]
		public void Foo_InlineClass_VerifyCallCount()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.Foo([]);
			obj.Foo([]);

			stub.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Concrete param (Foo)

		[Fact]
		public void Foo_OpenGenericClass_CallbackCapturesArg()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "e", "f" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Class type param (Quux) — when T=int, call Quux(3)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Class type param (Quux)

		[Fact]
		public void Quux_StandaloneClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		[Fact]
		public void Quux_StandaloneClass_VerifyCallCount()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.Quux(1);
			obj.Quux(2);

			stub.Quux.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Class type param (Quux)

		[Fact]
		public void Quux_GenericStandaloneClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		#endregion

		#region Pattern 6 (Inline Class): Class type param (Quux)

		[Fact]
		public void Quux_InlineClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		[Fact]
		public void Quux_InlineClass_VerifyCallCount()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.Quux(1);
			obj.Quux(2);

			stub.Quux.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Class type param (Quux)

		[Fact]
		public void Quux_OpenGenericClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Method type param (Bar) — Bar<int>(3), Bar<string>("x")
		// ====================================================================

		#region Pattern 3 (Standalone Class): Method type param (Bar)

		[Fact]
		public void Bar_StandaloneClass_DifferentTypeArgs()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			obj.Bar(3);
			obj.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		[Fact]
		public void Bar_StandaloneClass_VerifyPerTypeArg()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.Bar(1);
			obj.Bar("a");
			obj.Bar(2);

			stub.Bar.Of<int>().Verify(Called.Exactly(2));
			stub.Bar.Of<string>().Verify(Called.Once);
			stub.Bar.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Method type param (Bar)

		[Fact]
		public void Bar_GenericStandaloneClass_DifferentTypeArgs()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			obj.Bar(3);
			obj.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		#endregion

		#region Pattern 6 (Inline Class): Method type param (Bar)

		[Fact]
		public void Bar_InlineClass_DifferentTypeArgs()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			obj.Bar(3);
			obj.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		[Fact]
		public void Bar_InlineClass_VerifyPerTypeArg()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.Bar(1);
			obj.Bar("a");
			obj.Bar(2);

			stub.Bar.Of<int>().Verify(Called.Exactly(2));
			stub.Bar.Of<string>().Verify(Called.Once);
			stub.Bar.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Method type param (Bar)

		[Fact]
		public void Bar_OpenGenericClass_DifferentTypeArgs()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			obj.Bar(3);
			obj.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Concrete return (FooReturn) — configure return, assert
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var returnValue = new List<string> { "hello" };
			stub.FooReturn.Return(returnValue);

			var result = obj.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_GenericStandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var returnValue = new List<string> { "world" };
			stub.FooReturn.Return(returnValue);

			var result = obj.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_InlineClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var returnValue = new List<string> { "hello" };
			stub.FooReturn.Return(returnValue);

			var result = obj.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_OpenGenericClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var returnValue = new List<string> { "world" };
			stub.FooReturn.Return(returnValue);

			var result = obj.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Class type param return (QuuxReturn)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(42);

			var result = obj.QuuxReturn();

			Assert.Equal(42, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_GenericStandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(99);

			var result = obj.QuuxReturn();

			Assert.Equal(99, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_InlineClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(42);

			var result = obj.QuuxReturn();

			Assert.Equal(42, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_OpenGenericClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(99);

			var result = obj.QuuxReturn();

			Assert.Equal(99, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Method type param return (BarReturn) — per-type dispatch
		// ====================================================================

		#region Pattern 3 (Standalone Class): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_StandaloneClass_DifferentReturnTypes()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 42);
			stub.BarReturn.Of<string>().Call(() => "hello");

			Assert.Equal(42, obj.BarReturn<int>());
			Assert.Equal("hello", obj.BarReturn<string>());
		}

		[Fact]
		public void BarReturn_StandaloneClass_VerifyPerTypeArg()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.BarReturn<int>();
			obj.BarReturn<string>();
			obj.BarReturn<int>();

			stub.BarReturn.Of<int>().Verify(Called.Exactly(2));
			stub.BarReturn.Of<string>().Verify(Called.Once);
			stub.BarReturn.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_GenericStandaloneClass_DifferentReturnTypes()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 99);
			stub.BarReturn.Of<string>().Call(() => "world");

			Assert.Equal(99, obj.BarReturn<int>());
			Assert.Equal("world", obj.BarReturn<string>());
		}

		#endregion

		#region Pattern 6 (Inline Class): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_InlineClass_DifferentReturnTypes()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 42);
			stub.BarReturn.Of<string>().Call(() => "hello");

			Assert.Equal(42, obj.BarReturn<int>());
			Assert.Equal("hello", obj.BarReturn<string>());
		}

		[Fact]
		public void BarReturn_InlineClass_VerifyPerTypeArg()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			obj.BarReturn<int>();
			obj.BarReturn<string>();
			obj.BarReturn<int>();

			stub.BarReturn.Of<int>().Verify(Called.Exactly(2));
			stub.BarReturn.Of<string>().Verify(Called.Once);
			stub.BarReturn.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_OpenGenericClass_DifferentReturnTypes()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 99);
			stub.BarReturn.Of<string>().Call(() => "world");

			Assert.Equal(99, obj.BarReturn<int>());
			Assert.Equal("world", obj.BarReturn<string>());
		}

		#endregion

		// ====================================================================
		// Scenario 7: Nullable generic (NullableValues)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		[Fact]
		public void NullableValues_StandaloneClass_HandlesNullInput()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => data == null ? "was-null" : data);

			var result = obj.NullableValues<string>(null);

			Assert.Equal("was-null", result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_GenericStandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_InlineClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		[Fact]
		public void NullableValues_InlineClass_HandlesNullInput()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => data == null ? "was-null" : data);

			var result = obj.NullableValues<string>(null);

			Assert.Equal("was-null", result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_OpenGenericClass_ReturnsConfiguredValue()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		#endregion

		// ====================================================================
		// Scenario 8: Unconfigured defaults
		// Abstract methods return default(T) when unconfigured (no base to call).
		// ====================================================================

		#region Pattern 3 (Standalone Class): Unconfigured defaults

		[Fact]
		public void FooReturn_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			// Abstract FooReturn() returns default(List<string>) => null
			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			// Abstract QuuxReturn() returns default(int) => 0
			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		[Fact]
		public void BarReturn_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.BarReturn<int>();

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Unconfigured defaults

		[Fact]
		public void FooReturn_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		[Fact]
		public void BarReturn_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.BarReturn<int>();

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Unconfigured defaults

		[Fact]
		public void FooReturn_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Unconfigured defaults

		[Fact]
		public void FooReturn_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		#endregion

		// ====================================================================
		// Scenario 9: Unconfigured void abstract methods — no-op, no throw
		// ====================================================================

		#region Pattern 3 (Standalone Class): Unconfigured void no-op

		[Fact]
		public void Foo_StandaloneClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_StandaloneClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_StandaloneClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodStandaloneKnockOff();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion

		#region Pattern 6 (Inline Class): Unconfigured void no-op

		[Fact]
		public void Foo_InlineClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_InlineClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_InlineClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodInlineTests.Stubs.AbstractClassGenericMethod();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Unconfigured void no-op

		[Fact]
		public void Foo_GenericStandaloneClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_GenericStandaloneClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_GenericStandaloneClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodGenericStandaloneKnockOff<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Unconfigured void no-op

		[Fact]
		public void Foo_OpenGenericClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_OpenGenericClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_OpenGenericClass_UnconfiguredDoesNotThrow()
		{
			var stub = new AbstractClassGenericMethodOpenGenericTests.Stubs.AbstractClassGenericMethod<int>();
			AbstractClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion
	}
}
