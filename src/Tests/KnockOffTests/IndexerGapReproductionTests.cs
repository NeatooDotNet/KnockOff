namespace KnockOff.Tests;

// =============================================================================
// Gap #3: Multi-parameter indexers
// Rocks: expectations.Setups[3, "b"].Gets()
// KnockOff: indexer API only supports single-key
// =============================================================================

public interface IMultiParamIndexerGetterSetter
{
	int this[int a, string b] { get; set; }
}

public interface IMultiParamIndexerGetter
{
	int this[int a, string b] { get; }
}

// =============================================================================
// N-parameter indexers (3+)
// Verifies the logic is generic, not hardcoded for 2 params
// =============================================================================

public interface IThreeParamIndexer
{
	double this[int x, int y, int z] { get; set; }
}

public interface IFourParamIndexer
{
	string this[string schema, string table, string column, int index] { get; }
}

// =============================================================================
// Gap #4: Init-only indexers
// Rocks: int this[int a] { get; init; }
// KnockOff: no init accessor support for indexers
// =============================================================================

public interface IInitIndexer
{
	int this[int a] { get; init; }
}

public interface IInitOnlyIndexer
{
#pragma warning disable CA1044
	int this[int a] { init; }
#pragma warning restore CA1044
}

// =============================================================================
// Bug #35: Init-only indexers on classes produce CS8853
// Generator emitted 'set' instead of 'init' in class override
// =============================================================================

public abstract class AbstractInitIndexer
{
	public abstract int this[int a] { get; init; }
}

public class VirtualInitIndexer
{
	public VirtualInitIndexer() { }

	public virtual int this[int a]
	{
		get => 0;
		init { }
	}
}

// =============================================================================
// Gap #17: Multi-parameter indexers with params arrays
// Rocks: int this[int a, params string[] b] { get; }
// KnockOff: generator fails to produce valid code
// =============================================================================

public interface IParamsIndexer
{
	int this[int a, params string[] b] { get; }
}

// =============================================================================
// Inline stubs for all gap interfaces
// =============================================================================

[KnockOff<IMultiParamIndexerGetterSetter>]
[KnockOff<IMultiParamIndexerGetter>]
[KnockOff<IThreeParamIndexer>]
[KnockOff<IFourParamIndexer>]
[KnockOff<IInitIndexer>]
[KnockOff<IInitOnlyIndexer>]
[KnockOff<IParamsIndexer>]
[KnockOff<AbstractInitIndexer>]
[KnockOff<VirtualInitIndexer>]
public partial class IndexerGapTestClass
{
}

// =============================================================================
// Standalone stubs for gap interfaces
// =============================================================================

[KnockOff]
public partial class MultiParamIndexerGetterSetterKnockOff : IMultiParamIndexerGetterSetter
{
}

[KnockOff]
public partial class MultiParamIndexerGetterKnockOff : IMultiParamIndexerGetter
{
}

[KnockOff]
public partial class InitIndexerKnockOff : IInitIndexer
{
}

[KnockOff]
public partial class InitOnlyIndexerKnockOff : IInitOnlyIndexer
{
}

[KnockOff]
public partial class ParamsIndexerKnockOff : IParamsIndexer
{
}

[KnockOff]
public partial class ThreeParamIndexerKnockOff : IThreeParamIndexer
{
}

[KnockOff]
public partial class FourParamIndexerKnockOff : IFourParamIndexer
{
}

// Bug #35: Standalone class stubs for init-only indexers

[KnockOffBase<AbstractInitIndexer>]
public partial class AbstractInitIndexerStandaloneStub
{
}

[KnockOffBase<VirtualInitIndexer>]
public partial class VirtualInitIndexerStandaloneStub
{
}

// =============================================================================
// Tests
// =============================================================================

/// <summary>
/// Reproduction tests for indexer gaps identified from Rocks testing comparison.
/// These tests document what works, what doesn't compile, and what behaves incorrectly.
/// </summary>
public class IndexerGapReproductionTests
{
	// =========================================================================
	// Gap #3: Multi-parameter indexers
	// =========================================================================

	[Fact]
	public void Gap3_Inline_MultiParamIndexer_GetFromBacking()
	{
		// Arrange - multi-param indexer should use tuple key
		var stub = new IndexerGapTestClass.Stubs.IMultiParamIndexerGetterSetter();

		// Per-key configuration using multi-param flattened indexer accessor
		stub.Indexer[3, "b"].Returns(42);

		// Act
		IMultiParamIndexerGetterSetter svc = stub;
		var value = svc[3, "b"];

		// Assert
		Assert.Equal(42, value);
	}

	[Fact]
	public void Gap3_Inline_MultiParamIndexer_GetCallback()
	{
		// Arrange
		var stub = new IndexerGapTestClass.Stubs.IMultiParamIndexerGetterSetter();

		// Get callback receives the key as a tuple (for multi-param indexers)
		stub.Indexer.Get(key => key.a + key.b.Length);

		// Act
		IMultiParamIndexerGetterSetter svc = stub;
		var value = svc[3, "hello"];

		// Assert - 3 + 5 = 8
		Assert.Equal(8, value);
	}

	[Fact]
	public void Gap3_Inline_MultiParamIndexer_SetCallback()
	{
		// Arrange
		var stub = new IndexerGapTestClass.Stubs.IMultiParamIndexerGetterSetter();

		(int a, string b, int val)? captured = null;
		stub.Indexer.Set((key, value) =>
		{
			captured = (key.a, key.b, value);
		});

		// Act
		IMultiParamIndexerGetterSetter svc = stub;
		svc[3, "b"] = 42;

		// Assert
		Assert.NotNull(captured);
		Assert.Equal(3, captured.Value.a);
		Assert.Equal("b", captured.Value.b);
		Assert.Equal(42, captured.Value.val);
	}

	[Fact]
	public void Gap3_Inline_MultiParamIndexer_Tracking()
	{
		// Arrange
		var stub = new IndexerGapTestClass.Stubs.IMultiParamIndexerGetterSetter();
		stub.Indexer[1, "x"].Returns(10);

		// Act
		IMultiParamIndexerGetterSetter svc = stub;
		_ = svc[1, "x"];

		// Assert
		stub.Indexer.VerifyGet(Called.Once);
		Assert.Equal((1, "x"), stub.Indexer.LastGetKey);
	}

	[Fact]
	public void Gap3_Inline_MultiParamIndexer_GetOnly()
	{
		// Arrange
		var stub = new IndexerGapTestClass.Stubs.IMultiParamIndexerGetter();
		stub.Indexer[3, "b"].Returns(99);

		// Act
		IMultiParamIndexerGetter svc = stub;
		var value = svc[3, "b"];

		// Assert
		Assert.Equal(99, value);
	}

	[Fact]
	public void Gap3_Standalone_MultiParamIndexer_GetFromBacking()
	{
		// Arrange
		var stub = new MultiParamIndexerGetterSetterKnockOff();
		stub.Indexer[3, "b"].Returns(42);

		// Act
		IMultiParamIndexerGetterSetter svc = stub;
		var value = svc[3, "b"];

		// Assert
		Assert.Equal(42, value);
	}

	[Fact]
	public void Gap3_Standalone_MultiParamIndexer_GetCallback()
	{
		// Arrange
		var stub = new MultiParamIndexerGetterSetterKnockOff();
		stub.Indexer.Get(key => key.a + key.b.Length);

		// Act
		IMultiParamIndexerGetterSetter svc = stub;
		var value = svc[3, "hello"];

		// Assert
		Assert.Equal(8, value);
	}

	// =========================================================================
	// Gap #4: Init-only indexers
	// =========================================================================

	[Fact]
	public void Gap4_Inline_InitIndexer_GetWorks()
	{
		// Arrange - indexer with { get; init; }
		var stub = new IndexerGapTestClass.Stubs.IInitIndexer();
		stub.Indexer[3].Returns(42);

		// Act
		IInitIndexer svc = stub;
		var value = svc[3];

		// Assert
		Assert.Equal(42, value);
	}

	[Fact]
	public void Gap4_Inline_InitOnlyIndexer_Compiles()
	{
		// Arrange - indexer with { init; } only
		// This should at minimum compile and generate a stub
		var stub = new IndexerGapTestClass.Stubs.IInitOnlyIndexer();
		Assert.NotNull(stub);
	}

	[Fact]
	public void Gap4_Standalone_InitIndexer_GetWorks()
	{
		// Arrange
		var stub = new InitIndexerKnockOff();
		stub.Indexer[3].Returns(42);

		// Act
		IInitIndexer svc = stub;
		var value = svc[3];

		// Assert
		Assert.Equal(42, value);
	}

	// =========================================================================
	// Bug #35: Init-only indexers on classes (CS8853 fix)
	// =========================================================================

	[Fact]
	public void Bug35_InlineClass_AbstractInitIndexer_GetWorks()
	{
		// Arrange - abstract class with { get; init; } indexer
		var stub = new IndexerGapTestClass.Stubs.AbstractInitIndexer();
		stub.Indexer[3].Returns(42);

		// Act
		var value = stub.Object[3];

		// Assert
		Assert.Equal(42, value);
	}

	[Fact]
	public void Bug35_InlineClass_VirtualInitIndexer_GetWorks()
	{
		// Arrange - virtual class with { get; init; } indexer
		var stub = new IndexerGapTestClass.Stubs.VirtualInitIndexer();
		stub.Indexer[3].Returns(42);

		// Act
		var value = stub.Object[3];

		// Assert
		Assert.Equal(42, value);
	}

	[Fact]
	public void Bug35_StandaloneClass_AbstractInitIndexer_GetWorks()
	{
		// Arrange - standalone class stub for abstract class with { get; init; }
		var stub = new AbstractInitIndexerStandaloneStub();
		stub.Indexer[3].Returns(42);

		// Act
		var value = stub.Object[3];

		// Assert
		Assert.Equal(42, value);
	}

	[Fact]
	public void Bug35_StandaloneClass_VirtualInitIndexer_GetWorks()
	{
		// Arrange - standalone class stub for virtual class with { get; init; }
		var stub = new VirtualInitIndexerStandaloneStub();
		stub.Indexer[3].Returns(42);

		// Act
		var value = stub.Object[3];

		// Assert
		Assert.Equal(42, value);
	}

	// =========================================================================
	// Gap #5: Argument-specific indexer configuration (design gap, not bug)
	// =========================================================================
	// KnockOff now supports per-key configuration directly:
	//   stub.Indexer[3, "b"].Returns(42)  // matches only key (3, "b")

	[Fact]
	public void Gap5_PerKeyConfiguration()
	{
		// Arrange - KnockOff per-key configuration
		var stub = new IndexerGapTestClass.Stubs.IMultiParamIndexerGetter();

		stub.Indexer[3, "b"].Returns(42);
		stub.Indexer[5, "x"].Returns(99);

		// Act
		IMultiParamIndexerGetter svc = stub;

		// Assert
		Assert.Equal(42, svc[3, "b"]);
		Assert.Equal(99, svc[5, "x"]);
		Assert.Equal(0, svc[1, "other"]); // unconfigured returns default
	}

	// =========================================================================
	// Gap #17: Multi-parameter indexers with params arrays
	// =========================================================================

	[Fact]
	public void Gap17_Inline_ParamsIndexer_PerKey_KnownLimitation()
	{
		// Known limitation: params array indexers use (int, string[]) as the per-key
		// dictionary key type. Arrays use reference equality, so per-key configuration
		// will NOT match a different array instance with the same contents.
		// Use Get() callbacks instead of per-key Returns for params indexers.

		var stub = new IndexerGapTestClass.Stubs.IParamsIndexer();
		stub.Indexer[1, new[] { "b" }].Returns(42);

		IParamsIndexer svc = stub;
		// svc[1, "b"] creates a NEW string[] instance that won't match the per-key builder
		var value = svc[1, "b"];

		// Returns default (0) because array reference equality fails
		Assert.Equal(0, value);
	}

	[Fact]
	public void Gap17_Inline_ParamsIndexer_GetCallback_Simple()
	{
		// Use Get callback for params indexers instead of per-key
		var stub = new IndexerGapTestClass.Stubs.IParamsIndexer();

		stub.Indexer.Get(key => key.a * 100);

		IParamsIndexer svc = stub;
		var value = svc[1, "b"];

		Assert.Equal(100, value);
	}

	[Fact]
	public void Gap17_Inline_ParamsIndexer_GetCallback_MultiValue()
	{
		// Arrange
		var stub = new IndexerGapTestClass.Stubs.IParamsIndexer();
		stub.Indexer.Get(key => key.a + key.b.Length);

		// Act
		IParamsIndexer svc = stub;
		var value = svc[1, "x", "y"];

		// Assert - 1 + 2 = 3
		Assert.Equal(3, value);
	}

	[Fact]
	public void Gap17_Standalone_ParamsIndexer_PerKey_KnownLimitation()
	{
		// Known limitation: same as inline -- params array reference equality
		var stub = new ParamsIndexerKnockOff();
		stub.Indexer[1, new[] { "b" }].Returns(42);

		IParamsIndexer svc = stub;
		var value = svc[1, "b"];

		// Returns default (0) because array reference equality fails
		Assert.Equal(0, value);
	}

	[Fact]
	public void Gap17_Inline_ParamsIndexer_NoParamsProvided()
	{
		// Arrange - calling with no params array values
		var stub = new IndexerGapTestClass.Stubs.IParamsIndexer();
		stub.Indexer.Get(key => key.a * 10);

		// Act
		IParamsIndexer svc = stub;
		var value = svc[5]; // No params args

		// Assert
		Assert.Equal(50, value);
	}

	// =========================================================================
	// N-parameter indexers (3+ params) - verifies generic logic
	// =========================================================================

	[Fact]
	public void ThreeParam_Inline_GetCallback()
	{
		var stub = new IndexerGapTestClass.Stubs.IThreeParamIndexer();
		stub.Indexer.Get(key => key.x + key.y + key.z);

		IThreeParamIndexer svc = stub;
		var value = svc[10, 20, 30];

		Assert.Equal(60.0, value);
	}

	[Fact]
	public void ThreeParam_Inline_SetCallback()
	{
		var stub = new IndexerGapTestClass.Stubs.IThreeParamIndexer();

		(int x, int y, int z, double val)? captured = null;
		stub.Indexer.Set((key, value) =>
		{
			captured = (key.x, key.y, key.z, value);
		});

		IThreeParamIndexer svc = stub;
		svc[1, 2, 3] = 99.5;

		Assert.NotNull(captured);
		Assert.Equal(1, captured.Value.x);
		Assert.Equal(2, captured.Value.y);
		Assert.Equal(3, captured.Value.z);
		Assert.Equal(99.5, captured.Value.val);
	}

	[Fact]
	public void ThreeParam_Inline_PerKey()
	{
		var stub = new IndexerGapTestClass.Stubs.IThreeParamIndexer();
		stub.Indexer[1, 2, 3].Returns(42.0);

		IThreeParamIndexer svc = stub;
		Assert.Equal(42.0, svc[1, 2, 3]);
		stub.Indexer.VerifyGet(Called.Once);
		Assert.Equal((1, 2, 3), stub.Indexer.LastGetKey);
	}

	[Fact]
	public void ThreeParam_Standalone_GetCallback()
	{
		var stub = new ThreeParamIndexerKnockOff();
		stub.Indexer.Get(key => key.x * key.y * key.z);

		IThreeParamIndexer svc = stub;
		Assert.Equal(60.0, svc[3, 4, 5]);
	}

	[Fact]
	public void FourParam_Inline_GetCallback()
	{
		var stub = new IndexerGapTestClass.Stubs.IFourParamIndexer();
		stub.Indexer.Get(key => $"{key.schema}.{key.table}.{key.column}[{key.index}]");

		IFourParamIndexer svc = stub;
		var value = svc["dbo", "Users", "Name", 0];

		Assert.Equal("dbo.Users.Name[0]", value);
	}

	[Fact]
	public void FourParam_Inline_PerKey()
	{
		var stub = new IndexerGapTestClass.Stubs.IFourParamIndexer();
		stub.Indexer["dbo", "Users", "Id", 0].Returns("int");

		IFourParamIndexer svc = stub;
		Assert.Equal("int", svc["dbo", "Users", "Id", 0]);
	}

	[Fact]
	public void FourParam_Standalone_GetCallback()
	{
		var stub = new FourParamIndexerKnockOff();
		stub.Indexer.Get(key => $"{key.schema}/{key.table}");

		IFourParamIndexer svc = stub;
		Assert.Equal("public/Orders", svc["public", "Orders", "Total", 5]);
	}
}
