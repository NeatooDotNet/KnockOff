namespace KnockOff.Tests;

/// <summary>
/// Tests for Bug 1: Method overloads with different parameter and return types.
///
/// When an interface has method overloads with different parameter types (long vs SampleEntity)
/// and different return types (Task{T} vs T), KnockOff generates a single Fetch interceptor
/// with multiple OnCall overloads. The compiler resolves the correct overload based on the
/// callback's parameter type (the lambda signature).
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

		// Set up sync callback - compiler resolves based on lambda parameter type (SampleEntity vs long)
		var trackingSync = stub.Fetch.Returns((e) => new SampleArea { Id = e.Id });

		// Call the sync overload
		var result = factory.Fetch(entity);

		// Verify tracking
		trackingSync.Verify();
		Assert.Equal(1, result.Id);
	}

	[Fact]
	public async Task OverloadWithDifferentReturnTypes_AsyncOverload_CanBeCalledAndTracked()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		// Set up async callback
		var trackingAsync = stub.Fetch.Returns((id) => Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id }));

		// Call the async overload
		var result = await factory.Fetch(42L);

		Assert.NotNull(result);
		Assert.Equal(42, result!.Id);
		trackingAsync.Verify(Times.Once);
	}

	[Fact]
	public void OverloadWithDifferentReturnTypes_BothOverloads_TrackSeparately()
	{
		var stub = new FactoryWithMixedReturnTypesKnockOff();
		IFactoryWithMixedReturnTypes factory = stub;

		var entity = new SampleEntity { Id = 99 };

		// Set up callbacks for both - different parameter types resolve to different OnCall overloads
		var trackingAsync = stub.Fetch.Returns((id) => Task.FromResult<ISampleArea?>(new SampleArea { Id = (int)id }));
		var trackingSync = stub.Fetch.Returns((e) => new SampleArea { Id = e.Id });

		// Call both overloads
		_ = factory.Fetch(1L);     // Async overload
		_ = factory.Fetch(entity); // Sync overload

		// Each overload has separate tracking
		trackingAsync.Verify(Times.Once);
		trackingSync.Verify(Times.Once);
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
/// Interface with method overloads that have different parameter AND return types.
/// This pattern is common in Neatoo factories:
/// - Async method takes an ID (long) and returns Task{T}
/// - Sync method takes an EF entity (SampleEntity) and returns T directly
///
/// The methods are valid C# overloads because they have different parameter types.
/// The generator creates a single Fetch interceptor with multiple OnCall overloads
/// that are resolved by the compiler based on the lambda's parameter type.
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
