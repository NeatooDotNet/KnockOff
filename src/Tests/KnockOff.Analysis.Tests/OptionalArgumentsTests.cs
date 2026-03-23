// ============================================================================
// OptionalArgumentsTests: Edge case tests for optional/default parameter values
// Inspired by Rocks.Analysis.IntegrationTests.OptionalArgumentsTests
//
// Tests three categories of optional argument edge cases:
// 1. Interface with optional arguments on methods and indexers
// 2. Interface with optional struct default argument
// 3. Class with nullable annotation context shift (methods declared in
//    #nullable disable context where `object` with `= null` default is valid)
//
// Each edge case is tested with applicable patterns:
// - Pattern 1 (Standalone Interface) for interfaces
// - Pattern 3 (Standalone Class) for classes
// - Pattern 5 (Inline Interface) for interfaces
// - Pattern 6 (Inline Class) for classes
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.OptionalArgumentsTestTypes
{
	// ========================================================================
	// Edge Case 1: Interface with optional arguments on methods and indexers
	// ========================================================================

	public interface IHaveOptionalArguments
	{
		void Foo(int a, string b = "b", double c = 3.2);
		int this[int a, string b = "b"] { get; set; }
	}

	// Pattern 1: Standalone stub
	[KnockOff]
	public partial class OptionalArgsStandaloneKnockOff : IHaveOptionalArguments
	{
	}

	// ========================================================================
	// Edge Case 2: Interface with optional struct default argument
	// ========================================================================

	public struct OptionalDefault { }

	public interface IHaveOptionalStructDefaultArgument
	{
		void Foo(OptionalDefault value = default);
	}

	// Pattern 1: Standalone stub
	[KnockOff]
	public partial class OptionalStructDefaultStandaloneKnockOff : IHaveOptionalStructDefaultArgument
	{
	}

	// ========================================================================
	// Edge Case 3: Class with nullable annotation context shift
	// Methods declared in #nullable disable context where object = null is
	// valid, then #nullable restore changes the context.
	// ========================================================================

#nullable disable
#pragma warning disable IDE0060 // Remove unused parameter
	public class NeedNullableAnnotation
	{
		public NeedNullableAnnotation(object initializationData = null) { }

		public virtual int IntReturn(object initializationData = null) =>
			initializationData is null ? 1 : 0;
		public virtual void VoidReturn(object initializationData = null) =>
			this.VoidReturnData = initializationData;
#nullable restore

		public object? VoidReturnData { get; private set; }
	}
#pragma warning restore IDE0060

	// Pattern 3: Standalone class stub
	[KnockOffBase<NeedNullableAnnotation>]
	public partial class NullableAnnotationStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.OptionalArgumentsTestTypes;

	// Pattern 5: Inline stubs (interfaces)
	// Pattern 6: Inline stub (class)
	[KnockOff<IHaveOptionalArguments>]
	[KnockOff<IHaveOptionalStructDefaultArgument>]
	[KnockOff<NeedNullableAnnotation>]
	public partial class OptionalArgumentsInlineTests
	{
	}

	public class OptionalArgumentsTests
	{
		// ====================================================================
		// Edge Case 1: Interface with optional arguments on methods and indexers
		// ====================================================================

		#region Standalone: optional args method

		[Fact]
		public void OptionalArgs_Standalone_CallWithAllOptionalArgsOmitted()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			// Call with only required arg — b and c should use defaults
			service.Foo(1);

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalArgs_Standalone_CallWithSomeOptionalArgsSpecified()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			// Call with a and b — c should use default
			service.Foo(1, "custom");

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalArgs_Standalone_CallWithAllArgsSpecified()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			// Call with all args explicitly
			service.Foo(1, "custom", 5.0);

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalArgs_Standalone_CallbackReceivesDefaultValues()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			int capturedA = 0;
			string? capturedB = null;
			double capturedC = 0;
			knockOff.Foo.Call((int a, string b, double c) =>
			{
				capturedA = a;
				capturedB = b;
				capturedC = c;
			});

			// Call with defaults — compiler fills in "b" and 3.2
			service.Foo(42);

			Assert.Equal(42, capturedA);
			Assert.Equal("b", capturedB);
			Assert.Equal(3.2, capturedC);
		}

		[Fact]
		public void OptionalArgs_Standalone_CallbackReceivesExplicitValues()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			int capturedA = 0;
			string? capturedB = null;
			double capturedC = 0;
			knockOff.Foo.Call((int a, string b, double c) =>
			{
				capturedA = a;
				capturedB = b;
				capturedC = c;
			});

			// Call with explicit values
			service.Foo(10, "explicit", 9.9);

			Assert.Equal(10, capturedA);
			Assert.Equal("explicit", capturedB);
			Assert.Equal(9.9, capturedC);
		}

		[Fact]
		public void OptionalArgs_Standalone_VerifyMultipleCalls()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			service.Foo(1);
			service.Foo(2, "x");
			service.Foo(3, "y", 7.7);

			knockOff.Foo.Verify(Called.Exactly(3));
		}

		#endregion

		#region Standalone: optional args indexer

		[Fact]
		public void OptionalIndexer_Standalone_GetWithOptionalArgOmitted()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			knockOff.Indexer.Get(key => key.a * 10);

			// Call with only required key — b defaults to "b"
			var result = service[1];

			Assert.Equal(10, result);
		}

		[Fact]
		public void OptionalIndexer_Standalone_GetWithOptionalArgSpecified()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			knockOff.Indexer.Get(key => key.a * 100);

			// Call with explicit optional arg
			var result = service[2, "custom"];

			Assert.Equal(200, result);
		}

		[Fact]
		public void OptionalIndexer_Standalone_SetWithOptionalArgOmitted()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			// Set indexer with only required key — b defaults to "b"
			service[1] = 42;

			knockOff.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void OptionalIndexer_Standalone_SetWithOptionalArgSpecified()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			// Set indexer with explicit optional arg
			service[1, "custom"] = 99;

			knockOff.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void OptionalIndexer_Standalone_VerifyGetAndSet()
		{
			var knockOff = new OptionalArgsStandaloneKnockOff();
			IHaveOptionalArguments service = knockOff;

			knockOff.Indexer.Get(key => 0);

			_ = service[1];
			_ = service[2, "x"];
			service[3] = 10;

			knockOff.Indexer.VerifyGet(Called.Exactly(2));
			knockOff.Indexer.VerifySet(Called.Once);
		}

		#endregion

		#region Inline: optional args method

		[Fact]
		public void OptionalArgs_Inline_CompilesAndCanCall()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			service.Foo(1);

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalArgs_Inline_CallbackReceivesDefaultValues()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			int capturedA = 0;
			string? capturedB = null;
			double capturedC = 0;
			stub.Foo.Call((int a, string b, double c) =>
			{
				capturedA = a;
				capturedB = b;
				capturedC = c;
			});

			service.Foo(42);

			Assert.Equal(42, capturedA);
			Assert.Equal("b", capturedB);
			Assert.Equal(3.2, capturedC);
		}

		[Fact]
		public void OptionalArgs_Inline_CallWithSomeArgsSpecified()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			string? capturedB = null;
			stub.Foo.Call((int a, string b, double c) =>
			{
				capturedB = b;
			});

			service.Foo(1, "overridden");

			Assert.Equal("overridden", capturedB);
		}

		[Fact]
		public void OptionalArgs_Inline_CallWithAllArgsSpecified()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			double capturedC = 0;
			stub.Foo.Call((int a, string b, double c) =>
			{
				capturedC = c;
			});

			service.Foo(1, "x", 99.9);

			Assert.Equal(99.9, capturedC);
		}

		#endregion

		#region Inline: optional args indexer

		[Fact]
		public void OptionalIndexer_Inline_GetWithOptionalArgOmitted()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			stub.Indexer.Get(key => key.a * 10);

			var result = service[5];

			Assert.Equal(50, result);
		}

		[Fact]
		public void OptionalIndexer_Inline_GetWithOptionalArgSpecified()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			stub.Indexer.Get(key => key.a * 100);

			var result = service[3, "custom"];

			Assert.Equal(300, result);
		}

		[Fact]
		public void OptionalIndexer_Inline_SetWithOptionalArgOmitted()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			service[1] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void OptionalIndexer_Inline_SetWithOptionalArgSpecified()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalArguments();
			IHaveOptionalArguments service = stub;

			service[2, "custom"] = 99;

			stub.Indexer.VerifySet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Interface with optional struct default argument
		// ====================================================================

		#region Standalone: optional struct default

		[Fact]
		public void OptionalStructDefault_Standalone_CompilesAndCanCall()
		{
			var knockOff = new OptionalStructDefaultStandaloneKnockOff();
			IHaveOptionalStructDefaultArgument service = knockOff;

			// Call with no args — value defaults to default(OptionalDefault)
			service.Foo();

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalStructDefault_Standalone_CallWithExplicitDefault()
		{
			var knockOff = new OptionalStructDefaultStandaloneKnockOff();
			IHaveOptionalStructDefaultArgument service = knockOff;

			// Call with explicit default
			service.Foo(default);

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalStructDefault_Standalone_CallWithExplicitValue()
		{
			var knockOff = new OptionalStructDefaultStandaloneKnockOff();
			IHaveOptionalStructDefaultArgument service = knockOff;

			// Call with explicit value
			service.Foo(new OptionalDefault());

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalStructDefault_Standalone_CallbackReceivesValue()
		{
			var knockOff = new OptionalStructDefaultStandaloneKnockOff();
			IHaveOptionalStructDefaultArgument service = knockOff;

			OptionalDefault capturedValue = default;
			bool callbackInvoked = false;
			knockOff.Foo.Call((OptionalDefault value) =>
			{
				capturedValue = value;
				callbackInvoked = true;
			});

			service.Foo();

			Assert.True(callbackInvoked);
			Assert.Equal(default(OptionalDefault), capturedValue);
		}

		#endregion

		#region Inline: optional struct default

		[Fact]
		public void OptionalStructDefault_Inline_CompilesAndCanCall()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalStructDefaultArgument();
			IHaveOptionalStructDefaultArgument service = stub;

			service.Foo();

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void OptionalStructDefault_Inline_CallbackReceivesValue()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.IHaveOptionalStructDefaultArgument();
			IHaveOptionalStructDefaultArgument service = stub;

			bool callbackInvoked = false;
			stub.Foo.Call((OptionalDefault value) =>
			{
				callbackInvoked = true;
			});

			service.Foo();

			Assert.True(callbackInvoked);
		}

		#endregion

		// ====================================================================
		// Edge Case 3: Class with nullable annotation context shift
		// ====================================================================

		#region Standalone Class: nullable annotation

		[Fact]
		public void NullableAnnotation_StandaloneClass_IntReturn_DefaultNull()
		{
			var stub = new NullableAnnotationStandaloneKnockOff();
			NeedNullableAnnotation obj = stub.Object;

			stub.IntReturn.Call((object initializationData) => 42);

			// Call with default null
			var result = obj.IntReturn();

			Assert.Equal(42, result);
		}

		[Fact]
		public void NullableAnnotation_StandaloneClass_IntReturn_ExplicitArg()
		{
			var stub = new NullableAnnotationStandaloneKnockOff();
			NeedNullableAnnotation obj = stub.Object;

			stub.IntReturn.Call((object initializationData) =>
				initializationData is null ? 1 : 0);

			var result = obj.IntReturn("not null");

			Assert.Equal(0, result);
		}

		[Fact]
		public void NullableAnnotation_StandaloneClass_VoidReturn_DefaultNull()
		{
			var stub = new NullableAnnotationStandaloneKnockOff();
			NeedNullableAnnotation obj = stub.Object;

			object? capturedData = "sentinel";
			stub.VoidReturn.Call((object initializationData) =>
			{
				capturedData = initializationData;
			});

			// Call with default null
			obj.VoidReturn();

			Assert.Null(capturedData);
		}

		[Fact]
		public void NullableAnnotation_StandaloneClass_VoidReturn_ExplicitArg()
		{
			var stub = new NullableAnnotationStandaloneKnockOff();
			NeedNullableAnnotation obj = stub.Object;

			object? capturedData = null;
			stub.VoidReturn.Call((object initializationData) =>
			{
				capturedData = initializationData;
			});

			obj.VoidReturn("some data");

			Assert.Equal("some data", capturedData);
		}

		[Fact]
		public void NullableAnnotation_StandaloneClass_VerifyMultipleCalls()
		{
			var stub = new NullableAnnotationStandaloneKnockOff();
			NeedNullableAnnotation obj = stub.Object;

			obj.IntReturn();
			obj.IntReturn("with data");
			obj.VoidReturn();

			stub.IntReturn.Verify(Called.Exactly(2));
			stub.VoidReturn.Verify(Called.Once);
		}

		#endregion

		#region Inline Class: nullable annotation

		[Fact]
		public void NullableAnnotation_InlineClass_IntReturn_DefaultNull()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.NeedNullableAnnotation();
			NeedNullableAnnotation obj = stub.Object;

			stub.IntReturn.Call((object initializationData) => 99);

			var result = obj.IntReturn();

			Assert.Equal(99, result);
		}

		[Fact]
		public void NullableAnnotation_InlineClass_VoidReturn_DefaultNull()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.NeedNullableAnnotation();
			NeedNullableAnnotation obj = stub.Object;

			object? capturedData = "sentinel";
			stub.VoidReturn.Call((object initializationData) =>
			{
				capturedData = initializationData;
			});

			obj.VoidReturn();

			Assert.Null(capturedData);
		}

		[Fact]
		public void NullableAnnotation_InlineClass_VerifyCalls()
		{
			var stub = new OptionalArgumentsInlineTests.Stubs.NeedNullableAnnotation();
			NeedNullableAnnotation obj = stub.Object;

			obj.IntReturn();
			obj.VoidReturn();

			stub.IntReturn.Verify(Called.Once);
			stub.VoidReturn.Verify(Called.Once);
		}

		#endregion
	}
}
