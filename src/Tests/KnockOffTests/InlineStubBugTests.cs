namespace KnockOff.Tests;

/// <summary>
/// Tests for inline stub versions of Bug 1 and Bug 2.
/// These verify that inline stubs ([KnockOff&lt;T&gt;]) handle:
/// - Method overloads with different parameter AND return types
/// - Generic interface inheritance type mismatches
/// </summary>
public class InlineStubBugTests
{
	#region Bug 1 - Different Return Types with Inline Stubs

	[Fact]
	public void InlineStub_DifferentReturnTypes_SyncOverload_Works()
	{
		var stub = new InlineMixedReturnTypesStub.Stubs.IFactoryWithMixedReturnTypes();
		IFactoryWithMixedReturnTypes factory = stub;

		var entity = new SampleEntity { Id = 1 };

		// Set up sync callback - C# overload resolution picks the right OnCall based on lambda parameter type
		stub.Fetch.Return((SampleEntity e) => new SampleArea { Id = e.Id });

		// Call the sync overload
		var result = factory.Fetch(entity);

		stub.Fetch.Verify();
		Assert.Equal(1, result.Id);
	}

	[Fact]
	public async Task InlineStub_DifferentReturnTypes_AsyncOverload_Works()
	{
		var stub = new InlineMixedReturnTypesStub.Stubs.IFactoryWithMixedReturnTypes();
		IFactoryWithMixedReturnTypes factory = stub;

		// Set up async callback - C# overload resolution picks the right OnCall based on lambda parameter type
		stub.Fetch.Return((long id) =>
			Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id }));

		// Call the async overload
		var result = await factory.Fetch(42L);

		Assert.NotNull(result);
		Assert.Equal(42, result!.Id);
		stub.Fetch.Verify();
	}

	#endregion

	#region Bug 2 - Generic Inheritance with Inline Stubs

	[Fact]
	public void InlineStub_GenericInheritance_TypedMethod_Works()
	{
		var stub = new InlineGenericInheritanceStub.Stubs.ISampleValidationRule();
		ISampleRule<ISampleTarget> rule = stub;

		var target = new SampleTarget { Value = "test" };
		var expectedResult = new SampleResult { Success = true };

		// Set up callback for typed version - Execute takes ISampleTarget
		stub.Execute.Return((ISampleTarget t, CancellationToken? ct) =>
			Task.FromResult<ISampleResult>(expectedResult));

		// Call via typed interface
		var result = rule.Execute(target, CancellationToken.None);

		stub.Execute.Verify();
	}

	[Fact]
	public void InlineStub_GenericInheritance_BaseMethod_Works()
	{
		var stub = new InlineGenericInheritanceStub.Stubs.ISampleValidationRule();
		ISampleRule rule = stub; // Cast to base interface

		var target = new SampleTarget { Value = "base-call" };
		var expectedResult = new SampleResult { Success = true };

		// Set up callback for base version - Execute takes ISampleRuleTarget
		stub.Execute.Return((ISampleRuleTarget t, CancellationToken? ct) =>
			Task.FromResult<ISampleResult>(expectedResult));

		// Call via base interface
		var result = rule.Execute(target, CancellationToken.None);

		Assert.NotNull(result);
		stub.Execute.Verify();
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
