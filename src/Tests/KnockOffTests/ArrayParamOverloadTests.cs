namespace KnockOff.Tests;

/// <summary>
/// Tests for array parameter overloads.
/// Verifies that GetTypeSuffix correctly handles array brackets ([]) in type names,
/// producing valid generated identifiers (e.g., "StringArray" instead of "string[]").
/// </summary>
public class ArrayParamOverloadTests
{
	#region Standalone Pattern (Pattern 1) - OnCall

	[Fact]
	public void Standalone_ArrayParam_OnCall_ParameterlessOverload()
	{
		var knockOff = new ArrayParamOverloadKnockOff();
		IArrayParamOverloadService service = knockOff;

		knockOff.GetItems.Call(() => new List<string> { "all" });

		var result = service.GetItems();

		Assert.Single(result);
		Assert.Equal("all", result[0]);
	}

	[Fact]
	public void Standalone_ArrayParam_OnCall_SingleArrayParam()
	{
		var knockOff = new ArrayParamOverloadKnockOff();
		IArrayParamOverloadService service = knockOff;

		knockOff.GetItems.Call((string[] filters) => new List<string>(filters));

		var result = service.GetItems(new[] { "a", "b", "c" });

		Assert.Equal(3, result.Count);
		Assert.Equal("a", result[0]);
		Assert.Equal("c", result[2]);
	}

	[Fact]
	public void Standalone_ArrayParam_OnCall_ArrayAndIntParams()
	{
		var knockOff = new ArrayParamOverloadKnockOff();
		IArrayParamOverloadService service = knockOff;

		knockOff.GetItems.Call(((string[] filters, int maxCount) args) => new List<string>(args.filters.Take(args.maxCount)));

		var result = service.GetItems(new[] { "a", "b", "c" }, 2);

		Assert.Equal(2, result.Count);
		Assert.Equal("a", result[0]);
		Assert.Equal("b", result[1]);
	}

	#endregion

	#region Standalone Pattern (Pattern 1) - Verify

	[Fact]
	public void Standalone_ArrayParam_Verify_EachOverloadTrackedSeparately()
	{
		var knockOff = new ArrayParamOverloadKnockOff();
		IArrayParamOverloadService service = knockOff;

		var tracking1 = knockOff.GetItems.Call(() => new List<string>());
		var tracking2 = knockOff.GetItems.Call((string[] filters) => new List<string>(filters));
		var tracking3 = knockOff.GetItems.Call(((string[] filters, int maxCount) args) => new List<string>());

		service.GetItems();
		service.GetItems(new[] { "x" });
		service.GetItems(new[] { "y" }, 5);

		tracking1.Verify(Called.Once);
		tracking2.Verify(Called.Once);
		tracking3.Verify(Called.Once);
	}

	[Fact]
	public void Standalone_ArrayParam_Verify_InterceptorCountsAllOverloads()
	{
		var knockOff = new ArrayParamOverloadKnockOff();
		IArrayParamOverloadService service = knockOff;

		knockOff.GetItems.Call(() => new List<string>());
		knockOff.GetItems.Call((string[] filters) => new List<string>());
		knockOff.GetItems.Call(((string[] filters, int maxCount) args) => new List<string>());

		service.GetItems();
		service.GetItems(new[] { "a" });
		service.GetItems(new[] { "b" }, 1);

		knockOff.GetItems.Verify(Called.Exactly(3));
	}

	#endregion

	#region Inline Pattern (Pattern 5) - OnCall

	[Fact]
	public void Inline_ArrayParam_OnCall_ParameterlessOverload()
	{
		var stub = new ArrayParamOverloadInlineTests.Stubs.IArrayParamOverloadService();
		IArrayParamOverloadService service = stub;

		stub.GetItems.Call(() => new List<string> { "all" });

		var result = service.GetItems();

		Assert.Single(result);
		Assert.Equal("all", result[0]);
	}

	[Fact]
	public void Inline_ArrayParam_OnCall_SingleArrayParam()
	{
		var stub = new ArrayParamOverloadInlineTests.Stubs.IArrayParamOverloadService();
		IArrayParamOverloadService service = stub;

		stub.GetItems.Call((string[] filters) => new List<string>(filters));

		var result = service.GetItems(new[] { "x", "y" });

		Assert.Equal(2, result.Count);
		Assert.Equal("x", result[0]);
		Assert.Equal("y", result[1]);
	}

	[Fact]
	public void Inline_ArrayParam_OnCall_ArrayAndIntParams()
	{
		var stub = new ArrayParamOverloadInlineTests.Stubs.IArrayParamOverloadService();
		IArrayParamOverloadService service = stub;

		stub.GetItems.Call(((string[] filters, int maxCount) args) => new List<string>(args.filters.Take(args.maxCount)));

		var result = service.GetItems(new[] { "a", "b", "c", "d" }, 3);

		Assert.Equal(3, result.Count);
	}

	#endregion

	#region Inline Pattern (Pattern 5) - Verify

	[Fact]
	public void Inline_ArrayParam_Verify_EachOverloadTrackedSeparately()
	{
		var stub = new ArrayParamOverloadInlineTests.Stubs.IArrayParamOverloadService();
		IArrayParamOverloadService service = stub;

		var tracking1 = stub.GetItems.Call(() => new List<string>());
		var tracking2 = stub.GetItems.Call((string[] filters) => new List<string>());
		var tracking3 = stub.GetItems.Call(((string[] filters, int maxCount) args) => new List<string>());

		service.GetItems();
		service.GetItems();
		service.GetItems(new[] { "x" });
		service.GetItems(new[] { "y" }, 5);
		service.GetItems(new[] { "z" }, 10);

		tracking1.Verify(Called.Exactly(2));
		tracking2.Verify(Called.Once);
		tracking3.Verify(Called.Exactly(2));
	}

	[Fact]
	public void Inline_ArrayParam_Verify_InterceptorCountsAllOverloads()
	{
		var stub = new ArrayParamOverloadInlineTests.Stubs.IArrayParamOverloadService();
		IArrayParamOverloadService service = stub;

		stub.GetItems.Call(() => new List<string>());
		stub.GetItems.Call((string[] filters) => new List<string>());
		stub.GetItems.Call(((string[] filters, int maxCount) args) => new List<string>());

		service.GetItems();
		service.GetItems(new[] { "a" });
		service.GetItems(new[] { "b" }, 1);
		service.GetItems(new[] { "c" }, 2);

		stub.GetItems.Verify(Called.Exactly(4));
	}

	#endregion
}
