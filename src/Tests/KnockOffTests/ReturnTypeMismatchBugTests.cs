namespace KnockOff.Tests;

/// <summary>
/// Tests for Bug 1: Method overloads with different return types.
///
/// When an interface has method overloads where one returns T and another returns Task{T},
/// KnockOff generates a single Fetch interceptor with multiple OnCall overloads.
/// The compiler resolves the correct overload based on the callback's return type.
///
/// Example: IFactoryWithMixedReturnTypes with Fetch(long) -> Task{T} and Fetch(entity) -> T
/// </summary>
public class ReturnTypeMismatchBugTests
{
	[Fact]
	public void OverloadWithDifferentReturnTypes_SyncOverload_CanBeCalledAndTracked()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		var entity = new SampleEntity { Id = 1 };

		// Set up sync callback - compiler resolves based on return type (ISampleArea vs Task<ISampleArea?>)
		var trackingSync = stub.Fetch.OnCall((ko, e) => new SampleArea { Id = e.Id });

		// Call the sync overload
		var result = factory.Fetch(entity);

		// Verify tracking
		Assert.True(trackingSync.WasCalled);
		Assert.Equal(1, result.Id);
	}

	[Fact]
	public async Task OverloadWithDifferentReturnTypes_AsyncOverload_CanBeCalledAndTracked()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		// Set up async callback
		var trackingAsync = stub.Fetch.OnCall((ko, id) => Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id }));

		// Call the async overload
		var result = await factory.Fetch(42L);

		Assert.NotNull(result);
		Assert.Equal(42, result!.Id);
		Assert.Equal(1, trackingAsync.CallCount);
	}

	[Fact]
	public void OverloadWithDifferentReturnTypes_BothOverloads_TrackSeparately()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		var entity = new SampleEntity { Id = 99 };

		// Set up callbacks for both - different return types resolve to different OnCall overloads
		var trackingAsync = stub.Fetch.OnCall((ko, id) => Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id }));
		var trackingSync = stub.Fetch.OnCall((ko, e) => new SampleArea { Id = e.Id });

		// Call both overloads
		_ = factory.Fetch(1L);     // Async overload
		_ = factory.Fetch(entity); // Sync overload

		// Each overload has separate tracking
		Assert.Equal(1, trackingAsync.CallCount);
		Assert.Equal(1, trackingSync.CallCount);
		Assert.Equal(1L, trackingAsync.LastArg);
		Assert.Same(entity, trackingSync.LastArg);
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
/// The generator creates a single Fetch interceptor with multiple OnCall
/// overloads that are resolved by the compiler based on return type.
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
/// The generator creates a single Fetch interceptor with multiple OnCall
/// overloads for each return type variant.
/// </summary>
[KnockOff]
public partial class FactoryWithMixedReturnTypesKnockOff : IFactoryWithMixedReturnTypes
{
}

#endregion
