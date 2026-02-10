namespace KnockOff.Tests;

/// <summary>
/// Tests to verify inline stubs support multiple indexer overloads.
/// Multi-indexers use C# indexer overloads on the interceptor and type-suffixed tracking properties.
/// </summary>
public class InlineMultiIndexerTests
{
	[Fact]
	public void InlineStub_MultiIndexer_HasIndexerInterceptor()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();

		// Verify IndexerInterceptor exists and supports both key types via overloaded indexer
		Assert.NotNull(stub.Indexer);
		Assert.NotNull(stub.Indexer["key"]); // string indexer returns PerKeyBuilder
		Assert.NotNull(stub.Indexer[0]); // int indexer returns PerKeyBuilder
	}

	[Fact]
	public void InlineStub_StringIndexer_PerKeyReturns()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer["key1"].Returns("value1");

		IMultiIndexerService svc = stub;
		var result = svc["key1"];

		Assert.Equal("value1", result);
		stub.Indexer.VerifyGet(Called.Once);
		Assert.Equal("key1", stub.Indexer.LastStringGetKey);
	}

	[Fact]
	public void InlineStub_IntIndexer_PerKeyReturns()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer[0].Returns(100);
		stub.Indexer[1].Returns(200);

		IMultiIndexerService svc = stub;
		Assert.Equal(100, svc[0]);
		Assert.Equal(200, svc[1]);

		stub.Indexer.VerifyGet(Called.Exactly(2));
		Assert.Equal(1, stub.Indexer.LastInt32GetKey);
	}

	[Fact]
	public void InlineStub_StringIndexer_GetCallback()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer.Get((string key) => $"Value for {key}");

		IMultiIndexerService svc = stub;
		Assert.Equal("Value for foo", svc["foo"]);
		Assert.Equal("Value for bar", svc["bar"]);

		stub.Indexer.VerifyGet(Called.Exactly(2));
	}

	[Fact]
	public void InlineStub_StringIndexer_SetTracking()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();

		IMultiIndexerService svc = stub;
		svc["key1"] = "value1";
		svc["key2"] = "value2";

		stub.Indexer.VerifySet(Called.Exactly(2));
		Assert.NotNull(stub.Indexer.LastStringSetEntry);
		Assert.Equal("key2", stub.Indexer.LastStringSetEntry.Value.Key);
		Assert.Equal("value2", stub.Indexer.LastStringSetEntry.Value.Value);
	}

	[Fact]
	public void InlineStub_StringIndexer_SetCallback()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		var callbackCalls = new List<(string key, string value)>();

		stub.Indexer.Set((string key, string value) =>
		{
			callbackCalls.Add((key, value));
		});

		IMultiIndexerService svc = stub;
		svc["a"] = "1";
		svc["b"] = "2";

		Assert.Equal(2, callbackCalls.Count);
		Assert.Contains(("a", "1"), callbackCalls);
		Assert.Contains(("b", "2"), callbackCalls);
	}

	[Fact]
	public void InlineStub_BothIndexers_IndependentTracking()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer["test"].Returns("string value");
		stub.Indexer[42].Returns(42);

		IMultiIndexerService svc = stub;

		// Access both
		Assert.Equal("string value", svc["test"]);
		Assert.Equal(42, svc[42]);

		// Verify total tracking
		stub.Indexer.VerifyGet(Called.Exactly(2));
		Assert.Equal("test", stub.Indexer.LastStringGetKey);
		Assert.Equal(42, stub.Indexer.LastInt32GetKey);
	}

	[Fact]
	public void InlineStub_IndexerInterceptor_Reset()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer["key"].Returns("value");
		stub.Indexer[0].Returns(100);

		IMultiIndexerService svc = stub;
		_ = svc["key"];
		_ = svc[0];

		stub.Indexer.VerifyGet(Called.Exactly(2));

		// Reset clears tracking
		stub.Indexer.Reset();

		stub.Indexer.VerifyGet(Called.Never);
	}
}

#region Test Types

public interface IMultiIndexerService
{
	string this[string key] { get; set; }
	int this[int index] { get; }
}

[KnockOff<IMultiIndexerService>]
public partial class InlineIndexerTestClass
{
}

#endregion
