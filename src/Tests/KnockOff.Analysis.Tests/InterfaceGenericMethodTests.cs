// ============================================================================
// InterfaceGenericMethodTests: Edge case tests for a generic interface with
// both class-level and method-level type parameters.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceGenericMethodTests
//
// Target interface:
//   public interface IInterfaceGenericMethod<T>
//   {
//       void Foo(List<string> values);           // concrete param
//       void Quux(T value);                      // class type param
//       void Bar<TParam>(TParam value);          // method type param
//       List<string> FooReturn();                // concrete return
//       T QuuxReturn();                          // class type param return
//       TReturn BarReturn<TReturn>();             // method type param return
//       TData? NullableValues<TData>(TData? data); // nullable generic
//   }
//
// Scenarios:
// 1. Concrete param (Foo) — call with List<string>, verify
// 2. Class type param (Quux) — when T=int, call Quux(3), verify
// 3. Method type param (Bar) — call Bar<int>(3), Bar<string>("x"), verify per-type
// 4. Concrete return (FooReturn) — configure return, assert
// 5. Class type param return (QuuxReturn) — configure return, assert
// 6. Method type param return (BarReturn) — configure per-type, assert
// 7. Nullable generic (NullableValues) — configure with nullable, assert
// 8. Unconfigured defaults — verify default returns
//
// Applicable patterns (generic interface):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterfaceGenericMethod<int> {}
// - Pattern 2 (Generic Standalone): [KnockOff] partial class Stub<T> : IInterfaceGenericMethod<T> {}
// - Pattern 5 (Inline Interface): [KnockOff<IInterfaceGenericMethod<int>>]
// - Pattern 8 (Open Generic Interface): [KnockOff(typeof(IInterfaceGenericMethod<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceGenericMethodTestTypes
{
	public interface IInterfaceGenericMethod<T>
	{
		void Foo(List<string> values);
		void Quux(T value);
		void Bar<TParam>(TParam value);
		List<string> FooReturn();
		T QuuxReturn();
		TReturn BarReturn<TReturn>();
		TData? NullableValues<TData>(TData? data);
	}

	// Pattern 1: Standalone interface stub (closed generic)
	[KnockOff]
	public partial class InterfaceGenericMethodStandaloneKnockOff : IInterfaceGenericMethod<int>
	{
	}

	// Pattern 2: Generic standalone interface stub (open generic)
	[KnockOff]
	public partial class InterfaceGenericMethodGenericStandaloneKnockOff<T> : IInterfaceGenericMethod<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceGenericMethodTestTypes;

	// Pattern 5: Inline interface stub (closed generic)
	[KnockOff<IInterfaceGenericMethod<int>>]
	public partial class InterfaceGenericMethodInlineTests
	{
	}

	// Pattern 8: Open generic interface stub
	[KnockOff(typeof(IInterfaceGenericMethod<>))]
	public partial class InterfaceGenericMethodOpenGenericTests
	{
	}

	public class InterfaceGenericMethodTests
	{
		// ====================================================================
		// Scenario 1: Concrete param (Foo) — call with List<string>, verify
		// Foo(List<string>) uses a concrete generic type, not a type parameter.
		// ====================================================================

		#region Pattern 1 (Standalone): Concrete param (Foo)

		[Fact]
		public void Foo_Standalone_CallbackCapturesArg()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "a", "b" };
			service.Foo(input);

			Assert.Same(input, captured);
		}

		[Fact]
		public void Foo_Standalone_VerifyCallCount()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			service.Foo([]);
			service.Foo([]);

			stub.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Concrete param (Foo)

		[Fact]
		public void Foo_GenericStandalone_CallbackCapturesArg()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "x", "y" };
			service.Foo(input);

			Assert.Same(input, captured);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Concrete param (Foo)

		[Fact]
		public void Foo_Inline_CallbackCapturesArg()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "c", "d" };
			service.Foo(input);

			Assert.Same(input, captured);
		}

		[Fact]
		public void Foo_Inline_VerifyCallCount()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			service.Foo([]);
			service.Foo([]);

			stub.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Concrete param (Foo)

		[Fact]
		public void Foo_OpenGeneric_CallbackCapturesArg()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "e", "f" };
			service.Foo(input);

			Assert.Same(input, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Class type param (Quux) — when T=int, call Quux(3)
		// Quux(T) uses the class-level type parameter, not a method-level one.
		// ====================================================================

		#region Pattern 1 (Standalone): Class type param (Quux)

		[Fact]
		public void Quux_Standalone_CallbackCapturesClassTypeParamArg()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			service.Quux(3);

			Assert.Equal(3, captured);
		}

		[Fact]
		public void Quux_Standalone_VerifyCallCount()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			service.Quux(1);
			service.Quux(2);

			stub.Quux.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Class type param (Quux)

		[Fact]
		public void Quux_GenericStandalone_CallbackCapturesClassTypeParamArg()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			service.Quux(3);

			Assert.Equal(3, captured);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Class type param (Quux)

		[Fact]
		public void Quux_Inline_CallbackCapturesClassTypeParamArg()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			service.Quux(3);

			Assert.Equal(3, captured);
		}

		[Fact]
		public void Quux_Inline_VerifyCallCount()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			service.Quux(1);
			service.Quux(2);

			stub.Quux.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Class type param (Quux)

		[Fact]
		public void Quux_OpenGeneric_CallbackCapturesClassTypeParamArg()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			service.Quux(3);

			Assert.Equal(3, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Method type param (Bar) — call Bar<int>(3), Bar<string>("x")
		// Bar<TParam> is a generic method. Access via Of<T>() pattern.
		// ====================================================================

		#region Pattern 1 (Standalone): Method type param (Bar)

		[Fact]
		public void Bar_Standalone_DifferentTypeArgs()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			service.Bar(3);
			service.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		[Fact]
		public void Bar_Standalone_VerifyPerTypeArg()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			service.Bar(1);
			service.Bar("a");
			service.Bar(2);

			stub.Bar.Of<int>().Verify(Called.Exactly(2));
			stub.Bar.Of<string>().Verify(Called.Once);
			stub.Bar.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Method type param (Bar)

		[Fact]
		public void Bar_GenericStandalone_DifferentTypeArgs()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			service.Bar(3);
			service.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Method type param (Bar)

		[Fact]
		public void Bar_Inline_DifferentTypeArgs()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			service.Bar(3);
			service.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		[Fact]
		public void Bar_Inline_VerifyPerTypeArg()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			service.Bar(1);
			service.Bar("a");
			service.Bar(2);

			stub.Bar.Of<int>().Verify(Called.Exactly(2));
			stub.Bar.Of<string>().Verify(Called.Once);
			stub.Bar.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Method type param (Bar)

		[Fact]
		public void Bar_OpenGeneric_DifferentTypeArgs()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			int capturedInt = 0;
			string? capturedString = null;

			stub.Bar.Of<int>().Call((value) => capturedInt = value);
			stub.Bar.Of<string>().Call((value) => capturedString = value);

			service.Bar(3);
			service.Bar("x");

			Assert.Equal(3, capturedInt);
			Assert.Equal("x", capturedString);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Concrete return (FooReturn) — configure return, assert
		// FooReturn() returns List<string>, a concrete generic type.
		// ====================================================================

		#region Pattern 1 (Standalone): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_Standalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			var returnValue = new List<string> { "hello" };
			stub.FooReturn.Return(returnValue);

			var result = service.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_GenericStandalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			var returnValue = new List<string> { "world" };
			stub.FooReturn.Return(returnValue);

			var result = service.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_Inline_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			var returnValue = new List<string> { "hello" };
			stub.FooReturn.Return(returnValue);

			var result = service.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_OpenGeneric_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			var returnValue = new List<string> { "world" };
			stub.FooReturn.Return(returnValue);

			var result = service.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Class type param return (QuuxReturn) — configure, assert
		// QuuxReturn() returns T, which is int when T=int.
		// ====================================================================

		#region Pattern 1 (Standalone): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_Standalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			stub.QuuxReturn.Return(42);

			var result = service.QuuxReturn();

			Assert.Equal(42, result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_GenericStandalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			stub.QuuxReturn.Return(99);

			var result = service.QuuxReturn();

			Assert.Equal(99, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_Inline_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			stub.QuuxReturn.Return(42);

			var result = service.QuuxReturn();

			Assert.Equal(42, result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_OpenGeneric_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			stub.QuuxReturn.Return(99);

			var result = service.QuuxReturn();

			Assert.Equal(99, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Method type param return (BarReturn) — per-type dispatch
		// BarReturn<TReturn>() uses Of<T>() pattern for configuration.
		// ====================================================================

		#region Pattern 1 (Standalone): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_Standalone_DifferentReturnTypes()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			stub.BarReturn.Of<int>().Call(() => 42);
			stub.BarReturn.Of<string>().Call(() => "hello");

			Assert.Equal(42, service.BarReturn<int>());
			Assert.Equal("hello", service.BarReturn<string>());
		}

		[Fact]
		public void BarReturn_Standalone_VerifyPerTypeArg()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			// Must configure returns — string is non-nullable ref type, throws unconfigured
			stub.BarReturn.Of<int>().Call(() => 0);
			stub.BarReturn.Of<string>().Call(() => "");

			service.BarReturn<int>();
			service.BarReturn<string>();
			service.BarReturn<int>();

			stub.BarReturn.Of<int>().Verify(Called.Exactly(2));
			stub.BarReturn.Of<string>().Verify(Called.Once);
			stub.BarReturn.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_GenericStandalone_DifferentReturnTypes()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			stub.BarReturn.Of<int>().Call(() => 99);
			stub.BarReturn.Of<string>().Call(() => "world");

			Assert.Equal(99, service.BarReturn<int>());
			Assert.Equal("world", service.BarReturn<string>());
		}

		#endregion

		#region Pattern 5 (Inline Interface): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_Inline_DifferentReturnTypes()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			stub.BarReturn.Of<int>().Call(() => 42);
			stub.BarReturn.Of<string>().Call(() => "hello");

			Assert.Equal(42, service.BarReturn<int>());
			Assert.Equal("hello", service.BarReturn<string>());
		}

		[Fact]
		public void BarReturn_Inline_VerifyPerTypeArg()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			// Must configure returns — string is non-nullable ref type, throws unconfigured
			stub.BarReturn.Of<int>().Call(() => 0);
			stub.BarReturn.Of<string>().Call(() => "");

			service.BarReturn<int>();
			service.BarReturn<string>();
			service.BarReturn<int>();

			stub.BarReturn.Of<int>().Verify(Called.Exactly(2));
			stub.BarReturn.Of<string>().Verify(Called.Once);
			stub.BarReturn.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_OpenGeneric_DifferentReturnTypes()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			stub.BarReturn.Of<int>().Call(() => 99);
			stub.BarReturn.Of<string>().Call(() => "world");

			Assert.Equal(99, service.BarReturn<int>());
			Assert.Equal("world", service.BarReturn<string>());
		}

		#endregion

		// ====================================================================
		// Scenario 7: Nullable generic (NullableValues)
		// NullableValues<TData>(TData? data) => TData?
		// Method-level generic with nullable param and return.
		// ====================================================================

		#region Pattern 1 (Standalone): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_Standalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = service.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		[Fact]
		public void NullableValues_Standalone_HandlesNullInput()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			stub.NullableValues.Of<string>().Call((data) => data == null ? "was-null" : data);

			var result = service.NullableValues<string>(null);

			Assert.Equal("was-null", result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_GenericStandalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodGenericStandaloneKnockOff<int>();
			IInterfaceGenericMethod<int> service = stub;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = service.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_Inline_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = service.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		[Fact]
		public void NullableValues_Inline_HandlesNullInput()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			stub.NullableValues.Of<string>().Call((data) => data == null ? "was-null" : data);

			var result = service.NullableValues<string>(null);

			Assert.Equal("was-null", result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_OpenGeneric_ReturnsConfiguredValue()
		{
			var stub = new InterfaceGenericMethodOpenGenericTests.Stubs.IInterfaceGenericMethod<int>();
			IInterfaceGenericMethod<int> service = stub;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = service.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		#endregion

		// ====================================================================
		// Scenario 8: Unconfigured defaults
		// Interface stubs return default for unconfigured methods.
		// ====================================================================

		#region Pattern 1 (Standalone): Unconfigured defaults

		[Fact]
		public void Foo_Standalone_UnconfiguredDoesNotThrow()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			var exception = Record.Exception(() => service.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void QuuxReturn_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericMethodStandaloneKnockOff();
			IInterfaceGenericMethod<int> service = stub;

			var result = service.QuuxReturn();

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Unconfigured defaults

		[Fact]
		public void Foo_Inline_UnconfiguredDoesNotThrow()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			var exception = Record.Exception(() => service.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void QuuxReturn_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericMethodInlineTests.Stubs.IInterfaceGenericMethod();
			IInterfaceGenericMethod<int> service = stub;

			var result = service.QuuxReturn();

			Assert.Equal(default, result);
		}

		#endregion
	}
}
