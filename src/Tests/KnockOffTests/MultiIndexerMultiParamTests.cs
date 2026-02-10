namespace KnockOff.Tests;

/// <summary>
/// Tests for multi-indexer interfaces where at least one indexer has multiple parameters.
/// Bug: The generated stub body calls InvokeGet_inta_stringb (using parameter names)
/// but the interceptor method is named InvokeGet_Int32_String (using type names).
/// This mismatch causes a compilation error in the generated code.
/// </summary>
public class MultiIndexerMultiParamTests
{
	// =========================================================================
	// Inline Interface Pattern (Pattern 5)
	// =========================================================================

	[Fact]
	public void Inline_MultiIndexerWithMultiParam_GetterWorks()
	{
		var stub = new InlineMultiIndexerMultiParamTestClass.Stubs.IMultiIndexerWithMultiParamKeys();
		stub.Indexer["hello"].Returns("world");
		stub.Indexer[1, "a"].Returns(42);

		IMultiIndexerWithMultiParamKeys svc = stub;
		Assert.Equal("world", svc["hello"]);
		Assert.Equal(42, svc[1, "a"]);
	}

	[Fact]
	public void Inline_MultiIndexerWithMultiParam_SetterWorks()
	{
		var stub = new InlineMultiIndexerMultiParamTestClass.Stubs.IMultiIndexerWithMultiParamKeys();

		IMultiIndexerWithMultiParamKeys svc = stub;
		svc["key"] = "val";
		svc[1, "a"] = 99;

		stub.Indexer.VerifySet(Called.Exactly(2));
	}

	[Fact]
	public void Inline_MultiIndexerWithMultiParam_PerKeyCallbackWorks()
	{
		var stub = new InlineMultiIndexerMultiParamTestClass.Stubs.IMultiIndexerWithMultiParamKeys();
		stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

		IMultiIndexerWithMultiParamKeys svc = stub;
		Assert.Equal(4, svc[1, "abc"]);
	}

	[Fact]
	public void Inline_MultiIndexerWithMultiParam_TrackingWorks()
	{
		var stub = new InlineMultiIndexerMultiParamTestClass.Stubs.IMultiIndexerWithMultiParamKeys();

		IMultiIndexerWithMultiParamKeys svc = stub;
		_ = svc["test"];
		_ = svc[5, "x"];

		stub.Indexer.VerifyGet(Called.Exactly(2));
	}

	// =========================================================================
	// Standalone Pattern (Pattern 1)
	// =========================================================================

	[Fact]
	public void Standalone_MultiIndexerWithMultiParam_GetterWorks()
	{
		var stub = new StandaloneMultiIndexerMultiParamKnockOff();
		stub.Indexer["hello"].Returns("world");
		stub.Indexer[1, "a"].Returns(42);

		IMultiIndexerWithMultiParamKeys svc = stub;
		Assert.Equal("world", svc["hello"]);
		Assert.Equal(42, svc[1, "a"]);
	}

	[Fact]
	public void Standalone_MultiIndexerWithMultiParam_SetterWorks()
	{
		var stub = new StandaloneMultiIndexerMultiParamKnockOff();

		IMultiIndexerWithMultiParamKeys svc = stub;
		svc["key"] = "val";
		svc[1, "a"] = 99;

		stub.Indexer.VerifySet(Called.Exactly(2));
	}
}

#region Test Types

/// <summary>
/// Interface with multiple indexer overloads, one of which has multiple parameters.
/// This triggers the bug where the invoke suffix uses parameter names instead of type names.
/// </summary>
public interface IMultiIndexerWithMultiParamKeys
{
	string this[string key] { get; set; }
	int this[int a, string b] { get; set; }
}

[KnockOff<IMultiIndexerWithMultiParamKeys>]
public partial class InlineMultiIndexerMultiParamTestClass
{
}

[KnockOff]
public partial class StandaloneMultiIndexerMultiParamKnockOff : IMultiIndexerWithMultiParamKeys
{
	public string this[string key] { get => default!; set { } }
	public int this[int a, string b] { get => default; set { } }
}

#endregion
