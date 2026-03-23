// ============================================================================
// ClassGenericMethodTests: Edge case tests for a generic class with both
// class-level and method-level type parameters.
// Inspired by Rocks.Analysis.IntegrationTests.ClassGenericMethodTests
//
// Target class:
//   public class ClassGenericMethod<T>
//   {
//       public virtual void Foo(List<string> values) { }           // concrete param
//       public virtual void Quux(T value) { }                      // class type param
//       public virtual void Bar<TParam>(TParam value) { }          // method type param
//       public virtual List<string>? FooReturn() => default;       // concrete return
//       public virtual T? QuuxReturn() => default;                 // class type param return
//       public virtual TReturn BarReturn<TReturn>() => default!;   // method type param return
//       public virtual TData? NullableValues<TData>(TData? data) => default!; // nullable generic
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
// 9. Virtual fallback — unconfigured calls fall through to base
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassGenericMethod<int>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(ClassGenericMethod<>))]
// - Pattern 6 (Inline Class): [KnockOff<ClassGenericMethod<int>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(ClassGenericMethod<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassGenericMethodTestTypes
{
	public class ClassGenericMethod<T>
	{
		public virtual void Foo(List<string> values) { }
		public virtual void Quux(T value) { }
		public virtual void Bar<TParam>(TParam value) { }
		public virtual List<string>? FooReturn() => default;
		public virtual T? QuuxReturn() => default;
		public virtual TReturn BarReturn<TReturn>() => default!;
		public virtual TData? NullableValues<TData>(TData? data) => default!;
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<ClassGenericMethod<int>>]
	public partial class ClassGenericMethodStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(ClassGenericMethod<>))]
	public partial class ClassGenericMethodGenericStandaloneKnockOff<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassGenericMethodTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<ClassGenericMethod<int>>]
	public partial class ClassGenericMethodInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(ClassGenericMethod<>))]
	public partial class ClassGenericMethodOpenGenericTests
	{
	}

	public class ClassGenericMethodTests
	{
		// ====================================================================
		// Scenario 1: Concrete param (Foo) — call with List<string>, verify
		// Foo(List<string>) is a plain virtual method with no type params.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete param (Foo)

		[Fact]
		public void Foo_StandaloneClass_CallbackCapturesArg()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "a", "b" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		[Fact]
		public void Foo_StandaloneClass_VerifyCallCount()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			obj.Foo([]);
			obj.Foo([]);

			stub.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Concrete param (Foo)

		[Fact]
		public void Foo_GenericStandaloneClass_CallbackCapturesArg()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "c", "d" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		[Fact]
		public void Foo_InlineClass_VerifyCallCount()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			obj.Foo([]);
			obj.Foo([]);

			stub.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Concrete param (Foo)

		[Fact]
		public void Foo_OpenGenericClass_CallbackCapturesArg()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			List<string>? captured = null;
			stub.Foo.Call((List<string> values) => captured = values);

			var input = new List<string> { "e", "f" };
			obj.Foo(input);

			Assert.Same(input, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Class type param (Quux) — when T=int, call Quux(3)
		// Quux(T) uses the class-level type parameter, not a method-level one.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Class type param (Quux)

		[Fact]
		public void Quux_StandaloneClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		[Fact]
		public void Quux_StandaloneClass_VerifyCallCount()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			obj.Quux(1);
			obj.Quux(2);

			stub.Quux.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Class type param (Quux)

		[Fact]
		public void Quux_GenericStandaloneClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		[Fact]
		public void Quux_InlineClass_VerifyCallCount()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			obj.Quux(1);
			obj.Quux(2);

			stub.Quux.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Class type param (Quux)

		[Fact]
		public void Quux_OpenGenericClass_CallbackCapturesClassTypeParamArg()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			int captured = 0;
			stub.Quux.Call((int value) => captured = value);

			obj.Quux(3);

			Assert.Equal(3, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Method type param (Bar) — call Bar<int>(3), Bar<string>("x")
		// Bar<TParam> is a generic method. Access via Of<T>() pattern.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Method type param (Bar)

		[Fact]
		public void Bar_StandaloneClass_DifferentTypeArgs()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

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
		// FooReturn() returns List<string>?, a concrete nullable type.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete return (FooReturn)

		[Fact]
		public void FooReturn_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var returnValue = new List<string> { "world" };
			stub.FooReturn.Return(returnValue);

			var result = obj.FooReturn();

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Class type param return (QuuxReturn) — configure, assert
		// QuuxReturn() returns T?, which is int? when T=int.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(42);

			var result = obj.QuuxReturn();

			Assert.Equal(42, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_GenericStandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(99);

			var result = obj.QuuxReturn();

			Assert.Equal(99, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_InlineClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(42);

			var result = obj.QuuxReturn();

			Assert.Equal(42, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Class type param return (QuuxReturn)

		[Fact]
		public void QuuxReturn_OpenGenericClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			stub.QuuxReturn.Return(99);

			var result = obj.QuuxReturn();

			Assert.Equal(99, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Method type param return (BarReturn) — per-type dispatch
		// BarReturn<TReturn>() uses Of<T>() pattern for configuration.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Method type param return (BarReturn)

		[Fact]
		public void BarReturn_StandaloneClass_DifferentReturnTypes()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 42);
			stub.BarReturn.Of<string>().Call(() => "hello");

			Assert.Equal(42, obj.BarReturn<int>());
			Assert.Equal("hello", obj.BarReturn<string>());
		}

		[Fact]
		public void BarReturn_StandaloneClass_VerifyPerTypeArg()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 42);
			stub.BarReturn.Of<string>().Call(() => "hello");

			Assert.Equal(42, obj.BarReturn<int>());
			Assert.Equal("hello", obj.BarReturn<string>());
		}

		[Fact]
		public void BarReturn_InlineClass_VerifyPerTypeArg()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

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
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			stub.BarReturn.Of<int>().Call(() => 99);
			stub.BarReturn.Of<string>().Call(() => "world");

			Assert.Equal(99, obj.BarReturn<int>());
			Assert.Equal("world", obj.BarReturn<string>());
		}

		#endregion

		// ====================================================================
		// Scenario 7: Nullable generic (NullableValues)
		// NullableValues<TData>(TData? data) => TData?
		// Method-level generic with nullable param and return.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		[Fact]
		public void NullableValues_StandaloneClass_HandlesNullInput()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => data == null ? "was-null" : data);

			var result = obj.NullableValues<string>(null);

			Assert.Equal("was-null", result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_GenericStandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_InlineClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		[Fact]
		public void NullableValues_InlineClass_HandlesNullInput()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => data == null ? "was-null" : data);

			var result = obj.NullableValues<string>(null);

			Assert.Equal("was-null", result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Nullable generic (NullableValues)

		[Fact]
		public void NullableValues_OpenGenericClass_ReturnsConfiguredValue()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			stub.NullableValues.Of<string>().Call((data) => "result-" + data);

			var result = obj.NullableValues("input");

			Assert.Equal("result-input", result);
		}

		#endregion

		// ====================================================================
		// Scenario 8: Unconfigured defaults
		// Verify that unconfigured calls return base implementation values.
		// For class stubs, unconfigured virtual methods fall through to base.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Unconfigured defaults

		[Fact]
		public void FooReturn_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			// base.FooReturn() => default => null
			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			// base.QuuxReturn() => default => null for int? (0 for int)
			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		[Fact]
		public void BarReturn_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			// base.BarReturn<int>() => default! => 0
			var result = obj.BarReturn<int>();

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Unconfigured defaults

		[Fact]
		public void FooReturn_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		[Fact]
		public void BarReturn_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.BarReturn<int>();

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Unconfigured defaults

		[Fact]
		public void FooReturn_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Unconfigured defaults

		[Fact]
		public void FooReturn_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.FooReturn();

			Assert.Null(result);
		}

		[Fact]
		public void QuuxReturn_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var result = obj.QuuxReturn();

			Assert.Equal(default, result);
		}

		#endregion

		// ====================================================================
		// Scenario 9: Virtual fallback — unconfigured void calls fall to base
		// Void virtual methods should call base without throwing.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Virtual fallback

		[Fact]
		public void Foo_StandaloneClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_StandaloneClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_StandaloneClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodStandaloneKnockOff();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion

		#region Pattern 6 (Inline Class): Virtual fallback

		[Fact]
		public void Foo_InlineClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_InlineClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_InlineClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodInlineTests.Stubs.ClassGenericMethod();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Virtual fallback

		[Fact]
		public void Foo_GenericStandaloneClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_GenericStandaloneClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_GenericStandaloneClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodGenericStandaloneKnockOff<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Virtual fallback

		[Fact]
		public void Foo_OpenGenericClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Foo([]));

			Assert.Null(exception);
		}

		[Fact]
		public void Quux_OpenGenericClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Quux(42));

			Assert.Null(exception);
		}

		[Fact]
		public void Bar_OpenGenericClass_UnconfiguredFallsToBaseWithoutThrowing()
		{
			var stub = new ClassGenericMethodOpenGenericTests.Stubs.ClassGenericMethod<int>();
			ClassGenericMethod<int> obj = stub.Object;

			var exception = Record.Exception(() => obj.Bar(99));

			Assert.Null(exception);
		}

		#endregion
	}
}
