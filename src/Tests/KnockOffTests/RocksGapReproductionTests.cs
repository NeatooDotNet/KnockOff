namespace KnockOff.Tests;

/// <summary>
/// Reproduction tests for gaps discovered while using KnockOff in Rocks test library.
/// Each gap is numbered to match the original findings.
///
/// REPRODUCTION RESULTS:
/// - Gap 25 (ref returns): NOT REPRODUCED — KnockOff handles ref returns correctly
/// - Gap 26 (in params): REPRODUCED — generator strips 'in' from indexer params (CS0535/CS0539)
/// - Gap 27 (out params): REPRODUCED — generic methods with out params fail (CS1615) in inline pattern
/// - Gap 28 (ref params): REPRODUCED — generic methods with ref params fail (CS1615) in inline pattern
/// - Gap 29 (all interfaces): NEEDS INVESTIGATION — requires examining generator predicate/filter
/// - Gap 30 (multiple closed generics): NOT REPRODUCED — KnockOff generates both stubs correctly
/// - Gap 31 (generic methods 2+ type params): REPRODUCED — both standalone AND inline fail
///     Inline: TInput leaks outside method scope (CS0246)
///     Standalone: Of&lt;TReturn&gt;() only takes 1 type arg but method has 2 (CS0305)
/// </summary>

// ============================================================================
// Gap 25: ref return values — NOT REPRODUCED
// KnockOff generates correct code for ref/ref readonly returns.
// Already covered by RefReturnTests.cs (standalone + inline + class patterns).
// ============================================================================

public interface IGap25_RefReturn
{
	ref int MethodRefReturn();
	ref int PropertyRefReturn { get; }
	ref int this[int a] { get; }
	ref readonly int MethodRefReadonlyReturn();
	ref readonly int PropertyRefReadonlyReturn { get; }
	ref readonly int this[string a] { get; }
}

[KnockOff<IGap25_RefReturn>]
public partial class Gap25InlineTest { }

// ============================================================================
// Gap 26: in parameters — REPRODUCED
// BUG: Generator strips 'in' modifier from indexer parameters.
// - Method params: 'in' correctly preserved in InArgument(in int a) ✓
// - Indexer params: 'in' STRIPPED → this[int a] instead of this[in int a] ✗
// Errors: CS0535 (does not implement member), CS0539 (not found in interface)
// Affects: Both standalone and inline patterns. Indexer pipeline only.
// ============================================================================

public interface IGap26_InParameter
{
	void InArgument(in int a);
	int this[in int a] { get; }
}

[KnockOff<IGap26_InParameter>]
public partial class Gap26InlineTest { }

[KnockOff]
public partial class Gap26StandaloneKnockOff : IGap26_InParameter { }

// ============================================================================
// Gap 27: out parameters on generic methods — REPRODUCED
// Non-generic out params work fine (OutParameterTests.cs proves it).
// BUG: Generic methods with out params fail in inline pattern.
// The generated invoke call passes the out param directly but the interceptor
// method signature doesn't accept it as out. CS1615.
// Affects: Inline pattern for generic methods with out params.
// Non-generic out methods compile fine.
// ============================================================================

public interface IGap27_OutParameter
{
	void OutArgument(out int a);              // Works fine
	void OutArgumentsWithGenerics<T1, T2>(T1 a, out T2 b);  // FAILS in inline
}

[KnockOff<IGap27_OutParameter>]
public partial class Gap27InlineTest { }

// Test that non-generic out works in inline
public interface IGap27_OutParameterSimple
{
	void OutArgument(out int a);
}

[KnockOff<IGap27_OutParameterSimple>]
public partial class Gap27SimpleInlineTest { }

// ============================================================================
// Gap 28: ref parameters on generic methods — REPRODUCED
// Non-generic ref params work fine (RefParameterTests.cs proves it).
// BUG: Same root cause as Gap 27. Generic methods with ref params fail in inline.
// CS1615: Argument 2 may not be passed with the 'ref' keyword.
// Affects: Inline pattern for generic methods with ref params.
// ============================================================================

public interface IGap28_RefParameter
{
	void RefArgument(ref int a);              // Works fine
	void RefArgumentsWithGenerics<T1, T2>(T1 a, ref T2 b);  // FAILS in inline
}

[KnockOff<IGap28_RefParameter>]
public partial class Gap28InlineTest { }

// Test that non-generic ref works in inline
public interface IGap28_RefParameterSimple
{
	void RefArgument(ref int a);
}

[KnockOff<IGap28_RefParameterSimple>]
public partial class Gap28SimpleInlineTest { }

// ============================================================================
// Gap 29: Stubs generated for all interfaces in namespace — NEEDS INVESTIGATION
// Cannot easily reproduce in KnockOffTests without contaminating other tests.
// The reported behavior is that [KnockOff<IFoo>] generates stubs for ALL
// interfaces in IFoo's namespace, not just IFoo.
// ============================================================================

// ============================================================================
// Gap 30: Multiple closed generic stubs of same open generic — NOT REPRODUCED
// KnockOff generates both stubs correctly with mangled names:
// - IGap30_GenericServiceInt32String (for IGap30_GenericService<int, string>)
// - IGap30_GenericServiceStringInt32 (for IGap30_GenericService<string, int>)
// ============================================================================

public interface IGap30_GenericService<T, TReturn>
{
	TReturn Service(T data);
}

[KnockOff<IGap30_GenericService<int, string>>]
[KnockOff<IGap30_GenericService<string, int>>]
public partial class Gap30ClosedGenericTest { }

// ============================================================================
// Gap 31: Generic methods with 2+ type parameters — REPRODUCED (ALL patterns)
// BUG 1 (Inline): Interceptor class emits "TInput" as member type but TInput
//   is a method-level type parameter. CS0246: TInput not found.
// BUG 2 (Standalone): Run.Of<TInput, TReturn>() called but only Of<TReturn>()
//   exists (1 type arg). CS0305. Also delegate doesn't accept the input param.
// Root cause: The generic method interceptor doesn't support methods with more
//   than 1 type parameter. It only generates Of<T>() for single-type-param methods.
// ============================================================================

public interface IGap31_GenericMethods
{
	void Sprint<TReturn>();
	TReturn Run<TReturn>() where TReturn : new();
	TReturn Run<TInput, TReturn>(TInput input) where TReturn : new();
}

[KnockOff<IGap31_GenericMethods>]
public partial class Gap31InlineTest { }

[KnockOff]
public partial class Gap31StandaloneKnockOff : IGap31_GenericMethods { }

// Test that single-type-param generic methods work fine
public interface IGap31_SingleTypeParamOnly
{
	void Sprint<TReturn>();
	TReturn Run<TReturn>() where TReturn : new();
}

[KnockOff<IGap31_SingleTypeParamOnly>]
public partial class Gap31SingleTypeInlineTest { }

[KnockOff]
public partial class Gap31SingleTypeStandaloneKnockOff : IGap31_SingleTypeParamOnly { }

// ============================================================================
// Test class — only tests for gaps that compile successfully
// ============================================================================

public class RocksGapReproductionTests
{
	// ---- Gap 25: ref returns (inline) — PASS ----

	[Fact]
	public void Gap25_RefReturn_InlineCompiles()
	{
		var stub = new Gap25InlineTest.Stubs.IGap25_RefReturn();
		IGap25_RefReturn service = stub;
		Assert.NotNull(stub);
	}

	[Fact]
	public void Gap25_RefReturn_MethodRefReturn()
	{
		var stub = new Gap25InlineTest.Stubs.IGap25_RefReturn();
		IGap25_RefReturn service = stub;
		ref int result = ref service.MethodRefReturn();
		stub.MethodRefReturn.Verify(Called.Once);
	}

	[Fact]
	public void Gap25_RefReturn_RefReadonly()
	{
		var stub = new Gap25InlineTest.Stubs.IGap25_RefReturn();
		IGap25_RefReturn service = stub;
		ref readonly int result = ref service.MethodRefReadonlyReturn();
		stub.MethodRefReadonlyReturn.Verify(Called.Once);
	}

	// ---- Gap 27: non-generic out params still work in inline ----

	[Fact]
	public void Gap27_SimpleOutParam_InlineCompiles()
	{
		var stub = new Gap27SimpleInlineTest.Stubs.IGap27_OutParameterSimple();
		IGap27_OutParameterSimple service = stub;
		service.OutArgument(out var value);
		Assert.Equal(0, value);
	}

	// ---- Gap 28: non-generic ref params still work in inline ----

	[Fact]
	public void Gap28_SimpleRefParam_InlineCompiles()
	{
		var stub = new Gap28SimpleInlineTest.Stubs.IGap28_RefParameterSimple();
		IGap28_RefParameterSimple service = stub;
		int value = 42;
		service.RefArgument(ref value);
	}

	// ---- Gap 30: multiple closed generic stubs — PASS ----

	[Fact]
	public void Gap30_MultipleClosedGenerics_BothGenerate()
	{
		var intStringStub = new Gap30ClosedGenericTest.Stubs.IGap30_GenericServiceInt32String();
		var stringIntStub = new Gap30ClosedGenericTest.Stubs.IGap30_GenericServiceStringInt32();

		IGap30_GenericService<int, string> service1 = intStringStub;
		IGap30_GenericService<string, int> service2 = stringIntStub;

		Assert.NotNull(service1);
		Assert.NotNull(service2);
	}

	[Fact]
	public void Gap30_MultipleClosedGenerics_ReturnValues()
	{
		var intStringStub = new Gap30ClosedGenericTest.Stubs.IGap30_GenericServiceInt32String();
		intStringStub.Service.Return((data) => $"value:{data}");

		IGap30_GenericService<int, string> service = intStringStub;
		Assert.Equal("value:3", service.Service(3));
	}

	// ---- Gap 31: single type param generic methods work fine ----

	[Fact]
	public void Gap31_SingleTypeParam_InlineCompiles()
	{
		var stub = new Gap31SingleTypeInlineTest.Stubs.IGap31_SingleTypeParamOnly();
		IGap31_SingleTypeParamOnly service = stub;
		Assert.NotNull(stub);
	}

	[Fact]
	public void Gap31_SingleTypeParam_StandaloneCompiles()
	{
		var knockOff = new Gap31SingleTypeStandaloneKnockOff();
		IGap31_SingleTypeParamOnly service = knockOff;
		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Gap31_MixedArity_InlineCompiles()
	{
		var stub = new Gap31InlineTest.Stubs.IGap31_GenericMethods();
		IGap31_GenericMethods service = stub;
		Assert.NotNull(stub);
	}

	[Fact]
	public void Gap31_MixedArity_StandaloneCompiles()
	{
		var knockOff = new Gap31StandaloneKnockOff();
		IGap31_GenericMethods service = knockOff;
		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Gap31_MixedArity_InlineCanConfigureBothArities()
	{
		var stub = new Gap31InlineTest.Stubs.IGap31_GenericMethods();
		// Single-type-param arity: Run<TReturn>()
		stub.Run.Of<int>().Return(() => 42);
		// Two-type-param arity: Run<TInput, TReturn>(TInput input)
		stub.Run.Of<string, int>().Return((input) => 99);
		IGap31_GenericMethods service = stub;
		Assert.Equal(42, service.Run<int>());
		Assert.Equal(99, service.Run<string, int>("hello"));
	}

	[Fact]
	public void Gap31_MixedArity_StandaloneCanConfigureBothArities()
	{
		var knockOff = new Gap31StandaloneKnockOff();
		// Single-type-param arity: Run<TReturn>()
		knockOff.Run.Of<int>().Return(() => 42);
		// Two-type-param arity: Run<TInput, TReturn>(TInput input)
		knockOff.Run.Of<string, int>().Return((input) => 99);
		IGap31_GenericMethods service = knockOff;
		Assert.Equal(42, service.Run<int>());
		Assert.Equal(99, service.Run<string, int>("hello"));
	}
}
