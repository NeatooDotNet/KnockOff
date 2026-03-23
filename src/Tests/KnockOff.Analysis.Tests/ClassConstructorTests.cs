// ============================================================================
// ClassConstructorTests: Edge case tests for class constructor parameters
// Inspired by Rocks.Analysis.IntegrationTests.ClassConstructorTests
//
// Tests three categories of constructor edge cases:
// 1. Class with ref/out/params constructor parameters
// 2. Class with multiple constructors (public + protected)
// 3. Class with [SetsRequiredMembers] constructor
//
// Each edge case is tested with applicable patterns:
// - Pattern 3 (Standalone Class): [KnockOffBase<T>]
// - Pattern 6 (Inline Class): [KnockOff<T>]
// ============================================================================

using System.Diagnostics.CodeAnalysis;
using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassConstructorTestTypes
{
	// ========================================================================
	// Edge Case 1: Class with ref/out/params constructor parameters
	// ========================================================================

	public class ClassConstructorWithSpecialParameters
	{
		public ClassConstructorWithSpecialParameters(int a, ref string b, out string c, params string[] d)
		{
			c = "42";
			(this.A, this.B, this.C, this.D) = (a, b, c, d);
		}

		public virtual void Foo() { }

		public int A { get; }

		public string B { get; }

		public string C { get; }

#pragma warning disable CA1819 // Properties should not return arrays
		public string[] D { get; }
#pragma warning restore CA1819 // Properties should not return arrays
	}

	// ========================================================================
	// Edge Case 2: Class with multiple constructors (public + protected)
	// ========================================================================

	public class ClassConstructor
	{
		protected ClassConstructor(string stringData) =>
			this.StringData = stringData;
		public ClassConstructor(int intData) =>
			this.IntData = intData;

		public virtual int NoParameters() => default;

		public int IntData { get; }
		public string? StringData { get; }
	}

	// ========================================================================
	// Edge Case 3: Class with [SetsRequiredMembers] constructor
	// ========================================================================

	public class ClassRequiredConstructor
	{
		[SetsRequiredMembers]
		public ClassRequiredConstructor() { }

		public virtual void DoRequired() { }
	}

	// Pattern 3: Standalone class stubs
	[KnockOffBase<ClassConstructorWithSpecialParameters>]
	public partial class SpecialParamsStandaloneKnockOff
	{
	}

	[KnockOffBase<ClassConstructor>]
	public partial class ClassConstructorStandaloneKnockOff
	{
	}

	[KnockOffBase<ClassRequiredConstructor>]
	public partial class RequiredConstructorStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassConstructorTestTypes;

	// Pattern 6: Inline class stubs
	[KnockOff<ClassConstructorWithSpecialParameters>]
	[KnockOff<ClassConstructor>]
	[KnockOff<ClassRequiredConstructor>]
	public partial class ClassConstructorInlineTests
	{
	}

	public class ClassConstructorTests
	{
		// ====================================================================
		// Edge Case 1: ref/out/params constructor parameters
		// ====================================================================

		#region Standalone Class: ref/out/params constructor

		[Fact]
		public void RefOutParamsConstructor_StandaloneClass_PropertiesSetCorrectly()
		{
			var bValue = "b";
			var stub = new SpecialParamsStandaloneKnockOff(2, ref bValue, out var cValue, "d1", "d2");
			ClassConstructorWithSpecialParameters obj = stub.Object;

			Assert.Equal(2, obj.A);
			Assert.Equal("b", obj.B);
			Assert.Equal("42", obj.C);
			Assert.Equal("42", cValue);
			Assert.Equal(new[] { "d1", "d2" }, obj.D);
		}

		[Fact]
		public void RefOutParamsConstructor_StandaloneClass_VirtualMethodCanBeCalled()
		{
			var bValue = "b";
			var stub = new SpecialParamsStandaloneKnockOff(2, ref bValue, out _, "d1");
			ClassConstructorWithSpecialParameters obj = stub.Object;

			obj.Foo();

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void RefOutParamsConstructor_StandaloneClass_CallbackOnVirtualMethod()
		{
			var bValue = "b";
			var stub = new SpecialParamsStandaloneKnockOff(2, ref bValue, out _, "d1");
			ClassConstructorWithSpecialParameters obj = stub.Object;

			var callbackInvoked = false;
			stub.Foo.Call(() => { callbackInvoked = true; });

			obj.Foo();

			Assert.True(callbackInvoked);
		}

		[Fact]
		public void RefOutParamsConstructor_StandaloneClass_EmptyParamsArray()
		{
			var bValue = "b";
			var stub = new SpecialParamsStandaloneKnockOff(1, ref bValue, out var cValue);
			ClassConstructorWithSpecialParameters obj = stub.Object;

			Assert.Equal(1, obj.A);
			Assert.Equal("b", obj.B);
			Assert.Equal("42", obj.C);
			Assert.Equal("42", cValue);
			Assert.Empty(obj.D);
		}

		#endregion

		#region Inline Class: ref/out/params constructor

		[Fact]
		public void RefOutParamsConstructor_InlineClass_PropertiesSetCorrectly()
		{
			var bValue = "b";
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructorWithSpecialParameters(2, ref bValue, out var cValue, "d1", "d2");
			ClassConstructorWithSpecialParameters obj = stub.Object;

			Assert.Equal(2, obj.A);
			Assert.Equal("b", obj.B);
			Assert.Equal("42", obj.C);
			Assert.Equal("42", cValue);
			Assert.Equal(new[] { "d1", "d2" }, obj.D);
		}

		[Fact]
		public void RefOutParamsConstructor_InlineClass_VirtualMethodCanBeCalled()
		{
			var bValue = "b";
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructorWithSpecialParameters(2, ref bValue, out _, "d1");
			ClassConstructorWithSpecialParameters obj = stub.Object;

			obj.Foo();

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void RefOutParamsConstructor_InlineClass_CallbackOnVirtualMethod()
		{
			var bValue = "b";
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructorWithSpecialParameters(2, ref bValue, out _, "d1");
			ClassConstructorWithSpecialParameters obj = stub.Object;

			var callbackInvoked = false;
			stub.Foo.Call(() => { callbackInvoked = true; });

			obj.Foo();

			Assert.True(callbackInvoked);
		}

		[Fact]
		public void RefOutParamsConstructor_InlineClass_EmptyParamsArray()
		{
			var bValue = "b";
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructorWithSpecialParameters(1, ref bValue, out var cValue);
			ClassConstructorWithSpecialParameters obj = stub.Object;

			Assert.Equal(1, obj.A);
			Assert.Equal("b", obj.B);
			Assert.Equal("42", obj.C);
			Assert.Equal("42", cValue);
			Assert.Empty(obj.D);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Multiple constructors (public + protected)
		// ====================================================================

		#region Standalone Class: public constructor

		[Fact]
		public void PublicConstructor_StandaloneClass_IntDataSetCorrectly()
		{
			var stub = new ClassConstructorStandaloneKnockOff(3);
			ClassConstructor obj = stub.Object;

			Assert.Equal(3, obj.IntData);
			Assert.Null(obj.StringData);
		}

		[Fact]
		public void PublicConstructor_StandaloneClass_VirtualMethodCanBeCalled()
		{
			var stub = new ClassConstructorStandaloneKnockOff(3);
			ClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			// Unconfigured virtual method falls through to base which returns default (0)
			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		[Fact]
		public void PublicConstructor_StandaloneClass_ConfiguredReturnValue()
		{
			var stub = new ClassConstructorStandaloneKnockOff(3);
			ClassConstructor obj = stub.Object;

			stub.NoParameters.Return(42);

			var result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Standalone Class: protected constructor

		[Fact]
		public void ProtectedConstructor_StandaloneClass_StringDataSetCorrectly()
		{
			var stub = new ClassConstructorStandaloneKnockOff("b");
			ClassConstructor obj = stub.Object;

			Assert.Equal(0, obj.IntData);
			Assert.Equal("b", obj.StringData);
		}

		[Fact]
		public void ProtectedConstructor_StandaloneClass_VirtualMethodCanBeCalled()
		{
			var stub = new ClassConstructorStandaloneKnockOff("b");
			ClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		#endregion

		#region Inline Class: public constructor

		[Fact]
		public void PublicConstructor_InlineClass_IntDataSetCorrectly()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructor(3);
			ClassConstructor obj = stub.Object;

			Assert.Equal(3, obj.IntData);
			Assert.Null(obj.StringData);
		}

		[Fact]
		public void PublicConstructor_InlineClass_VirtualMethodCanBeCalled()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructor(3);
			ClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		[Fact]
		public void PublicConstructor_InlineClass_ConfiguredReturnValue()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructor(3);
			ClassConstructor obj = stub.Object;

			stub.NoParameters.Return(42);

			var result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Inline Class: protected constructor

		[Fact]
		public void ProtectedConstructor_InlineClass_StringDataSetCorrectly()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructor("b");
			ClassConstructor obj = stub.Object;

			Assert.Equal(0, obj.IntData);
			Assert.Equal("b", obj.StringData);
		}

		[Fact]
		public void ProtectedConstructor_InlineClass_VirtualMethodCanBeCalled()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassConstructor("b");
			ClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 3: [SetsRequiredMembers] constructor
		// ====================================================================

		#region Standalone Class: [SetsRequiredMembers] constructor

		[Fact]
		public void RequiredConstructor_StandaloneClass_CanInstantiate()
		{
			var stub = new RequiredConstructorStandaloneKnockOff();
			ClassRequiredConstructor obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void RequiredConstructor_StandaloneClass_VirtualMethodCanBeCalled()
		{
			var stub = new RequiredConstructorStandaloneKnockOff();
			ClassRequiredConstructor obj = stub.Object;

			obj.DoRequired();

			stub.DoRequired.Verify(Called.Once);
		}

		[Fact]
		public void RequiredConstructor_StandaloneClass_CallbackOnVirtualMethod()
		{
			var stub = new RequiredConstructorStandaloneKnockOff();
			ClassRequiredConstructor obj = stub.Object;

			var callbackInvoked = false;
			stub.DoRequired.Call(() => { callbackInvoked = true; });

			obj.DoRequired();

			Assert.True(callbackInvoked);
		}

		#endregion

		#region Inline Class: [SetsRequiredMembers] constructor

		[Fact]
		public void RequiredConstructor_InlineClass_CanInstantiate()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassRequiredConstructor();
			ClassRequiredConstructor obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void RequiredConstructor_InlineClass_VirtualMethodCanBeCalled()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassRequiredConstructor();
			ClassRequiredConstructor obj = stub.Object;

			obj.DoRequired();

			stub.DoRequired.Verify(Called.Once);
		}

		[Fact]
		public void RequiredConstructor_InlineClass_CallbackOnVirtualMethod()
		{
			var stub = new ClassConstructorInlineTests.Stubs.ClassRequiredConstructor();
			ClassRequiredConstructor obj = stub.Object;

			var callbackInvoked = false;
			stub.DoRequired.Call(() => { callbackInvoked = true; });

			obj.DoRequired();

			Assert.True(callbackInvoked);
		}

		#endregion
	}
}
