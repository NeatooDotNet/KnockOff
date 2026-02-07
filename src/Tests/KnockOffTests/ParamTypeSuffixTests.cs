namespace KnockOff.Tests;

/// <summary>
/// Tests for GetTypeSuffix bug fixes.
/// Verifies that parameter types which previously produced invalid C# identifiers
/// now generate valid, compilable interceptor code.
///
/// Bug categories tested:
/// - Nullable inside generics (Bug 1): List&lt;string?&gt; -> embedded ? stripped
/// - Tuples (Bug 2): (int, string) -> parentheses stripped
/// - Multidimensional arrays (Bug 3): int[,] -> Array2D suffix
/// - Array of nullable elements (Bug 4): string?[] -> StringArray
/// - nint/nuint (Bug 5): mapped to IntPtr/UIntPtr
/// </summary>
public class ParamTypeSuffixTests
{
	#region Standalone Pattern (Pattern 1) - OnCall

	[Fact]
	public void Standalone_NullableInsideGeneric_OnCall()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		List<string?>? captured = null;
		knockOff.Process.Execute((List<string?> data) => { captured = data; });

		var input = new List<string?> { "a", null, "b" };
		service.Process(input);

		Assert.Same(input, captured);
	}

	[Fact]
	public void Standalone_Tuple_OnCall()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		(int, string)? captured = null;
		knockOff.Process.Execute(((int, string) pair) => { captured = pair; });

		service.Process((42, "hello"));

		Assert.Equal((42, "hello"), captured);
	}

	[Fact]
	public void Standalone_MultidimensionalArray_OnCall()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		int[,]? captured = null;
		knockOff.Process.Execute((int[,] matrix) => { captured = matrix; });

		var input = new int[,] { { 1, 2 }, { 3, 4 } };
		service.Process(input);

		Assert.Same(input, captured);
	}

	[Fact]
	public void Standalone_NullableArray_OnCall()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		string?[]? captured = null;
		knockOff.Process.Execute((string?[] items) => { captured = items; });

		var input = new string?[] { "x", null, "y" };
		service.Process(input);

		Assert.Same(input, captured);
	}

	[Fact]
	public void Standalone_Nint_OnCall()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		nint? captured = null;
		knockOff.Process.Execute((nint value) => { captured = value; });

		service.Process((nint)123);

		Assert.Equal((nint)123, captured);
	}

	[Fact]
	public void Standalone_Nuint_OnCall()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		nuint? captured = null;
		knockOff.Process.Execute((nuint value) => { captured = value; });

		service.Process((nuint)456);

		Assert.Equal((nuint)456, captured);
	}

	#endregion

	#region Standalone Pattern (Pattern 1) - Verify

	[Fact]
	public void Standalone_AllOverloads_TrackedIndependently()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		var tracking1 = knockOff.Process.Execute((List<string?> data) => { });
		var tracking2 = knockOff.Process.Execute(((int, string) pair) => { });
		var tracking3 = knockOff.Process.Execute((int[,] matrix) => { });
		var tracking4 = knockOff.Process.Execute((string?[] items) => { });
		var tracking5 = knockOff.Process.Execute((nint value) => { });
		var tracking6 = knockOff.Process.Execute((nuint value) => { });

		service.Process(new List<string?> { "a" });
		service.Process((1, "b"));
		service.Process(new int[,] { { 1 } });
		service.Process(new string?[] { "c" });
		service.Process((nint)1);
		service.Process((nuint)2);

		tracking1.Verify(Times.Once);
		tracking2.Verify(Times.Once);
		tracking3.Verify(Times.Once);
		tracking4.Verify(Times.Once);
		tracking5.Verify(Times.Once);
		tracking6.Verify(Times.Once);
	}

	[Fact]
	public void Standalone_InterceptorCountsAllOverloads()
	{
		var knockOff = new ParamTypeSuffixKnockOff();
		IParamTypeSuffixService service = knockOff;

		knockOff.Process.Execute((List<string?> data) => { });
		knockOff.Process.Execute(((int, string) pair) => { });
		knockOff.Process.Execute((int[,] matrix) => { });
		knockOff.Process.Execute((string?[] items) => { });
		knockOff.Process.Execute((nint value) => { });
		knockOff.Process.Execute((nuint value) => { });

		service.Process(new List<string?>());
		service.Process((1, "x"));
		service.Process(new int[,] { { 1 } });
		service.Process(new string?[] { "y" });
		service.Process((nint)1);
		service.Process((nuint)2);

		knockOff.Process.Verify(Times.Exactly(6));
	}

	#endregion

	#region Inline Pattern (Pattern 5) - OnCall

	[Fact]
	public void Inline_NullableInsideGeneric_OnCall()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		List<string?>? captured = null;
		stub.Process.Execute((List<string?> data) => { captured = data; });

		var input = new List<string?> { "a", null, "b" };
		service.Process(input);

		Assert.Same(input, captured);
	}

	[Fact]
	public void Inline_Tuple_OnCall()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		(int, string)? captured = null;
		stub.Process.Execute(((int, string) pair) => { captured = pair; });

		service.Process((42, "hello"));

		Assert.Equal((42, "hello"), captured);
	}

	[Fact]
	public void Inline_MultidimensionalArray_OnCall()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		int[,]? captured = null;
		stub.Process.Execute((int[,] matrix) => { captured = matrix; });

		var input = new int[,] { { 1, 2 }, { 3, 4 } };
		service.Process(input);

		Assert.Same(input, captured);
	}

	[Fact]
	public void Inline_NullableArray_OnCall()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		string?[]? captured = null;
		stub.Process.Execute((string?[] items) => { captured = items; });

		var input = new string?[] { "x", null, "y" };
		service.Process(input);

		Assert.Same(input, captured);
	}

	[Fact]
	public void Inline_Nint_OnCall()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		nint? captured = null;
		stub.Process.Execute((nint value) => { captured = value; });

		service.Process((nint)123);

		Assert.Equal((nint)123, captured);
	}

	[Fact]
	public void Inline_Nuint_OnCall()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		nuint? captured = null;
		stub.Process.Execute((nuint value) => { captured = value; });

		service.Process((nuint)456);

		Assert.Equal((nuint)456, captured);
	}

	#endregion

	#region Inline Pattern (Pattern 5) - Verify

	[Fact]
	public void Inline_AllOverloads_TrackedIndependently()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		var tracking1 = stub.Process.Execute((List<string?> data) => { });
		var tracking2 = stub.Process.Execute(((int, string) pair) => { });
		var tracking3 = stub.Process.Execute((int[,] matrix) => { });
		var tracking4 = stub.Process.Execute((string?[] items) => { });
		var tracking5 = stub.Process.Execute((nint value) => { });
		var tracking6 = stub.Process.Execute((nuint value) => { });

		service.Process(new List<string?> { "a" });
		service.Process((1, "b"));
		service.Process(new int[,] { { 1 } });
		service.Process(new string?[] { "c" });
		service.Process((nint)1);
		service.Process((nuint)2);

		tracking1.Verify(Times.Once);
		tracking2.Verify(Times.Once);
		tracking3.Verify(Times.Once);
		tracking4.Verify(Times.Once);
		tracking5.Verify(Times.Once);
		tracking6.Verify(Times.Once);
	}

	[Fact]
	public void Inline_InterceptorCountsAllOverloads()
	{
		var stub = new ParamTypeSuffixInlineTests.Stubs.IParamTypeSuffixService();
		IParamTypeSuffixService service = stub;

		stub.Process.Execute((List<string?> data) => { });
		stub.Process.Execute(((int, string) pair) => { });
		stub.Process.Execute((int[,] matrix) => { });
		stub.Process.Execute((string?[] items) => { });
		stub.Process.Execute((nint value) => { });
		stub.Process.Execute((nuint value) => { });

		service.Process(new List<string?>());
		service.Process((1, "x"));
		service.Process(new int[,] { { 1 } });
		service.Process(new string?[] { "y" });
		service.Process((nint)1);
		service.Process((nuint)2);

		stub.Process.Verify(Times.Exactly(6));
	}

	#endregion
}
