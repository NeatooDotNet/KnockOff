namespace KnockOff.Tests;

/// <summary>
/// Tests for inline stub versions of Bug 1 and Bug 2.
/// These verify that inline stubs ([KnockOff&lt;T&gt;]) handle:
/// - Method overloads with different return types (T vs Task{T})
/// - Generic interface inheritance type mismatches
/// </summary>
public class InlineStubBugTests
{
	#region Bug 1 - Different Return Types with Inline Stubs

	[Fact]
	public void InlineStub_DifferentReturnTypes_SyncOverload_Works()
	{
		var stub = new InlineMixedReturnTypesStub();
		IFactoryWithMixedReturnTypes factory = stub.IFactoryWithMixedReturnTypes;

		var entity = new SampleEntity { Id = 1 };

		// Set up sync callback
		stub.IFactoryWithMixedReturnTypes.Fetch2.OnCall = (ko, e) => new SampleArea { Id = e.Id };

		// Call the sync overload
		var result = factory.Fetch(entity);

		Assert.True(stub.IFactoryWithMixedReturnTypes.Fetch2.WasCalled);
		Assert.Equal(1, result.Id);
	}

	[Fact]
	public async Task InlineStub_DifferentReturnTypes_AsyncOverload_Works()
	{
		var stub = new InlineMixedReturnTypesStub();
		IFactoryWithMixedReturnTypes factory = stub.IFactoryWithMixedReturnTypes;

		// Set up async callback
		stub.IFactoryWithMixedReturnTypes.Fetch1.OnCall = (ko, id) =>
			Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id });

		// Call the async overload
		var result = await factory.Fetch(42L);

		Assert.NotNull(result);
		Assert.Equal(42, result!.Id);
		Assert.True(stub.IFactoryWithMixedReturnTypes.Fetch1.WasCalled);
	}

	#endregion

	#region Bug 2 - Generic Inheritance with Inline Stubs

	[Fact]
	public void InlineStub_GenericInheritance_TypedMethod_Works()
	{
		var stub = new InlineGenericInheritanceStub();
		ISampleRule<ISampleTarget> rule = stub.ISampleValidationRule;

		var target = new SampleTarget { Value = "test" };
		var expectedResult = new SampleResult { Success = true };

		// Set up callback for typed version
		stub.ISampleValidationRule.Execute1.OnCall = (ko, t, ct) =>
			Task.FromResult<ISampleResult>(expectedResult);

		// Call via typed interface
		var result = rule.Execute(target, CancellationToken.None);

		Assert.True(stub.ISampleValidationRule.Execute1.WasCalled);
	}

	[Fact]
	public void InlineStub_GenericInheritance_BaseMethod_Works()
	{
		var stub = new InlineGenericInheritanceStub();
		ISampleRule rule = stub.ISampleValidationRule; // Cast to base interface

		var target = new SampleTarget { Value = "base-call" };
		var expectedResult = new SampleResult { Success = true };

		// Set up callback for base version
		stub.ISampleValidationRule.Execute2.OnCall = (ko, t, ct) =>
			Task.FromResult<ISampleResult>(expectedResult);

		// Call via base interface
		var result = rule.Execute(target, CancellationToken.None);

		Assert.NotNull(result);
		Assert.True(stub.ISampleValidationRule.Execute2.WasCalled);
	}

	#endregion
}

#region Inline Stub Definitions

/// <summary>
/// Inline stub for testing Bug 1 (different return types).
/// </summary>
[KnockOff<IFactoryWithMixedReturnTypes>]
public partial class InlineMixedReturnTypesStub
{
}

/// <summary>
/// Inline stub for testing Bug 2 (generic inheritance).
/// </summary>
[KnockOff<ISampleValidationRule>]
public partial class InlineGenericInheritanceStub
{
}

#endregion
