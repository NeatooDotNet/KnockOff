namespace KnockOff.Tests;

/// <summary>
/// Tests for Bug 1: Method overloads with different return types.
///
/// When an interface has method overloads where one returns T and another returns Task{T},
/// KnockOff should generate separate interceptors for each because they have incompatible
/// return types.
///
/// Example: ISymptomsAreaFactory with Fetch(long) -> Task{T} and Fetch(entity) -> T
///
/// These tests verify the fix: Fetch1 (async) and Fetch2 (sync) should be separate interceptors
/// with correct return types.
/// </summary>
public class ReturnTypeMismatchBugTests
{
	[Fact]
	public void OverloadWithDifferentReturnTypes_SyncOverload_CanBeCalledAndTracked()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		var entity = new SampleEntity { Id = 1 };

		// Set up sync callback (Fetch2 is the sync overload)
		stub.Fetch2.OnCall = (ko, e) => new SampleArea { Id = e.Id };

		// Call the sync overload
		var result = factory.Fetch(entity);

		// Verify tracking on the correct interceptor
		Assert.True(stub.Fetch2.WasCalled);
		Assert.False(stub.Fetch1.WasCalled); // Async overload not called
		Assert.Equal(1, result.Id);
	}

	[Fact]
	public async Task OverloadWithDifferentReturnTypes_AsyncOverload_CanBeCalledAndTracked()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		// Set up async callback (Fetch1 is the async overload)
		stub.Fetch1.OnCall = (ko, id) => Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id });

		// Call the async overload
		var result = await factory.Fetch(42L);

		Assert.NotNull(result);
		Assert.Equal(42, result!.Id);
		Assert.Equal(1, stub.Fetch1.CallCount);
		Assert.False(stub.Fetch2.WasCalled); // Sync overload not called
	}

	[Fact]
	public void OverloadWithDifferentReturnTypes_BothOverloads_TrackSeparately()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		var entity = new SampleEntity { Id = 99 };

		// Set up callbacks for both
		stub.Fetch1.OnCall = (ko, id) => Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id });
		stub.Fetch2.OnCall = (ko, e) => new SampleArea { Id = e.Id };

		// Call both overloads
		_ = factory.Fetch(1L);     // Async overload (Fetch1)
		_ = factory.Fetch(entity); // Sync overload (Fetch2)

		// Each overload has separate tracking
		Assert.Equal(1, stub.Fetch1.CallCount);
		Assert.Equal(1, stub.Fetch2.CallCount);
		Assert.Equal(1L, stub.Fetch1.LastCallArg);
		Assert.Same(entity, stub.Fetch2.LastCallArg);
	}
}

#region Bug 1 Test Types - Different Return Types

/// <summary>
/// Simulates the ISymptomsArea pattern from Neatoo factories.
/// </summary>
public interface ISampleArea
{
	int Id { get; }
}

public class SampleArea : ISampleArea
{
	public int Id { get; set; }
}

/// <summary>
/// Simulates the PnSymptomsArea EF entity.
/// </summary>
public class SampleEntity
{
	public int Id { get; set; }
}

/// <summary>
/// Interface with method overloads that have DIFFERENT return types.
/// This pattern is common in Neatoo factories:
/// - Async method takes an ID and returns Task{T}
/// - Sync method takes an EF entity and returns T directly
///
/// Bug: KnockOff generates ONE interceptor typed for Task{T}, causing
/// the sync method implementation to fail compilation.
/// </summary>
public interface IFactoryWithMixedReturnTypes
{
	/// <summary>
	/// Async fetch by ID - returns Task{ISampleArea?}
	/// </summary>
	Task<ISampleArea?> Fetch(long id);

	/// <summary>
	/// Sync fetch from entity - returns ISampleArea directly (NOT Task)
	/// </summary>
	ISampleArea Fetch(SampleEntity entity);
}

/// <summary>
/// Stub for IFactoryWithMixedReturnTypes.
///
/// Expected: Each Fetch overload gets its own interceptor (Fetch1, Fetch2 or similar).
/// Bug: Both overloads share one interceptor with Task{T} return type, causing CS0266.
/// </summary>
[KnockOff]
public partial class FactoryWithMixedReturnTypesKnockOff : IFactoryWithMixedReturnTypes
{
}

#endregion
