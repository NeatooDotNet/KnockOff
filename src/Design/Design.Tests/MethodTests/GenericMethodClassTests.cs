// -----------------------------------------------------------------------------
// Design.Tests - Generic Method Tests for Class Stubs
// -----------------------------------------------------------------------------
// Tests generic method support (Of<T>() pattern) across all four class stub patterns:
// - Pattern 6: Inline Class ([KnockOff<GenericMethodBase>])
// - Pattern 3: Standalone Class ([KnockOffBase<GenericMethodBase>])
// - Pattern 4: Generic Standalone Class ([KnockOffBase(typeof(GenericMethodRepositoryBase<>))])
// - Pattern 9: Open Generic Class ([KnockOff(typeof(GenericMethodRepositoryBase<>))])
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

#region Pattern 6: Inline Class

/// <summary>
/// Tests generic methods on inline class stubs ([KnockOff&lt;GenericMethodBase&gt;]).
/// </summary>
public class InlineClassGenericMethodTests
{
	[Fact]
	public void Return_ConfiguresGenericMethod()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();
		stub.Convert.Of<int>().Return((value) => 42);

		var result = stub.Object.Convert<int>("anything");

		Assert.Equal(42, result);
	}

	[Fact]
	public void Call_ConfiguresVoidGenericMethod()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();
		var called = false;
		stub.Register.Of<string>().Call(() => called = true);

		stub.Object.Register<string>();

		Assert.True(called);
	}

	[Fact]
	public void Verify_TracksGenericMethodCalls()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();

		stub.Object.Convert<int>("a");
		stub.Object.Convert<string>("b");

		stub.Convert.Of<int>().Verify(Called.Once);
		stub.Convert.Of<string>().Verify(Called.Once);
		stub.Convert.Verify(Called.Exactly(2));
	}

	[Fact]
	public void MultipleTypeParams_WithConstraints()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();
		stub.Transform.Of<string, List<int>>().Return((input) => new List<int> { input.Length });

		var result = stub.Object.Transform<string, List<int>>("hello");

		Assert.Single(result);
		Assert.Equal(5, result[0]);
	}

	[Fact]
	public void MixedOverloads_SeparateInterceptors()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();

		// Non-generic Process(string) uses stub.Process
		var nonGenericCalled = false;
		stub.Process.Call((label) => nonGenericCalled = true);

		// Generic Process<T>(T, string) uses stub.ProcessGeneric.Of<T>()
		string? capturedLabel = null;
		stub.ProcessGeneric.Of<int>().Call((item, label) => capturedLabel = label);

		stub.Object.Process("non-generic");
		stub.Object.Process(42, "generic");

		Assert.True(nonGenericCalled);
		Assert.Equal("generic", capturedLabel);
	}

	[Fact]
	public void VirtualMethod_FallsBackToBase_WhenUnconfigured()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();

		// Convert<T> has base implementation returning default!
		var result = stub.Object.Convert<int>("anything");

		// Should call base implementation which returns default!
		Assert.Equal(0, result);
	}

	[Fact]
	public void AbstractMethod_ReturnsDefault_WhenUnconfigured()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();

		// Register<T> is abstract void - should not throw
		stub.Object.Register<string>();

		// Verify it was tracked
		stub.Register.Of<string>().Verify(Called.Once);
	}

	[Fact]
	public void NonGenericMethod_StillWorks()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();
		stub.GetName.Return("test-name");

		var result = stub.Object.GetName();

		Assert.Equal("test-name", result);
	}

	[Fact]
	public void Reset_ClearsAllTypedHandlers()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();

		stub.Object.Convert<int>("a");
		stub.Object.Convert<string>("b");

		stub.Convert.Reset();

		stub.Convert.Verify(Called.Never);
		Assert.Empty(stub.Convert.CalledTypeArguments);
	}

	[Fact]
	public void CalledTypeArguments_TracksTypes()
	{
		var stub = new GenericMethodInlineClassTest.Stubs.GenericMethodBase();

		stub.Object.Convert<int>("a");
		stub.Object.Convert<string>("b");

		var calledTypes = stub.Convert.CalledTypeArguments;
		Assert.Equal(2, calledTypes.Count);
		Assert.Contains(typeof(int), calledTypes);
		Assert.Contains(typeof(string), calledTypes);
	}
}

#endregion

#region Pattern 3: Standalone Class

/// <summary>
/// Tests generic methods on standalone class stubs ([KnockOffBase&lt;GenericMethodBase&gt;]).
/// </summary>
public class StandaloneClassGenericMethodTests
{
	[Fact]
	public void Return_ConfiguresGenericMethod()
	{
		var stub = new GenericMethodStandaloneStub();
		stub.Convert.Of<int>().Return((value) => 42);

		var result = stub.Object.Convert<int>("anything");

		Assert.Equal(42, result);
	}

	[Fact]
	public void Call_ConfiguresVoidGenericMethod()
	{
		var stub = new GenericMethodStandaloneStub();
		var called = false;
		stub.Register.Of<string>().Call(() => called = true);

		stub.Object.Register<string>();

		Assert.True(called);
	}

	[Fact]
	public void Verify_TracksGenericMethodCalls()
	{
		var stub = new GenericMethodStandaloneStub();

		stub.Object.Convert<int>("a");
		stub.Object.Convert<string>("b");

		stub.Convert.Of<int>().Verify(Called.Once);
		stub.Convert.Of<string>().Verify(Called.Once);
		stub.Convert.Verify(Called.Exactly(2));
	}

	[Fact]
	public void MultipleTypeParams_WithConstraints()
	{
		var stub = new GenericMethodStandaloneStub();
		stub.Transform.Of<string, List<int>>().Return((input) => new List<int> { input.Length });

		var result = stub.Object.Transform<string, List<int>>("hello");

		Assert.Single(result);
		Assert.Equal(5, result[0]);
	}

	[Fact]
	public void MixedOverloads_SeparateInterceptors()
	{
		var stub = new GenericMethodStandaloneStub();

		// Non-generic Process(string) uses stub.Process
		var nonGenericCalled = false;
		stub.Process.Call((label) => nonGenericCalled = true);

		// Generic Process<T>(T, string) uses stub.ProcessGeneric.Of<T>()
		string? capturedLabel = null;
		stub.ProcessGeneric.Of<int>().Call((item, label) => capturedLabel = label);

		stub.Object.Process("non-generic");
		stub.Object.Process(42, "generic");

		Assert.True(nonGenericCalled);
		Assert.Equal("generic", capturedLabel);
	}

	[Fact]
	public void VirtualMethod_FallsBackToBase_WhenUnconfigured()
	{
		var stub = new GenericMethodStandaloneStub();

		var result = stub.Object.Convert<int>("anything");

		Assert.Equal(0, result);
	}

	[Fact]
	public void AbstractMethod_ReturnsDefault_WhenUnconfigured()
	{
		var stub = new GenericMethodStandaloneStub();

		stub.Object.Register<string>();

		stub.Register.Of<string>().Verify(Called.Once);
	}

	[Fact]
	public void NonGenericMethod_StillWorks()
	{
		var stub = new GenericMethodStandaloneStub();
		stub.GetName.Return("test-name");

		var result = stub.Object.GetName();

		Assert.Equal("test-name", result);
	}

	[Fact]
	public void Reset_ClearsAllTypedHandlers()
	{
		var stub = new GenericMethodStandaloneStub();

		stub.Object.Convert<int>("a");
		stub.Object.Convert<string>("b");

		stub.Convert.Reset();

		stub.Convert.Verify(Called.Never);
		Assert.Empty(stub.Convert.CalledTypeArguments);
	}
}

#endregion

#region Pattern 4: Generic Standalone Class

/// <summary>
/// Tests generic methods on generic standalone class stubs
/// ([KnockOffBase(typeof(GenericMethodRepositoryBase&lt;&gt;))]).
/// Exercises method-level type params alongside class-level type params.
/// </summary>
public class GenericStandaloneClassGenericMethodTests
{
	[Fact]
	public void Return_ConfiguresGenericMethod()
	{
		var stub = new GenericMethodRepositoryStub<string>();
		stub.ConvertEntity.Of<int>().Return((entity) => entity.Length);

		var result = stub.Object.ConvertEntity<int>("hello");

		Assert.Equal(5, result);
	}

	[Fact]
	public void Call_ConfiguresVoidGenericMethod()
	{
		var stub = new GenericMethodRepositoryStub<string>();
		string? capturedSource = null;
		stub.MapTo.Of<int>().Call((source) => capturedSource = source);

		stub.Object.MapTo<int>("test-entity");

		Assert.Equal("test-entity", capturedSource);
	}

	[Fact]
	public void Verify_TracksGenericMethodCalls()
	{
		var stub = new GenericMethodRepositoryStub<string>();

		stub.Object.ConvertEntity<int>("a");
		stub.Object.ConvertEntity<double>("b");

		stub.ConvertEntity.Of<int>().Verify(Called.Once);
		stub.ConvertEntity.Of<double>().Verify(Called.Once);
		stub.ConvertEntity.Verify(Called.Exactly(2));
	}

	[Fact]
	public void NonGenericMethod_StillWorks()
	{
		var stub = new GenericMethodRepositoryStub<string>();
		stub.GetEntityName.Return("StringEntity");

		var result = stub.Object.GetEntityName();

		Assert.Equal("StringEntity", result);
	}

	[Fact]
	public void ClassLevelTypeParam_MethodStillWorks()
	{
		// GetById uses TEntity (class-level) not a method-level type param
		var stub = new GenericMethodRepositoryStub<string>();
		stub.GetById.Return((id) => $"entity-{id}");

		var result = stub.Object.GetById(42);

		Assert.Equal("entity-42", result);
	}

	[Fact]
	public void AbstractVoidGenericMethod_TracksWithoutThrowing()
	{
		var stub = new GenericMethodRepositoryStub<string>();

		// MapTo<TTarget> is abstract void
		stub.Object.MapTo<int>("source");

		stub.MapTo.Of<int>().Verify(Called.Once);
	}
}

#endregion

#region Pattern 9: Open Generic Class

/// <summary>
/// Tests generic methods on open generic class stubs
/// ([KnockOff(typeof(GenericMethodRepositoryBase&lt;&gt;))]).
/// </summary>
public class OpenGenericClassGenericMethodTests
{
	[Fact]
	public void Return_ConfiguresGenericMethod()
	{
		var stub = new GenericMethodOpenGenericClassTest.Stubs.GenericMethodRepositoryBase<string>();
		stub.ConvertEntity.Of<int>().Return((entity) => entity.Length);

		var result = stub.Object.ConvertEntity<int>("hello");

		Assert.Equal(5, result);
	}

	[Fact]
	public void Call_ConfiguresVoidGenericMethod()
	{
		var stub = new GenericMethodOpenGenericClassTest.Stubs.GenericMethodRepositoryBase<string>();
		string? capturedSource = null;
		stub.MapTo.Of<int>().Call((source) => capturedSource = source);

		stub.Object.MapTo<int>("test-entity");

		Assert.Equal("test-entity", capturedSource);
	}

	[Fact]
	public void Verify_TracksGenericMethodCalls()
	{
		var stub = new GenericMethodOpenGenericClassTest.Stubs.GenericMethodRepositoryBase<string>();

		stub.Object.ConvertEntity<int>("a");
		stub.Object.ConvertEntity<double>("b");

		stub.ConvertEntity.Of<int>().Verify(Called.Once);
		stub.ConvertEntity.Of<double>().Verify(Called.Once);
		stub.ConvertEntity.Verify(Called.Exactly(2));
	}

	[Fact]
	public void NonGenericMethod_StillWorks()
	{
		var stub = new GenericMethodOpenGenericClassTest.Stubs.GenericMethodRepositoryBase<string>();
		stub.GetEntityName.Return("StringEntity");

		var result = stub.Object.GetEntityName();

		Assert.Equal("StringEntity", result);
	}

	[Fact]
	public void ClassLevelTypeParam_MethodStillWorks()
	{
		var stub = new GenericMethodOpenGenericClassTest.Stubs.GenericMethodRepositoryBase<string>();
		stub.GetById.Return((id) => $"entity-{id}");

		var result = stub.Object.GetById(42);

		Assert.Equal("entity-42", result);
	}

	[Fact]
	public void AbstractVoidGenericMethod_TracksWithoutThrowing()
	{
		var stub = new GenericMethodOpenGenericClassTest.Stubs.GenericMethodRepositoryBase<string>();

		stub.Object.MapTo<int>("source");

		stub.MapTo.Of<int>().Verify(Called.Once);
	}
}

#endregion
