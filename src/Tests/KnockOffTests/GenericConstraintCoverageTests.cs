namespace KnockOff.Tests;

/// <summary>
/// Coverage tests for common C# generic constraint patterns.
/// If the generated code compiles, the constraint handling works.
/// </summary>
public class GenericConstraintCoverageTests
{
	#region struct alone (single type param)

	[Fact]
	public void StructConstraint_NullableReturn_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.StructReturn.Of<int>().Call((v) => v * 2);

		var result = service.StructReturn(21);

		Assert.Equal(42, result);
	}

	[Fact]
	public void StructConstraint_NullableReturn_CanReturnNull()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.StructReturn.Of<int>().Call((_) => null);

		var result = service.StructReturn(5);

		Assert.Null(result);
	}

	#endregion

	#region notnull constraint

	[Fact]
	public void NotNullConstraint_WithReferenceType_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.NotNullReturn.Of<string>().Call((v) => v + "!");

		var result = service.NotNullReturn("hello");

		Assert.Equal("hello!", result);
	}

	[Fact]
	public void NotNullConstraint_WithValueType_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.NotNullReturn.Of<int>().Call((v) => v + 1);

		var result = service.NotNullReturn(41);

		Assert.Equal(42, result);
	}

	#endregion

	#region class + new() combined

	[Fact]
	public void ClassNewConstraint_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.ClassNewReturn.Of<List<int>>().Call(() => new List<int> { 1, 2, 3 });

		var result = service.ClassNewReturn<List<int>>();

		Assert.Equal(3, result.Count);
	}

	#endregion

	#region Multiple interface constraints

	[Fact]
	public void MultipleInterfaceConstraints_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.MultiInterfaceReturn.Of<string>().Call((_) => "result");

		var result = service.MultiInterfaceReturn("input");

		Assert.Equal("result", result);
	}

	#endregion

	#region Self-referential constraint

	[Fact]
	public void SelfReferentialConstraint_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.SelfReferentialReturn.Of<int>().Call((v) => v);

		var result = service.SelfReferentialReturn(42);

		Assert.Equal(42, result);
	}

	#endregion

	#region notnull + interface

	[Fact]
	public void NotNullInterfaceConstraint_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		knockOff.NotNullInterfaceReturn.Of<MemoryStream>().Call((_) => stream);

		var result = service.NotNullInterfaceReturn(new MemoryStream());

		Assert.Same(stream, result);
	}

	#endregion

	#region Type param constrained by another type param

	[Fact]
	public void TypeParamConstrainedByAnother_Works()
	{
		var knockOff = new GenericConstraintCoverageKnockOff();
		IGenericConstraintCoverage service = knockOff;

		knockOff.ConvertWithConstraint.Of<object, string>().Call((input) => input.ToString()!);

		var result = service.ConvertWithConstraint<object, string>(42);

		Assert.Equal("42", result);
	}

	#endregion

	#region Inline pattern coverage

	[Fact]
	public void Inline_StructConstraint_Works()
	{
		var stub = new GenericConstraintInlineTests.Stubs.IGenericConstraintCoverage();
		IGenericConstraintCoverage service = stub;

		stub.StructReturn.Of<int>().Call((v) => v * 3);

		var result = service.StructReturn(10);

		Assert.Equal(30, result);
	}

	[Fact]
	public void Inline_NotNullConstraint_Works()
	{
		var stub = new GenericConstraintInlineTests.Stubs.IGenericConstraintCoverage();
		IGenericConstraintCoverage service = stub;

		stub.NotNullReturn.Of<string>().Call((v) => v.ToUpper());

		var result = service.NotNullReturn("test");

		Assert.Equal("TEST", result);
	}

	[Fact]
	public void Inline_ClassNewConstraint_Works()
	{
		var stub = new GenericConstraintInlineTests.Stubs.IGenericConstraintCoverage();
		IGenericConstraintCoverage service = stub;

		stub.ClassNewReturn.Of<List<string>>().Call(() => new List<string> { "a" });

		var result = service.ClassNewReturn<List<string>>();

		Assert.Single(result);
	}

	[Fact]
	public void Inline_TypeParamConstrainedByAnother_Works()
	{
		var stub = new GenericConstraintInlineTests.Stubs.IGenericConstraintCoverage();
		IGenericConstraintCoverage service = stub;

		stub.ConvertWithConstraint.Of<object, string>().Call((input) => $"converted:{input}");

		var result = service.ConvertWithConstraint<object, string>("hello");

		Assert.Equal("converted:hello", result);
	}

	#endregion
}

#region Test Types

/// <summary>
/// Interface covering common generic constraint patterns.
/// If KnockOff generates a compilable stub, the constraints are handled correctly.
/// </summary>
public interface IGenericConstraintCoverage
{
	/// <summary>struct alone with T? return (Nullable&lt;T&gt;)</summary>
	T? StructReturn<T>(T value) where T : struct;

	/// <summary>notnull constraint with T? return</summary>
	T? NotNullReturn<T>(T value) where T : notnull;

	/// <summary>class + new() combined</summary>
	T ClassNewReturn<T>() where T : class, new();

	/// <summary>Multiple interface constraints (class + two interfaces)</summary>
	T? MultiInterfaceReturn<T>(T value) where T : class, IComparable, IConvertible;

	/// <summary>Self-referential constraint</summary>
	T SelfReferentialReturn<T>(T value) where T : IComparable<T>;

	/// <summary>notnull + interface</summary>
	T? NotNullInterfaceReturn<T>(T value) where T : notnull, IDisposable;

	/// <summary>Type param constrained by another type param</summary>
	TResult ConvertWithConstraint<TBase, TResult>(TBase input) where TResult : TBase;
}

/// <summary>Pattern 1: Standalone stub for generic constraint coverage.</summary>
[KnockOff]
public partial class GenericConstraintCoverageKnockOff : IGenericConstraintCoverage
{
}

/// <summary>Pattern 5: Inline stub for generic constraint coverage.</summary>
[KnockOff<IGenericConstraintCoverage>]
public partial class GenericConstraintInlineTests
{
}

#endregion
