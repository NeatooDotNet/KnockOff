namespace KnockOff.Tests;

/// <summary>
/// Tests to verify inline stubs support multiple indexer overloads with OfXxx pattern.
/// </summary>
public class InlineMultiIndexerTests
{
	[Fact]
	public void InlineStub_MultiIndexer_HasIndexerContainer()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();

		// Verify IndexerContainer exists and has OfXxx properties
		Assert.NotNull(stub.Indexer);
		Assert.NotNull(stub.Indexer.OfString);
		Assert.NotNull(stub.Indexer.OfInt32);
	}

	[Fact]
	public void InlineStub_StringIndexer_GetFromBacking()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer.OfString.Backing["key1"] = "value1";

		IMultiIndexerService svc = stub;
		var result = svc["key1"];

		Assert.Equal("value1", result);
		stub.Indexer.OfString.VerifyGet(Times.Once);
		Assert.Equal("key1", stub.Indexer.OfString.LastGetKey);
	}

	[Fact]
	public void InlineStub_IntIndexer_GetFromBacking()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer.OfInt32.Backing[0] = 100;
		stub.Indexer.OfInt32.Backing[1] = 200;

		IMultiIndexerService svc = stub;
		Assert.Equal(100, svc[0]);
		Assert.Equal(200, svc[1]);

		stub.Indexer.OfInt32.VerifyGet(Times.Exactly(2));
		Assert.Equal(1, stub.Indexer.OfInt32.LastGetKey);
	}

	[Fact]
	public void InlineStub_StringIndexer_OnGetCallback()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer.OfString.OnGet = (key) => $"Value for {key}";

		IMultiIndexerService svc = stub;
		Assert.Equal("Value for foo", svc["foo"]);
		Assert.Equal("Value for bar", svc["bar"]);

		stub.Indexer.OfString.VerifyGet(Times.Exactly(2));
	}

	[Fact]
	public void InlineStub_StringIndexer_SetTracking()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();

		IMultiIndexerService svc = stub;
		svc["key1"] = "value1";
		svc["key2"] = "value2";

		stub.Indexer.OfString.VerifySet(Times.Exactly(2));
		Assert.NotNull(stub.Indexer.OfString.LastSetEntry);
		Assert.Equal("key2", stub.Indexer.OfString.LastSetEntry.Value.Key);
		Assert.Equal("value2", stub.Indexer.OfString.LastSetEntry.Value.Value);
	}

	[Fact]
	public void InlineStub_StringIndexer_OnSetCallback()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		var callbackCalls = new List<(string key, string value)>();

		stub.Indexer.OfString.OnSet = (key, value) =>
		{
			callbackCalls.Add((key, value));
		};

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
		stub.Indexer.OfString.Backing["test"] = "string value";
		stub.Indexer.OfInt32.Backing[42] = 42;

		IMultiIndexerService svc = stub;

		// Access both
		Assert.Equal("string value", svc["test"]);
		Assert.Equal(42, svc[42]);

		// Verify independent tracking
		stub.Indexer.OfString.VerifyGet(Times.Once);
		stub.Indexer.OfInt32.VerifyGet(Times.Once);
		Assert.Equal("test", stub.Indexer.OfString.LastGetKey);
		Assert.Equal(42, stub.Indexer.OfInt32.LastGetKey);
	}

	[Fact]
	public void InlineStub_IndexerContainer_Reset()
	{
		var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
		stub.Indexer.OfString.Backing["key"] = "value";
		stub.Indexer.OfInt32.Backing[0] = 100;

		IMultiIndexerService svc = stub;
		_ = svc["key"];
		_ = svc[0];

		stub.Indexer.OfString.VerifyGet(Times.Once);
		stub.Indexer.OfInt32.VerifyGet(Times.Once);

		// Reset through container should reset all
		stub.Indexer.Reset();

		stub.Indexer.OfString.VerifyGet(Times.Never);
		stub.Indexer.OfInt32.VerifyGet(Times.Never);
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
